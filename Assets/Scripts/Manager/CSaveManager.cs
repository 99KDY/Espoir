using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static UnityEngine.Rendering.PostProcessing.SubpixelMorphologicalAntialiasing;

[Flags]
public enum SaveSlot
{
    None = 0,
    Save01 = 1 << 0,
    Save02 = 1 << 1,
    Save03 = 1 << 2,
}

[Serializable]
public class CSaveData
{
    #region Member Variable
    public int version;
    public int currentStage;
    public int money;
    public List<int> itemIds;
    public List<int> itemQuantity;
    public List<int> itemEquipments;
    public List<int> itemQuickslot;
    public int availableQuest;
    public int actingQuest;
    public int completedQuest;
    public int bossClear;
    public int pradonaPower;
    #endregion

    #region Property
    public int Version
    {
        get { return version; }
        private set { version = value; }
    }

    public int CurrentStage
    {
        get { return currentStage; }
        private set { currentStage = value; }
    }

    public int Money
    {
        get { return money; }
        private set { money = value; }
    }

    public List<CItem> Inventory
    {
        get
        {
            List<CItem> list = new List<CItem>();
            for(int i = 0; i < itemIds.Count; i++)
            {
                if (itemIds[i] == 0) list.Add(null);
                else
                {
                    CItem targetItem = CItem.MakeItem(itemIds[i]);
                    targetItem.Quantity = itemQuantity[i];
                    list.Add(targetItem);
                }
            }
            return list;
        }
        private set
        {
            itemIds = new List<int>();
            itemQuantity = new List<int>();
            for(int i = 0; i < value.Count; i++)
            {
                if (value[i] == null)
                {
                    itemIds.Add(0);
                    itemQuantity.Add(0);
                }
                else
                {
                    itemIds.Add(value[i].Id);
                    itemQuantity.Add(value[i].Quantity);
                }
            }
        }
    }

    public Dictionary<EEquipmentSlot, CItemEquipment> Equipment
    {
        get
        {
            Dictionary<EEquipmentSlot, CItemEquipment> target = new Dictionary<EEquipmentSlot, CItemEquipment>();
            if (itemEquipments[0] == 0) target[EEquipmentSlot.Necklace] = null;
            else target[EEquipmentSlot.Necklace] = (CItemEquipment) CItem.MakeItem(itemEquipments[0]);
            if (itemEquipments[1] == 0) target[EEquipmentSlot.Earring] = null;
            else target[EEquipmentSlot.Earring] = (CItemEquipment)CItem.MakeItem(itemEquipments[1]);
            if (itemEquipments[2] == 0) target[EEquipmentSlot.Bracelet] = null;
            else target[EEquipmentSlot.Bracelet] = (CItemEquipment)CItem.MakeItem(itemEquipments[2]);
            if (itemEquipments[3] == 0) target[EEquipmentSlot.Ring] = null;
            else target[EEquipmentSlot.Ring] = (CItemEquipment)CItem.MakeItem(itemEquipments[3]);
            return target;
        }
        private set
        {
            List<int> target = new List<int>(0);
            if (value[EEquipmentSlot.Necklace] == null) target.Add(0);
            else target.Add(value[EEquipmentSlot.Necklace].Id);
            if (value[EEquipmentSlot.Earring] == null) target.Add(0);
            else target.Add(value[EEquipmentSlot.Earring].Id);
            if (value[EEquipmentSlot.Bracelet] == null) target.Add(0);
            else target.Add(value[EEquipmentSlot.Bracelet].Id);
            if (value[EEquipmentSlot.Ring] == null) target.Add(0);
            else target.Add(value[EEquipmentSlot.Ring].Id);
            itemEquipments = target;
        }
    }

    public List<int> Quickslot
    {
        get
        {
            return itemQuickslot;
        }
        set
        {
            itemQuickslot = value;
        }
    }

