using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SDropTable
{
    [Tooltip("스테이지 정보 (소모품은 무시)")]
    public int stage;
    [Tooltip("장비유무 (체크시 장비, 해제시 소모품)")]
    public bool isEquipment;
    [Tooltip("레어도")]
    public EItemRarity rarity;
    [Tooltip("확률")]
    public float chance;

    public SDropTable(int stage, bool isEquipment, EItemRarity rarity, float chance)
    {
        this.stage = stage;
        this.isEquipment = isEquipment;
        this.rarity = rarity;
        this.chance = chance;
    }
}
public class CItemSO : ScriptableObject
{
    [Tooltip("아이템 id")]
    [SerializeField] protected int _id;
    [Tooltip("아이템 이름")]
    [SerializeField] protected string _name;
    [Tooltip("아이템 레어도")]
    [SerializeField] protected EItemRarity _rarity;
    [Tooltip("아이템 아이콘")]
    [SerializeField] protected Sprite _itemIcon;
    [Tooltip("아이템 설명")]
    [SerializeField][TextArea(1, 5)] protected string _description;
    [Tooltip("아이템 가격")]
    [SerializeField] protected int _cost;

    private static Dictionary<int, CItemSO> _idToItem = new Dictionary<int, CItemSO>();
    private static Dictionary<string, CItemSO> _stringToItem = new Dictionary<string, CItemSO>();
    private static bool _isBuilded = false;

    //Debug
    private static List<int> _equipmentPools = new List<int>();
    private static List<int> _consumablePools = new List<int>();
    private static Dictionary<int, List<int>> _stageEquipmentPools = new Dictionary<int, List<int>>();
    private static Dictionary<EItemRarity, List<int>> _rarityEquipmentPools = new Dictionary<EItemRarity, List<int>>();
    private static Dictionary<(int stage, EItemRarity rarity), List<int>> _stageRarityEquipmentPools = new Dictionary<(int stage, EItemRarity rarity), List<int>>();
    private static Dictionary<EItemRarity, List<int>> _rarityConsumablePools = new Dictionary<EItemRarity, List<int>>();

    public static bool IsBuilded => _isBuilded;

    public int Id => _id;
    public string Name => _name;
    public Sprite ItemIcon => _itemIcon;
    public string Description => _description;
    public int Cost => _cost;
    public EItemRarity Rarity => _rarity;

    public static void Build(bool force = false)
    {
        if (force || !_isBuilded) BuildInternal();
    }

    private static void BuildInternal()
    {
        _idToItem.Clear();
        _stringToItem.Clear();
        _equipmentPools.Clear();
        _consumablePools.Clear();
        _stageEquipmentPools.Clear();
        _rarityEquipmentPools.Clear();
        _stageRarityEquipmentPools.Clear();
        _rarityConsumablePools.Clear();
        CItemSO[] targetItem = Resources.LoadAll<CItemSO>("ItemSO");
        if (targetItem == null || targetItem.Length == 0)
        {
            CLogger.Warn($"CItemSO : 소모품이 없음");
        }
        else
        {
            for (int i = 0; i < targetItem.Length; i++)
            {
                if (targetItem[i] == null)
                {
                    CLogger.Error($"CStatusSO : 딕셔너리 생성 중 에러 발생 - {i}");
                    continue;
                }
                _idToItem.Add(targetItem[i]._id, targetItem[i]);
                _stringToItem.Add(targetItem[i]._name, targetItem[i]);

                #region 테스트 빌드가 끝나면 지울것!
                if (targetItem[i] is CItemEquipmentSO)
                {
                    _equipmentPools.Add(targetItem[i]._id);
                    if (!_stageEquipmentPools.ContainsKey(targetItem[i]._id / 100))
                    {
                        _stageEquipmentPools.Add(targetItem[i]._id / 100, new List<int>());
                    }
                    _stageEquipmentPools[targetItem[i]._id / 100].Add(targetItem[i]._id);
                    if (!_rarityEquipmentPools.ContainsKey(targetItem[i]._rarity))
                    {
                        _rarityEquipmentPools.Add(targetItem[i]._rarity, new List<int>());
                    }
                    _rarityEquipmentPools[targetItem[i]._rarity].Add(targetItem[i]._id);
                    if (!_stageRarityEquipmentPools.ContainsKey((targetItem[i]._id / 100, targetItem[i]._rarity)))
                    {
                        _stageRarityEquipmentPools.Add((targetItem[i]._id / 100, targetItem[i]._rarity), new List<int>());
                    }
                    _stageRarityEquipmentPools[(targetItem[i]._id / 100, targetItem[i]._rarity)].Add(targetItem[i]._id);
                    
                }
                else if (targetItem[i] is CItemConsumableSO targetConsumable)
                {
                    if (!targetConsumable.IsThrow)
                    {
                        _consumablePools.Add(targetItem[i]._id);
                        if (!_rarityConsumablePools.ContainsKey(targetItem[i]._rarity))
                        {
                            _rarityConsumablePools.Add(targetItem[i]._rarity, new List<int>());
                        }
                        _rarityConsumablePools[targetItem[i]._rarity].Add(targetItem[i]._id);
                    }
                }
                #endregion
            }
        }

        CLogger.Success($"CItemSO : 빌드 완료");
        CLogger.Success($"CItemSO : 아이템 풀 : {_idToItem.Count}개");
        CLogger.Success($"CItemSO : 장비 풀 : {_equipmentPools.Count}개");
        CLogger.Success($"CItemSO : 소모품 풀 : {_consumablePools.Count}개");
        _isBuilded = true;
        return;
    }

