using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EStatusStack
{
    StackAndRefresh,    // 스택이 쌓이고 지속시간 갱신
    StackIndividual,    // 별개의 상태이상으로 적용
    Refresh,            // 지속시간만 갱신
    Ignore              // 이미 있을경우 적용 안함
}

[Flags]
public enum EStatusTrigger
{
    None = 0,           // 자체적으로 효과 없는 경우
    OnApply = 1 << 0,   // 적용될 때 적용되는 효과가 있는 경우
    OnRemove = 1 << 1,  // 사라질 때 적용되는 효과가 있는 경우 (Apply한거 있으면 여기서 해제해야함!
    OnAttack = 1 << 2,  // 공격할 때 적용되는 효과가 있는 경우
    OnDefence = 1 << 2, // 방어할 때 적용되는 효과가 있는 경우
    OnDelta = 1 << 2,   // Time.deltaTime마다 적용되는 효과가 있는 경우
}

public enum EStatusAction
{
    None,
    IncreaseHpMax,
    MultiplyHpMax,
    IncreaseAttack,
    MultiplyAttack,
    IncreaseDefence,
    MultiplyDefence,
    IncreaseDamage,
    MultiplyDamage,
    MultiplyMovement,
    Stun,
    DamageOverTime,
    DamageOverTimeByRatio,
    CustomActionOnApply,
    CustomActionOnAttack,
    CustomActionOnDefence,
    CustomActionOnDelta,
}

[Serializable]
public struct SStatusAction
{
    public EStatusAction action;
    public float param;
}

public class CStatus
{

}
