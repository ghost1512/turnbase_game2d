using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Headless integration: target selection is an API; keyboard shortcuts are optional.</summary>
public sealed class CardBattleSystem : MonoBehaviour
{
    public static CardBattleSystem Instance { get; private set; }
    [SerializeField] private CardCatalog catalog;
    [SerializeField] private bool keyboardControls = true;
    [SerializeField] private bool useFixedSeed;
    [SerializeField] private int shuffleSeed = 12345;
    private CardSupplySchedule supply;
    private TurnManager turns;
    private ProgressStore store;
    private PlayerProgress progress;
    private bool canSave = true;
    private bool completed;
    private Unit selectedCaster;
    private static readonly Key[] HandKeys =
        { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0 };
    private static readonly Key[] NumpadHandKeys =
        { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9, Key.Numpad0 };
    public GameDatabase Database { get; private set; }
    public DeckState Deck { get; private set; }
    public CardInstance SelectedCard { get; private set; }
    public Unit SelectedCaster => selectedCaster;

    public Unit GetAvailableCaster()
    {
        Unit caster = selectedCaster;
        if (caster == null || !caster.IsAlive || !caster.isActiveAndEnabled || caster.IsMoving)
            caster = UnitSelectionManager.Instance != null ? UnitSelectionManager.Instance.selectedUnit : null;
        if (caster != null && caster.Team == UnitTeam.Player && caster.IsAlive && caster.isActiveAndEnabled && !caster.IsMoving)
            return caster;
        return FindObjectsByType<Unit>().Where(unit => unit.Team == UnitTeam.Player && unit.IsAlive && unit.isActiveAndEnabled && !unit.IsMoving)
            .OrderBy(unit => unit.GridPosition.y).ThenBy(unit => unit.GridPosition.x).FirstOrDefault();
    }
    public PlayerProgress Progress => progress?.Copy();
    public string SavePath => Path.Combine(Application.persistentDataPath, "player-progress-v1.json");
    public event Action HandChanged;
    public event Action<CardInstance> SelectionChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    public void Initialize(TurnManager manager)
    {
        turns = manager;
        if (catalog == null) catalog = Resources.Load<CardCatalog>("CardCatalog");
        Database = catalog != null ? catalog.CreateDatabase() : GameDatabase.CreateDefault();
        progress = new PlayerProgress { deck = catalog != null ? catalog.CreateStarterDeck(Database) : GameDatabase.DefaultDeck() };
        store = new ProgressStore(SavePath, Database, data => JsonUtility.ToJson(data, true),
            json => JsonUtility.FromJson<PlayerProgress>(json));
        if (store.TryLoad(out var loaded, out string error)) progress = loaded;
        else if (error != null)
        {
            // Keep damaged/incompatible user data intact; play with a temporary starter deck.
            canSave = false;
            Debug.LogWarning("Save not loaded; automatic saving disabled: " + error);
        }
        BuildDeck();
        turns.TurnChanged += OnTurnChanged;
    }

    private void BuildDeck()
    {
        if (Deck != null) Deck.Changed -= PublishHand;
        int seed = useFixedSeed ? shuffleSeed : Guid.NewGuid().GetHashCode();
        Deck = new DeckState(Database.Resolve(progress.deck), seed, catalog != null ? Mathf.Max(3, catalog.handLimit) : 10);
        supply = new CardSupplySchedule();
        Deck.Changed += PublishHand;
    }

    public void BeginTurn()
    {
        CancelSelection();
        Deck.BeginTurn(catalog != null ? Mathf.Max(0, catalog.energyPerTurn) : 3,
            supply.GetDrawCount(turns.RoundNumber));
    }

    public void EndTurn() { CancelSelection(); Deck.EndTurn(); }

    public bool SelectCard(int handIndex, Unit caster)
    {
        if (turns == null || !turns.CanStartPlayerAction || Deck == null || handIndex < 0 || handIndex >= Deck.Hand.Count ||
            caster == null || !caster.IsAlive || !caster.isActiveAndEnabled || caster.IsMoving || caster.Team != UnitTeam.Player) return false;
        var card = Deck.Hand[handIndex];
        if (!Deck.CanPlay(card)) return false;
        SelectedCard = card;
        selectedCaster = caster;
        UnitSelectionManager.Instance?.ClearSelection();
        SelectionChanged?.Invoke(card);
        return true;
    }

    public void CancelSelection()
    {
        SelectedCard = null;
        selectedCaster = null;
        SelectionChanged?.Invoke(null);
    }

    public bool TryPlaySelected(Unit target)
    {
        bool played = TryPlayCard(SelectedCard, selectedCaster, target);
        if (!played && SelectedCard != null)
            Debug.Log($"Chưa dùng được {SelectedCard.Definition.Name}: kiểm tra lượt, năng lượng, phe mục tiêu và tầm {SelectedCard.Definition.Range} ô.", this);
        return played;
    }

    public bool TryPlayCard(CardInstance card, Unit caster, Unit target)
    {
        if (turns == null || !turns.CanStartPlayerAction || Deck == null || !Deck.CanPlay(card) ||
            caster == null || !caster.IsAlive || caster.IsMoving || !caster.isActiveAndEnabled || caster.Team != UnitTeam.Player) return false;
        if (card.Definition.Target == CardTarget.Self) target = caster;
        if (target == null || !target.IsAlive || !target.isActiveAndEnabled || target.IsMoving) return false;
        int distance = Mathf.Abs(caster.GridPosition.x - target.GridPosition.x) + Mathf.Abs(caster.GridPosition.y - target.GridPosition.y);
        if (!CardRules.IsValidTarget(card.Definition, caster == target, caster.Team == target.Team, distance)) return false;
        return turns.TryResolvePlayerEffect(ResolveCard(card, caster, target));
    }

