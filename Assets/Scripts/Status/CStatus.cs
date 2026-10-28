using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EStatusStack
{
    [InspectorName("스택이 쌓이고, 지속시간이 갱신됨")]
    StackAndRefresh,    // 스택이 쌓이고 지속시간 갱신
    [InspectorName("별개의 상태이상으로 적용")]
    StackIndividual,    // 별개의 상태이상으로 적용
    [InspectorName("지속시간을 갱신함")]
    Refresh,            // 지속시간만 갱신
    [InspectorName("이미 있는 경우 적용안함")]
    Ignore              // 이미 있을경우 적용 안함
}

[Flags]
public enum EStatusTrigger
{
    None = 0,           // 자체적으로 효과 없는 경우
    // 트리거 플래그
    OnEquip = 1 << 0,   // 장비로서 적용되는 경우
    OnApply = 1 << 1,   // 적용될 때 적용되는 효과가 있는 경우
    OnRemove = 1 << 2,  // 사라질 때 적용되는 효과가 있는 경우 (Apply한거 있으면 여기서 해제해야함!
    OnAttack = 1 << 3,  // 공격할 때 적용되는 효과가 있는 경우
    OnDefence = 1 << 4, // 방어할 때 적용되는 효과가 있는 경우
    OnDelta = 1 << 5,   // Time.deltaTime마다 적용되는 효과가 있는 경우
    OnBattleEnd = 1 << 6, // 전투가 끝날 때 적용되는 효과가 있는 경우 (그냥 상태이상 해제용)
    // 효과 플래그
    IsStun = 1 << 16    // 기절 계열
}

public enum EStatusAction
{
    None,
    [InspectorName("최대체력 비율 변화")]
    MultiplyHpMax,
    [InspectorName("공격력 비율 변화")]
    MultiplyAttack,
    [InspectorName("방어력 비율 변화")]
    MultiplyDefence,
    [InspectorName("치명타확률 비율 변화 (상수/플레이어용)")]
    AddCriticalRate,
    [InspectorName("치명타확률 비율 변화 (비율/플레이어용)")]
    MultiplyCriticalRate,
    [InspectorName("치명타피해 비율 변화 (플레이어용)")]
    MultiplyCriticalDmg,
    [InspectorName("주는피해량 비율 변화")]
    MultiplyAttackerDamage,
    [InspectorName("받는피해량 비율 변화")]
    MultiplyDefenderDamage,
    [InspectorName("이동속도 변화")]
    MultiplyMovement,
    [InspectorName("기절")]
    Stun,
    [InspectorName("지속피해")]
    DamageOverTime,
    [InspectorName("최대체력 비례 지속피해")]
    DamageOverTimeByRatio,
    [InspectorName("커스텀 - OnApply")]
    CustomActionOnApply,
    [InspectorName("커스텀 - OnAttack")]
    CustomActionOnAttack,
    [InspectorName("커스텀 - OnDefence")]
    CustomActionOnDefence,
    [InspectorName("커스텀 - OnDelta")]
    CustomActionOnDelta,
}

[Serializable]
public struct SStatusAction
{
    public EStatusAction action;
    [Tooltip("비율은 증가시 1.xx 감소시 0.xx로!")]
    public float param;
}

[Serializable]
public struct SStatusApply
{
    [Tooltip("상태이상 Id")]
    public int statusId;
    [Tooltip("상태이상 지속시간 (0 : 영속, 음수 : 기본값)")]
    public float duration;
}


public class CStatus
{
    private EStatusTrigger _trigger;

    public EStatusTrigger Trigger
    {
        get { return _trigger; }
    }

    private List<Action<CPlayer>> _onEquip = new List<Action<CPlayer>>();
    private List<Action<CPlayer>> _onApply = new List<Action<CPlayer>>();
    private List<Action<CPlayer>> _onRemove = new List<Action<CPlayer>>();

    public List<Action<CPlayer>> OnEquip
    {
        get => _onEquip;
    }

    public List<Action<CPlayer>> OnApply
    {
        get => _onApply;
    }

    public List<Action<CPlayer>> OnRemove
    {
        get => _onRemove;
    }
}
