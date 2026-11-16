using UnityEngine;

/// <summary>
/// 레비아탄의 버프와 피니쉬를 설정합니다.
/// 첫 단계의 모션과 이펙트는 피니쉬에 사용하며, 첫 타격의 위치와 크기로 범위를 정합니다.
/// </summary>
[CreateAssetMenu(fileName = "Skill", menuName = "Skill/Player Leviathan Skill")]
public class CLeviathanSkillSO : CSkillSO
{
    [Header("레비아탄 버프")]
    [SerializeField] private CStatusSO _buffStatus;
    [Tooltip("시전 후 자동 피니쉬까지의 시간(초). 피니쉬 중에는 버프를 유지하고 종료 시 해제한다.")]
    [SerializeField, Min(1f)] private float _finisherDelay = 30f;

    [Header("피니쉬 스택")]
    [SerializeField, Min(1)] private int _maxFinisherStack = 10;

    [Header("피니쉬 타격")]
    [SerializeField, Min(1)] private int _baseHitCount = 10;
    [SerializeField, Min(0)] private int _bonusHitCount = 20;
    [SerializeField, Min(0f)] private float _hitDamageRatio = 1f;
    [SerializeField, Min(0f)] private float _lastHitDamageRatio = 10f;
    [SerializeField, Range(0f, 1f)] private float _firstHitTime = 0.387f;
    [SerializeField, Range(0f, 1f)] private float _lastHitTime = 0.6f;

    [Header("전투의 흐름 조건")]
    [SerializeField] private CStatusSO _flowStatus;
    [SerializeField, Min(1)] private int _requiredFlowStack = 50;
    [SerializeField, Min(0)] private int _penetrationHitCount = 10;
    [SerializeField, Range(0f, 1f)] private float _armorPenetration = 0.25f;

    public CStatusSO BuffStatus => _buffStatus;
    public float FinisherDelay => _finisherDelay;
    public int MaxFinisherStack => _maxFinisherStack;
    public int BaseHitCount => _baseHitCount;
    public int BonusHitCount => _bonusHitCount;
    public float HitDamageRatio => _hitDamageRatio;
    public float LastHitDamageRatio => _lastHitDamageRatio;
    public float FirstHitTime => _firstHitTime;
    public float LastHitTime => _lastHitTime;
    public CStatusSO FlowStatus => _flowStatus;
    public int RequiredFlowStack => _requiredFlowStack;
    public int PenetrationHitCount => _penetrationHitCount;
    public float ArmorPenetration => _armorPenetration;
}
