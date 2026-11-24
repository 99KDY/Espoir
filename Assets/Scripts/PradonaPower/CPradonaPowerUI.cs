using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CPradonaPowerUI : MonoBehaviour
{
    [SerializeField] private CPlayer _player;
    [SerializeField] private GameObject _powerPanel;
    [Tooltip("질투, 탐욕, 나태, 식욕, 색욕, 분노 순서")]
    [SerializeField] private Button[] _powerButtons = new Button[6];
    [SerializeField] private TMP_Text _selectionCountText;
    [SerializeField] private Button _applyButton;
    [SerializeField] private Button _closeButton;

    [Header("공용 툴팁")]
    [SerializeField] private RectTransform _powerTooltip;
    [SerializeField] private TMP_Text _tooltipName;
    [SerializeField] private TMP_Text _tooltipDescription;

    private const int MaxSelected = 3;
    private readonly EPradonaPower[] _powers =
    {
        EPradonaPower.Invidia,
        EPradonaPower.Avaritia,
        EPradonaPower.Pigritia,
        EPradonaPower.Gula,
        EPradonaPower.Luxuria,
        EPradonaPower.Ira
    };

    private readonly string[] _names =
    {
        "질투", "탐욕", "나태", "식욕", "색욕", "분노"
    };

    // 팀 HUD의 효과 설명을 바탕으로 작성한 선택창 문구입니다.
    private readonly string[] _descriptions =
    {
        "다음 공격 적중 시, 최대 체력의 15% 회복합니다.",
        "다음 공격 적중 시, 적이 받는 피해를 20% 증가시킵니다.",
        "다음 자신이 공격받을 때, 피해를 99% 감소 시킵니다.",
        "일반 기술의 재사용 대기시간을 초기화합니다.",
        "기절 효과를 해제합니다.",
        "3초간 치명타 확률을 1.5배 증가시킵니다."
    };

    private readonly GameObject[] _selectedMarks = new GameObject[6];
    private EPradonaPower _draft;
    private bool _isOpen;
    private bool _wasInputLocked;
    private bool _changedGameState;
    private float _noticeUntil;
    private readonly ColorBlock[] _originalColors = new ColorBlock[6];

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        if (_player == null || _powerPanel == null ||
            _selectionCountText == null || _applyButton == null || _closeButton == null ||
            _powerTooltip == null || _tooltipName == null || _tooltipDescription == null ||
            _powerButtons == null || _powerButtons.Length != _powers.Length)
        {
            enabled = false;
            return;
        }

        for (int i = 0; i < _powers.Length; i++)
        {
            Transform mark = _powerButtons[i] != null ? _powerButtons[i].transform.Find("SelectedMark") : null;

            if (mark == null)
            {
                enabled = false;
                return;
            }
            _selectedMarks[i] = mark.gameObject;
            _originalColors[i] = _powerButtons[i].colors;
            TMP_Text markText = mark.GetComponent<TMP_Text>();
            if (markText != null)
            {
                markText.color = Color.black;
                markText.raycastTarget = false;
            }
        }

        for (int i = 0; i < _powers.Length; i++)
        {
            int index = i;
            _powerButtons[i].onClick.AddListener(() => TogglePower(index));
            BindTooltip(i);
        }

        _applyButton.onClick.AddListener(Apply);
        _closeButton.onClick.AddListener(Close);
        // 선택 수와 안내가 버튼을 침범하지 않도록 패널 아래쪽에 고정합니다.
        RectTransform countRect = _selectionCountText.rectTransform;
        countRect.anchorMin = countRect.anchorMax = new Vector2(0.5f, 0f);
        countRect.pivot = new Vector2(0.5f, 0f);
        countRect.anchoredPosition = new Vector2(0f, 75f);
        countRect.sizeDelta = new Vector2(660f, 85f);
        _selectionCountText.alignment = TextAlignmentOptions.Center;
        _selectionCountText.enableAutoSizing = true;
        _selectionCountText.fontSizeMin = 14f;
        _selectionCountText.fontSizeMax = 24f;
        _selectionCountText.overflowMode = TextOverflowModes.Truncate;
        _selectionCountText.raycastTarget = false;
        _powerTooltip.pivot = new Vector2(0f, 1f);
        HideTooltip();
        _powerPanel.SetActive(false);
    }

    private void Update()
    {
        if (_isOpen && _noticeUntil > 0f && Time.unscaledTime >= _noticeUntil)
        {
            _noticeUntil = 0f;
            RefreshSelection();
        }
        CGameManager manager = CGameManager.Instance;

        if (_isOpen && (manager == null || manager.BattleState != EBattleState.None))
        {
            Close();
            return;
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            if (_isOpen) Close();
            else Open();
        }
    }

    public void Open()
    {
        if (_isOpen) return;

        CGameManager manager = CGameManager.Instance;
        if (manager == null || manager.BattleState != EBattleState.None ||
            manager.GameState != EGameState.Idle || _player.IsInputLocked)
        {
            return;
        }

        _draft = EPradonaPower.None;
        foreach (EPradonaPower power in _powers)
        {
            if (_player.PradonaPower.HasFlag(power)) _draft |= power;
        }

        _wasInputLocked = _player.IsInputLocked;
        _player.SetInputLocked(true);
        _changedGameState = true;
        manager.SetGameState(EGameState.UI);

        _isOpen = true;
        _noticeUntil = 0f;
        RefreshSelection();
        HideTooltip();
        _powerPanel.SetActive(true);
    }

    public void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;
        _powerPanel.SetActive(false);
        HideTooltip();
        if (_player != null) _player.SetInputLocked(_wasInputLocked);

        CGameManager manager = CGameManager.Instance;
        if (_changedGameState && manager != null && manager.GameState == EGameState.UI)
        {
            manager.SetGameState(EGameState.Idle);
        }
        _changedGameState = false;
    }

    private void TogglePower(int index)
    {
        if (!_isOpen) return;

        EPradonaPower power = _powers[index];
        if (_draft.HasFlag(power))
        {
            _draft &= ~power;
        }
        else
        {
            if (CountSelected() >= MaxSelected)
            {
                _noticeUntil = Time.unscaledTime + 10f;
                RefreshSelection();
                return;
            }
            _draft |= power;
        }
        RefreshSelection();
    }

    private int CountSelected()
    {
        int count = 0;
        foreach (EPradonaPower power in _powers)
        {
            if (_draft.HasFlag(power)) count++;
        }
        return count;
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < _powers.Length; i++)
        {
            bool selected = _draft.HasFlag(_powers[i]);
            _selectedMarks[i].SetActive(selected);
            ColorBlock colors = _originalColors[i];
            if (selected)
            {
                Color gold = new Color(1f, 0.78f, 0.3f, 1f);
                colors.normalColor = gold;
                colors.highlightedColor = gold;
                colors.selectedColor = gold;
                colors.pressedColor = new Color(0.85f, 0.6f, 0.2f, 1f);
            }
            _powerButtons[i].transition = Selectable.Transition.ColorTint;
            _powerButtons[i].colors = colors;
        }
        _selectionCountText.text = $"선택 {CountSelected()} / {MaxSelected}개";
        if (_noticeUntil > Time.unscaledTime)
            _selectionCountText.text += "\n최대 3개까지 선택할 수 있습니다.";
        _applyButton.interactable = CountSelected() <= MaxSelected;
    }

    public void Apply()
    {
        if (!_isOpen || CountSelected() > MaxSelected) return;

        CGameManager manager = CGameManager.Instance;
        if (manager == null || manager.BattleState != EBattleState.None)
        {
            Close();
            return;
        }

        // 기존 프로퍼티가 팀 HUD의 권능 표시까지 갱신합니다.
        if (FindFirstObjectByType<CPlayerUI_Build>() == null)
        {
            return;
        }

        _player.PradonaPower = _draft;
        Close();
    }

    private void BindTooltip(int index)
    {
        EventTrigger trigger = _powerButtons[index].GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = _powerButtons[index].gameObject.AddComponent<EventTrigger>();
        }

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ShowTooltip(index));
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideTooltip());
        trigger.triggers.Add(exit);
    }

    private void ShowTooltip(int index)
    {
        if (!_isOpen) return;

        _tooltipName.text = _names[index];
        _tooltipDescription.text = _descriptions[index];
        Vector3[] corners = new Vector3[4];
        ((RectTransform)_powerButtons[index].transform).GetWorldCorners(corners);
        _powerTooltip.position = corners[2];
        _powerTooltip.SetAsLastSibling();
        _powerTooltip.gameObject.SetActive(true);
    }

    private void HideTooltip()
    {
        _powerTooltip.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        Close();
    }
}