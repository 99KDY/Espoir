using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

public enum EItemRarity
{
    [InspectorName("일반")]
    Normal,
    [InspectorName("레어")]
    Rare,
    [InspectorName("에픽")]
    Epic,
    [InspectorName("유니크")]
    Unique,
    [InspectorName("특수")]
    Special,
}

public abstract class CItem
{
    protected CItemSO _originItem;
    protected int _id;
    protected string _name;
    protected EItemRarity _rarity;
    protected Sprite _itemIcon;
    protected string _description;
    protected int _quantity;
    protected int _cost;

    public CItemSO OriginItem => _originItem;
    public int Id => _id;
    public string Name => _name;
    public EItemRarity Rarity => _rarity;
    public Sprite ItemIcon => _itemIcon;
    public string Description => _description;
    public int Cost => _cost;
    
    public abstract int Quantity { get; set;}

    public abstract bool AvailableInLobby();
    public abstract bool AvailableInBattle();

    public abstract bool Setup(CItemSO originItem);

    /// <summary>
    /// 버릴 때 한번 실행해야 합니다.
    /// 버린 후, 남은 아이템이 없다면 true를 반환합니다.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="quantity"></param>
    /// <returns></returns>
    public virtual bool OnDiscard(CPlayer player, int quantity) { return true; }

    public static CItem MakeItem(int id)
    {
        if (!CItemSO.IsBuilded) CItemSO.Build();
        CItemSO targetSO = CItemSO.Find(id);
        if (targetSO is CItemConsumableSO consumableSO)
        {
            CItemConsumable targetConsumable = new CItemConsumable(consumableSO);
            return targetConsumable;
        }
        else if (targetSO is CItemEquipmentSO equipmentSO)
        {
            CItemEquipment targetEquipment = new CItemEquipment(equipmentSO);
            return targetEquipment;
        }
        else
        {
            CLogger.Error("CItem.MakeItem에서 오류남");
            return null;
        }
    }

    public static CItem GetRandomEquipment(int stage, EItemRarity rarity)
    {
        if (!CItemSO.IsBuilded) CItemSO.Build();
        if (CItemSO.MakeRandomEquipment(stage, rarity) is CItemEquipmentSO equipmentSO) return new CItemEquipment(equipmentSO);
        else
        {
            CLogger.Error($"CItem.GetRandomEquipment에서 오류남 : stage {stage} rarity {rarity}");
            return null;
        }
    }

    public static CItem GetRandomEquipment(int stage)
    {
        if (!CItemSO.IsBuilded) CItemSO.Build();
        if (CItemSO.MakeRandomEquipment(stage) is CItemEquipmentSO equipmentSO) return new CItemEquipment(equipmentSO);
        else
        {
            CLogger.Error($"CItem.GetRandomEquipment에서 오류남 : stage {stage}");
            return null;
        }
    }

    public static CItem GetRandomEquipment(EItemRarity rarity)
    {
        if (!CItemSO.IsBuilded) CItemSO.Build();
        if (CItemSO.MakeRandomEquipment(rarity) is CItemEquipmentSO equipmentSO) return new CItemEquipment(equipmentSO);
        else
        {
            CLogger.Error($"CItem.GetRandomEquipment에서 오류남 : rarity {rarity}");
            return null;
        }
    }

    public static CItem GetRandomEquipment()
    {
        if (!CItemSO.IsBuilded) CItemSO.Build();
        if (CItemSO.MakeRandomEquipment() is CItemEquipmentSO equipmentSO) return new CItemEquipment(equipmentSO);
        else
        {
            CLogger.Error($"CItem.GetRandomEquipment에서 오류남");
            return null;
        }
    }

    public static CItem GetRandomConsumable()
    {
        if (!CItemSO.IsBuilded) CItemSO.Build();
        if (CItemSO.MakeRandomEquipment() is CItemConsumableSO consumableSO) return new CItemConsumable(consumableSO);
        else
        {
            CLogger.Error($"CItem.GetRandomConsumable에서 오류남");
            return null;
        }
    }
}