using UnityEngine;

/// <summary>Screen-space health text anchored just below this unit's sprite.</summary>
[RequireComponent(typeof(Unit))]
public sealed class UnitHealthText : MonoBehaviour
{
    [SerializeField, Min(0)] private float gapPixels = 3f;
    private Unit unit;
    private SpriteRenderer unitSprite;
    private GUIStyle style;

    private void Awake()
    {
        unit = GetComponent<Unit>();
        unitSprite = GetComponent<SpriteRenderer>();
    }

    private void OnGUI()
    {
        if (unit == null || !unit.isActiveAndEnabled || !unit.IsAlive) return;
        Camera camera = Camera.main;
        if (camera == null) return;
        Vector3 feet = unitSprite != null
            ? new Vector3(unitSprite.bounds.center.x, unitSprite.bounds.min.y, transform.position.z)
            : transform.position + Vector3.down * 0.5f;
        Vector3 screen = camera.WorldToScreenPoint(feet);
        if (screen.z <= 0 || !camera.pixelRect.Contains(new Vector2(screen.x, screen.y))) return;
        // Do not draw into the area reserved for the card table.
        if (screen.y - gapPixels - 20 < camera.pixelRect.yMin) return;
        if (style == null)
            style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 12, fontStyle = FontStyle.Bold };
        style.normal.textColor = unit.CurrentHP * 1f / unit.MaxHP <= 0.3f
            ? new Color(1f, 0.4f, 0.4f) : Color.white;
        GUI.Box(new Rect(screen.x - 40, Screen.height - screen.y + gapPixels, 80, 20),
            $"HP {unit.CurrentHP}/{unit.MaxHP}", style);
    }
}