    public static CItemSO Find(int id)
    {
        if (!_isBuilded) Build();
        return _idToItem[id];
    }

    public static CItemSO Find(string name)
    {
        if (!_isBuilded) Build();
        return _stringToItem[name];
    }

    // 저장데이터 검토용
    public static bool TryFind(int id, out CItemSO item)
    {
        if (!_isBuilded) Build();
        return _idToItem.TryGetValue(id, out item) && item != null;
    }

    public static CItemSO MakeRandomConsumable()
    {
        if (!_isBuilded) Build();
        if (_consumablePools.Count == 0)
        {
            CLogger.Warn($"CItemSO : 소모품이 존재하지 않습니다.");
            return null;
        }
        return Find(_consumablePools[Random.Range(0, _consumablePools.Count)]);
    }
    public static CItemSO MakeRandomConsumable(EItemRarity rarity)
    {
        if (!_isBuilded) Build();
        if (!_rarityConsumablePools.ContainsKey((rarity)))
        {
            CLogger.Warn($"CItemSO : 해당 희귀도({rarity})의 소모품이 존재하지 않습니다.");
            return null;
        }
        return Find(_rarityConsumablePools[rarity][Random.Range(0, _rarityConsumablePools[rarity].Count)]);
    }

    public static CItemSO MakeRandomEquipment(int stage, EItemRarity rarity)
    {
        if (!_isBuilded) Build();
        if (stage < 0)
        {
            return MakeRandomEquipment(rarity);
        }
        if (!_stageRarityEquipmentPools.ContainsKey((stage, rarity)))
        {
            CLogger.Warn($"CItemSO : 해당 스테이지({stage})와 희귀도({rarity})의 아이템이 존재하지 않습니다.");
            return null;
        }
        return Find(_stageRarityEquipmentPools[(stage, rarity)][Random.Range(0, _stageRarityEquipmentPools[(stage, rarity)].Count)]);
    }

    public static CItemSO MakeRandomEquipment(int stage)
    {
        if (!_isBuilded) Build();
        if (stage < 0)
        {
            return Find(_equipmentPools[Random.Range(0, _equipmentPools.Count)]);
        }
        if (!_stageEquipmentPools.ContainsKey(stage))
        {
            CLogger.Warn($"CItemSO : 해당 스테이지({stage})의 아이템이 존재하지 않습니다.");
            return null;
        }
        return Find(_stageEquipmentPools[stage][Random.Range(0, _stageEquipmentPools[stage].Count)]);
    }

    public static CItemSO MakeRandomEquipment(EItemRarity rarity)
    {
        if (!_isBuilded) Build();
        if (!_rarityEquipmentPools.ContainsKey(rarity))
        {
            CLogger.Warn($"CItemSO : 해당 희귀도({rarity})의 아이템이 존재하지 않습니다.");
            return null;
        }
        return Find(_rarityEquipmentPools[rarity][Random.Range(0, _rarityEquipmentPools[rarity].Count)]);
    }

    public static CItemSO MakeRandomEquipment()
    {
        if (!_isBuilded) Build();
        if (_equipmentPools.Count == 0)
        {
            CLogger.Warn($"CItemSO : 장비가 존재하지 않습니다.");
            return null;
        }
        return Find(_equipmentPools[Random.Range(0, _equipmentPools.Count)]);
    }
    public static bool TryMakeDropItem(List<SDropTable> dropTable, out CItemSO targetItem)
    {
        targetItem = null;
        if (!_isBuilded) Build();
        if (dropTable == null || dropTable.Count == 0) return false;

        float targetChance = UnityEngine.Random.Range(0f, 1f);
        foreach(SDropTable target in dropTable)
        {
            if (targetChance > target.chance)
            {
                targetChance -= target.chance;
                continue;
            }
            if (target.isEquipment)
            {
                targetItem = MakeRandomEquipment(target.stage, target.rarity);
                if (targetItem == null) return false;
                else return true;
            }
            else
            {
                targetItem = MakeRandomConsumable(target.rarity);
                if (targetItem == null) return false;
                else return true;
            }
        }
        
        return false;
    }
}