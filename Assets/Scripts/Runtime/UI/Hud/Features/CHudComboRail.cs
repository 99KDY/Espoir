using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 평타 진행 횟수와 Q, W 연계 시간을 표시합니다.
/// </summary>
[DefaultExecutionOrder(260)]
public class CHudComboRail : MonoBehaviour
{
    private sealed class CColorTween
    {
        private Color _from;
        private Color _to;
        private float _start = -10f;
        private bool _hasValue;

        /// <summary>다음 값부터 새 보간을 시작하도록 초기화합니다.</summary>
        public void Reset()
        {
            _hasValue = false;
            _start = -10f;
        }

        /// <summary>현재 시각의 보간 색상을 반환합니다.</summary>
        public Color Get(float now, float duration)
        {
            if (duration <= 0f) return _to;

            float k = Mathf.Clamp01((now - _start) / duration);
            return PremulLerp(_from, _to, CHudEase.Ease(k));
        }

        /// <summary>목표가 바뀌면 현재 값에서 보간을 시작합니다.</summary>
        public void Set(Color target, float now, float duration)
        {
            if (!_hasValue)
            {
                _from = target;
                _to = target;
                _hasValue = true;
                return;
            }

            if (target == _to) return;

            _from = Get(now, duration);
            _to = target;
            _start = now;
        }
    }

    private sealed class CFloatTween
    {
        private float _from;
        private float _to;
        private float _start = -10f;
        private bool _hasValue;

        /// <summary>다음 값부터 새 보간을 시작하도록 초기화합니다.</summary>
        public void Reset()
        {
            _hasValue = false;
            _start = -10f;
        }

        /// <summary>현재 시각의 보간 값을 반환합니다.</summary>
        public float Get(float now, float duration, float x1, float y1, float x2, float y2)
        {
            float k = Mathf.Clamp01((now - _start) / duration);
            return Mathf.Lerp(_from, _to, CHudEase.Bezier(x1, y1, x2, y2, k));
        }

        /// <summary>목표가 바뀌면 현재 값에서 보간을 시작합니다.</summary>
        public void Set(float target, float now, float duration, float x1, float y1, float x2, float y2)
        {
            if (!_hasValue)
            {
                _from = target;
                _to = target;
                _hasValue = true;
                return;
            }

            if (Mathf.Approximately(target, _to)) return;

            _from = Get(now, duration, x1, y1, x2, y2);
            _to = target;
            _start = now;
        }
    }

    private sealed class CCell
    {
        public readonly CColorTween BG = new CColorTween();
        public readonly CColorTween BORDER = new CColorTween();
        public bool isPlayingHit;
        public float hitStart = -10f;
    }

    private enum EFourthHitAnimation
    {
        None,
        Grow,
        Shrink,
    }

    [System.Serializable]
    private class CLayoutSettings
    {
        [Min(0f)] public float width = 230f;
        [Min(0f)] public float cellWidth = 7f;
        [Min(0f)] public float cellHeight = 24f;
        [Min(0f)] public float cellGap = 9f;
        [Min(0f)] public float trackTop = 34f;
        [Min(0f)] public float trackHeight = 5f;
        [Min(0f)] public float letterTop = 9f;
        [Min(0f)] public float letterBaseline = 12f;
        [Min(0f)] public float wLetterX = 218.266f;
    }

    [SerializeField, InspectorName("배치")] private CLayoutSettings _layout = new CLayoutSettings();

    [System.Serializable]
    private class CAnimationSettings
    {
        [Min(0.01f)] public float colorTime = 0.1f;
        [Min(0.01f)] public float hitTime = 0.28f;
        [Min(0.01f)] public float openTime = 0.15f;
        [Min(0.01f)] public float fourthHitSizeTime = 0.45f;
        [Min(0.01f)] public float fourthHitFadeTime = 0.3f;
        [Min(0.01f)] public float fourthHitBorderTime = 0.2f;
        [Min(0.01f), InspectorName("4타 전환 최소 유지 시간")] public float fourthHitAnimationTime = 1.1f;
        [Min(0.01f)] public float dropTime = 0.38f;
        [Min(0f)] public float dropDistance = 40f;
        [Min(0f)] public float landDelay = 0.38f;
        [Min(0.01f)] public float landTime = 0.3f;
        [Min(0f)] public float dustDelay = 0.36f;
        [Min(0.01f)] public float dustTime = 0.55f;
        [Min(0.01f)] public float riseTime = 0.35f;
        [Min(0f)] public float riseDistance = 30f;
    }

