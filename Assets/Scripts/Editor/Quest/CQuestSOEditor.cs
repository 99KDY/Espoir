using UnityEditor;

[CustomEditor(typeof(CQuestSO))]
[CanEditMultipleObjects]
public class CQuestSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script");
        serializedObject.ApplyModifiedProperties();

        if (targets.Length != 1) return;

        foreach (string problem in CQuestEditorUtility.GetProblems((CQuestSO)target))
        {
            EditorGUILayout.HelpBox(problem, MessageType.Warning);
        }
    }
}