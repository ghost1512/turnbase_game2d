using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Unit Data")]
    [SerializeField]
    private UnitData unitData;

    [Header("Runtime")]
    [SerializeField]
    private UnitTeam team;

    [SerializeField]
    private int currentHP;

    [Header("Movement")]
    [SerializeField]
    private float moveSpeed = 5f;

    private Vector2Int gridPosition;

    private GridManager gridManager;

    private bool isMoving;
    private GridCell occupiedCell;
    private bool deathHandled;
    public CharacterStats Stats { get; private set; }

    private Vector3 originalScale;

    private SpriteRenderer spriteRenderer;


    // =========================
    // PROPERTIES
    // =========================

    public UnitTeam Team => team;

    public bool IsMoving => isMoving;
    public bool IsAlive => Stats != null && Stats.IsAlive;

    public Vector2Int GridPosition =>
        gridPosition;

    public string UnitName =>
        unitData != null
            ? unitData.unitName
            : gameObject.name;

    public int MaxHP =>
        unitData != null
            ? Mathf.Max(1, unitData.maxHP)
            : 1;

    public int CurrentHP =>
        currentHP;

    public int Damage => Stats != null ? Stats.CalculateAttack(unitData != null ? unitData.damage : 0) : 0;

    public int MoveRange =>
        unitData != null
            ? Mathf.Max(0, unitData.moveRange)
            : 0;

    public int AttackRange =>
        unitData != null
            ? Mathf.Max(0, unitData.attackRange)
            : 0;


    // =========================
    // UNITY
    // =========================

    private void Awake()
    {
        originalScale =
            transform.localScale;

        spriteRenderer =
            GetComponent<SpriteRenderer>();

        LoadData();
        Stats = new CharacterStats(Mathf.Max(1, MaxHP));
        Stats.Changed += SyncStats;
        Stats.DamageTaken += LogDamage;
        SyncStats();
        if (GetComponent<UnitHealthText>() == null) gameObject.AddComponent<UnitHealthText>();
    }

    private void Start()
    {
        gridManager =
            FindAnyObjectByType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogError(
                $"{UnitName}: Không tìm thấy GridManager!"
            );

            return;
        }

        SetupSpawnPosition();
    }


    // =========================
    // DATA
    // =========================

    private void LoadData()
    {
        if (unitData == null)
        {
            Debug.LogError(
                $"{gameObject.name}: Chưa gán UnitData!"
            );

            return;
        }

        team =
            unitData.defaultTeam;

        currentHP =
            unitData.maxHP;

        if (spriteRenderer != null &&
            unitData.unitSprite != null)
        {
            spriteRenderer.sprite =
                unitData.unitSprite;
        }

        gameObject.name =
            unitData.unitName;
    }


    // =========================
    // SPAWN
    // =========================

    private void SetupSpawnPosition()
    {
        if (team == UnitTeam.Player)
        {
            Vector2Int playerPosition =
                gridManager.GetPlayerSpawnPosition();

            SnapToGrid(
                playerPosition
            );

            return;
        }

        if (team == UnitTeam.Enemy)
        {
            Vector2Int playerPosition =
                new Vector2Int(1, 1);

            Vector2Int enemyPosition =
                gridManager
                    .GetRandomEnemySpawnPosition(
                        playerPosition,
                        6
                    );

            if (enemyPosition.x < 0)
            {
                Debug.LogError(
                    $"{UnitName}: Không tìm được vị trí spawn."
                );

                return;
            }

            SnapToGrid(
                enemyPosition
            );
        }
    }

    private void SnapToGrid(
        Vector2Int position)
    {
        GridCell cell =
            gridManager.GetCell(position);

        if (cell == null)
        {
            Debug.LogError(
                $"{UnitName}: Cell {position} không tồn tại."
            );

            return;
        }

        if (!cell.isWalkable)
        {
            Debug.LogError(
                $"{UnitName}: Cell {position} bị blocked."
            );

            return;
        }

        if (cell.currentUnit != null &&
            cell.currentUnit != this)
        {
            Debug.LogError(
                $"{UnitName}: Cell {position} đã có Unit khác."
            );

            return;
        }

        gridPosition =
            position;

        transform.position =
            cell.transform.position;

        cell.currentUnit =
            this;
        occupiedCell = cell;

        Debug.Log(
            $"{UnitName} spawned at {gridPosition}"
        );
    }


    // =========================
    // CLICK
    // =========================

    private void OnMouseDown()
    {
        if (CardTableUI.BlocksWorldPointer) return;
        if (CardBattleSystem.Instance != null && CardBattleSystem.Instance.SelectedCard != null)
        {
            CardBattleSystem.Instance.TryPlaySelected(this);
            return;
        }
        if (isMoving)
            return;

        // Click Enemy trong Attack Phase
        if (team == UnitTeam.Enemy)
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance
                    .TryAttack(this);
            }

            return;
        }

        // Player
        if (team == UnitTeam.Player)
        {
            if (TurnManager.Instance != null &&
                !TurnManager.Instance.IsPlayerTurn)
            {
                return;
            }

            if (UnitSelectionManager.Instance != null)
            {
                UnitSelectionManager.Instance
                    .SelectUnit(this);
            }
        }
    }


    // =========================
    // SELECTION
    // =========================

    public void SetSelected(
        bool selected)
    {
        transform.localScale =
            selected
                ? originalScale * 1.15f
                : originalScale;
    }


    // =========================
    // MOVEMENT
    // =========================

    public void MoveAlongPath(
        List<GridCell> path)
    {
        if (isMoving || !IsAlive || !isActiveAndEnabled || gridManager == null || moveSpeed <= 0f)
            return;

        if (path == null ||
            path.Count == 0)
        {
            return;
        }

        if (path.Count > MoveRange) return;
        Vector2Int previous = gridPosition;
        foreach (GridCell step in path)
        {
            if (step == null || gridManager.GetCell(step.gridPosition) != step ||
                !step.isWalkable || step.currentUnit != null ||
                Mathf.Abs(step.gridPosition.x - previous.x) + Mathf.Abs(step.gridPosition.y - previous.y) != 1)
                return;
            previous = step.gridPosition;
        }

        if (team == UnitTeam.Player &&
            (TurnManager.Instance == null || !TurnManager.Instance.TryBeginPlayerAction(this)))
            return;
        if (team == UnitTeam.Enemy &&
            (TurnManager.Instance == null || !TurnManager.Instance.IsEnemyTurn))
            return;

        StartCoroutine(
            MovePathCoroutine(new List<GridCell>(path))
        );
    }

    private IEnumerator MovePathCoroutine(
        List<GridCell> path)
    {
        isMoving = true;

        foreach (GridCell cell in path)
        {
            if (cell == null || !cell.isWalkable || (cell.currentUnit != null && cell.currentUnit != this))
                break;
            if (occupiedCell != null && occupiedCell.currentUnit == this) occupiedCell.currentUnit = null;
            occupiedCell = cell;
            cell.currentUnit = this; // Reserve the destination during animation.
            gridPosition = cell.gridPosition;
            Vector3 targetPosition =
                cell.transform.position;

            while (
                Vector3.Distance(
                    transform.position,
                    targetPosition
                ) > 0.01f)
            {
                transform.position =
                    Vector3.MoveTowards(
                        transform.position,
                        targetPosition,
                        moveSpeed *
                        Time.deltaTime
                    );

                yield return null;
            }

            transform.position =
                targetPosition;
        }

        isMoving = false;

        Debug.Log(
            $"{UnitName} moved to {gridPosition}"
        );

        // Player move xong -> Attack Phase
        if (team == UnitTeam.Player)
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance
                    .BeginPlayerAttackPhase(this);
            }
            else if (TurnManager.Instance != null)
            {
                // fallback
                TurnManager.Instance
                    .OnPlayerFinishedAction();
            }
        }
    }


    // =========================
    // COMBAT
    // =========================

    public void TakeDamage(
        int damageAmount, string source = null)
    {
        Stats?.TakeDamage(damageAmount, false, source);
    }

    private void LogDamage(DamageReport report)
    {
        Debug.Log($"[Combat] {report.Source} đã gây {report.HealthLost} sát thương HP cho {UnitName}. " +
            $"Block hấp thụ: {report.BlockAbsorbed}. HP còn: {report.RemainingHealth}/{Stats.MaxHealth}.", this);
    }

    private void SyncStats()
    {
        currentHP = Stats.Health;
        if (!Stats.IsAlive && !deathHandled)
        {
            deathHandled = true;
            Die();
        }
    }

    private void Die()
    {
        GridCell currentCell =
            gridManager != null ? gridManager.GetCell(
                gridPosition
            ) : null;

        if (currentCell != null &&
            currentCell.currentUnit == this)
        {
            currentCell.currentUnit = null;
        }

        Debug.Log(
            $"{UnitName} died."
        );

        Destroy(gameObject);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isMoving = false;
        if (occupiedCell != null && occupiedCell.currentUnit == this) occupiedCell.currentUnit = null;
        if (UnitSelectionManager.Instance != null && UnitSelectionManager.Instance.selectedUnit == this)
            UnitSelectionManager.Instance.ClearSelection();
    }
}


