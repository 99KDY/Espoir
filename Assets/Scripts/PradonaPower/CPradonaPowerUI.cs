using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CPradonaPowerUI : MonoBehaviour
{
    [SerializeField] private CPlayer _player;
    [Tooltip("질투, 탐욕, 나태, 식욕, 색욕, 분노 순서")]
    [SerializeField] private Button[] _powerButtons = new Button[6];
    [SerializeField] private TMP_Text _selectionCountText;
    // [SerializeField] private Button _applyButton;

    [Header("공용 툴팁")]
    [SerializeField] private RectTransform _powerTooltip;
    [SerializeField] private TMP_Text _tooltipName;
    [SerializeField] private TMP_Text _tooltipDescription;

    [Header("디버그")]
    [SerializeField] private bool _debug;

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
    private int _displayedMaxSelected = -1;
    private float _noticeUntil;
    private readonly ColorBlock[] _originalColors = new ColorBlock[6];

    public bool IsOpen => _isOpen;
    private int GetMaxSelected()
    {
        if (_debug) return 3;

        CGameManager manager = CGameManager.Instance;
        return manager != null ? manager.GetAvailableStage() : 0;
    }

    private void Awake()
    {
        if (_selectionCountText == null || // _applyButton == null ||
            _powerTooltip == null || _tooltipName == null ||
            _tooltipDescription == null || _powerButtons == null ||
            _powerButtons.Length != _powers.Length)
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

        // _applyButton.onClick.AddListener(Apply);

        _selectionCountText.raycastTarget = false;
        _powerTooltip.pivot = new Vector2(0.5f, 0f);
        HideTooltip();
    }

    private void Update()
    {
        if (_isOpen && _noticeUntil > 0f && Time.unscaledTime >= _noticeUntil)
        {
            _noticeUntil = 0f;
            RefreshSelection();
        }
    }
    private bool CanEdit()
    {
        CGameManager manager = CGameManager.Instance;

        return _isOpen &&
               _player != null &&
               manager != null &&
               manager.BattleState == EBattleState.None;
    }


    public void Show(CPlayer player)
    {
        if (!isActiveAndEnabled || player == null) return;

        _player = player;
        _draft = player.PradonaPower & GetSelectablePowers();

        _isOpen = true;
        _noticeUntil = 0f;
        HideTooltip();
        RefreshSelection();
    }

    private void TogglePower(int index)
    {
        if (!CanEdit()) return;

        EPradonaPower power = _powers[index];
        EPradonaPower available = GetSelectablePowers();
        if (!available.HasFlag(power)) return;

        bool selected = _draft.HasFlag(power);
        if (!selected && CountSelected() >= GetMaxSelected())
        {
            _noticeUntil = Time.unscaledTime + 10f;
            RefreshSelection();
            return;
        }

        EPradonaPower next = selected ? _draft & ~power : _draft | power;
        if ((next & ~available) != EPradonaPower.None) return;
        if (FindFirstObjectByType<CPlayerUI_Build>() == null) return;

        _draft = next;
        _player.PradonaPower = next;
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
        bool canEdit = CanEdit();
        EPradonaPower available = GetSelectablePowers();

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
            _powerButtons[i].interactable = canEdit && available.HasFlag(_powers[i]);
        }
        _displayedMaxSelected = GetMaxSelected();
        _selectionCountText.text = $"선택 {CountSelected()} / {_displayedMaxSelected}개";

        if (_noticeUntil > Time.unscaledTime)
        {
            _selectionCountText.text += $"\n최대 {_displayedMaxSelected}개까지 선택할 수 있습니다.";
        }
        // _applyButton.interactable = CanEdit() && CountSelected() <= MaxSelected;
    }
    private EPradonaPower GetSelectablePowers()
    {
        if (_debug)
        {
            EPradonaPower all = EPradonaPower.None;
            foreach (EPradonaPower power in _powers)
            {
                all |= power;
            }
            return all;
        }

        CGameManager manager = CGameManager.Instance;
        EPradonaPower available = EPradonaPower.Invidia;
        if (manager == null) return available;

        EBossClear cleared = manager.BossClearState;

        if (cleared.HasFlag(EBossClear.Avaritia)) available |= EPradonaPower.Avaritia;
        if (cleared.HasFlag(EBossClear.Pigritia)) available |= EPradonaPower.Pigritia;
        if (cleared.HasFlag(EBossClear.Gula)) available |= EPradonaPower.Gula;
        if (cleared.HasFlag(EBossClear.Luxuria)) available |= EPradonaPower.Luxuria;
        if (cleared.HasFlag(EBossClear.Ira)) available |= EPradonaPower.Ira;

        return available;
    }

    //public void Apply()
    //{
    //    if (!CanEdit() || CountSelected() > MaxSelected) return;

    //    // 어짜피 프로퍼티의 setter가 갱신함!
    //    if (FindFirstObjectByType<CPlayerUI_Build>() == null) return;
    //    if ((_draft & ~GetSelectablePowers()) != EPradonaPower.None) return;

    //    _player.PradonaPower = _draft;
    //}

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

        RectTransform button = (RectTransform)_powerButtons[index].transform;
        Vector3 tooltipOrigin = new Vector3(
            button.rect.center.x,
            button.rect.yMax + 12f,
            0f
        );

        _powerTooltip.position = button.TransformPoint(tooltipOrigin);
        _powerTooltip.SetAsLastSibling();
        _powerTooltip.gameObject.SetActive(true);
    }

    private void HideTooltip()
    {
        _powerTooltip.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        _isOpen = false;
        _noticeUntil = 0f;
        _player = null;

        if (_powerTooltip != null)
        {
            HideTooltip();
        }
    }
}