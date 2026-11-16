using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// Q의 막타 연계 가능 상태와 E의 쿨타임 감소를 표시합니다.
/// Q 연계 중에는 MP 표시도 연계 비용으로 바꿉니다.
/// </summary>
[DefaultExecutionOrder(260)]
public class CHudSkillTag : MonoBehaviour
{
    /// <summary>딱지가 따르는 게임 상태.</summary>
    public enum EMode
    {
        StrikeLink = 0,
        CooldownCut = 1,
    }

    [SerializeField] private CPlayerHudView _hudView;
    [SerializeField] private EMode _mode;
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private RectTransform _box;
    [Tooltip("Q 의 MP 딱지 글자 (연계 창이면 연계 MP)")]
    [SerializeField] private TMP_Text _mpText;
    [SerializeField] private ESkillType _skillType = ESkillType.Strike;

    [Header("표시 전환")]
    [SerializeField] private float _transition = 0.2f;
    [SerializeField] private float _hiddenOffset = 6f;
    [SerializeField] private float _cutShowTime = 1.4f;

    private CPlayerSkill _skill;
    private CSkillSO _data;
    private float _restY;
    private float _from;
    private float _to;
    private float _start = -10f;
    private float _cutUntil = -10f;
    private bool _isShown;
    private string _shownMp;

    private void Awake()
    {
        if (_box != null) _restY = _box.anchoredPosition.y;
        Apply(0f);
    }

    private void OnDestroy()
    {
        if (_skill != null) _skill.CooldownReduced -= OnCooldownReduced;
    }

    private void LateUpdate()
    {
        if (_skill == null && !TryConnect()) return;

        if (_data == null)
        {
            foreach (CSkillSO s in _skill.RegisteredSkills)
            {
                if (s != null && s.Type == _skillType) _data = s;
            }
        }

        float now = Time.unscaledTime;
        bool show = _mode == EMode.StrikeLink ? _skill.IsStrikeLinkOpen : now < _cutUntil;
        if (show != _isShown)
        {

            _from = Current(now);
            _to = show ? 1f : 0f;
            _start = now;
            _isShown = show;
        }

        Apply(Current(now));

        if (_mode == EMode.StrikeLink && _mpText != null && _data != null)
        {
            string mp = (show ? _data.FinisherMpCost : _data.MpCost).ToString(CultureInfo.InvariantCulture);
            if (mp != _shownMp)
            {
                _mpText.text = mp;
                _shownMp = mp;
            }
        }
    }

    private bool TryConnect()
    {
        if (_hudView == null || _hudView.Player == null) return false;

        _skill = _hudView.Player.GetComponent<CPlayerSkill>();
        if (_skill == null) return false;

        _skill.CooldownReduced += OnCooldownReduced;
        return true;
    }

    private void OnCooldownReduced(CSkillSO skill, float seconds)
    {
        if (_mode != EMode.CooldownCut || skill == null || skill.Type != _skillType) return;

        _cutUntil = Time.unscaledTime + _cutShowTime;
    }

    private float Current(float now)
    {
        float k = Mathf.Clamp01((now - _start) / _transition);
        return Mathf.Lerp(_from, _to, CHudEase.Ease(k));
    }

    private void Apply(float v)
    {
        if (_group != null) _group.alpha = v;
        if (_box != null)
        {
            _box.anchoredPosition = new Vector2(_box.anchoredPosition.x, _restY - _hiddenOffset * (1f - v));
        }
    }
}
