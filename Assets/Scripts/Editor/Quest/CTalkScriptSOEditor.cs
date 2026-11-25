using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CTalkScriptSO))]
public class CTalkScriptSOEditor : Editor
{
    private const string _NONE = "(원래 시작 대사)";

    private SerializedProperty _link;

    private void OnEnable()
    {
        _link = serializedObject.FindProperty("_questLink");
    }

    /// <summary>
    /// 대사와 퀘스트 연결을 표시합니다.
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "_questLink");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("퀘스트", EditorStyles.boldLabel);

        SerializedProperty quest = _link.FindPropertyRelative("quest");
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(quest, new GUIContent("연결할 퀘스트"));
            if (GUILayout.Button("새 퀘스트", GUILayout.ExpandWidth(false)))
            {
                quest.objectReferenceValue = CQuestEditorUtility.CreateQuest(target.name + "Quest");
            }
        }

        if (quest.objectReferenceValue != null)
        {
            if (GUILayout.Button("목표 편집")) Selection.activeObject = quest.objectReferenceValue;
            DrawLinkFields();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawLinkFields()
    {
        CTalkScriptSO script = (CTalkScriptSO)target;
        List<string> nodes = new List<string> { _NONE };
        foreach (CTalkNode node in script.Nodes)
        {
            if (node != null && !string.IsNullOrEmpty(node.id)) nodes.Add(node.id);
        }

        string[] options = nodes.ToArray();
        EditorGUILayout.LabelField("단계별 시작 대사", EditorStyles.miniBoldLabel);
        DrawNodePopup("수락 전", "offerNode", options);
        DrawNodePopup("진행 중", "progressNode", options);
        DrawNodePopup("보고 가능", "reportNode", options);
        DrawNodePopup("완료 후", "doneNode", options);

        foreach (string problem in GetProblems(script))
        {
            EditorGUILayout.HelpBox(problem, MessageType.Warning);
        }
    }

    private void DrawNodePopup(string label, string field, string[] nodes)
    {
        SerializedProperty property = _link.FindPropertyRelative(field);
        int index = string.IsNullOrEmpty(property.stringValue) ? 0 : System.Array.IndexOf(nodes, property.stringValue);
        EditorGUI.BeginChangeCheck();
        int picked = EditorGUILayout.Popup(label, index, nodes);
        if (EditorGUI.EndChangeCheck() && picked >= 0)
        {
            property.stringValue = picked == 0 ? string.Empty : nodes[picked];
        }
        if (index < 0) EditorGUILayout.HelpBox($"'{property.stringValue}' 대사를 찾을 수 없습니다.", MessageType.Warning);
    }

    private static List<string> GetProblems(CTalkScriptSO script)
    {
        CTalkQuestLink link = script.QuestLink;
        List<string> problems = CQuestEditorUtility.GetProblems(link.quest);
        foreach (CTalkNode node in script.Nodes)
        {
            if (node == null || !node.HasChoices) continue;
            foreach (CTalkChoice choice in node.choices)
            {
                if (choice == null || choice.questAction == ETalkQuestAction.None) continue;
                string start = choice.questAction == ETalkQuestAction.Accept ? link.offerNode : link.reportNode;
                if (string.IsNullOrEmpty(start)) start = script.StartNode;
                if (!IsReachable(script, start, node.id))
                {
                    problems.Add($"'{start}'에서 '{node.id}' 선택지로 갈 수 없습니다.");
                }
            }
        }

        return problems;
    }

    private static bool IsReachable(CTalkScriptSO script, string from, string to)
    {
        HashSet<string> visited = new HashSet<string>();
        Queue<string> queue = new Queue<string>();
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            string id = queue.Dequeue();
            if (string.IsNullOrEmpty(id) || !visited.Add(id)) continue;
            if (id == to) return true;

            CTalkNode node = script.Find(id);
            if (node == null) continue;

            if (!node.HasChoices)
            {
                queue.Enqueue(node.next);
                continue;
            }

            foreach (CTalkChoice choice in node.choices)
            {
                if (choice != null) queue.Enqueue(choice.to);
            }
        }

        return false;
    }
}
