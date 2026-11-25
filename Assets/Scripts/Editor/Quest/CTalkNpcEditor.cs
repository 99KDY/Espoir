using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CTalkNpc))]
[CanEditMultipleObjects]
public class CTalkNpcEditor : Editor
{
    /// <summary>
    /// NPC의 대화와 퀘스트 연결을 표시합니다.
    /// </summary>
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (targets.Length != 1) return;

        SerializedProperty script = serializedObject.FindProperty("_script");
        CTalkScriptSO talk = script.objectReferenceValue as CTalkScriptSO;

        EditorGUILayout.Space();
        if (talk == null) return;

        if (GUILayout.Button("대화 편집")) Selection.activeObject = talk;

        CQuestSO quest = talk.QuestLink.quest;
        string state = quest == null ? "연결된 퀘스트 없음" : $"퀘스트: {quest.Title} (목표 {quest.Objectives.Count}개)";
        EditorGUILayout.LabelField(state, EditorStyles.miniLabel);
    }
}
