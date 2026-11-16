using UnityEngine;

/// <summary>
/// CUnit.OnHit 에서 확정된 피해 한 번의 결과. 데미지 숫자 · 대상 UI 처럼 표시하는 쪽이 읽기만 한다.
/// </summary>
public struct SDamageResult
{
    public CUnit attacker;
    public CUnit target;
    // DamageCalculate 최종값(치명 · 방어 · 상태 보정 포함, 남은 HP 를 넘는 초과 피해도 포함)
    public float damage;
    // 실제로 줄어든 HP. 플레이어 무적 플래그 · 이미 HP 0 이면 0
    public float hpLost;
    public bool isCritical;
    // 피해가 확정된 순간 대상의 transform.position
    public Vector3 worldPosition;

    /// <summary>
    /// 계산된 피해와 실제 체력 감소량을 표시용 결과로 저장합니다.
    /// </summary>
    public SDamageResult(CUnit attacker, CUnit target, float damage, float hpLost, bool isCritical,
        Vector3 worldPosition)
    {
        this.attacker = attacker;
        this.target = target;
        this.damage = damage;
        this.hpLost = hpLost;
        this.isCritical = isCritical;
        this.worldPosition = worldPosition;
    }
}
