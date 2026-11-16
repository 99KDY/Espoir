using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하단 HUD 의 HP·MP 글자, 전투의 흐름 고리, MP 소모 딱지, 메뉴 버튼을 표시한다.
/// 막대·쿨타임·퀵슬롯은 CPlayerUI_Build 가 그리고, 이 컴포넌트는 CPlayer 값을 읽기만 한다.
/// </summary>
public class CPlayerHudView : MonoBehaviour
{
    [Serializable]
    private struct SSkillCostTag
    {
        public ESkillType type;
        public GameObject tag;
        public TMP_Text text;
    }

    [SerializeField] private CPlayer _player;

    [Header("HP · MP 글자")]
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _mpText;
    [SerializeField] private TMP_Text _hpGlow;
    [SerializeField] private TMP_Text _mpGlow;
    [SerializeField, Range(0f, 1f)] private float _hpEase = 0.35f;
    [SerializeField] private float _labelSize = 12f;
    [SerializeField] private float _labelSpacing = 2.4f;
    [SerializeField] private float _maxSize = 14f;
    [SerializeField] private float _gap = 8f;

    [Header("전투의 흐름 (StatusSO id)")]
    [SerializeField] private int _flowStatusId = 10;
    [SerializeField] private Image _flowRing;
    [SerializeField] private CHudRing _flowRingView;
    [SerializeField] private GameObject _flowTag;
    [Tooltip("흐름 최대 중첩일 때 숫자와 패시브 슬롯의 강조 색을 설정합니다.")]
    [SerializeField] private CHudBox _flowTagBox;
    [SerializeField] private TMP_Text _flowTagText;
    [SerializeField] private CHudSlotStyle _flowSlot;
    [SerializeField] private Color _flowTagLine = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color _flowTagTextColor = Color.white;
    [SerializeField] private Color _flowFullColor = new Color(1f, 192f / 255f, 112f / 255f);

    [Header("MP 소모 딱지")]
    [SerializeField] private SSkillCostTag[] _costTags;

    private int _shownHp = -1;
    private float _easedHp = -1f;
    private int _shownHpMax = -1;
    private int _shownMp = -1;
    private int _shownMpMax = -1;
    private int _shownFlow = -1;
    private bool _costsShown;
    private float _flowRingWeight = -1f;

    public CPlayer Player => _player;