    [SerializeField, InspectorName("연출")] private CAnimationSettings _animation = new CAnimationSettings();

    [System.Serializable]
    private class CColorSettings
    {
        public Color emptyBg = CreateColor(8, 10, 18, 0.3f);
        public Color emptyBgUlt = CreateColor(8, 10, 18, 0.5f);
        public Color emptyBorder = CreateColor(255, 255, 255, 0.6f);
        public Color white = CreateColor(255, 255, 255);
        public Color cream = CreateColor(255, 246, 232);
        public Color amber = CreateColor(255, 192, 112);
        public Color fullGlow1 = CreateColor(255, 240, 214);
        public Color fullGlow2 = CreateColor(255, 170, 90, 0.6f);
        public Color ultGlow = CreateColor(255, 192, 112, 0.6f);
        public Color wGlow = CreateColor(255, 170, 90, 0.7f);
        public Color qGlow = CreateColor(255, 240, 214, 0.7f);
        public Color qUlt = CreateColor(255, 240, 220);
        public Color qUltGlow = CreateColor(255, 240, 218);
        public Color track = CreateColor(255, 255, 255, 0.16f);
        public Color divider = CreateColor(255, 255, 255, 0.6f);
        public Color qText = CreateColor(255, 240, 220);
    }

    [SerializeField, InspectorName("색상")] private CColorSettings _colors = new CColorSettings();

    private const float _SKEW_TAN = -0.48773258856f;

    private static readonly Color _CLEAR = new Color(0f, 0f, 0f, 0f);
    private static readonly Color[] _FLOW =
    {
        CreateColor(255, 246, 232), CreateColor(255, 226, 184),
        CreateColor(255, 208, 138), CreateColor(255, 192, 112)
    };
    private static readonly float[] _GLOW_A1 = { 63f / 255f, 84f / 255f, 105f / 255f, 126f / 255f };
    private static readonly float[] _GLOW_A2 = { 29f / 255f, 38f / 255f, 48f / 255f, 57f / 255f };

    [SerializeField] private CPlayerHudView _hudView;
    [SerializeField] private CLeviathanHudView _ultView;

    [Header("평타 진행 표시")]
    [SerializeField] private CHudComboRailShape[] _cells = new CHudComboRailShape[4];

    [SerializeField] private CHudComboRailShape _dust;

    [Header("Q 및 W 연계 표시")]
    [SerializeField] private CanvasGroup _track;
    [SerializeField] private CHudComboRailShape _trackBack;
    [SerializeField] private CHudComboRailShape _divider;
    [SerializeField] private CHudComboRailShape _wBar;
    [SerializeField] private CHudComboRailShape _qBar;
    [SerializeField] private TMP_Text _qLetter;
    [SerializeField] private TMP_Text _wLetter;

    [SerializeField] private GameObject _wGlow;

    [SerializeField] private GameObject _wShadow;

    [Header("다단히트 구분 간격")]
    [SerializeField] private float _strikeGap = 0.06f;

    [Header("막타 연계 표시")]
    [SerializeField] private bool _showAtFinisher = true;

    private readonly CCell[] _CELLS = { new CCell(), new CCell(), new CCell(), new CCell() };
    private readonly CFloatTween _FOURTH_HIT_WIDTH = new CFloatTween();
    private readonly CFloatTween _FOURTH_HIT_MARGIN = new CFloatTween();
    private readonly CFloatTween _FOURTH_HIT_OPACITY = new CFloatTween();
    private readonly CFloatTween _FOURTH_HIT_BORDER = new CFloatTween();
    private readonly CFloatTween _TRACK_ALPHA = new CFloatTween();
    private readonly CFloatTween _W_ALPHA = new CFloatTween();
    private RawImage _wGlowImage;
    private RawImage _wShadowImage;

