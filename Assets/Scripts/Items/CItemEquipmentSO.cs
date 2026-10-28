using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipment", menuName = "Espoir/EquipmentSO")]
public class CItemEquipmentSO : CItemSO
{
    [Tooltip("장비 장착시 적용되는 능력치 변화")]
    [SerializeField] private List<SEquipmentAction> _action;
    [Tooltip("장비 장착시 적용되는 상태이상 (duration은 무시됨)")]
    [SerializeField] private List<SStatusApply> _equipmentStatus;

    public IReadOnlyList<SEquipmentAction> Action => _action;

    public IReadOnlyList<SStatusApply> EquipmentStatus => _equipmentStatus;
}