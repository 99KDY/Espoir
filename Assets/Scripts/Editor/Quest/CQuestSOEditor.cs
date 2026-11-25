using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CQuestSO))]
[CanEditMultipleObjects]
public class CQuestSOEditor : Editor
{
    /// <summary>
    /// 퀘스트 조회에 사용할 플래그와 수락 조건, 목표를 표시합니다.
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty flag = serializedObject.FindProperty("_questFlag");
        EQuestList current = (EQuestList)flag.intValue;
        EditorGUI.BeginDisabledGroup(targets.Length > 1);
        EditorGUI.BeginChangeCheck();
        EQuestList selected = (EQuestList)EditorGUILayout.EnumPopup("퀘스트 플래그", current);
        if (EditorGUI.EndChangeCheck())
        {
            CQuestSO owner = FindFlagOwner(selected);
            if (owner == null) flag.intValue = (int)selected;
            else EditorUtility.DisplayDialog("퀘스트 플래그", $"'{owner.name}'에서 이미 사용 중입니다.", "확인");
        }

        EditorGUI.EndDisabledGroup();

        DrawPropertiesExcluding(serializedObject, "m_Script", "_questFlag");
        serializedObject.ApplyModifiedProperties();
        if (targets.Length != 1) return;

        foreach (string problem in CQuestEditorUtility.GetProblems((CQuestSO)target))
        {
            EditorGUILayout.HelpBox(problem, MessageType.Warning);
        }
    }

    private CQuestSO FindFlagOwner(EQuestList flag)
    {
        if (flag == EQuestList.None) return null;

        foreach (CQuestSO quest in Resources.LoadAll<CQuestSO>(CQuestSO.RESOURCES_FOLDER))
        {
            if (quest != target && quest.QuestFlag == flag) return quest;
        }

        return null;
    }
}