    public EQuestList AvailableQuest
    {
        get
        {
            return (EQuestList) availableQuest;
        }
        set
        {
            availableQuest = (int)value;
        }
    }
    public EQuestList ActingQuest
    {
        get
        {
            return (EQuestList) actingQuest;
        }
        set
        {
            actingQuest = (int)value;
        }
    }
    public EQuestList CompletedQuest
    {
        get
        {
            return (EQuestList) completedQuest;
        }
        set
        {
            completedQuest = (int)value;
        }
    }
    public EBossClear BossClear
    {
        get
        {
            return (EBossClear) bossClear;
        }
        set
        {
            bossClear = (int)value;
        }
    }
    public EPradonaPower PradonaPower
    {
        get
        {
            return (EPradonaPower) pradonaPower;
        }
        set
        {
            pradonaPower = (int)value;
        }
    }
    #endregion

    public CSaveData
        (
            int version,
            int currentStage,
            int money,
            List<CItem> inventory,
            Dictionary<EEquipmentSlot, CItemEquipment> equipment,
            List<int> quickslot,
            EQuestList availableQuest,
            EQuestList actingQuest,
            EQuestList completedQuest,
            EBossClear bossClear,
            EPradonaPower pradonaPower
        )
    {
        this.version = version;
        this.currentStage = currentStage;
        this.money = money;
        this.Inventory = inventory;
        this.Equipment = equipment;
        this.Quickslot = quickslot;
        this.AvailableQuest = availableQuest;
        this.ActingQuest = actingQuest;
        this.CompletedQuest = completedQuest;
        this.BossClear = bossClear;
        this.PradonaPower = pradonaPower;
    }
}

public static class CSaveManager 
{
    private static int _maxSaveSlot = System.Enum.GetValues(typeof(SaveSlot)).Length - 1;
    private static CSaveData[] _saveData = new CSaveData[_maxSaveSlot];
    private static int[] _saveDataErrorCode = new int[_maxSaveSlot];
    private static SaveSlot _saveFlag = SaveSlot.None;

#if UNITY_EDITOR
    private readonly static bool _isPersist = false;
#else
    private readonly static bool _isPersist = true;
#endif
    public readonly static int Version = 1;

    public static int MaxSaveSlot
    {
        get { return _maxSaveSlot; }
        private set { _maxSaveSlot = value; }
    }

    public static SaveSlot SaveFlag
    {
        get { return _saveFlag; }
        private set { _saveFlag = value; }
    }
    public static int[] SaveErrorCode
    {
        get { return _saveDataErrorCode; }
        private set { _saveDataErrorCode = value; }
    }

    private static string GetSavepath(int slot)
    {
        if (slot >= _maxSaveSlot || slot < 0)
        {
            CLogger.Error($"{slot} : 잘못된 슬롯 번호");
            return null;
        }
        if (_isPersist) return Path.Combine(Application.persistentDataPath, $"save{slot:D2}.json");
        else return Path.Combine(Application.dataPath, $"save{slot:D2}.json");
    }
    public static void RefreshAllData()
    {
        FindAvailableData();
        CLogger.Log($"_saveFlag = {_saveFlag}");
    }

