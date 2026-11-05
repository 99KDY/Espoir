using System;
using UnityEngine;

public enum ESkillType
{
    Melee,
    Dash,
    Rush,
    Slam,
    Crown
}

[Serializable]
public struct SSkillHit
{
    [Range(0f, 1f)] public float time;
    [Min(0f)] public float damageRatio;
    [Min(0f)] public float hitStopDuration;
    public Vector3 offset;
    public Vector3 size;
    public bool appliesMark;
    public bool opensCrownWindow;
}

[Serializable]
public struct SSkillStep
{
    public string stateName;
    public string recoverStateName;
    [Min(0f)] public float moveDistance;
    [Range(0f, 1f)] public float moveStart;
    [Range(0f, 1f)] public float moveEnd;
    public AnimationCurve moveCurve;
    public AnimationCurve sideMoveCurve;
    public SSkillHit[] hits;
    public GameObject effectPrefab;
    [Range(0f, 1f)] public float effectTime;
    [Min(0f)] public float effectLifetime;
}

[CreateAssetMenu(fileName = "Skill", menuName = "Skill/Player Skill")]
public class CSkillSO : ScriptableObject
{
    [Header("스킬 설정")]
    [SerializeField] private ESkillType _type;
    [SerializeField, Min(0f)] private float _cooldown;
    [SerializeField, Min(0)] private int _mpCost;
    [SerializeField] private string _startTrigger;
    [SerializeField] private string _comboTrigger;
    [SerializeField, Min(0f)] private float _rotationSpeed = 1080f;
    [SerializeField, Min(0f)] private float _comboGraceTime = 0.8f;

    [Header("대쉬 설정")]
    [SerializeField, Min(0.01f)] private float _dashDuration = 0.25f;
    [SerializeField, Min(0f)] private float _dashStartDelay;
    [SerializeField, Min(1f)] private float _dashEasePower = 1f;
    [SerializeField] private SDashTrailSettings _dashTrail;

    [Header("돌진 설정")]
    [SerializeField] private AnimationCurve _movementCurve;
    [SerializeField] private GameObject _followEffect;
    [SerializeField, Min(0f)] private float _markedCooldownReduction;

    [Header("내리꽂기 설정")]
    [SerializeField, Min(0)] private int _fullHitFlow;

    [Header("왕관 설정")]
    [SerializeField] private CStatusSO _crownStatus;
    [SerializeField, Min(0f)] private float _crownWindow = 2f;
    [SerializeField, Min(0f)] private float _empowerDuration = 8f;
    [SerializeField] private CSkillSO[] _empoweredAttacks;
    [SerializeField] private GameObject[] _crownEffects;
    [SerializeField] private float _crownEffectYaw = 225f;

    [Header("이펙트 재생 설정")]
    [SerializeField] private bool _syncEffectSpeed;

    [Header("단계별 설정")]
    [SerializeField] private SSkillStep[] _steps;

    public ESkillType Type => _type;
    public float Cooldown => _cooldown;
    public int MpCost => _mpCost;
    public string StartTrigger => _startTrigger;
    public string ComboTrigger => _comboTrigger;
    public float RotationSpeed => _rotationSpeed;
    public float ComboGraceTime => _comboGraceTime;
    public float DashDuration => _dashDuration;
    public float DashStartDelay => _dashStartDelay;
    public float DashEasePower => _dashEasePower;
    public SDashTrailSettings DashTrail => _dashTrail;
    public AnimationCurve MovementCurve => _movementCurve;
    public GameObject FollowEffect => _followEffect;
    public float MarkedCooldownReduction => _markedCooldownReduction;
    public int FullHitFlow => _fullHitFlow;
    public CStatusSO CrownStatus => _crownStatus;
    public float CrownWindow => _crownWindow;
    public float EmpowerDuration => _empowerDuration;
    public CSkillSO[] EmpoweredAttacks => _empoweredAttacks;
    public GameObject[] CrownEffects => _crownEffects;
    public float CrownEffectYaw => _crownEffectYaw;
    public bool SyncEffectSpeed => _syncEffectSpeed;
    public SSkillStep[] Steps => _steps;
}
