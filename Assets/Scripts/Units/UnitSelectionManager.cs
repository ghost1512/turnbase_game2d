using System.Collections.Generic;
using UnityEngine;

public class UnitSelectionManager :
    MonoBehaviour
{
    public static UnitSelectionManager Instance;

    [Header("Selection")]
    public Unit selectedUnit;

    private GridManager gridManager;


    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    private void Start()
    {
        gridManager =
            FindAnyObjectByType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogError(
                "UnitSelectionManager không tìm thấy GridManager!"
            );
        }
    }


    public void SelectUnit(Unit unit)
    {
        if (unit == null)
            return;

        if (TurnManager.Instance == null ||
            !TurnManager.Instance.CanStartPlayerAction)
        {
            return;
        }

        if (CombatManager.Instance != null &&
            CombatManager.Instance.IsAttackPhase)
        {
            return;
        }

        if (unit.IsMoving || !unit.IsAlive || gridManager == null)
            return;

        if (unit.Team !=
            UnitTeam.Player)
        {
            return;
        }

        if (selectedUnit != null)
        {
            selectedUnit
                .SetSelected(false);
        }

        selectedUnit =
            unit;

        selectedUnit
            .SetSelected(true);

        gridManager
            .HighlightMoveRange(unit);

        Debug.Log(
            $"Selected Unit: {unit.UnitName}"
        );
    }


    public void OnTileClicked(
        GridCell cell)
    {
        if (TurnManager.Instance == null ||
            !TurnManager.Instance.CanStartPlayerAction)
        {
            return;
        }

        if (CombatManager.Instance != null &&
            CombatManager.Instance.IsAttackPhase)
        {
            return;
        }

        if (selectedUnit == null)
            return;

        if (selectedUnit.IsMoving)
            return;

        if (cell == null)
            return;

        if (!cell.isWalkable)
        {
            Debug.Log(
                "Cell bị blocked."
            );

            return;
        }

        if (cell.currentUnit != null)
        {
            Debug.Log(
                "Cell đã có Unit."
            );

            return;
        }

        List<GridCell> reachableCells =
            gridManager
                .GetReachableCells(
                    selectedUnit.GridPosition,
                    selectedUnit.MoveRange
                );

        if (!reachableCells.Contains(cell))
        {
            Debug.Log(
                "Cell nằm ngoài Move Range."
            );

            return;
        }

        List<GridCell> path =
            gridManager.FindPath(
                selectedUnit.GridPosition,
                cell.gridPosition
            );

        if (path == null ||
            path.Count == 0)
        {
            Debug.Log(
                "Không tìm thấy đường đi."
            );

            return;
        }

        Unit unitToMove =
            selectedUnit;

        unitToMove
            .SetSelected(false);

        selectedUnit = null;

        gridManager
            .ClearHighlights();

        unitToMove
            .MoveAlongPath(path);
    }


    public void ClearSelection()
    {
        if (selectedUnit != null)
        {
            selectedUnit
                .SetSelected(false);
        }

        selectedUnit =
            null;

        if (gridManager != null)
        {
            gridManager
                .ClearHighlights();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