    private CPlayer _player;
    private RectTransform _rect;
    private Canvas _canvas;
    private Vector2 _origin;
    private float _scale = 1f;
    private CPlayerSkill _skill;
    private CSkillSO _attackData;
    private int _attackCount = 3;
    private float _cachedStrikeGap = -1f;
    private float _crownWindow = 2f;
    private float _finisherWindow = 1f;
    private int _lit;
    private bool _isCompleted;
    private float _holdUntil = -1f;
    private int _shownCount;
    private EFourthHitAnimation _fourthHitAnimation;
    private float _fourthHitStart = -10f;
    private bool _isUlt;

    /// <summary>지금 켜진 평타 칸 수.</summary>
    public int Hits { get; private set; }

    /// <summary>W 쿨타임이 끝나 막대와 글자를 표시할 수 있는지 반환합니다.</summary>
    public bool IsWReady { get; private set; } = true;

    /// <summary>Q 또는 W 연계 표시가 열려 있는지 반환합니다.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>W 막대 비율 (W 사용 기회 남은 시간 / 전체).</summary>
    public float WRatio { get; private set; }

    /// <summary>Q 막대 비율 (Q 연계 남은 시간 / W 사용 기회 전체).</summary>
    public float QRatio { get; private set; }

    private void OnDisable()
    {
        Disconnect();
    }

    private void Disconnect()
    {
        if (_skill != null) _skill.BasicAttackHitProcessed -= OnHitProcessed;

        _skill = null;
        _player = null;
        _attackData = null;
        _attackCount = 3;
        _cachedStrikeGap = -1f;
        _crownWindow = 2f;
        _finisherWindow = 1f;
        _lit = 0;
        _isCompleted = false;
        _holdUntil = -1f;
        _shownCount = 0;
        _fourthHitAnimation = EFourthHitAnimation.None;
        _fourthHitStart = -10f;
        Hits = 0;
        IsOpen = false;
        IsWReady = false;
        WRatio = 0f;
        QRatio = 0f;
        if (_track != null) _track.alpha = 0f;

        _FOURTH_HIT_WIDTH.Reset();
        _FOURTH_HIT_MARGIN.Reset();
        _FOURTH_HIT_OPACITY.Reset();
        _FOURTH_HIT_BORDER.Reset();
        _TRACK_ALPHA.Reset();
        _W_ALPHA.Reset();

        foreach (CCell cell in _CELLS)
        {
            cell.BG.Reset();
            cell.BORDER.Reset();
            cell.isPlayingHit = false;
            cell.hitStart = -10f;
        }
    }

