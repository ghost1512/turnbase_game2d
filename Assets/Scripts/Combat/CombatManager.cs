using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    private Unit activeAttacker;

    public bool IsAttackPhase => activeAttacker != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void BeginPlayerAttackPhase(Unit attacker)
    {
        if (attacker == null || !attacker.IsAlive || attacker.IsMoving ||
            TurnManager.Instance == null || !TurnManager.Instance.IsActingPlayer(attacker))
            return;

        activeAttacker = attacker;

        Debug.Log($"Attack Phase: {attacker.UnitName}");

        bool hasTarget = HasEnemyInRange(attacker);

        if (!hasTarget)
        {
            Debug.Log("Không có Enemy trong Attack Range.");

            EndAttackPhase();
            return;
        }

        Debug.Log("Có Enemy trong Attack Range. Click Enemy để attack.");
    }

    public bool HasEnemyInRange(Unit attacker)
    {
        if (attacker == null)
            return false;

        Unit[] units =
            FindObjectsByType<Unit>();

        foreach (Unit unit in units)
        {
            if (unit == null || !unit.IsAlive)
                continue;

            if (unit == attacker)
                continue;

            if (unit.Team == attacker.Team)
                continue;

            if (IsInAttackRange(attacker, unit))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsInAttackRange(
        Unit attacker,
        Unit target)
    {
        if (attacker == null ||
            target == null)
        {
            return false;
        }

        int distance =
            Mathf.Abs(
                attacker.GridPosition.x -
                target.GridPosition.x
            )
            +
            Mathf.Abs(
                attacker.GridPosition.y -
                target.GridPosition.y
            );

        return distance <=
               attacker.AttackRange;
    }

    public void TryAttack(Unit target)
    {
        // A selected player can attack without being forced to move first.
        if (!IsAttackPhase && TurnManager.Instance != null && TurnManager.Instance.CanStartPlayerAction)
        {
            Unit selected = UnitSelectionManager.Instance != null ? UnitSelectionManager.Instance.selectedUnit : null;
            if (!CanAttack(selected, target) || !TurnManager.Instance.TryBeginPlayerAction(selected)) return;
            activeAttacker = selected;
            UnitSelectionManager.Instance?.ClearSelection();
        }
        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn ||
            activeAttacker == null || !activeAttacker.IsAlive)
            return;
        if (!IsAttackPhase)
        {
            Debug.Log("Hiện tại không phải Attack Phase.");
            return;
        }

        if (target == null || !target.IsAlive)
            return;

        if (target.Team ==
            activeAttacker.Team)
        {
            Debug.Log("Không thể attack Unit cùng Team.");
            return;
        }

        if (!IsInAttackRange(
                activeAttacker,
                target))
        {
            Debug.Log(
                $"{target.UnitName} nằm ngoài Attack Range."
            );

            return;
        }

        if (!CanAttack(activeAttacker, target)) return;
        Unit attacker = activeAttacker;
        TurnManager.Instance.EnqueueResolution(ResolveAttack(attacker, target));

        EndAttackPhase();
    }

    private void Attack(
        Unit attacker,
        Unit target)
    {
        if (attacker == null ||
            target == null)
        {
            return;
        }

        target.TakeDamage(
            attacker.Damage, attacker.UnitName
        );
    }

    public bool CanAttack(Unit attacker, Unit target)
    {
        return attacker != null && target != null && attacker.IsAlive && target.IsAlive &&
            attacker.isActiveAndEnabled && target.isActiveAndEnabled &&
            !attacker.IsMoving && !target.IsMoving && attacker.Team != target.Team &&
            IsInAttackRange(attacker, target);
    }

    private System.Collections.IEnumerator ResolveAttack(Unit attacker, Unit target)
    {
        if (CanAttack(attacker, target)) Attack(attacker, target);
        yield break;
    }

    // EnemyAI owns the one-attack-per-enemy budget and calls this once per turn.
    public bool TryEnemyAttack(Unit attacker, Unit target)
    {
        if (TurnManager.Instance == null || !TurnManager.Instance.IsEnemyTurn ||
            attacker == null || attacker.Team != UnitTeam.Enemy || !CanAttack(attacker, target)) return false;
        Attack(attacker, target);
        return true;
    }

    public void CancelAttackPhase() => activeAttacker = null;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void EndAttackPhase()
    {
        Debug.Log("Attack Phase kết thúc.");

        activeAttacker = null;

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance
                .OnPlayerFinishedAction();
        }
    }
}