    public static void RefreshData(int index)
    {
        if (index < 0 || index >= _maxSaveSlot)
        {
            CLogger.Error($"{index} : 잘못된 슬롯 번호");
            return;
        }

        // 파일을 다시 읽어야해서 캐시 지워두기
        _saveData[index] = null;
        _saveFlag &= ~(SaveSlot)(1 << index);

        if (LoadData(index))
        {
            CLogger.Success($"{index} : 저장데이터 불러옴");
            _saveFlag |= (SaveSlot)(1 << index);
            return;
        }

        if (_saveDataErrorCode[index] == -10 || _saveDataErrorCode[index] == -11)
        {
            CLogger.Log($"{index} : 저장데이터 새로고침 - 데이터 없음");
            _saveFlag &= ~(SaveSlot)(1 << index);
            return;
        }
        else
        {
            CLogger.Error($"{index} : 저장데이터 새로고침 - 데이터 오류있지만, UI에서 처리하도록.");
            _saveFlag |= (SaveSlot)(1 << index);
            return;
        }
    }
    public static SaveSlot FindAvailableData()
    {
        for (int i = 0; i < _maxSaveSlot; i++)
        {
            RefreshData(i);
        }
        return _saveFlag;
    }
    public static bool SaveData(int index, CSaveData data)
    {
        if (index < 0 || index >= _maxSaveSlot)
        {
            CLogger.Error($"{index} : 잘못된 슬롯 번호");
            return false;
        }

        if (CheckSaveData(data) != 0)
        {
            CLogger.Error($"{index} : 저장데이터 검증 실패");
            return false;
        }

        try
        {
            string path = GetSavepath(index);
            // 임시 파일 만들기
            string tempPath = path + ".tmp";
            string backupPath = path + ".bak";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(tempPath, JsonUtility.ToJson(data));

            if (File.Exists(path))
            {
                // 백업
                File.Replace(tempPath, path, backupPath);
            }
            else
            {
                // 백업 없음
                File.Move(tempPath, path);
            }

            _saveData[index] = data;
            _saveDataErrorCode[index] = 0;
            _saveFlag |= (SaveSlot)(1 << index);

            CLogger.Success($"{index} : 저장 성공, 데이터 저장함");
            return true;
        }
        catch (Exception ex)
        {
            CLogger.Error($"저장 실패: {ex.Message}");
            return false;
        }
    }

    public static bool LoadData(int index)
    {
        if (index < 0 || index >= _maxSaveSlot)
        {
            CLogger.Error($"{index} : 잘못된 슬롯 번호");
            return false;
        }
        _saveDataErrorCode[index] = -1;
        if (_saveData[index] == null)
        {
            CLogger.Log($"{index} : 불러오기 시도 - 데이터가 없어, 로컬에서 시도함.");
            string path = GetSavepath(index);

            try
            {
                CSaveData loadedData = JsonUtility.FromJson<CSaveData>
                    (
                        File.ReadAllText(path)
                    );

                _saveDataErrorCode[index] = CheckSaveData(loadedData);
                if (_saveDataErrorCode[index] != 0)
                {
                    _saveData[index] = null;
                    return false;
                }
                else
                {
                    _saveData[index] = loadedData;
                    return true;
                }
            }
            catch (FileNotFoundException)
            {
                _saveDataErrorCode[index] = -10;
                _saveData[index] = null;
                // 파일 없음 (보통 위에서 처리되지만 포함함)
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                _saveDataErrorCode[index] = -11;
                _saveData[index] = null;
                // 저장 폴더 에러
                return false;
            }
            catch (IOException ex)
            {
                _saveDataErrorCode[index] = -12;
                _saveData[index] = null;
                CLogger.Error($"{index} : 파일 읽기 실패 - {ex.Message}");
                // 파일 읽기 실패
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                _saveDataErrorCode[index] = -13;
                _saveData[index] = null;
                CLogger.Error($"{index} : 파일 접근 실패 - {ex.Message}");
                // 파일 접근 실패
                return false;
            }
            catch (ArgumentException ex)
            {
                _saveDataErrorCode[index] = -14;
                _saveData[index] = null;
                CLogger.Error($"{index} : 파일 형식 오류 - {ex.Message}");
                // 파일 형식 오류
                return false;
            }
        }
        else
        {
            _saveDataErrorCode[index] = 0;
            return true;
        }
    }

