using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Player action -> Resolution -> Enemy phase -> Resolution.</summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    [SerializeField] private TurnState currentTurn = TurnState.PlayerTurn;
    [SerializeField, Min(0)] private float enemyStartDelay = 0.4f;
    [SerializeField, Min(0)] private float enemyEndDelay = 0.3f;
    private readonly Queue<IEnumerator> effects = new Queue<IEnumerator>();
    private bool ready;
    private bool advancing;
    private Unit actor;
    public CardBattleSystem Cards { get; private set; }

    public TurnState CurrentTurn => currentTurn;
    public int RoundNumber { get; private set; }
    public UnitTeam ActiveTeam { get; private set; } = UnitTeam.Player;
    public bool IsBattleOver { get; private set; }
    public UnitTeam? Winner { get; private set; }
    public bool IsPlayerTurn => ready && isActiveAndEnabled && !IsBattleOver && currentTurn == TurnState.PlayerTurn;
    public bool IsEnemyTurn => ready && isActiveAndEnabled && !IsBattleOver && currentTurn == TurnState.EnemyTurn;
    public bool CanStartPlayerAction => IsPlayerTurn && !advancing && actor == null;
    public bool CanEndPlayerTurn => IsPlayerTurn && !advancing && (actor == null || !actor.IsMoving);
    public event Action<TurnState> TurnChanged;
    // Null winner represents a draw.
    public event Action<UnitTeam?> BattleEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Cards = GetComponent<CardBattleSystem>();
        if (Cards == null) Cards = gameObject.AddComponent<CardBattleSystem>();
        if (GetComponent<CardTableUI>() == null) gameObject.AddComponent<CardTableUI>();
    }

    private IEnumerator Start()
    {
        // Wait for Unit, GridManager and EnemyAI initialization.
        yield return null;
        if (Instance != this) yield break;
        ready = true;
        RoundNumber = 1;
        try { Cards.Initialize(this); }
        catch (Exception error)
        {
            ready = false;
            Debug.LogException(error, this);
            yield break;
        }
        if (!CheckBattleEnded()) yield return BeginPlayerTurn();
    }

    public bool TryBeginPlayerAction(Unit unit)
    {
        if (!CanStartPlayerAction || unit == null || !unit.IsAlive ||
            !unit.isActiveAndEnabled || unit.Team != UnitTeam.Player) return false;
        actor = unit;
        return true;
    }

    public bool IsActingPlayer(Unit unit) => IsPlayerTurn && unit != null && actor == unit;

    /// <summary>Queue card effects, buffs or animations for the next Resolution, FIFO.</summary>
    public void EnqueueResolution(IEnumerator effect)
    {
        if (effect == null) throw new ArgumentNullException(nameof(effect));
        if (IsBattleOver) throw new InvalidOperationException("Battle has ended.");
        effects.Enqueue(effect);
    }

    public void OnPlayerFinishedAction()
    {
        if (!CanEndPlayerTurn) return;
        advancing = true;
        actor = null;
        CombatManager.Instance?.CancelAttackPhase();
        StartCoroutine(AdvanceTurn());
    }

    // Can be connected directly to a Unity UI Button.
    public void EndPlayerTurn() => OnPlayerFinishedAction();

    public bool TryResolvePlayerEffect(IEnumerator effect)
    {
        if (effect == null || !CanStartPlayerAction) return false;
        advancing = true; // Acquire before coroutine execution and event publication.
        StartCoroutine(ResolvePlayerEffect(effect));
        return true;
    }

    private IEnumerator ResolvePlayerEffect(IEnumerator effect)
    {
        effects.Enqueue(effect);
        yield return Resolve();
        if (IsBattleOver) yield break;
        advancing = false;
        SetState(TurnState.PlayerTurn); // Do not refill energy or draw again.
    }

    private IEnumerator BeginPlayerTurn()
    {
        ActiveTeam = UnitTeam.Player;
        SetState(TurnState.StartTurn);
        TickTeam(UnitTeam.Player, true);
        foreach (EnemyAI enemy in FindObjectsByType<EnemyAI>())
            if (enemy.isActiveAndEnabled) enemy.PlanIntent();
        Cards.BeginTurn();
        yield return Resolve();
        if (IsBattleOver) yield break;
        advancing = false;
        SetState(TurnState.PlayerTurn);
    }

    private void TickTeam(UnitTeam team, bool start)
    {
        foreach (Unit unit in FindObjectsByType<Unit>())
        {
            if (unit.Team != team || !unit.isActiveAndEnabled || !unit.IsAlive) continue;
            if (start) unit.Stats.BeginTurn(); else unit.Stats.EndTurn();
        }
    }

    private IEnumerator AdvanceTurn()
    {
        yield return Resolve();
        if (IsBattleOver) yield break;
        SetState(TurnState.EndTurn);
        Cards.EndTurn();
        TickTeam(UnitTeam.Player, false);
        yield return Resolve();
        if (IsBattleOver) yield break;
        ActiveTeam = UnitTeam.Enemy;
        SetState(TurnState.StartTurn);
        TickTeam(UnitTeam.Enemy, true);
        yield return Resolve();
        if (IsBattleOver) yield break;
        SetState(TurnState.EnemyTurn);
        yield return new WaitForSeconds(Mathf.Max(0, enemyStartDelay));
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>();
        Array.Sort(enemies, (left, right) =>
        {
            Unit a = left.GetComponent<Unit>();
            Unit b = right.GetComponent<Unit>();
            if (a == null || b == null) return string.CompareOrdinal(left.name, right.name);
            int row = a.GridPosition.y.CompareTo(b.GridPosition.y);
            return row != 0 ? row : a.GridPosition.x.CompareTo(b.GridPosition.x);
        });
        foreach (EnemyAI enemy in enemies)
        {
            if (enemy == null || !enemy.isActiveAndEnabled) continue;
            Unit unit = enemy.GetComponent<Unit>();
            if (unit == null || !unit.IsAlive || !unit.isActiveAndEnabled || unit.Team != UnitTeam.Enemy) continue;
            yield return enemy.ExecuteTurn();
            if (CheckBattleEnded()) yield break;
            yield return new WaitForSeconds(Mathf.Max(0, enemyEndDelay));
        }
        SetState(TurnState.EndTurn);
        TickTeam(UnitTeam.Enemy, false);
        yield return Resolve();
        if (IsBattleOver) yield break;
        RoundNumber++;
        yield return BeginPlayerTurn();
    }

    private IEnumerator Resolve()
    {
        SetState(TurnState.Resolution);
        while (effects.Count > 0) yield return effects.Dequeue();
        CheckBattleEnded();
    }

    private bool CheckBattleEnded()
    {
        if (IsBattleOver) return true;
        bool player = false, enemy = false;
        foreach (Unit unit in FindObjectsByType<Unit>())
        {
            if (!unit.isActiveAndEnabled || !unit.IsAlive) continue;
            player |= unit.Team == UnitTeam.Player;
            enemy |= unit.Team == UnitTeam.Enemy;
        }
        if (player && enemy) return false;
        IsBattleOver = true;
        actor = null;
        effects.Clear();
        CombatManager.Instance?.CancelAttackPhase();
        Winner = player ? UnitTeam.Player : enemy ? UnitTeam.Enemy : (UnitTeam?)null;
        SetState(TurnState.Resolution);
        Cards.CompleteBattle(Winner);
        Debug.Log(Winner.HasValue ? $"Battle ended: {Winner.Value} wins." : "Battle ended: draw.");
        BattleEnded?.Invoke(Winner);
        return true;
    }

    private void Update()
    {
        // Works without creating or wiring a UI button.
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            EndPlayerTurn();
        if (IsPlayerTurn && !advancing && !ReferenceEquals(actor, null) &&
            (actor == null || !actor.IsAlive || !actor.isActiveAndEnabled))
            OnPlayerFinishedAction();
    }

    private void SetState(TurnState state)
    {
        currentTurn = state;
        if (state != TurnState.PlayerTurn) UnitSelectionManager.Instance?.ClearSelection();
        TurnChanged?.Invoke(state);
    }

    private void OnDisable()
    {
        if (Instance != this) return;
        // Disabling aborts the battle. Reload the scene to start a new battle.
        StopAllCoroutines();
        ready = false;
        actor = null;
        effects.Clear();
        CombatManager.Instance?.CancelAttackPhase();
        UnitSelectionManager.Instance?.ClearSelection();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

