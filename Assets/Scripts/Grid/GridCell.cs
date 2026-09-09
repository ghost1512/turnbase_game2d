using UnityEngine;

public class GridCell : MonoBehaviour
{
    [Header("Grid Info")]
    public Vector2Int gridPosition;

    [Header("Cell State")]
    public bool isWalkable = true;
    public Unit currentUnit;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.green;
    [SerializeField] private Color blockedColor = Color.gray;

    private SpriteRenderer cellRenderer;

    private void Awake()
    {
        cellRenderer = GetComponent<SpriteRenderer>();

        if (cellRenderer == null)
        {
            Debug.LogError($"{gameObject.name}: không có SpriteRenderer!");
        }
    }

    public void Setup(int x, int y)
    {
        gridPosition = new Vector2Int(x, y);
        gameObject.name = $"Tile_{x}_{y}";

        UpdateVisual();
    }

    private void OnMouseDown()
    {
        if (CardTableUI.BlocksWorldPointer) return;
        if (UnitSelectionManager.Instance != null)
        {
            UnitSelectionManager.Instance.OnTileClicked(this);
        }
    }

    public void Highlight()
    {
        if (cellRenderer == null)
            return;

        cellRenderer.color = highlightColor;
    }

    public void ClearHighlight()
    {
        UpdateVisual();
    }

    public void SetWalkable(bool value)
    {
        isWalkable = value;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (cellRenderer == null)
            return;

        cellRenderer.color = isWalkable
            ? normalColor
            : blockedColor;
    }
}