    public static int CheckSaveData(CSaveData loadedData)
    {
        if (loadedData == null)
        {
            // 저장 데이터 비어있음
            return 1;
        }
        if (loadedData.Version != Version)
        {
            // 저장 데이터 버전 다름
            return 2;
        }
        if (loadedData.currentStage > 4 || loadedData.currentStage < 0)
        {
            // 스테이지 데이터 오류
            return 3;
        }

        if (loadedData.itemIds == null || loadedData.itemQuantity == null ||
            loadedData.itemIds.Count != CInventory.MaxSlot ||
            loadedData.itemIds.Count != loadedData.itemQuantity.Count)
        {
            // 아이템 데이터 오류
            return 4;
        }

        for (int i = 0; i < loadedData.itemIds.Count; i++)
        {
            int id = loadedData.itemIds[i];
            int quantity = loadedData.itemQuantity[i];
            // 아이템 없는 칸
            if (id == 0)
            {
                if (quantity != 0) return 4;
                continue;
            }
            // 아이템이 있어야하는데도 갯수정보가 없거나, 너무 큼
            if (quantity <= 0 || quantity > CInventory.MaxQuantity)
            {
                return 4;
            }
            
            // 아이템id가 존재하지 않는 아이템임
            if (!CItemSO.TryFind(id, out CItemSO item))
            {
                return 4;
            }

            // 아이템이 장비인데, 갯수정보가 잘못됨
            if (item is CItemEquipmentSO)
            {
                if (quantity != 1) return 4;
            }
            // 장비가 아닌데 소모품도 아님
            else if (!(item is CItemConsumableSO))
            {
                return 4;
            }
        }

        if (loadedData.itemEquipments == null || loadedData.itemEquipments.Count != 4)
        {
            // 장비 데이터 오류
            return 5;
        }

        EEquipmentSlot[] equipmentSlots =
            {
                EEquipmentSlot.Necklace,
                EEquipmentSlot.Earring,
                EEquipmentSlot.Bracelet,
                EEquipmentSlot.Ring
            };

        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            int id = loadedData.itemEquipments[i];
            // 빈 부위면 넘어가기
            if (id == 0) continue;

            // 장비칸에 장비가 아닌 아이템이거나, 부위가 잘못됨
            if (!CItemSO.TryFind(id, out CItemSO item)
                || !(item is CItemEquipmentSO equipment)
                || equipment.Slot != equipmentSlots[i])
            {
                return 5;
            }
        }

        if (loadedData.itemQuickslot == null || loadedData.itemQuickslot.Count != 4)
        {
            // 퀵슬롯 데이터 오류
            return 6;
        }

        for (int i = 0; i < loadedData.itemQuickslot.Count; i++)
        {
            // 빈 퀵슬롯은 패스
            if (loadedData.itemQuickslot[i] == -1) continue;

            // 존재하지 않거나, 소모품이 아님
            if (!CItemSO.TryFind(loadedData.itemQuickslot[i], out CItemSO item) || !(item is CItemConsumableSO))
            {
                return 6;
            }
        }
        return 0;
    }

    public static bool DeleteData(int index)
    {
        if (index < 0 || index >= _maxSaveSlot)
        {
            CLogger.Error($"{index} : 잘못된 슬롯 번호");
            return false;
        }

        try
        {
            string path = GetSavepath(index);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            _saveData[index] = null;
            _saveDataErrorCode[index] = -10;
            _saveFlag &= ~(SaveSlot)(1 << index);
            return true;
        }
        catch (Exception ex)
        {
            CLogger.Error($"{index} : 삭제 오류 - {ex.Message}");
            return false;
        }
    }

    public static bool Available(int index)
    {
        if (index < 0 || index >= _maxSaveSlot)
        {
            CLogger.Error($"{index} : 잘못된 슬롯 번호");
            return false;
        }
        SaveSlot targetFlag = (SaveSlot)(1 << index);
        if ((_saveFlag & targetFlag) != SaveSlot.None) return true;
        else return false;
    }

    public static bool GetData(int index, out CSaveData data)
    {
        data = null;

        if (!LoadData(index))
        {
            return false;
        }

        data = _saveData[index];
        return true;
    }
}