    private void LateUpdate()
    {
        float now = Time.unscaledTime;
        CPlayer player = _hudView != null ? _hudView.Player : null;
        if (_player != player || (player == null && !ReferenceEquals(_player, null)) ||
            (_skill == null && player != null))
        {
            Disconnect();
            TryConnect();
        }

        int count = 3;
        bool wReady = true;
        bool isOpen = false;
        float wRatio = 0f;
        float qRatio = 0f;
        if (_skill != null)
        {
            CSkillSO attack = _skill.BasicAttackData;
            if (_attackData != attack || _cachedStrikeGap != _strikeGap)
            {
                _attackData = attack;
                _cachedStrikeGap = _strikeGap;
                _attackCount = 0;
                if (attack != null && attack.Steps != null)
                {
                    foreach (SSkillStep step in attack.Steps)
                    {
                        _attackCount += CountAttacks(step.hits, int.MaxValue);
                    }
                }
                _attackCount = Mathf.Clamp(_attackCount, 3, 4);
            }
            count = _attackCount;
            float gameNow = Time.time;
            float crownLeft = _skill.CrownWindowRemaining;
            float linkLeft = _skill.StrikeLinkRemaining;
            bool isBasicComboAlive = _skill.IsBasicComboAlive;
            _skill.GetSkillCooldown(ESkillType.Crown, out _, out float crownCool);
            wReady = crownCool <= 0f;
            float crownShown = wReady ? crownLeft : 0f;
            if (crownShown > 0f) _holdUntil = Mathf.Max(_holdUntil, gameNow + crownShown);
            if (linkLeft > 0f) _holdUntil = Mathf.Max(_holdUntil, gameNow + linkLeft);
            if (!isBasicComboAlive && gameNow >= _holdUntil)
            {
                _lit = 0;
                _isCompleted = false;
            }
            if (_showAtFinisher)
            {
                int hitsNow = _isCompleted ? count : Mathf.Min(_lit, count);
                bool atFinisher = hitsNow > 0 && hitsNow >= count;
                _skill.GetSkillCooldown(ESkillType.Strike, out _, out float strikeCool);
                bool isLinkPending = strikeCool <= 0f && atFinisher && linkLeft <= 0f &&
                    _skill.IsBasicFinisherActive;
                float qShown = linkLeft > 0f ? linkLeft : (isLinkPending ? _finisherWindow : 0f);
                isOpen = atFinisher && (crownShown > 0f || qShown > 0f);
                wRatio = isOpen ? Mathf.Clamp01(crownShown / _crownWindow) : 0f;
                qRatio = isOpen ? Mathf.Clamp01(qShown / _crownWindow) : 0f;
            }
            else
            {
                isOpen = crownShown > 0f || linkLeft > 0f;
                wRatio = isOpen ? Mathf.Clamp01(crownShown / _crownWindow) : 0f;
                qRatio = isOpen ? Mathf.Clamp01(linkLeft / _crownWindow) : 0f;
            }
        }

        _isUlt = _ultView != null && _ultView.IsUltStateOn;
        UpdateOrigin();
        UpdateCount(count, now);
        Hits = _isCompleted ? _shownCount : Mathf.Min(_lit, _shownCount);
        IsOpen = isOpen;
        IsWReady = wReady;
        WRatio = wRatio;
        QRatio = qRatio;

        DrawCells(now);
        DrawTrack(now);
    }

    private void TryConnect()
    {
        if (_hudView == null || _hudView.Player == null) return;

        CPlayerSkill skill = _hudView.Player.GetComponent<CPlayerSkill>();
        if (skill == null) return;

        _player = _hudView.Player;
        _skill = skill;
        _skill.BasicAttackHitProcessed += OnHitProcessed;
        foreach (CSkillSO skillData in _skill.RegisteredSkills)
        {
            if (skillData != null && skillData.Type == ESkillType.Strike)
            {
                _finisherWindow = Mathf.Max(0.01f, skillData.FinisherWindow);
            }
            if (skillData != null && skillData.Type == ESkillType.Crown && skillData.CrownStatus != null)
            {
                _crownWindow = Mathf.Max(0.01f, skillData.CrownWindow);
            }
        }
    }

    private void OnHitProcessed(CSkillSO skill, int step, int hit)
    {
        if (skill == null || skill.Steps == null || step < 0 || step >= skill.Steps.Length) return;

        if (step == 0 && hit == 0)
        {
            _lit = 0;
            _isCompleted = false;
        }

        _lit = Mathf.Max(_lit, GetAttackIndex(skill, step, hit) + 1);
        SSkillHit[] hits = skill.Steps[step].hits;
        if (step == skill.Steps.Length - 1 && hits != null && hit == hits.Length - 1) _isCompleted = true;
    }

    private int GetAttackIndex(CSkillSO skill, int step, int hit)
    {
        int index = 0;
        for (int s = 0; s < step; s++)
        {
            index += CountAttacks(skill.Steps[s].hits, int.MaxValue);
        }

        return index + Mathf.Max(0, CountAttacks(skill.Steps[step].hits, hit) - 1);
    }

