using System.Collections.Generic;
using UnityEngine;

public class CItemSO : ScriptableObject
{
    [SerializeField] protected int _id;
    [SerializeField] protected string _name;
    [SerializeField] protected Sprite _itemIcon;
    [SerializeField] protected string _description;

    private static Dictionary<int, CItemSO> _idToItem = new Dictionary<int, CItemSO>();
    private static Dictionary<string, CItemSO> _stringToItem = new Dictionary<string, CItemSO>();
    private static bool _isBuilded = false;

    public int Id
    {
        get { return _id; }
    }

    public string Name
    {
        get { return _name; }
    }
    
    public Sprite ItemIcon
    {
        get { return _itemIcon; }
    }

    public string Description
    {
        get { return _description; }
    }

    public static void Build(bool force)
    {
        if (force || !_isBuilded) BuildInternal();
    }

    private static void BuildInternal()
    {
        _isBuilded = true;
        _idToItem.Clear();
        _stringToItem.Clear();
        CItemConsumableSO[] consumables = Resources.LoadAll<CItemConsumableSO>("ConsumableSO");
        if (consumables == null || consumables.Length == 0)
        {
            CLogger.Warn($"CItemSO : 소모품이 없음");
        }
        else
        {
            for (int i = 0; i < consumables.Length; i++)
            {
                if (consumables[i] == null)
                {
                    CLogger.Error($"CStatusSO : 딕셔너리 생성 중 에러 발생 - {i}");
                    continue;
                }
                _idToItem.Add(consumables[i]._id, consumables[i]);
                _stringToItem.Add(consumables[i]._name, consumables[i]);
            }
        }

        CItemEquipmentSO[] equipments = Resources.LoadAll<CItemEquipmentSO>("EquipmentSO");
        if (equipments == null || equipments.Length == 0)
        {
            CLogger.Warn($"CItemSO : 장비가 없음");
        }
        else
        {
            for (int i = 0; i < equipments.Length; i++)
            {
                if (equipments[i] == null)
                {
                    CLogger.Error($"CStatusSO : 딕셔너리 생성 중 에러 발생 - {i}");
                    continue;
                }
                _idToItem.Add(equipments[i]._id, equipments[i]);
                _stringToItem.Add(equipments[i]._name, equipments[i]);
            }
        }

        return;
    }


}