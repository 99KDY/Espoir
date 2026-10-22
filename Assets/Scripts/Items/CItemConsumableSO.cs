using UnityEngine;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "Espoir/ConsumableSO")]
public class CItemConsumableSO : CItemSO
{
    [SerializeField] private CConsumableAction _action;

    public CConsumableAction Action
    {
        get { return _action; }
    }
}