    private int CountAttacks(SSkillHit[] hits, int last)
    {
        if (hits == null) return 0;

        int count = 0;
        int end = Mathf.Min(last, hits.Length - 1);
        for (int i = 0; i <= end; i++)
        {
            if (i == 0 || hits[i].time - hits[i - 1].time > _strikeGap) count++;
        }

        return count;
    }

    private void UpdateCount(int count, float now)
    {
        bool isFour = count == 4;
        bool isFirst = _shownCount == 0;
        if (!isFirst && count != _shownCount)
        {
            _fourthHitAnimation = isFour ? EFourthHitAnimation.Grow : EFourthHitAnimation.Shrink;
            _fourthHitStart = now;
        }

        float animationTime = Mathf.Max(_animation.fourthHitAnimationTime,
            Mathf.Max(_animation.dropTime, _animation.riseTime));
        animationTime = Mathf.Max(animationTime,
            Mathf.Max(_animation.landDelay + _animation.landTime, _animation.dustDelay + _animation.dustTime));
        if (_fourthHitAnimation != EFourthHitAnimation.None && now - _fourthHitStart > animationTime)
        {
            _fourthHitAnimation = EFourthHitAnimation.None;
        }

        _shownCount = count;
        _FOURTH_HIT_WIDTH.Set(isFour ? _layout.cellWidth : 0f, now, _animation.fourthHitSizeTime, 0.2f, 0.8f, 0.2f, 1f);
        _FOURTH_HIT_MARGIN.Set(isFour ? 0f : -_layout.cellGap, now, _animation.fourthHitSizeTime, 0.2f, 0.8f, 0.2f, 1f);
        _FOURTH_HIT_OPACITY.Set(isFour ? 1f : 0f, now, _animation.fourthHitFadeTime, 0.25f, 0.1f, 0.25f, 1f);
        _FOURTH_HIT_BORDER.Set(isFour ? 1f : 0f, now, _animation.fourthHitBorderTime, 0.25f, 0.1f, 0.25f, 1f);
    }

