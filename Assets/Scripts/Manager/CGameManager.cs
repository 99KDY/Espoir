using System.Collections;
using System.Collections.Generic;
using TMPro;
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
}

public enum EBattleState
{
    None = 0,
    NormalBattle = 1,
    BossBattle = 2,
}

public class CGameManager : MonoBehaviour
{
    [SerializeField] private AudioMixer _audioMixer;

    [SerializeField] GameObject _forcePauseSet;
    [SerializeField] CanvasGroup _menuBackground;
    [SerializeField] CanvasGroup _menuBase;

    [SerializeField] List<Button> _quilitySetting;
    [SerializeField] Slider _bgmSlider;
    [SerializeField] Slider _seSlider;
    [SerializeField] TMP_InputField _bgmInput;
    [SerializeField] TMP_InputField _seInput;


    private EGameState _gameState;
    private EBattleState _battleState;
    private ESettingState _settingState;
    private bool _isPaused;
    private float _timeScaleBeforePause;

    public EGameState GameState => _gameState;
    public EBattleState BattleState => _battleState;
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
        _gameState = EGameState.Idle;
        _battleState = EBattleState.None;
        _settingState = ESettingState.None;

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
        });
        _seSlider.onValueChanged.AddListener((float _) =>
        {
            SyncSoundBySlider(_seInput, _seSlider);
            SetVolume();
        });
        _bgmInput.onValueChanged.AddListener((string _) =>
        {
            SyncSoundByText(_bgmInput, _bgmSlider);
            SetVolume();
        });
        _seInput.onValueChanged.AddListener((string _) =>
        {
            SyncSoundByText(_seInput, _seSlider);
            SetVolume();
        });
        _bgmSlider.onValueChanged.Invoke(_bgmSlider.value);
        _seSlider.onValueChanged.Invoke(_seSlider.value);

        _menuBackground.alpha = 0;
        _menuBackground.interactable = false;
        _menuBackground.blocksRaycasts = false;
        _menuBase.alpha = 0;
        _menuBase.interactable = false;
        _menuBase.blocksRaycasts = false;
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
            if (_gameState == EGameState.Idle)
            {
                SetGameState(EGameState.UI);
                SetMenuState(ESettingState.BaseMenu);
            }
            else if (_gameState == EGameState.UI)
            {
                SetGameState(EGameState.Idle);
                SetMenuState(ESettingState.None);
            }
        }
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

    public void SetGameState(EGameState gameState)
    {
        switch (_gameState)
        {
            case EGameState.Idle:
                break;
            case EGameState.Story:
                break;
            case EGameState.UI:
                _menuBackground.alpha = 0;
                _menuBackground.interactable = false;
                _menuBackground.blocksRaycasts = false;
                SetPaused(false);
                break;
            case EGameState.Setting:
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
                break;
            case EGameState.UI:
                _menuBackground.alpha = 1;
                _menuBackground.interactable = true;
                _menuBackground.blocksRaycasts = true;
                SetPaused(true);
                break;
            case EGameState.Setting:
                break;
            case EGameState.ForcePause:
                _forcePauseSet.SetActive(true);
                SetPaused(true);
                break;
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

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