    private IEnumerator ResolveCard(CardInstance card, Unit caster, Unit target)
    {
        if (!Deck.TryConsume(card)) yield break;
        CancelSelection();
        foreach (var effect in card.Definition.Effects)
        {
            if (caster == null || !caster.IsAlive) break;
            CardRules.Apply(effect, caster.Stats, target != null ? target.Stats : null, amount => Deck.Draw(amount),
                $"{caster.UnitName} [bài {card.Definition.Name}]");
        }
        Debug.Log("Played: " + card.Definition.Name);
    }

    public bool SaveProgress(out string error)
    {
        error = null;
        if (!canSave || store == null) { error = "Saving is unavailable; inspect the original save before replacing it."; return false; }
        return store.TrySave(progress, out error);
    }

    // Deck edits apply to the next battle, never mutate a hand during combat.
    public bool SetNextBattleDeck(IEnumerable<string> ids)
    {
        if (turns == null || !turns.IsBattleOver || ids == null) return false;
        var candidate = progress.Copy();
        candidate.deck = ids.Take(201).ToList();
        if (!candidate.IsValid(Database)) return false;
        if (!canSave || !store.TrySave(candidate, out string error)) return false;
        progress = candidate;
        return true;
    }

    public bool LoadProgress(out string error)
    {
        error = null;
        if (store == null || turns == null || !turns.IsBattleOver)
        { error = "Load progress between battles only."; return false; }
        if (!store.TryLoad(out var loaded, out error)) return false;
        progress = loaded;
        canSave = true;
        return true;
    }

    public void CompleteBattle(UnitTeam? winner)
    {
        if (completed) return;
        completed = true;
        CancelSelection();
        if (winner == UnitTeam.Player)
        {
            progress.battlesWon = (int)Math.Min(int.MaxValue, (long)progress.battlesWon + 1);
            progress.stage = (int)Math.Min(int.MaxValue, (long)progress.stage + 1);
        }
        else if (winner == UnitTeam.Enemy)
            progress.battlesLost = (int)Math.Min(int.MaxValue, (long)progress.battlesLost + 1);
        if (!SaveProgress(out string error)) Debug.LogWarning(error);
    }

    private void OnTurnChanged(TurnState state) { if (state != TurnState.PlayerTurn) CancelSelection(); }
    private void PublishHand()
    {
        HandChanged?.Invoke();
        Debug.Log($"Energy {Deck.Energy}; hand: " + string.Join(", ", Deck.Hand.Select((card, i) => $"{i + 1}:{card.Definition.Name}({card.Definition.Cost})")));
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (!keyboardControls || keyboard == null) return;
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            CancelSelection();
            return;
        }
        for (int i = 0; i < HandKeys.Length; i++)
        {
            if (!keyboard[HandKeys[i]].wasPressedThisFrame && !keyboard[NumpadHandKeys[i]].wasPressedThisFrame) continue;
            SelectFromKeyboard(i);
            return; // One command per frame, even when several keys are pressed together.
        }
        if (SelectedCard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
        {
            if (SelectedCard.Definition.Target == CardTarget.Self) TryPlaySelected(selectedCaster);
            else Debug.Log("Click vào quân mục tiêu để dùng lá bài đã chọn.", this);
        }
    }

    private void SelectFromKeyboard(int index)
    {
        if (turns == null || Deck == null)
        { Debug.Log("Bộ bài chưa khởi tạo.", this); return; }
        if (!turns.CanStartPlayerAction)
        { Debug.Log("Chưa thể chọn bài: cần lượt Player và không đang di chuyển/đánh/xử lý hiệu ứng.", this); return; }
        if (index >= Deck.Hand.Count)
        { Debug.Log($"Không có lá thứ {index + 1}; trên tay đang có {Deck.Hand.Count} lá.", this); return; }
        if (!Deck.CanPlay(Deck.Hand[index]))
        { Debug.Log($"Không đủ năng lượng: cần {Deck.Hand[index].Definition.Cost}, hiện có {Deck.Energy}.", this); return; }

        Unit caster = GetAvailableCaster();
        if (!SelectCard(index, caster))
        { Debug.Log("Không tìm được Player hợp lệ để dùng bài.", this); return; }
        var definition = SelectedCard.Definition;
        string instruction = definition.Target == CardTarget.Self
            ? "Enter hoặc click Player để dùng lên bản thân."
            : definition.Target == CardTarget.Enemy ? "Click Enemy trong tầm để dùng." : "Click Player cần hồi máu/buff trong tầm để dùng.";
        Debug.Log($"Đã chọn lá {index + 1}: {definition.Name} | Người dùng: {caster.UnitName} | Giá: {definition.Cost} | Tầm: {definition.Range}. {instruction} Escape để hủy.", this);
    }

    private void OnDestroy()
    {
        if (turns != null) turns.TurnChanged -= OnTurnChanged;
        if (Deck != null) Deck.Changed -= PublishHand;
        if (Instance == this) Instance = null;
    }
}
