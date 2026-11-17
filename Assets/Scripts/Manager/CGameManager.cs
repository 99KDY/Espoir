using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public enum EGameState
{
    Idle = 0,
    Story = 1,
    UI = 2,
    Setting = 3,
    ForcePause = 9,
}

public enum ESettingState
{
    None = 0,
    BaseMenu = 1,
    //Save = 1,
    //Load = 2,
    //Sound = 3,
    Confirm = 9
}

public enum EBattleState
{
    None = 0,
    NormalBattle = 1,
    BossBattle = 2,
}

[Flags]
public enum EQuestList
{
    None = 0,
    [InspectorName("질투의 부활")] Invidia = 1 << 0,
    [InspectorName("탐욕의 부탁")] Avaritia = 1 << 1,
    [InspectorName("나태의 몰락")] Pigritia = 1 << 2,
    [InspectorName("폭식의 부탁")] Gula = 1 << 3,
    [InspectorName("색욕의 몰락")] Luxuria = 1 << 4,
    [InspectorName("분노의 부탁")] Ira = 1 << 5,
    [InspectorName("오만의 몰락")] Superbia = 1 << 6,
    [InspectorName("악의 그릇")] Pradona = 1 << 7,
}

[Flags]
public enum EBossClear
{
    None = 0,
    Invidia     = 1 << 0,
    Avaritia    = 1 << 1,
    Pigritia    = 1 << 2,
    Gula        = 1 << 3,
    Luxuria     = 1 << 4,
    Ira         = 1 << 5,
    Superbia    = 1 << 6,
    Pradona     = 1 << 7,
}

[Flags]
public enum EPradonaPower
{
    None        = 0,
    Invidia     = 1 << 0,
    Avaritia    = 1 << 1,
    Pigritia    = 1 << 2,
    Gula        = 1 << 3,
    Luxuria     = 1 << 4,
    Ira         = 1 << 5,
    Superbia    = 1 << 6,
}

public class CGameManager : MonoBehaviour
{
    [SerializeField] private AudioMixer _audioMixer;
    [Tooltip("반드시 작성!!!")]
    [SerializeField] private int _currentStage;

    [SerializeField] Button _gameShutdown;
    [SerializeField] Button _screenChange;
    [SerializeField] TMP_Text _screenChangeText;
    [SerializeField] Button _gameGotoTitle;
    [SerializeField] Button _gameSave;
    [SerializeField] Toggle _gameAutosave;

    [SerializeField] GameObject _forcePauseSet;
    [SerializeField] CanvasGroup _menuBackground;
    [SerializeField] CanvasGroup _menuBase;

    [SerializeField] List<Button> _quilitySetting;
    [SerializeField] Slider _bgmSlider;
    [SerializeField] Slider _seSlider;
    [SerializeField] TMP_InputField _bgmInput;
    [SerializeField] TMP_InputField _seInput;

    [SerializeField] CanvasGroup _menuConfirm;
    [SerializeField] TMP_Text _confirmTitle;
    [SerializeField] TMP_Text _confirmDescription;
    [SerializeField] Button _confirmYes;
    [SerializeField] Button _confirmNo;

    [Header("디버그용")]
    [SerializeField] TMP_Text _zoneText;

    private const string BgmVolumeKey = "Espoir.Settings.BgmVolume";
    private const string SeVolumeKey = "Espoir.Settings.SeVolume";

    private EGameState _gameState;
    private EBattleState _battleState;
    private ESettingState _settingState;
    private ESettingState _lastSettingState;
    private bool _settingQuestion;
    private EBossClear _bossClearState;
    private EQuestList _availableQuest;
    private EQuestList _actingQuest;
    private EQuestList _completedQuest;
    private bool _isInLobby = false;
    private bool _isPaused = false;
    private float _timeScaleBeforePause = 1f;
    private bool _isFullscreen = false;
    private int _tempWidth;
    private int _tempHeight;

    private CSoundManager _soundManager;
    private CPlayer _player;
    private Action _confirmAction;

    private int _currentActiveSpawner = 0;

    public EGameState GameState => _gameState;
    public EBattleState BattleState => _battleState;
    public EBossClear BossClearState => _bossClearState;
    public EQuestList AvailableQuest => _availableQuest;
    public EQuestList ActingQuest => _actingQuest;
    public EQuestList CompletedQuest => _completedQuest;
    public bool IsInLobby => _isInLobby;
    public bool IsPaused => _isPaused;