    private void DrawCells(float now)
    {
        bool isFull = Hits >= _shownCount;
        float w4 = _FOURTH_HIT_WIDTH.Get(now, _animation.fourthHitSizeTime, 0.2f, 0.8f, 0.2f, 1f);
        float m4 = _FOURTH_HIT_MARGIN.Get(now, _animation.fourthHitSizeTime, 0.2f, 0.8f, 0.2f, 1f);
        float rowWidth = 3f * _layout.cellWidth + 2f * _layout.cellGap + _layout.cellGap + m4 + w4;
        float left = (_layout.width - rowWidth) * 0.5f;

        for (int i = 0; i < 4; i++)
        {
            CHudComboRailShape shape = i < _cells.Length ? _cells[i] : null;
            if (shape == null) continue;

            bool isH4 = i == 3;
            bool isOn = i < Hits;
            CCell cell = _CELLS[i];
            bool isFourthHitAnimating = isH4 && _fourthHitAnimation != EFourthHitAnimation.None;
            bool wantsHit = isOn && !isFourthHitAnimating;
            if (wantsHit && !cell.isPlayingHit) cell.hitStart = now;
            cell.isPlayingHit = wantsHit;
            Color bgTarget;
            Color borderTarget;
            if (!isOn)
            {
                bgTarget = _isUlt ? _colors.emptyBgUlt : _colors.emptyBg;
                borderTarget = _colors.emptyBorder;
            }
            else if (_isUlt)
            {
                bgTarget = isFull ? _colors.white : _colors.amber;
                borderTarget = bgTarget;
            }
            else
            {
                bgTarget = _CLEAR;
                borderTarget = _CLEAR;
            }
            float colorTime = isH4 ? 0f : _animation.colorTime;
            cell.BG.Set(bgTarget, now, colorTime);
            cell.BORDER.Set(borderTarget, now, colorTime);

            if (isOn && !_isUlt && isFull)
            {
                shape.SetFill(CHudComboRailShape.EFill.Vertical, 2,
                    _colors.white, _colors.cream, _colors.cream, _colors.cream, 0, 1);
                shape.SetShadows(10f, _colors.fullGlow1, 22f, _colors.fullGlow2);
            }
            else if (isOn && !_isUlt)
            {
                shape.SetFill(CHudComboRailShape.EFill.Horizontal, 4, _FLOW[0], _FLOW[1], _FLOW[2], _FLOW[3], i, 4);
                Color g = _FLOW[i];
                shape.SetShadows(7f, new Color(g.r, g.g, g.b, _GLOW_A1[i]), 15f, new Color(g.r, g.g, g.b, _GLOW_A2[i]));
            }
            else
            {
                shape.SetSolid(cell.BG.Get(now, colorTime));
                if (isOn) shape.SetShadows(isFull ? 12f : 8f, isFull ? _colors.amber : _colors.ultGlow, 0f, _CLEAR);
                else shape.SetShadows(0f, _CLEAR, 0f, _CLEAR);
            }
            float scaleY = 1f;
            float brightness = 1f;
            if (cell.isPlayingHit && now - cell.hitStart < _animation.hitTime)
            {
                float p = CHudEase.Bezier(0.2f, 0.9f, 0.3f, 1.4f, (now - cell.hitStart) / _animation.hitTime);
                scaleY = 2.2f - 1.2f * p;
                brightness = 2f - p;
            }

            float width = isH4 ? w4 : _layout.cellWidth;
            float x = SnapX(isH4
                ? left + 3f * (_layout.cellWidth + _layout.cellGap) + m4
                : left + i * (_layout.cellWidth + _layout.cellGap));
            float top = SnapY(0f);
            float borderWidth = isH4
                ? _FOURTH_HIT_BORDER.Get(now, _animation.fourthHitBorderTime, 0.25f, 0.1f, 0.25f, 1f) : 1f;
            float opacity = isH4
                ? _FOURTH_HIT_OPACITY.Get(now, _animation.fourthHitFadeTime, 0.25f, 0.1f, 0.25f, 1f) : 1f;
            float ty = 0f;
            float sx = 1f;
            float sy = 1f;
            if (isH4) ApplyFourthHitAnimation(now, ref opacity, ref ty, ref sx, ref sy);

            shape.SetBorder(borderWidth, cell.BORDER.Get(now, colorTime));
            shape.SetFilter(brightness, opacity);
            float ox = width * 0.5f;
            float oy = _layout.cellHeight;
            float a = sx;
            float c = sx * _SKEW_TAN * scaleY;
            float d = sy * scaleY;
            float e = x + ox - (a * ox + c * oy);
            float f = top + ty + oy - d * oy;
            shape.SetGeometry(new Vector2(width, _layout.cellHeight), a, 0f, c, d, e, f);

            if (isH4) DrawDust(now, width, borderWidth, opacity, a, c, d, e, f);
        }
    }

    private void ApplyFourthHitAnimation(float now, ref float opacity, ref float ty, ref float sx, ref float sy)
    {
        float t = now - _fourthHitStart;
        if (_fourthHitAnimation == EFourthHitAnimation.Grow)
        {
            if (t < _animation.dropTime)
            {
                float u = t / _animation.dropTime;
                ty = -_animation.dropDistance + _animation.dropDistance * CHudEase.Bezier(0.55f, 0f, 0.85f, 0.5f, u);
                opacity = u < 0.35f
                    ? CHudEase.Bezier(0.55f, 0f, 0.85f, 0.5f, u / 0.35f)
                    : Mathf.Lerp(1f, opacity, CHudEase.Bezier(0.55f, 0f, 0.85f, 0.5f, (u - 0.35f) / 0.65f));
            }

            if (t >= _animation.landDelay && t < _animation.landDelay + _animation.landTime)
            {
                float p = CHudEase.Bezier(0.2f, 0.9f, 0.3f, 1.5f, (t - _animation.landDelay) / _animation.landTime);
                sx = Mathf.Lerp(1.2f, 1f, p);
                sy = Mathf.Lerp(0.76f, 1f, p);
            }
        }
        else if (_fourthHitAnimation == EFourthHitAnimation.Shrink)
        {
            float p = CHudEase.EaseIn(Mathf.Clamp01(t / _animation.riseTime));
            ty = -_animation.riseDistance * p;
            opacity = Mathf.Lerp(opacity, 0f, p);
        }
    }

