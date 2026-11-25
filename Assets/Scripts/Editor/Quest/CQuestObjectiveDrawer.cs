using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CQuestObjective))]
public class CQuestObjectiveDrawer : PropertyDrawer
{
    private const float _GAP = 2f;

    private static readonly Dictionary<string, string> _LABELS = new Dictionary<string, string>
    {
        { "kind", "종류" }, { "text", "목표 문구" }, { "hint", "안내 (작은 글씨)" }, { "count", "수량" },
        { "enemy", "몬스터 (원본 프리팹)" }, { "boss", "보스" }, { "skills", "스킬" },
        { "requireLink", "연계일 때만" }, { "requireFinisher", "막타일 때만" },
        { "status", "상태" }, { "statusOwner", "상태 효과 대상" }, { "stack", "필요 중첩 (동시)" },
        { "item", "아이템" }, { "countEarlierUse", "목표 시작 전 사용 포함" }, { "npcId", "NPC ID" },
        { "requireFullStack", "최대 중첩에서 직접 사용" }, { "useDestination", "목적지 표시" },
        { "destinationScene", "맵" }, { "destinationName", "목적지 이름" }, { "destinationPoint", "좌표 (월드)" },
        { "destinationNpcId", "따라갈 NPC ID" }, { "arriveRadius", "도착 반경 (m)" },
        { "supplyItem", "보급 아이템" }, { "supplyCount", "보급 수량" }, { "replacementItem", "폐기 시 대체 아이템" },
    };

    /// <summary>
    /// 목표 종류에 맞는 설정을 표시합니다.
    /// </summary>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, GetTitle(property), true);
        line.y += line.height + _GAP;

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            using (new EditorGUI.DisabledScope(true))
            {
                line = DrawField(line, property.FindPropertyRelative("id"), "목표 ID (저장용)");
            }

            foreach (string name in GetVisibleFields(property))
            {
                line = DrawField(line, property.FindPropertyRelative(name), _LABELS[name]);
            }

            foreach (string problem in GetProblems(property))
            {
                float height = GetHelpHeight(problem, line.width);
                EditorGUI.HelpBox(new Rect(line.x, line.y, line.width, height), problem, MessageType.Warning);
                line.y += height + _GAP;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    /// <summary>
    /// 펼쳐진 목표 항목의 높이를 반환합니다.
    /// </summary>
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight + _GAP;
        if (!property.isExpanded) return height;

        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("id"), true) + _GAP;

        foreach (string name in GetVisibleFields(property))
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(name), true) + _GAP;
        }

        float width = EditorGUIUtility.currentViewWidth - 40f;
        foreach (string problem in GetProblems(property))
        {
            height += GetHelpHeight(problem, width) + _GAP;
        }

        return height;
    }

    private static List<string> GetVisibleFields(SerializedProperty property)
    {
        List<string> fields = new List<string> { "kind", "text", "hint" };
        EQuestObjectiveKind kind = (EQuestObjectiveKind)property.FindPropertyRelative("kind").enumValueIndex;

        switch (kind)
        {
            case EQuestObjectiveKind.Kill:
                fields.AddRange(new[] { "enemy", "boss" });
                break;
            case EQuestObjectiveKind.SkillHit:
                fields.AddRange(new[] { "skills", "enemy", "boss", "requireLink", "requireFinisher" });
                break;
            case EQuestObjectiveKind.SkillUse:
            case EQuestObjectiveKind.CooldownReduced:
                fields.Add("skills");
                break;
            case EQuestObjectiveKind.StatusStack:
                fields.AddRange(new[] { "status", "statusOwner", "stack" });
                if (property.FindPropertyRelative("statusOwner").enumValueIndex == (int)EQuestStatusOwner.Enemy)
                {
                    fields.AddRange(new[] { "enemy", "boss" });
                }
                break;
            case EQuestObjectiveKind.Equip:
                fields.Add("item");
                break;
            case EQuestObjectiveKind.UseItem:
                fields.Add("item");
                if (property.FindPropertyRelative("count").intValue == 1) fields.Add("countEarlierUse");
                break;
            case EQuestObjectiveKind.Talk:
                fields.Add("npcId");
                break;
            case EQuestObjectiveKind.LeviathanFinisher:
                fields.Add("requireFullStack");
                break;
        }

        if (CQuestObjective.UsesCount(kind)) fields.Add("count");
        fields.Add("useDestination");
        if (property.FindPropertyRelative("useDestination").boolValue || kind == EQuestObjectiveKind.Arrive)
        {
            fields.AddRange(new[] { "destinationScene", "destinationName", "destinationPoint", "destinationNpcId" });
            if (kind == EQuestObjectiveKind.Arrive) fields.Add("arriveRadius");
        }

        fields.Add("supplyItem");
        Object supply = property.FindPropertyRelative("supplyItem").objectReferenceValue;
        if (supply != null)
        {
            fields.Add("supplyCount");
            if ((kind == EQuestObjectiveKind.Equip || kind == EQuestObjectiveKind.UseItem) &&
                supply == property.FindPropertyRelative("item").objectReferenceValue) fields.Add("replacementItem");
        }

        return fields;
    }

    private static List<string> GetProblems(SerializedProperty property)
    {
        if (!(property.boxedValue is CQuestObjective objective)) return new List<string>();
        return CQuestEditorUtility.GetProblems(objective);
    }

    private static string GetTitle(SerializedProperty property)
    {
        string text = property.FindPropertyRelative("text").stringValue;
        EQuestObjectiveKind kind = (EQuestObjectiveKind)property.FindPropertyRelative("kind").enumValueIndex;
        string kindName = property.FindPropertyRelative("kind").enumDisplayNames[(int)kind];
        return string.IsNullOrEmpty(text) ? $"[{kindName}]" : $"[{kindName}] {text}";
    }

    private static Rect DrawField(Rect line, SerializedProperty field, string labelText)
    {
        if (field == null) return line;

        float height = EditorGUI.GetPropertyHeight(field, true);
        Rect rect = new Rect(line.x, line.y, line.width, height);
        EditorGUI.PropertyField(rect, field, new GUIContent(labelText, field.tooltip), true);
        line.y += height + _GAP;
        return line;
    }

    private static float GetHelpHeight(string text, float width)
    {
        return Mathf.Max(EditorGUIUtility.singleLineHeight * 1.5f,
            EditorStyles.helpBox.CalcHeight(new GUIContent(text), width - 30f) + 4f);
    }
}
