using System;
using UnityEditor;
using UnityEngine;

/// <summary>Inspector-only view. Runtime values are read from their actual source of truth.</summary>
[CustomEditor(typeof(Unit))]
[CanEditMultipleObjects]
public sealed class UnitInspector : Editor
{
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("unitData"), new GUIContent("Unit Data"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("moveSpeed"), new GUIContent("Tốc độ di chuyển"));
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.HelpBox("Thông số bên dưới chỉ để xem. Chỉnh chỉ số gốc trong UnitData. Khi Play, HP/Block/status hiển thị giá trị thực tế đang chạy.", MessageType.Info);
        foreach (UnityEngine.Object inspected in targets)
        {
            Unit unit = inspected as Unit;
            if (unit == null) continue;
            DrawUnit(unit);
        }
    }

    private static void DrawUnit(Unit unit)
    {
        var unitSerialized = new SerializedObject(unit);
        var data = unitSerialized.FindProperty("unitData").objectReferenceValue as UnitData;
        CharacterStats stats = unit.Stats;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(unit.name, EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("ID", data != null ? data.id : "");
            EditorGUILayout.TextField("Tên nhân vật", data != null ? data.unitName : unit.name);
            EditorGUILayout.EnumPopup("Phe", stats != null ? unit.Team : data != null ? data.defaultTeam : unit.Team);
            EditorGUILayout.ObjectField("Sprite", data != null ? data.unitSprite : null, typeof(Sprite), false);
            EditorGUILayout.LabelField("Mô tả");
            EditorGUILayout.TextArea(data != null ? data.description : "", GUILayout.MinHeight(35));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Chỉ số gốc — UnitData", EditorStyles.boldLabel);
            EditorGUILayout.IntField("HP tối đa (cấu hình)", data != null ? data.maxHP : 0);
            EditorGUILayout.IntField("Sát thương gốc", data != null ? data.damage : 0);
            EditorGUILayout.IntField("Tầm di chuyển (ô)", unit.MoveRange);
            EditorGUILayout.IntField("Tầm tấn công (ô)", unit.AttackRange);
        }

        if (data == null) EditorGUILayout.HelpBox("Chưa gán UnitData.", MessageType.Warning);
        if (stats == null)
        {
            EditorGUILayout.HelpBox("Chưa khởi tạo runtime. Chọn quân trong Hierarchy khi Play để xem HP, Block, Buff/Debuff và Intent.", MessageType.Info);
            return;
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime — cập nhật khi Play", EditorStyles.boldLabel);
            EditorGUILayout.IntField("HP hiện tại", stats.Health);
            EditorGUILayout.IntField("HP tối đa runtime", stats.MaxHealth);
            EditorGUILayout.IntField("Block", stats.Block);
            EditorGUILayout.IntField("Sát thương sau Buff/Debuff", unit.Damage);
            EditorGUILayout.Toggle("Còn sống", unit.IsAlive);
            EditorGUILayout.Toggle("Đang di chuyển", unit.IsMoving);
            EditorGUILayout.Toggle("Component đang hoạt động", unit.isActiveAndEnabled);
            EditorGUILayout.Vector2IntField("Vị trí lưới", unit.GridPosition);
            EditorGUILayout.Vector3Field("Vị trí thế giới", unit.transform.position);
            EditorGUILayout.ObjectField("Ô đang chiếm",
                unitSerialized.FindProperty("occupiedCell").objectReferenceValue, typeof(GridCell), true);
            EditorGUILayout.ObjectField("Grid Manager",
                unitSerialized.FindProperty("gridManager").objectReferenceValue, typeof(GridManager), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Buff / Debuff", EditorStyles.boldLabel);
            foreach (StatusKind kind in Enum.GetValues(typeof(StatusKind)))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(kind.ToString());
                EditorGUILayout.LabelField($"Lượng: {stats.StatusAmount(kind)} | Còn: {stats.StatusTurns(kind)} lượt");
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.LabelField("Weak: x0.75 sát thương; Vulnerable: x1.5 sát thương nhận.", EditorStyles.wordWrappedMiniLabel);

            EnemyAI enemy = unit.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Enemy AI", EditorStyles.boldLabel);
                EditorGUILayout.Toggle("AI đang hoạt động", enemy.isActiveAndEnabled);
                EditorGUILayout.EnumPopup("Ý định", enemy.Intent.Kind);
                EditorGUILayout.ObjectField("Mục tiêu", enemy.Intent.Target, typeof(Unit), true);
                EditorGUILayout.IntField("Sát thương dự kiến", enemy.Intent.Amount);
                EditorGUILayout.LabelField("Kết quả lượt gần nhất");
                EditorGUILayout.TextArea(enemy.LastTurnResult ?? "Chưa hành động", GUILayout.MinHeight(35));
            }
            TurnManager turns = TurnManager.Instance;
            if (turns != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Trạng thái trận đấu", EditorStyles.boldLabel);
                EditorGUILayout.IntField("Vòng", turns.RoundNumber);
                EditorGUILayout.EnumPopup("Phe đang tới lượt", turns.ActiveTeam);
                EditorGUILayout.EnumPopup("Trạng thái lượt", turns.CurrentTurn);
                EditorGUILayout.Toggle("Đã kết thúc trận", turns.IsBattleOver);
                EditorGUILayout.TextField("Phe thắng", turns.IsBattleOver ? turns.Winner?.ToString() ?? "Hòa" : "Chưa kết thúc");
                if (unit.Team == UnitTeam.Player)
                    EditorGUILayout.Toggle("Đang thực hiện hành động", turns.IsActingPlayer(unit));
            }
        }
    }
}
