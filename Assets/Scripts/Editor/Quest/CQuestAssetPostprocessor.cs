using System;
using UnityEditor;

public class CQuestAssetPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        UpdateQuestIds(importedAssets);
        UpdateQuestIds(movedAssets);
    }

    private static void UpdateQuestIds(string[] paths)
    {
        foreach (string path in paths)
        {
            if (!path.StartsWith(CQuestEditorUtility.QUEST_FOLDER + "/", StringComparison.Ordinal) ||
                !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) continue;

            CQuestSO quest = AssetDatabase.LoadAssetAtPath<CQuestSO>(path);
            if (quest == null || !quest.UpdateAssetGuid(AssetDatabase.AssetPathToGUID(path))) continue;
            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssetIfDirty(quest);
        }
    }
}
