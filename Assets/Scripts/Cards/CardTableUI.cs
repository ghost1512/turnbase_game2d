using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Temporary runtime card table. Uses the same validated actions as keyboard/world input.</summary>
public sealed class CardTableUI : MonoBehaviour
{
    private static CardTableUI instance;
    private Vector2 handScroll;
    private GUIStyle cardStyle;
    private GUIStyle labelStyle;
    private Camera battleCamera;
    private Rect originalViewport;
    private float originalSize;
    private bool cameraReserved;

    private float PanelHeight => Mathf.Min(210f, Screen.height * 0.38f);
    private Rect PanelRect => new Rect(0, Screen.height - PanelHeight, Screen.width, PanelHeight);
    public static bool BlocksWorldPointer
    {
        get
        {
            if (instance == null || !instance.isActiveAndEnabled || Mouse.current == null) return false;
            Vector2 position = Mouse.current.position.ReadValue();
            return instance.PanelRect.Contains(new Vector2(position.x, Screen.height - position.y));
        }
    }

    private void Awake() => instance = this;

    private void Update()
    {
        // Leave actual space for the table instead of covering the board with it.
        if (battleCamera == null)
        {
            battleCamera = Camera.main;
            if (battleCamera != null && battleCamera.orthographic)
            {
                originalViewport = battleCamera.rect;
                originalSize = battleCamera.orthographicSize;
                cameraReserved = true;
            }
        }
        if (cameraReserved && battleCamera != null && Screen.height > 0)
        {
            float lowerEdge = Mathf.Max(originalViewport.yMin, PanelHeight / Screen.height);
            float viewportHeight = Mathf.Max(0.05f, originalViewport.yMax - lowerEdge);
            battleCamera.rect = new Rect(originalViewport.x, lowerEdge, originalViewport.width, viewportHeight);
            battleCamera.orthographicSize = originalSize;
        }
    }

    private void OnGUI()
    {
        var cards = CardBattleSystem.Instance;
        var turns = TurnManager.Instance;
        if (cardStyle == null)
        {
            cardStyle = new GUIStyle(GUI.skin.button) { wordWrap = true, alignment = TextAnchor.UpperLeft, fontSize = 14 };
            cardStyle.padding = new RectOffset(12, 12, 10, 10);
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        }
        bool previousEnabled = GUI.enabled;
        Color previousColor = GUI.backgroundColor;
        GUILayout.BeginArea(PanelRect, GUI.skin.box);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Vòng {turns?.RoundNumber ?? 0}    Năng lượng: {cards?.Deck?.Energy ?? 0}", labelStyle);
        GUILayout.FlexibleSpace();
        GUI.enabled = cards != null && cards.SelectedCard != null && turns != null && turns.CanStartPlayerAction;
        if (GUILayout.Button("Hủy chọn", GUILayout.Width(90), GUILayout.Height(28))) cards.CancelSelection();
        GUI.enabled = turns != null && turns.CanEndPlayerTurn;
        if (GUILayout.Button("Kết thúc lượt", GUILayout.Width(125), GUILayout.Height(28))) turns.EndPlayerTurn();
        GUILayout.EndHorizontal();

        if (cards != null && cards.Deck != null) DrawHand(cards, turns);
        GUILayout.EndArea();
        GUI.enabled = previousEnabled;
        GUI.backgroundColor = previousColor;
    }

    private void DrawHand(CardBattleSystem cards, TurnManager turns)
    {
        handScroll = GUILayout.BeginScrollView(handScroll, false, false, GUILayout.ExpandHeight(true));
        GUILayout.BeginHorizontal();
        var hand = cards.Deck.Hand.ToArray();
        for (int i = 0; i < hand.Length; i++)
        {
            CardInstance card = hand[i];
            GUI.backgroundColor = cards.SelectedCard == card ? new Color(0.35f, 0.85f, 1f) : Color.white;
            GUI.enabled = turns != null && turns.CanStartPlayerAction && cards.Deck.CanPlay(card);
            string usage = string.Join(", ", card.Definition.Effects.Select(DescribeEffect));
            switch (card.Definition.Target)
            {
                case CardTarget.Self: usage += ". Click để dùng lên bản thân."; break;
                case CardTarget.Ally: usage += $". Chọn đồng minh trong {card.Definition.Range} ô."; break;
                case CardTarget.Enemy: usage += $". Chọn Enemy trong {card.Definition.Range} ô."; break;
            }
            if (GUILayout.Button(card.Definition.Name + "\n\n" + usage, cardStyle,
                GUILayout.Width(190), GUILayout.ExpandHeight(true)))
            {
                if (cards.SelectCard(i, cards.GetAvailableCaster()) && card.Definition.Target == CardTarget.Self)
                    cards.TryPlaySelected(cards.SelectedCaster);
            }
        }
        GUI.enabled = true;
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
    }
    private static string DescribeEffect(CardEffect effect)
    {
        switch (effect.kind)
        {
            case CardEffectKind.Damage: return $"Đánh {effect.amount}";
            case CardEffectKind.Block: return $"Block +{effect.amount}";
            case CardEffectKind.Heal: return $"Hồi {effect.amount} HP";
            case CardEffectKind.Draw: return $"Rút {effect.amount} lá";
            default: return $"{effect.status} {effect.amount} / {effect.duration} lượt";
        }
    }

    private void OnDisable()
    {
        if (cameraReserved && battleCamera != null)
        { battleCamera.rect = originalViewport; battleCamera.orthographicSize = originalSize; }
        cameraReserved = false;
        battleCamera = null;
    }

    private void OnDestroy() { if (instance == this) instance = null; }
}