    private void Start()
    {
        if (_player == null)
        {
            CLogger.Warn($"{name}: CPlayerHudView 에 플레이어가 연결되지 않았습니다.");
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        UpdateHpText();
        UpdateMpText();
        UpdateFlow();

        if (!_costsShown)
        {
            ShowSkillCosts();
        }
    }

    /// <summary>
    /// 기본 메뉴(ESC 메뉴)를 열거나 닫습니다. HUD 메뉴 버튼의 클릭 이벤트에 연결합니다.
    /// </summary>
    public void ToggleMenu()
    {
        if (CGameManager.Instance == null)
        {
            CLogger.Warn($"{name}: 씬에 CGameManager 가 없어 메뉴를 열 수 없습니다.");
            return;
        }

        CGameManager.Instance.ToggleBaseMenu();
    }

    private void UpdateHpText()
    {
        if (_hpText == null) return;

        // 표시 수치가 실제 체력에 부드럽게 따라가도록 보간한다.
        float target = Mathf.Max(0f, _player.HpCurrent);
        _easedHp = _easedHp < 0f ? target : _easedHp + (target - _easedHp) *
            (1f - Mathf.Pow(1f - _hpEase, Time.unscaledDeltaTime * 60f));
        if (Mathf.Abs(target - _easedHp) < 0.5f) _easedHp = target;

        int current = Mathf.RoundToInt(_easedHp);
        int max = Mathf.RoundToInt(_player.HpMax);
        if (current == _shownHp && max == _shownHpMax) return;

        _shownHp = current;
        _shownHpMax = max;
        _hpText.text = BuildValueText("HP", current, max, false);
        if (_hpGlow != null) _hpGlow.text = BuildValueText("HP", current, max, true);
    }

    private void UpdateMpText()
    {
        if (_mpText == null) return;

        int current = Mathf.Max(0, _player.CurrentMp);
        int max = Mathf.RoundToInt(_player.MaxMp);
        if (current == _shownMp && max == _shownMpMax) return;

        _shownMp = current;
        _shownMpMax = max;
        _mpText.text = BuildValueText("MP", current, max, false);
        if (_mpGlow != null) _mpGlow.text = BuildValueText("MP", current, max, true);
    }

    private string BuildValueText(string label, int current, int max, bool isGlowLayer)
    {
        // 현재값을 크게 표시하고 라벨과 최대값은 작게 표시한다.
        // 그림자 층(isGlowLayer, Espoir/UI/TextShadowBlur)은 같은 배치로 글자마다 정점 색으로 그림자 묶음을 고른다:
        // 값 = 흰색(값 text-shadow: 빛 0 0 10px + #000 0 1px 3px), 라벨 · 최대값 = 검정 · 알파 .8(컨테이너 text-shadow 0 1px 4px)
        CultureInfo culture = CultureInfo.InvariantCulture;
        string side = isGlowLayer ? "<color=#000000CC>" : "<alpha=#CC>";
        string main = isGlowLayer ? "<color=#FFFFFFFF>" : "<alpha=#FF>";
        return string.Format(culture,
            "<size={0}><cspace={1}>{7}{2}</cspace>{8}</size><space={3}>{4:N0}<space={3}>" +
            "<size={5}>{7}/ {6:N0}</size>",
            _labelSize, _labelSpacing, label, _gap, current, _maxSize, max, side, main);
    }

    private void UpdateFlow()
    {
        CStatus flow = _player.FindStatus(_flowStatusId);
        int stack = flow != null ? flow.Stack : 0;
        if (_flowRingView != null)
        {
            _flowRingView.Flow = flow != null && flow.MaxStack > 0 ? (float)stack / flow.MaxStack : 0f;

            // 흐름이 있으면 고리를 표시하고, 없어지면 서서히 숨긴다.
            float target = stack > 0 ? 1f : 0f;
            _flowRingWeight = _flowRingWeight < 0f ? target : Mathf.MoveTowards(_flowRingWeight, target,
                Time.unscaledDeltaTime / 0.3f);
            _flowRingView.canvasRenderer.SetAlpha(CHudEase.Ease(_flowRingWeight));
        }

        if (stack == _shownFlow) return;

        _shownFlow = stack;
        if (_flowRing != null)
        {
            _flowRing.fillAmount = flow != null && flow.MaxStack > 0 ? (float)stack / flow.MaxStack : 0f;
        }

        if (_flowTag != null)
        {
            _flowTag.SetActive(stack > 0);
        }

        bool isFull = flow != null && flow.MaxStack > 0 && stack >= flow.MaxStack;
        if (_flowTagBox != null)
        {
            _flowTagBox.BorderColor = isFull ? _flowFullColor : _flowTagLine;
            _flowTagBox.SetShadow(isFull ? 10f : 0f, isFull ? _flowFullColor : Color.clear);
        }

        if (_flowTagText != null) _flowTagText.color = isFull ? _flowFullColor : _flowTagTextColor;
        if (_flowSlot != null) _flowSlot.IsFull = isFull;
    }

    private void ShowSkillCosts()
    {
        if (_player.Skills == null || _player.Skills.Count == 0) return;

        for (int i = 0; i < _costTags.Length; i++)
        {
            if (_costTags[i].tag == null || _costTags[i].text == null) continue;

            CSkillSO skill = FindSkill(_costTags[i].type);
            bool hasCost = skill != null && skill.MpCost > 0;
            _costTags[i].tag.SetActive(hasCost);
            if (hasCost)
            {
                _costTags[i].text.text = skill.MpCost.ToString(CultureInfo.InvariantCulture);
            }
        }

        _costsShown = true;
    }

    private CSkillSO FindSkill(ESkillType type)
    {
        for (int i = 0; i < _player.Skills.Count; i++)
        {
            if (_player.Skills[i] != null && _player.Skills[i].Type == type)
            {
                return _player.Skills[i];
            }
        }

        return null;
    }
}
