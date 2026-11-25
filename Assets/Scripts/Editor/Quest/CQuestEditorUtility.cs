using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CQuestEditorUtility
{
    public const string QUEST_FOLDER = "Assets/Resources/" + CQuestSO.RESOURCES_FOLDER;

    /// <summary>
    /// 목표의 설정 오류를 반환합니다.
    /// </summary>
    public static List<string> GetProblems(CQuestObjective objective)
    {
        List<string> problems = new List<string>();
        if (objective == null) return problems;

        if (string.IsNullOrWhiteSpace(objective.text)) problems.Add("목표 문구를 입력하세요.");
        if (CQuestObjective.UsesCount(objective.kind) && objective.count < 1)
        {
            problems.Add("수량은 1 이상이어야 합니다.");
        }

        bool targetsEnemy = objective.kind == EQuestObjectiveKind.Kill || objective.kind == EQuestObjectiveKind.SkillHit
            || (objective.kind == EQuestObjectiveKind.StatusStack && objective.statusOwner == EQuestStatusOwner.Enemy);
        if (targetsEnemy && objective.IsBossTarget)
        {
            if (objective.enemy != null) problems.Add("몬스터와 보스 중 하나만 선택하세요.");
            if ((objective.boss & (objective.boss - 1)) != 0) problems.Add("보스를 하나만 선택하세요.");
        }

        switch (objective.kind)
        {
            case EQuestObjectiveKind.SkillHit:
            case EQuestObjectiveKind.SkillUse:
            case EQuestObjectiveKind.CooldownReduced:
                if (!HasAnySkill(objective)) problems.Add("스킬을 하나 이상 선택하세요.");
                break;
            case EQuestObjectiveKind.StatusStack:
                if (objective.status == null) problems.Add("확인할 상태를 선택하세요.");
                else if (objective.status.MaxStack > 0 && objective.stack > objective.status.MaxStack)
                {
                    problems.Add($"필요 중첩이 최대 중첩({objective.status.MaxStack})을 초과합니다.");
                }
                break;
            case EQuestObjectiveKind.Equip:
                if (objective.item == null) problems.Add("장착할 아이템을 선택하세요.");
                else if (!(objective.item is CItemEquipmentSO)) problems.Add("장비 아이템을 선택하세요.");
                break;
            case EQuestObjectiveKind.UseItem:
                if (objective.item == null) problems.Add("사용할 아이템을 선택하세요.");
                else if (!(objective.item is CItemConsumableSO)) problems.Add("소모품을 선택하세요.");
                break;
            case EQuestObjectiveKind.Arrive:
                if (!objective.useDestination) problems.Add("도착 목표에 목적지를 설정하세요.");
                break;
            case EQuestObjectiveKind.Talk:
                if (string.IsNullOrWhiteSpace(objective.npcId)) problems.Add("대화할 NPC ID를 입력하세요.");
                break;
        }

        if (objective.useDestination && string.IsNullOrWhiteSpace(objective.destinationName) &&
            string.IsNullOrEmpty(objective.destinationNpcId))
        {
            problems.Add("목적지 이름을 입력하세요.");
        }

        return problems;
    }

    /// <summary>
    /// 퀘스트의 설정 오류를 반환합니다.
    /// </summary>
    public static List<string> GetProblems(CQuestSO quest)
    {
        List<string> problems = new List<string>();
        if (quest == null) return problems;

        if (string.IsNullOrWhiteSpace(quest.Title)) problems.Add("퀘스트 제목을 입력하세요.");
        if (quest.Objectives.Count == 0) problems.Add("목표를 한 개 이상 추가하세요.");

        HashSet<CQuestSO> visited = new HashSet<CQuestSO>();
        for (CQuestSO prerequisite = quest; prerequisite != null; prerequisite = prerequisite.Prerequisite)
        {
            if (visited.Add(prerequisite)) continue;
            problems.Add("선행 퀘스트가 순환하도록 연결되어 있습니다.");
            break;
        }

        string path = AssetDatabase.GetAssetPath(quest);
        if (!string.IsNullOrEmpty(path) && !path.Replace('\\', '/').StartsWith(QUEST_FOLDER + "/"))
        {
            problems.Add($"퀘스트 에셋을 {QUEST_FOLDER} 폴더에 넣으세요.");
        }

        for (int i = 0; i < quest.Objectives.Count; i++)
        {
            foreach (string problem in GetProblems(quest.Objectives[i]))
            {
                problems.Add($"목표 {i + 1}: {problem}");
            }
        }

        return problems;
    }

    /// <summary>
    /// 퀘스트 폴더에 새 퀘스트 자산을 만들고 반환합니다.
    /// </summary>
    public static CQuestSO CreateQuest(string baseName)
    {
        if (!AssetDatabase.IsValidFolder(QUEST_FOLDER))
        {
            Directory.CreateDirectory(QUEST_FOLDER);
            AssetDatabase.Refresh();
        }

        CQuestSO quest = ScriptableObject.CreateInstance<CQuestSO>();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{QUEST_FOLDER}/{baseName}.asset");
        AssetDatabase.CreateAsset(quest, path);

        return quest;
    }

    private static bool HasAnySkill(CQuestObjective objective)
    {
        if (objective.skills == null) return false;

        foreach (CSkillSO skill in objective.skills)
        {
            if (skill != null) return true;
        }

        return false;
    }
}