    private void DrawDust(float now, float h4Width, float h4Border, float h4Opacity,
        float a, float c, float d, float e, float f)
    {
        if (_dust == null) return;

        float t = now - _fourthHitStart;
        if (_fourthHitAnimation != EFourthHitAnimation.Grow || t < _animation.dustDelay
            || t >= _animation.dustDelay + _animation.dustTime)
        {
            _dust.SetFilter(1f, 0f);
            return;
        }

        float p = CHudEase.EaseOut((t - _animation.dustDelay) / _animation.dustTime);
        float width = Mathf.Max(0f, h4Width - 2f * h4Border) + 18f;
        float px = h4Border - 9f;
        float py = _layout.cellHeight + 4f - h4Border;
        float sx = Mathf.Lerp(0.3f, 1.4f, p);
        float ca = sx;
        float cc = -sx * _SKEW_TAN;
        float ce = px + width * 0.5f - (ca * width * 0.5f + cc * 0.5f);
        float cf = py;
        float ra = a * ca;
        float rc = a * cc + c;
        float rd = d;
        float re = a * ce + c * cf + e;
        float rf = d * cf + f;
        _dust.SetFill(CHudComboRailShape.EFill.Horizontal, 3, _CLEAR, _colors.amber, _CLEAR, _CLEAR, 0, 1);
        _dust.SetShadows(0f, _CLEAR, 0f, _CLEAR);
        _dust.SetBorder(0f, _CLEAR);
        _dust.SetFilter(1f, (1f - p) * h4Opacity);
        _dust.SetGeometry(new Vector2(width, 1f), ra, 0f, rc, rd, re, rf);
    }

    private void DrawTrack(float now)
    {
        _TRACK_ALPHA.Set(IsOpen ? 1f : 0f, now, _animation.openTime, 0.25f, 0.1f, 0.25f, 1f);
        if (_track != null) _track.alpha = _TRACK_ALPHA.Get(now, _animation.openTime, 0.25f, 0.1f, 0.25f, 1f);

        if (_trackBack != null)
        {
            _trackBack.SetSolid(_colors.track);
            _trackBack.SetGeometry(new Vector2(SnapX(_layout.width) - SnapX(0f), _layout.trackHeight),
                1f, 0f, 0f, 1f, SnapX(0f), SnapY(_layout.trackTop));
        }

        if (_divider != null)
        {
            _divider.SetSolid(_colors.divider);
            _divider.SetGeometry(new Vector2(1f, 13f), 1f, 0f, 0f, 1f,
                SnapX(_layout.width * 0.5f), SnapY(_layout.trackTop - 4f));
        }

        if (_wBar != null)
        {
            if (_isUlt)
            {
                _wBar.SetSolid(_colors.amber);
                _wBar.SetShadows(8f, _colors.amber, 0f, _CLEAR);
            }
            else
            {
                _wBar.SetFill(CHudComboRailShape.EFill.Horizontal, 4, _FLOW[0], _FLOW[1], _FLOW[2], _FLOW[3], 0, 1);
                _wBar.SetShadows(8f, _colors.wGlow, 0f, _CLEAR);
            }

            _wBar.SetGeometry(new Vector2(SnapX(_layout.width * WRatio) - SnapX(0f), _layout.trackHeight),
                1f, 0f, 0f, 1f, SnapX(0f), SnapY(_layout.trackTop));
        }

        if (_qBar != null)
        {
            if (_isUlt)
            {
                _qBar.SetSolid(_colors.qUlt);
                _qBar.SetShadows(8f, _colors.qUltGlow, 0f, _CLEAR);
            }
            else
            {
                _qBar.SetFill(CHudComboRailShape.EFill.Horizontal, 2,
                    _colors.white, _colors.cream, _colors.cream, _colors.cream, 0, 1);
                _qBar.SetShadows(7f, _colors.qGlow, 0f, _CLEAR);
            }

            _qBar.SetGeometry(new Vector2(SnapX(_layout.width * QRatio) - SnapX(0f), _layout.trackHeight),
                1f, 0f, 0f, 1f, SnapX(0f), SnapY(_layout.trackTop));
        }
        float baseline = SnapY(_layout.trackTop + _layout.letterTop) + _layout.letterBaseline;
        PlaceLetter(_qLetter, SnapX(0f), baseline);
        PlaceLetter(_wLetter, SnapX(_layout.wLetterX), baseline);
        Color q = _isUlt ? _colors.white : _colors.qText;
        if (_qLetter != null && _qLetter.color != q) _qLetter.color = q;
        _W_ALPHA.Set(IsWReady ? 1f : 0f, now, _animation.openTime, 0.25f, 0.1f, 0.25f, 1f);
        float wa = _W_ALPHA.Get(now, _animation.openTime, 0.25f, 0.1f, 0.25f, 1f);
        if (_wLetter != null)
        {
            if (_wLetter.color != _colors.amber) _wLetter.color = _colors.amber;
            if (!Mathf.Approximately(_wLetter.canvasRenderer.GetAlpha(), wa)) _wLetter.canvasRenderer.SetAlpha(wa);
        }

        SetGlow(_wGlow, ref _wGlowImage, !_isUlt, wa);
        SetGlow(_wShadow, ref _wShadowImage, _isUlt, wa);
    }

