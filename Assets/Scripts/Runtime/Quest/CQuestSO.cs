using System;
using System.Collections.Generic;
using UnityEngine;

public enum EQuestObjectiveKind
{
    [InspectorName("처치")] Kill = 0,
    [InspectorName("스킬 적중")] SkillHit = 1,
    [InspectorName("스킬 사용")] SkillUse = 2,
    [InspectorName("상태 중첩")] StatusStack = 3,
    [InspectorName("대기시간 감소")] CooldownReduced = 4,
    [InspectorName("장착")] Equip = 5,
    [InspectorName("아이템 사용")] UseItem = 6,
    [InspectorName("도착")] Arrive = 7,
    [InspectorName("대화")] Talk = 8,
    [InspectorName("카메라 조작")] Camera = 9,
    [InspectorName("레비아탄 피니쉬")] LeviathanFinisher = 10,
    [InspectorName("프라도나 권능")] PradonaPower = 11,
}

public enum EQuestStatusOwner
{
    [InspectorName("플레이어")] Player = 0,
    [InspectorName("적")] Enemy = 1,
}

public enum EQuestState
{
    None = 0,
    Active = 1,
    Reportable = 2,
    Completed = 3,
}

[Serializable]
public class CQuestObjective
{
    public string id;
    public EQuestObjectiveKind kind;
    public string text;
    public string hint;
    [Min(1)] public int count = 1;

    public CEnemy enemy;
    public EBossClear boss;
    public CSkillSO[] skills;
    public bool requireLink;
    public bool requireFinisher;
    public CStatusSO status;
    public EQuestStatusOwner statusOwner;
    [Min(1)] public int stack = 1;
    public CItemSO item;
    public bool countEarlierUse;
    public string npcId;
    public bool requireFullStack = true;

    public bool useDestination;
    public ESceneId destinationScene;
    public string destinationName;
    public Vector3 destinationPoint;
    public string destinationNpcId;
    [Min(0.5f)] public float arriveRadius = 3f;

    public CItemSO supplyItem;
    [Min(1)] public int supplyCount = 1;
    public CItemSO replacementItem;

    public bool IsBossTarget => boss != EBossClear.None;

    public int RequiredCount => UsesCount(kind) ? Mathf.Max(1, count) : 1;

    /// <summary>
    /// 횟수를 지정할 수 있는 목표인지 확인합니다.
    /// </summary>
    public static bool UsesCount(EQuestObjectiveKind kind)
    {
        return kind is EQuestObjectiveKind.Kill or EQuestObjectiveKind.SkillHit or EQuestObjectiveKind.SkillUse or
            EQuestObjectiveKind.CooldownReduced or EQuestObjectiveKind.UseItem or EQuestObjectiveKind.PradonaPower;
    }

    /// <summary>
    /// 목표 아이템이나 대체 지급한 아이템인지 확인합니다.
    /// </summary>
    public bool MatchesItem(int itemId)
    {
        return (item != null && item.Id == itemId) || (replacementItem != null && replacementItem.Id == itemId);
    }

    /// <summary>
    /// 목표에 지정한 스킬인지 확인합니다.
    /// </summary>
    public bool HasSkill(CSkillSO target)
    {
        if (target == null || skills == null) return false;

        foreach (CSkillSO candidate in skills)
        {
            if (candidate == target) return true;
        }

        return false;
    }
}

[CreateAssetMenu(fileName = "Quest", menuName = "Espoir/Quest/Quest")]
public class CQuestSO : ScriptableObject
{
    public const string RESOURCES_FOLDER = "Quests";

    [SerializeField, HideInInspector] private string _questId;
#if UNITY_EDITOR
    [SerializeField, HideInInspector] private string _assetGuid;
#endif
    [SerializeField] private string _title = "새 퀘스트";
    [SerializeField] private List<CQuestObjective> _objectives = new List<CQuestObjective>();

    [SerializeField] private string _reportText = "의뢰인에게 보고하기";
    [SerializeField] private string _reportNpcId;

    [Header("수락 조건")]
    [SerializeField, Tooltip("보고까지 완료해야 하는 선행 퀘스트")] private CQuestSO _prerequisite;
    [SerializeField, Tooltip("선택한 보스를 모두 처치하면 조건을 충족합니다.")] private EBossClear _requiredBosses;

    public string QuestId => _questId;
    public string Title => _title;
    public string ReportText => _reportText;
    public string ReportNpcId => _reportNpcId;
    public CQuestSO Prerequisite => _prerequisite;
    public EBossClear RequiredBosses => _requiredBosses;
    public IReadOnlyList<CQuestObjective> Objectives => _objectives;

    /// <summary>
    /// 저장된 목표 ID에 해당하는 순번을 반환합니다.
    /// </summary>
    public int FindObjectiveIndex(string objectiveId)
    {
        if (string.IsNullOrEmpty(objectiveId)) return -1;

        for (int i = 0; i < _objectives.Count; i++)
        {
            if (_objectives[i] != null && _objectives[i].id == objectiveId) return i;
        }

        return -1;
    }

    /// <summary>
    /// 순번의 목표를 반환합니다. 범위를 벗어나면 null.
    /// </summary>
    public CQuestObjective GetObjective(int index)
    {
        if (index < 0 || index >= _objectives.Count) return null;
        return _objectives[index];
    }

#if UNITY_EDITOR
    /// <summary>
    /// 새 퀘스트와 복제한 퀘스트의 저장 식별값을 설정합니다.
    /// </summary>
    public bool UpdateAssetGuid(string guid)
    {
        if (_assetGuid == guid) return false;
        if (!string.IsNullOrEmpty(_assetGuid))
        {
            _questId = guid;
        }
        else if (string.IsNullOrEmpty(_questId)) _questId = guid;
        _assetGuid = guid;
        return true;
    }

    private void OnValidate()
    {
        HashSet<string> used = new HashSet<string>();

        foreach (CQuestObjective objective in _objectives)
        {
            if (objective == null) continue;
            if (string.IsNullOrEmpty(objective.id) || !used.Add(objective.id))
            {
                objective.id = Guid.NewGuid().ToString("N").Substring(0, 12);
                used.Add(objective.id);
            }
        }
    }
#endif
}
