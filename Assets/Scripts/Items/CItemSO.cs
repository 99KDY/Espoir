using System.Collections.Generic;
using UnityEngine;


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
    private static Dictionary<int, List<int>> _stagePools = new Dictionary<int, List<int>>();
    private static Dictionary<EItemRarity, List<int>> _rarityPools = new Dictionary<EItemRarity, List<int>>();
    private static Dictionary<(int stage, EItemRarity rarity), List<int>> _stageRarityPools = new Dictionary<(int stage, EItemRarity rarity), List<int>>();

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
        _isBuilded = true;
        _idToItem.Clear();
        _stringToItem.Clear();
        _equipmentPools.Clear();
        _consumablePools.Clear();
        _stagePools.Clear();
        _rarityPools.Clear();
        _stageRarityPools.Clear();
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
                    if (!_stagePools.ContainsKey(targetItem[i]._id / 100))
                    {
                        _stagePools.Add(targetItem[i]._id / 100, new List<int>());
                    }
                    _stagePools[targetItem[i]._id / 100].Add(targetItem[i]._id);
                    if (!_rarityPools.ContainsKey(targetItem[i]._rarity))
                    {
                        _rarityPools.Add(targetItem[i]._rarity, new List<int>());
                    }
                    _rarityPools[targetItem[i]._rarity].Add(targetItem[i]._id);
                    if (!_stageRarityPools.ContainsKey((targetItem[i]._id / 100, targetItem[i]._rarity)))
                    {
                        _stageRarityPools.Add((targetItem[i]._id / 100, targetItem[i]._rarity), new List<int>());
                    }
                    _stageRarityPools[(targetItem[i]._id / 100, targetItem[i]._rarity)].Add(targetItem[i]._id);
                    
                }
                else if (targetItem[i] is CItemConsumableSO targetConsumable)
                {
                    if (!targetConsumable.IsThrow) _consumablePools.Add(targetItem[i]._id);
                }
                #endregion
            }
        }

        CLogger.Success($"CItemSO : 빌드 완료");
        CLogger.Success($"CItemSO : 아이템 풀 : {_idToItem.Count}개");
        CLogger.Success($"CItemSO : 장비 풀 : {_equipmentPools.Count}개");
        CLogger.Success($"CItemSO : 소모품 풀 : {_consumablePools.Count}개");

        return;
    }

    public static CItemSO Find(int id)
    {
        return _idToItem[id];
    }

    public static CItemSO Find(string name)
    {
        return _stringToItem[name];
    }

    public static CItemSO MakeRandomConsumable()
    {
        if (_consumablePools.Count == 0)
        {
            CLogger.Warn($"CItemSO : 소모품이 존재하지 않습니다.");
            return null;
        }
        return Find(_consumablePools[Random.Range(0, _consumablePools.Count)]);
    }

    public static CItemSO MakeRandomEquipment(int stage, EItemRarity rarity)
    {
        if (!_stageRarityPools.ContainsKey((stage, rarity)))
        {
            CLogger.Warn($"CItemSO : 해당 스테이지({stage})와 희귀도({rarity})의 아이템이 존재하지 않습니다.");
            return null;
        }
        return Find(_stageRarityPools[(stage, rarity)][Random.Range(0, _stageRarityPools[(stage, rarity)].Count)]);
    }

    public static CItemSO MakeRandomEquipment(int stage)
    {
        if (!_stagePools.ContainsKey(stage))
        {
            CLogger.Warn($"CItemSO : 해당 스테이지({stage})의 아이템이 존재하지 않습니다.");
            return null;
        }
        return Find(_stagePools[stage][Random.Range(0, _stagePools[stage].Count)]);
    }

    public static CItemSO MakeRandomEquipment(EItemRarity rarity)
    {
        if (!_rarityPools.ContainsKey(rarity))
        {
            CLogger.Warn($"CItemSO : 해당 희귀도({rarity})의 아이템이 존재하지 않습니다.");
            return null;
        }
        return Find(_rarityPools[rarity][Random.Range(0, _rarityPools[rarity].Count)]);
    }

    public static CItemSO MakeRandomEquipment()
    {
        if (_equipmentPools.Count == 0)
        {
            CLogger.Warn($"CItemSO : 장비가 존재하지 않습니다.");
            return null;
        }
        return Find(_equipmentPools[Random.Range(0, _equipmentPools.Count)]);
    }
}