    private static void SetGlow(GameObject glow, ref RawImage image, bool isActive, float alpha)
    {
        if (glow == null) return;

        if (glow.activeSelf != isActive) glow.SetActive(isActive);
        if (!isActive) return;

        if (image == null) image = glow.GetComponent<RawImage>();
        if (image != null && !Mathf.Approximately(image.canvasRenderer.GetAlpha(), alpha))
        {
            image.canvasRenderer.SetAlpha(alpha);
        }
    }

    private static void PlaceLetter(TMP_Text letter, float x, float baseline)
    {
        if (letter == null) return;

        Vector2 p = new Vector2(x, -baseline);
        if (letter.rectTransform.anchoredPosition != p) letter.rectTransform.anchoredPosition = p;
    }

    private void UpdateOrigin()
    {
        if (_rect == null) _rect = (RectTransform)transform;
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
        Canvas root = _canvas != null ? _canvas.rootCanvas : null;
        _scale = root != null && root.renderMode == RenderMode.ScreenSpaceOverlay
            ? Mathf.Max(0.01f, root.scaleFactor) : 0f;
        Vector3 w = _rect.position;
        _origin = new Vector2(w.x, Screen.height - w.y);
    }

    private float SnapX(float x)
    {
        if (_scale <= 0f) return x;

        float px = _origin.x + x * _scale;
        return (Mathf.Floor(px + 0.5f) - _origin.x) / _scale;
    }

    private float SnapY(float y)
    {
        if (_scale <= 0f) return y;

        float px = _origin.y + y * _scale;
        return (Mathf.Floor(px + 0.5f) - _origin.y) / _scale;
    }

    private static Color PremulLerp(Color a, Color b, float t)
    {
        float alpha = Mathf.Lerp(a.a, b.a, t);
        if (alpha <= 1e-5f) return new Color(0f, 0f, 0f, 0f);

        float r = Mathf.Lerp(a.r * a.a, b.r * b.a, t) / alpha;
        float g = Mathf.Lerp(a.g * a.a, b.g * b.a, t) / alpha;
        float bl = Mathf.Lerp(a.b * a.a, b.b * b.a, t) / alpha;
        return new Color(r, g, bl, alpha);
    }

    private static Color CreateColor(int r, int g, int b, float a = 1f)
    {
        return new Color(r / 255f, g / 255f, b / 255f, a);
    }
}
