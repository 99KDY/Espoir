using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class CTitleManager : MonoBehaviour
{
    [Serializable]
    private class SlotView
    {
        public Button openButton;
        public Button deleteButton;
        public TMP_Text description;
    }

    [Header("메인 메뉴")]
    [SerializeField] private CanvasGroup _titleMenu;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _settingButton;
    [SerializeField] private Button _quitButton;

    [Header("세이브 창")]
    [SerializeField] private CanvasGroup _saveCanvas;
    [SerializeField] private Button _saveBack;
    [SerializeField] private SlotView[] _saveSlots = new SlotView[3];

    [Header("사운드 설정")]
    [SerializeField] private GameObject _settingCanvas;
    [SerializeField] private Button _settingBack;
    [SerializeField] private AudioMixer _mixer;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _seSlider;
    [SerializeField] private TMP_Text _bgmValue;
    [SerializeField] private TMP_Text _seValue;

    [Header("확인창")]
    [SerializeField] private GameObject _confirmCanvas;
    [SerializeField] private TMP_Text _confirmMessage;
    [SerializeField] private Button _confirmYes;
    [SerializeField] private Button _confirmNo;

    private const string BgmVolumeKey = "Espoir.Settings.BgmVolume";
    private const string SeVolumeKey = "Espoir.Settings.SeVolume";

    private CSceneFlowManager _sceneManager;
    private Action _confirmAction;
    private Button _returnFocus;
    private bool _isStarting;
    private CSoundManager _soundManager;

    void Start()
    {
        CStatusSO.Build(false);
        CItemSO.Build(false);
        CSetSO.Build(false);
        _soundManager = CSoundManager.Instance;
        _sceneManager = CSceneFlowManager.Instance;
        _titleMenu.gameObject.SetActive(true);
        SetTitleMenu(true);

        _saveCanvas.gameObject.SetActive(false);
        _settingCanvas.SetActive(false);
        _confirmCanvas.SetActive(false);

        _startButton.onClick.AddListener(CallGameStart);
        _saveBack.onClick.AddListener(CallGotoTitle);
        _quitButton.onClick.AddListener(CallGameExit);
        _settingButton.onClick.AddListener(CallGameSetting);
        _settingBack.onClick.AddListener(CallGameSettingBack);

        _confirmYes.onClick.AddListener(() =>
        {
            Action action = _confirmAction;
            HideQuestion();
            action?.Invoke();
        });
        _confirmNo.onClick.AddListener(HideQuestion);

        if (_saveSlots == null || _saveSlots.Length != CSaveManager.MaxSaveSlot)
        {
            CLogger.Error("세이브 슬롯 수 안맞음");
            enabled = false;
            return;
        }

        for (int i = 0; i < _saveSlots.Length; i++)
        {

            if (_saveSlots[i] == null ||
                _saveSlots[i].openButton == null ||
                _saveSlots[i].deleteButton == null ||
                _saveSlots[i].description == null)
            {
                CLogger.Error($"{i + 1}번 세이브 슬롯 인스펙터 확인");
                enabled = false;
                return;
            }
            // 클로져를 위해서 따로 담아두기
            int slot = i;

            _saveSlots[i].openButton.onClick.AddListener(() =>
            {
                _returnFocus = _saveSlots[slot].openButton;
                CSaveManager.RefreshData(slot);
                RefreshSaveslot(slot);

                bool availableData = CSaveManager.Available(slot);

                string message = availableData
                    ? $"{slot + 1}번 슬롯에서 게임을 이어가시겠습니까?"
                    : $"{slot + 1}번 슬롯에서 새 게임을 시작하시겠습니까?";

                ShowQuestion(message, () => CallGameScene(slot, availableData));
            });

            _saveSlots[i].deleteButton.onClick.AddListener(() =>
            {
                _returnFocus = _saveSlots[slot].deleteButton;
                ShowQuestion(
                    $"{slot + 1}번 슬롯을 삭제하시겠습니까?",
                    () => CallSlotDelete(slot));
            });
        }


        _bgmSlider.SetValueWithoutNotify(Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, 1f)));
        _seSlider.SetValueWithoutNotify(Mathf.Clamp01(PlayerPrefs.GetFloat(SeVolumeKey, 1f)));
        _bgmSlider.onValueChanged.AddListener(_ => ApplyVolume());
        _seSlider.onValueChanged.AddListener(_ => ApplyVolume());
        ApplyVolume();

        _startButton.Select();
    }

    void Update()
    {
        if (_isStarting || !Input.GetKeyDown(KeyCode.Escape)) return;

        if (_confirmCanvas.activeSelf)
            HideQuestion();
        else if (_settingCanvas.activeSelf)
            CallGameSettingBack();
        else if (_saveCanvas.gameObject.activeSelf)
            CallGotoTitle();
    }

    private void RefreshSaveslot()
    {
        for (int i = 0; i < _saveSlots.Length; i++)
        {
            RefreshSaveslot(i);
        }
    }

    private void RefreshSaveslot(int slot)
    {
        if (!CSaveManager.Available(slot))
        {
            _saveSlots[slot].deleteButton.gameObject.SetActive(false);
            _saveSlots[slot].openButton.interactable = true;
            _saveSlots[slot].description.text = "빈 슬롯";
        }
        else if (CSaveManager.GetData(slot, out CSaveData data))
        {
            _saveSlots[slot].deleteButton.gameObject.SetActive(true);
            _saveSlots[slot].openButton.interactable = true;
            string region = data.CurrentStage switch
            {
                0 => "야영지",
                1 => "나태의 사막",
                2 => "색욕의 정원",
                3 => "오만의 성지",
                _ => $"스테이지 {data.CurrentStage}"
            };
            int clearedCount = CGameManager.GetCompleteNumber(data.BossClear);
            int totalCount = CGameManager.GetMaxCompleteNumberStatic();
            _saveSlots[slot].description.text =
                $"{region}\n<size=75%>진척도 {clearedCount} / {totalCount}</size>";
        }
        else
        {
            _saveSlots[slot].deleteButton.gameObject.SetActive(true);
            _saveSlots[slot].openButton.interactable = false;
            switch (CSaveManager.SaveErrorCode[slot])
            {
                case -12:
                    _saveSlots[slot].description.text = "파일 읽기 실패";
                    break;
                case -13:
                    _saveSlots[slot].description.text = "파일 접근 실패";
                    break;
                case -14:
                    _saveSlots[slot].description.text = "파일 형식 오류";
                    break;
                case 1:
                    _saveSlots[slot].description.text = "빈 파일";
                    break;
                case 2:
                    _saveSlots[slot].description.text = "버전 오류";
                    break;
                case 3:
                    _saveSlots[slot].description.text = "스테이지 오류";
                    break;
                case 4:
                    _saveSlots[slot].description.text = "아이템 데이터 오류";
                    break;
                case 5:
                    _saveSlots[slot].description.text = "장비 데이터 오류";
                    break;
                case 6:
                    _saveSlots[slot].description.text = "퀵슬롯 데이터 오류";
                    break;
                default:
                    _saveSlots[slot].description.text = $": 알 수 없는 오류 - {CSaveManager.SaveErrorCode[slot]}";
                    break;
            }
        }
    }

    private void ApplyVolume()
    {
        float bgm = _bgmSlider.value;
        float se = _seSlider.value;

        _bgmValue.text = $"{Mathf.RoundToInt(bgm * 100f)}%";
        _seValue.text = $"{Mathf.RoundToInt(se * 100f)}%";

        if (_mixer == null)
            return;

        _mixer.SetFloat("BGMLevel", 20f * Mathf.Log10(Mathf.Max(0.0001f, bgm)));
        _mixer.SetFloat("SELevel", 20f * Mathf.Log10(Mathf.Max(0.0001f, se)));
    }

    private void SetTitleMenu(bool visible)
    {
        _titleMenu.alpha = visible ? 1f : 0f;
        _titleMenu.interactable = visible;
        _titleMenu.blocksRaycasts = visible;
    }

    private void CallGameExit()
    {
        _returnFocus = _quitButton;
        ShowQuestion
            (
                "게임을 종료하시겠습니까?",
                () =>
                {
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
                }
            );
    }

    private void CallGameStart()
    {
        _soundManager?.PlaySE(EEffectSound.QuestionAppear);

        CSaveManager.RefreshAllData();
        RefreshSaveslot();

        SetTitleMenu(false);
        _saveCanvas.gameObject.SetActive(true);

        for (int i = 0; i < _saveSlots.Length; i++)
        {
            if (!_saveSlots[i].openButton.IsActive()) continue;
            if (!_saveSlots[i].openButton.IsInteractable()) continue;
            _saveSlots[i].openButton.Select();
            return;
        }
        _saveBack.Select();
    }

    private void CallGameScene(int slot, bool useSaveData)
    {
        _soundManager?.PlaySE(EEffectSound.QuestionAppear);
        if (_isStarting) return;

        CSaveManager.RefreshData(slot);
        if (CSaveManager.Available(slot) != useSaveData)
        {
            RefreshSaveslot(slot);
            ShowQuestion("슬롯의 저장 상태가 바뀌었습니다. 다시 선택해 주세요.", null);
            return;
        }

        ESceneId? destination = ESceneId.Desert;
        if (useSaveData)
        {
            if (!CSaveManager.GetData(slot, out CSaveData data))
            {
                RefreshSaveslot(slot);
                ShowQuestion("저장 데이터를 읽을 수 없습니다.", null);
                return;
            }

            destination = data.CurrentStage switch
            {
                0 => ESceneId.PradonaCamp,
                1 => ESceneId.Desert,
                2 => ESceneId.Garden,
                3 => ESceneId.Tower,
                _ => null
            };
        }

        if (!destination.HasValue || _sceneManager == null ||
            !_sceneManager.TryGetSceneName(destination.Value, out string sceneName) ||
            !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            ShowQuestion("이 슬롯의 시작 씬을 열 수 없습니다.", null);
            return;
        }

        int previousSlot = _sceneManager.TargetSaveData;
        bool previousLoad = _sceneManager.LoadSavedData;

        _sceneManager.TargetSaveData = slot;
        _sceneManager.LoadSavedData = useSaveData;
        _isStarting = true;
        _sceneManager.FirstLoadFromTitle = true;

        if (!_sceneManager.Travel(destination.Value))
        {
            _sceneManager.TargetSaveData = previousSlot;
            _sceneManager.LoadSavedData = previousLoad;
            _isStarting = false;
            ShowQuestion("씬 이동을 시작할 수 없습니다.", null);
            return;
        }

        _saveCanvas.interactable = false;
        _saveCanvas.blocksRaycasts = false;
    }

    private void CallGotoTitle()
    {
        _soundManager?.PlaySE(EEffectSound.QuestionAppear);
        _saveCanvas.gameObject.SetActive(false);
        SetTitleMenu(true);
        _startButton.Select();
    }

    private void CallGameSetting()
    {
        SetTitleMenu(false);
        _settingCanvas.SetActive(true);
        _bgmSlider.Select();
    }

    private void CallGameSettingBack()
    {
        PlayerPrefs.SetFloat(BgmVolumeKey, _bgmSlider.value);
        PlayerPrefs.SetFloat(SeVolumeKey, _seSlider.value);
        PlayerPrefs.Save();

        _settingCanvas.SetActive(false);
        SetTitleMenu(true);
        _settingButton.Select();
    }

    private void ShowQuestion(string shownText, Action action)
    {
        _soundManager?.PlaySE(EEffectSound.QuestionAppear);
        SetTitleMenu(false);
        _saveCanvas.interactable = false;
        _saveCanvas.blocksRaycasts = false;
        _confirmMessage.text = shownText;
        _confirmAction = action;
        _confirmYes.gameObject.SetActive(action != null);
        _confirmCanvas.SetActive(true);
        _confirmNo.Select();
    }

    private void HideQuestion()
    {
        _confirmCanvas.SetActive(false);
        _confirmAction = null;
        _confirmMessage.text = "";
        SetTitleMenu(!_saveCanvas.gameObject.activeSelf);
        _saveCanvas.interactable = true;
        _saveCanvas.blocksRaycasts = true;
        if (_returnFocus != null && _returnFocus.IsInteractable())
            _returnFocus.Select();
        else
            _saveBack.Select();
    }

    private void CallSlotDelete(int slot)
    {
        if (CSaveManager.DeleteData(slot))
        {
            RefreshSaveslot(slot);
            _saveSlots[slot].openButton.Select();
        }
        else
        {
            CLogger.Error($"{slot} : 저장 데이터 삭제에 실패했습니다.");
            ShowQuestion("저장 데이터를 삭제하지 못했습니다.", null);
        }
    }
}