    private static CGameManager _instance;
    public static CGameManager Instance => _instance;
    public float BgmLevel => _bgmSlider.value;
    public float SeLevel => _seSlider.value;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
    }

    void Start()
    {
        _gameShutdown.onClick.AddListener(() =>
            ShowQuestion(
                "게임 종료",
                "게임을 종료합니까?",
                () =>
                {
                    _soundManager.PlaySE(EEffectSound.Success);
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #else
                    Application.Quit();
                    #endif
                }));
        _gameGotoTitle.onClick.AddListener(() =>
            ShowQuestion(
                "타이틀로",
                "타이틀로 돌아갑니까?",
                () =>
                {
                    _soundManager.PlaySE(EEffectSound.Success);
                    CSceneFlowManager.Instance.LoadScene(ESceneId.Title);
                }));
        _gameSave.onClick.AddListener(() =>
            ShowQuestion(
                "저장",
                "게임을 저장합니까?",
                () =>
                {
                    _soundManager.PlaySE(EEffectSound.Success);
                    SaveData();
                }));
        _confirmYes.onClick.AddListener(() =>
            {
                _confirmAction();
                HideQuestion();
            });
        _confirmNo.onClick.AddListener(() => 
            {
                _soundManager.PlaySE(EEffectSound.Beep);
                HideQuestion();
            });

        _screenChange.onClick.AddListener(() => OnGameResolutionChange());
        _player = FindFirstObjectByType<CPlayer>();
        _soundManager = FindFirstObjectByType<CSoundManager>();
        _soundManager.PlayBGM(EBackgroundSound.BaseBGM);
        SetGameState(EGameState.Idle);
        SetBattleState(EBattleState.None);
        _settingState = ESettingState.None;
        _tempWidth = Screen.width;
        _tempHeight = Screen.height;

        if (CMapInfo.Current == null)
        {
            CLogger.Error("현재 맵 정보를 찾을 수 없어 저장하지 못했습니다.");
            return;
        }

        _currentStage = CMapInfo.Current.SceneId switch
        {
            ESceneId.PradonaCamp => 0,
            ESceneId.Desert => 1,
            ESceneId.Garden => 2,
            ESceneId.Tower => 3,
            _ => -1
        };

        if (_currentStage < 0)
        {
            CLogger.Error($"맵 정보가 잘못되어있습니다.");
            return;
        }

        for (int i = 0; i < _quilitySetting.Count; i++)
        {
            int target = i;
            _quilitySetting[i].onClick.AddListener(() => OnQualityChanged(target));
        }
        OnQualityChanged(_quilitySetting.Count - 1);
        _bgmSlider.onValueChanged.AddListener((float _) =>
        {
            SyncSoundBySlider(_bgmInput, _bgmSlider);
            SetVolume();
            StoreVolumeSettings();
        });
        _seSlider.onValueChanged.AddListener((float _) =>
        {
            SyncSoundBySlider(_seInput, _seSlider);
            SetVolume();
            StoreVolumeSettings();
        });
        _bgmInput.onValueChanged.AddListener((string _) =>
        {
            SyncSoundByText(_bgmInput, _bgmSlider);
            SetVolume();
            StoreVolumeSettings();
        });
        _seInput.onValueChanged.AddListener((string _) =>
        {
            SyncSoundByText(_seInput, _seSlider);
            SetVolume();
            StoreVolumeSettings();
        });

        _menuBackground.alpha = 0;
        _menuBackground.interactable = false;
        _menuBackground.blocksRaycasts = false;
        _menuBase.alpha = 0;
        _menuBase.interactable = false;
        _menuBase.blocksRaycasts = false;

        LoadVolumeSettings();
        CSoundManager.Instance.PlayBGM(EBackgroundSound.OasisOfGold);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (_gameState == EGameState.Idle)
            {
                SetGameState(EGameState.ForcePause);
            }
            else if (_gameState == EGameState.ForcePause)
            {
                SetGameState(EGameState.Idle);
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleBaseMenu();
        }

        if (_zoneText != null)
        {
            _zoneText.text = $"Game: {_gameState}\nBattle: {_battleState}\nLobby: {_isInLobby}";
        }
    }

    private void LoadVolumeSettings()
    {
        float bgm = PlayerPrefs.GetFloat(BgmVolumeKey, _bgmSlider.value);
        float se = PlayerPrefs.GetFloat(SeVolumeKey, _seSlider.value);

        _bgmSlider.SetValueWithoutNotify(Mathf.Clamp01(bgm));
        _seSlider.SetValueWithoutNotify(Mathf.Clamp01(se));

        SyncSoundBySlider(_bgmInput, _bgmSlider);
        SyncSoundBySlider(_seInput, _seSlider);
    }

    private void StoreVolumeSettings()
    {
        PlayerPrefs.SetFloat(BgmVolumeKey, _bgmSlider.value);
        PlayerPrefs.SetFloat(SeVolumeKey, _seSlider.value);
    }

    private void SetPaused(bool paused)
    {
        if (_isPaused == paused) return;
        _isPaused = paused;
        if (paused)
        {
            _timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = _timeScaleBeforePause;
        }
    }

    private void OnGameResolutionChange()
    {
        _isFullscreen = !_isFullscreen;
        if (_isFullscreen)
        {
            Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
            _screenChangeText.text = "창모드";
        }
        else
        {
            Screen.SetResolution(_tempWidth, _tempHeight, FullScreenMode.Windowed);
            _screenChangeText.text = "전체화면";
        }
    }

    public void SetGameState(EGameState gameState)
    {
        switch (_gameState)
        {
            case EGameState.Idle:
                break;
            case EGameState.Story:
                _soundManager.PlayBGM(EBackgroundSound.BaseBGM);
                break;
            case EGameState.UI:
                SetPaused(false);
                break;
            case EGameState.Setting:
                _menuBackground.alpha = 0;
                _menuBackground.interactable = false;
                _menuBackground.blocksRaycasts = false;
                SetPaused(false);
                PlayerPrefs.Save();
                break;
            case EGameState.ForcePause:
                _forcePauseSet.SetActive(false);
                SetPaused(false);
                break;
        }
        _gameState = gameState;
        switch (_gameState)
        {
            case EGameState.Idle:
                break;
            case EGameState.Story:
                _soundManager.PlayBGM(EBackgroundSound.DialogueBGM);
                break;
            case EGameState.UI:
                SetPaused(true);
                break;
            case EGameState.Setting:
                _menuBackground.alpha = 1;
                _menuBackground.interactable = true;
                _menuBackground.blocksRaycasts = true;
                SetPaused(true);
                break;
            case EGameState.ForcePause:
                _forcePauseSet.SetActive(true);
                SetPaused(true);
                break;
        }
    }

    public void SetBattleState(EBattleState battleState)
    {
        switch (_battleState)
        {
            case EBattleState.None:
                _player.RemoveStatus(27);
                break;
            case EBattleState.NormalBattle:
                _player.InvokeOnBattleEnd(false);
                break;
            case EBattleState.BossBattle:
                _player.InvokeOnBattleEnd(true);
                break;
        }
        _battleState = battleState;
        switch (_battleState)
        {
            case EBattleState.None:
                _player.AddStatus(27);
                _soundManager.PlayBGM(EBackgroundSound.BaseBGM);
                break;
            case EBattleState.NormalBattle:
                _player.InvokeOnBattleStart(false);
                _soundManager.PlayBGM(EBackgroundSound.BattleBGM);
                break;
            case EBattleState.BossBattle:
                _player.InvokeOnBattleStart(true);
                _soundManager.PlayBGM(EBackgroundSound.BossBGM);
                break;
        }
    }

    /// <summary>
    /// ESC 와 같은 규칙으로 기본 메뉴를 열거나 닫습니다. HUD 메뉴 버튼도 이 메서드를 씁니다.
    /// </summary>
    public void ToggleBaseMenu()
    {
        if (_gameState == EGameState.Idle)
        {
            SetGameState(EGameState.Setting);
            SetMenuState(ESettingState.BaseMenu);
        }
        else if (_gameState == EGameState.Setting)
        {
            SetGameState(EGameState.Idle);
            SetMenuState(ESettingState.None);
        }
    }

    public void SetMenuState(ESettingState state)
    {
        switch (_settingState)
        {
            case ESettingState.None:
                break;
            case ESettingState.BaseMenu:
                _menuBase.alpha = 0;
                _menuBase.interactable = false;
                _menuBase.blocksRaycasts = false;
                break;
            case ESettingState.Confirm:
                _menuConfirm.alpha = 0;
                _menuConfirm.interactable = false;
                _menuConfirm.blocksRaycasts = false;
                break;
        }
        _settingState = state;
        switch (_settingState)
        {
            case ESettingState.None:
                break;
            case ESettingState.BaseMenu:
                _menuBase.alpha = 1;
                _menuBase.interactable = true;
                _menuBase.blocksRaycasts = true;
                break;
            case ESettingState.Confirm:
                _menuConfirm.alpha = 1;
                _menuConfirm.interactable = true;
                _menuConfirm.blocksRaycasts = true;
                break;
        }
    }

    public void OnQualityChanged(int index)
    {
        switch (index)
        {
            case 0:
                QualitySettings.SetQualityLevel(0, true);
                break;
            case 1:
                QualitySettings.SetQualityLevel(1, true);
                break;
            case 2:
                QualitySettings.SetQualityLevel(2, true);
                break;
            case 3:
                QualitySettings.SetQualityLevel(3, true);
                break;
            case 4:
                QualitySettings.SetQualityLevel(5, true);
                break;
            default:
                break;
        }
        
        RefreshQualityButtons(index);
    }

    public void SetLobbyIn(bool target)
    {
        if (target != _isInLobby)
        {
            _isInLobby = target;
            if (target)
            {
                _player.AddStatus(26, 0, 1);
            }
            else
            {
                _player.RemoveStatus(26, true);
            }
        }
    }

    private void RefreshQualityButtons(int index)
    {
        for (int i = 0; i < _quilitySetting.Count; i++)
        {
            _quilitySetting[i].interactable = !(i == index);
        }
    }

    private void SetVolume()
    {
        // SoundManager에서 처리함
    }

    private void SyncSoundByText(TMP_InputField input, Slider slider)
    {
        if (!int.TryParse(input.text, out int volume))
        {
            return;
        }

        volume = Mathf.Clamp(volume, 0, 100);
        slider.SetValueWithoutNotify(volume / 100f);
        input.SetTextWithoutNotify(volume.ToString());
    }
    private void SyncSoundBySlider(TMP_InputField input, Slider slider)
    {
        input.SetTextWithoutNotify(Mathf.RoundToInt(slider.value * 100f).ToString());
    }

    public void InvokeSpawnerActivation(bool isActive)
    {
        if (isActive)
        {
            if (_currentActiveSpawner == 0 && _battleState == EBattleState.None) SetBattleState(EBattleState.NormalBattle);
            _currentActiveSpawner += 1;
        }
        else
        {
            _currentActiveSpawner -= 1;
            if (_currentActiveSpawner == 0 && _battleState == EBattleState.NormalBattle) SetBattleState(EBattleState.None);
        }
    }

    public void InvokeBossBattleActivation(bool isActive)
    {
        if (isActive)
        {
            SetBattleState(EBattleState.BossBattle);
        }
        else
        {
            if (_currentActiveSpawner != 0) SetBattleState(EBattleState.NormalBattle);
            else SetBattleState(EBattleState.None);
        }
    }

    public void InvokeAutosave()
    {
        if (_gameAutosave.isOn)
        {
            SaveData();
        }
    }
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public bool InitializeSaveslot(int slot, bool useSaveData)
    {
        if (_player == null ||
            _player.Inventory == null ||
            slot < 0 || slot >= CSaveManager.MaxSaveSlot) return false;

        CSaveManager.RefreshData(slot);
        if (useSaveData)
        {
            if (!CSaveManager.GetData(slot, out CSaveData data)) return false;
            LoadData(data);
            return true;
        }
        
        // 새 시작인데 데이터가 이미 있을경우
        if (CSaveManager.Available(slot)) return false;

        return SaveData(slot);
    }

    public bool SaveData()
    {
        return SaveData(CSceneFlowManager.Instance.TargetSaveData);
    }

    private bool SaveData(int slot)
    {
        if (_player == null || _player.Inventory == null) return false;
        
        List<CItem> items = new List<CItem>(_player.Inventory.MaxItemSlot);
        int[] quickSlots = new int[_player.Inventory.QuickSlotLength];

        _player.Inventory.ExportState(items, quickSlots, new Dictionary<int, float>(), out int gold);

        CSaveData data = new CSaveData
            (
                CSaveManager.Version,
                _currentStage,
                gold,
                items,
                _player.Equipment,
                quickSlots,
                _availableQuest,
                _actingQuest,
                _completedQuest,
                _bossClearState,
                _player.PradonaPower);

        return CSaveManager.SaveData(slot, data);
    }

    public void LoadData()
    {
        LoadData(CSceneFlowManager.Instance.TargetSaveData);
    }

    private void LoadData(int slot)
    {
        CSaveManager.GetData(slot, out CSaveData data);
        LoadData(data);
    }

    private void LoadData(CSaveData data)
    {
        _player.Inventory.ImportState(data.Inventory, data.Quickslot.ToArray(), new Dictionary<int, float>(), data.Money);
        _availableQuest = data.AvailableQuest;
        _actingQuest = data.ActingQuest;
        _completedQuest = data.CompletedQuest;
        _bossClearState = data.BossClear;
        _player.PradonaPower = data.PradonaPower;
        foreach (KeyValuePair<EEquipmentSlot, CItemEquipment> pair in data.Equipment)
        {
            if (_player.Equipment.TryGetValue(pair.Key, out CItemEquipment previous) &&
                previous != null)
            {
                previous.OnUnequip(_player);
            }

            _player.Equipment[pair.Key] = null;
            pair.Value?.OnEquip(_player);
        }
        _player.EquipmentResync();
    }

    private void ShowQuestion(string shownTitle, string shownText, Action action)
    {
        _soundManager?.PlaySE(EEffectSound.QuestionAppear);
        _confirmTitle.text = shownTitle;
        _confirmDescription.text = shownText;
        _confirmAction = action;
        _lastSettingState = _settingState;
        SetMenuState(ESettingState.Confirm);
    }

    private void HideQuestion()
    {
        _confirmTitle.text = "";
        _confirmDescription.text = "";
        _confirmAction = null;
        SetMenuState(_lastSettingState);
        _lastSettingState = ESettingState.None;
    }

    public int GetAvailableStage()
    {
        int count = 0;
        if (_bossClearState.HasFlag(EBossClear.Invidia)) count += 1;
        if (_bossClearState.HasFlag(EBossClear.Pigritia)) count += 1;
        if (_bossClearState.HasFlag(EBossClear.Luxuria)) count += 1;
        return count;
    }

    public int GetCompleteNumber()
    {
        int count = 0;
        if (_bossClearState.HasFlag(EBossClear.Invidia)) count += 1;
        if (_bossClearState.HasFlag(EBossClear.Avaritia)) count += 1;
        if (_bossClearState.HasFlag(EBossClear.Pigritia)) count += 1;
        if (_bossClearState.HasFlag(EBossClear.Gula)) count += 1;
        if (_bossClearState.HasFlag(EBossClear.Luxuria)) count += 1;
        if (_bossClearState.HasFlag(EBossClear.Ira)) count += 1;
        if (_bossClearState.HasFlag(EBossClear.Superbia)) count += 1;
        if (_bossClearState.HasFlag(EBossClear.Pradona)) count += 1;
        return count;
    }

    public static int GetCompleteNumber(EBossClear target)
    {
        int count = 0;
        if (target.HasFlag(EBossClear.Invidia)) count += 1;
        if (target.HasFlag(EBossClear.Avaritia)) count += 1;
        if (target.HasFlag(EBossClear.Pigritia)) count += 1;
        if (target.HasFlag(EBossClear.Gula)) count += 1;
        if (target.HasFlag(EBossClear.Luxuria)) count += 1;
        if (target.HasFlag(EBossClear.Ira)) count += 1;
        if (target.HasFlag(EBossClear.Superbia)) count += 1;
        if (target.HasFlag(EBossClear.Pradona)) count += 1;
        return count;
    }

    public int GetMaxCompleteNumber()
    {
        return System.Enum.GetValues(typeof(EBossClear)).Length - 1;
    }

    public static int GetMaxCompleteNumberStatic()
    {
        return System.Enum.GetValues(typeof(EBossClear)).Length - 1;
    }

    public void SetBossCleared(EBossClear target)
    {
        _bossClearState |= target;
    }
}
