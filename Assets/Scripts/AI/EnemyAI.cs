using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EnemyIntentKind { Attack = 0, Wait = 2 }

public readonly struct EnemyIntent
{
    public EnemyIntentKind Kind { get; }
    public Unit Target { get; }
    public int Amount { get; }
    public EnemyIntent(EnemyIntentKind kind, Unit target, int amount)
    { Kind = kind; Target = target; Amount = amount; }
}

[RequireComponent(typeof(Unit))]
public class EnemyAI : MonoBehaviour
{
    private Unit enemyUnit;
    private GridManager gridManager;
    private int lastActedRound = -1;
    private int plannedRound = -1;
    private EnemyIntentKind plannedKind = EnemyIntentKind.Wait;
    private Unit plannedTarget;
    public string LastTurnResult { get; private set; }
    public EnemyIntent Intent => new EnemyIntent(plannedKind, plannedTarget,
        enemyUnit != null && plannedKind == EnemyIntentKind.Attack ? enemyUnit.Damage : 0);
    public event System.Action<EnemyIntent> IntentChanged;

    private void Awake() => enemyUnit = GetComponent<Unit>();
    private void Start()
    {
        gridManager = FindAnyObjectByType<GridManager>();
        if (enemyUnit != null && enemyUnit.Stats != null) enemyUnit.Stats.Changed += PublishIntent;
    }

    private Unit[] LivingPlayers() => FindObjectsByType<Unit>()
        .Where(unit => unit.Team == UnitTeam.Player && unit.IsAlive && unit.isActiveAndEnabled)
        .OrderBy(unit => unit.CurrentHP).ThenBy(unit => unit.GridPosition.y).ThenBy(unit => unit.GridPosition.x).ToArray();

    public void PlanIntent()
    {
        if (enemyUnit == null || !enemyUnit.IsAlive || TurnManager.Instance == null) return;
        plannedRound = TurnManager.Instance.RoundNumber;
        plannedTarget = LivingPlayers().FirstOrDefault();
        plannedKind = plannedTarget == null ? EnemyIntentKind.Wait : EnemyIntentKind.Attack;
        PublishIntent();
        Debug.Log($"[EnemyIntent] {enemyUnit.UnitName}: vòng {plannedRound}, {Intent.Kind} {Intent.Amount}, mục tiêu: {plannedTarget?.UnitName}", this);
    }

    private void PublishIntent() => IntentChanged?.Invoke(Intent);
    private void OnDestroy()
    {
        if (enemyUnit != null && enemyUnit.Stats != null) enemyUnit.Stats.Changed -= PublishIntent;
    }

    private bool CanAct => isActiveAndEnabled && enemyUnit != null && enemyUnit.IsAlive &&
        !enemyUnit.IsMoving && enemyUnit.Team == UnitTeam.Enemy &&
        TurnManager.Instance != null && TurnManager.Instance.IsEnemyTurn;

    private void Report(string message)
    {
        LastTurnResult = message;
        Debug.Log($"[EnemyTurn] {enemyUnit?.UnitName ?? name} | vòng {TurnManager.Instance?.RoundNumber}: {message}", this);
    }

    private void WaitWithReason(string reason)
    {
        plannedKind = EnemyIntentKind.Wait;
        PublishIntent();
        Report($"{reason} Chờ lượt sau để tìm lại đường/mục tiêu.");
    }
    public IEnumerator ExecuteTurn()
    {
        if (!CanAct) { Report("Không thể hành động: sai lượt, quân đã chết/tắt hoặc đang di chuyển."); yield break; }
        int round = TurnManager.Instance.RoundNumber;
        if (lastActedRound == round) { Report("Đã hành động trong vòng này; chặn gọi lượt trùng."); yield break; }
        lastActedRound = round;
        if (gridManager == null) gridManager = FindAnyObjectByType<GridManager>();
        if (plannedRound != round) PlanIntent(); // Includes enemies spawned after planning.
        Unit[] players = LivingPlayers();
        if (players.Length == 0) { Report("Không còn Player sống; không có mục tiêu."); yield break; }
        if (CombatManager.Instance == null)
        { Report("LỖI CẤU HÌNH: thiếu CombatManager."); yield break; }

        Unit previousTarget = plannedTarget;
        plannedTarget = EnemyTurnPolicy.ChooseTarget(players, plannedTarget,
            target => CombatManager.Instance.CanAttack(enemyUnit, target),
            target => { var path = FindAttackPath(target); return path != null && enemyUnit.MoveRange > 0 ? (int?)path.Count : null; },
            target => target.CurrentHP);
        if (plannedTarget == null)
        { WaitWithReason("Không có mục tiêu trong tầm hoặc đường di chuyển khả dụng."); yield break; }
        plannedKind = EnemyIntentKind.Attack;
        if (previousTarget != plannedTarget)
        {
            PublishIntent();
            Report($"Đổi mục tiêu sang {plannedTarget.UnitName}: ưu tiên quân trong tầm hoặc có đường tiếp cận.");
        }

        Vector2Int start = enemyUnit.GridPosition;
        if (!CombatManager.Instance.CanAttack(enemyUnit, plannedTarget))
        {
            bool moving = MoveTowardPlayer();
            while (enemyUnit != null && enemyUnit.isActiveAndEnabled && enemyUnit.IsAlive && enemyUnit.IsMoving)
                yield return null;
            if (!CanAct) { Report("Hành động bị gián đoạn khi di chuyển."); yield break; }
            if (!moving && enemyUnit.GridPosition == start)
            { WaitWithReason("Không bắt đầu được di chuyển (đường bị chiếm hoặc cấu hình movement)."); yield break; }
        }

        if (CombatManager.Instance != null && CombatManager.Instance.TryEnemyAttack(enemyUnit, plannedTarget))
            Report($"Đã tấn công {plannedTarget.UnitName}; vị trí {enemyUnit.GridPosition}.");
        else if (enemyUnit.GridPosition != start)
            Report($"Đã di chuyển {start} → {enemyUnit.GridPosition}; chưa có mục tiêu hợp lệ trong tầm {enemyUnit.AttackRange} ô.");
        else
            WaitWithReason("Mục tiêu không còn hợp lệ trước khi đánh.");
    }

    private List<GridCell> FindAttackPath(Unit target)
    {
        if (gridManager == null || target == null || !target.IsAlive) return null;
        List<GridCell> best = null;
        int range = enemyUnit.AttackRange;
        for (int x = -range; x <= range; x++)
        for (int y = -range; y <= range; y++)
        {
            if (Mathf.Abs(x) + Mathf.Abs(y) > range) continue;
            Vector2Int position = target.GridPosition + new Vector2Int(x, y);
            GridCell cell = gridManager.GetCell(position);
            if (cell == null || !cell.isWalkable || cell.currentUnit != null) continue;
            List<GridCell> path = gridManager.FindPath(enemyUnit.GridPosition, position);
            if (path != null && path.Count > 0 && (best == null || path.Count < best.Count)) best = path;
        }
        return best;
    }

    public bool MoveTowardPlayer()
    {
        if (!CanAct || plannedTarget == null || enemyUnit.MoveRange <= 0) return false;
        List<GridCell> path = FindAttackPath(plannedTarget);
        if (path == null) return false;
        enemyUnit.MoveAlongPath(path.GetRange(0, Mathf.Min(enemyUnit.MoveRange, path.Count)));
        return enemyUnit.IsMoving;
    }
}

