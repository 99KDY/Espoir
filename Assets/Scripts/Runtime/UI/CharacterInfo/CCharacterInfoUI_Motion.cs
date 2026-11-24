using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class CCharacterInfoUI
{
    [Serializable]
    private struct SReveal
    {
        public CanvasGroup group;
        public RectTransform rect;
        [NonSerialized] public Vector2 basePosition;
    }

    private static readonly Vector4 _CURVE_SETTLE = new Vector4(0.2f, 0.7f, 0.2f, 1f);
    private static readonly Vector4 _CURVE_PILL = new Vector4(0.2f, 0.8f, 0.2f, 1f);
    private static readonly Vector4 _CURVE_SWEEP = new Vector4(0.3f, 0.5f, 0.3f, 1f);
    private const float _NEVER = float.NegativeInfinity;

    [Header("내용")]
    [SerializeField] private RectTransform _page;
    [SerializeField] private CanvasGroup _pageGroup;
    [SerializeField] private GameObject[] _pages = new GameObject[_PAGE_COUNT];
    [Tooltip("정보 탭 표시 순서")]
    [SerializeField] private SReveal[] _infoItems = new SReveal[0];
    [Tooltip("능력치 탭의 칸")]
    [SerializeField] private SReveal[] _statItems = new SReveal[0];
    [Tooltip("기록 탭 표시 순서")]
    [SerializeField] private SReveal[] _recordItems = new SReveal[0];
    [Tooltip("기록 탭의 군주 줄")]
    [SerializeField] private SReveal[] _bossItems = new SReveal[0];

    private float _openTime;
    private float _fillTime;
    private float _fillOffset;
    private bool _isFillPending;
    private bool _isFirstFill;
    private int _shownPage = -1;
    private float _numberTime = _NEVER;
    private float _sweepTime = _NEVER;
    private float _nameSwapTime = _NEVER;
    private bool _isHelperIdentity;
    private bool _isNameStaggering;
    private float _appliedTheme = -1f;
    private SCssTween _pageAlpha;
    private SCssTween _pillY;
    private SCssTween _numberAlpha;
    private SCssTween _loadingAlpha;
    private SCssTween _theme;
    private readonly SCssTween[] _TAB_ON = new SCssTween[_TAB_COUNT];
    private Vector2 _pointer;
    private Vector2 _sideBase;
    private Vector2 _numberBase;
    private Vector2 _nameBase;
    private Vector2 _pageBase;

    private void CacheBasePositions()
    {
        _sideBase = _side.anchoredPosition;
        _numberBase = _numberRect.anchoredPosition;
        _nameBase = _nameRect.anchoredPosition;
        _pageBase = _page.anchoredPosition;
        CacheBasePositions(_infoItems);
        CacheBasePositions(_statItems);
        CacheBasePositions(_recordItems);
        CacheBasePositions(_bossItems);
    }

    private static void CacheBasePositions(SReveal[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            items[i].basePosition = items[i].rect.anchoredPosition;
        }
    }

    private void BeginOpen(float now)
    {
        _openTime = now;
        _isFirstFill = true;
        _isFillPending = false;
        _numberTime = now;
        _sweepTime = _NEVER;
        _pointer = Vector2.zero;
        _pageAlpha.Snap(1f);
        _pillY.Snap(_tabs[_tab].rect.anchoredPosition.y);
        _numberAlpha.Snap(1f);
        _loadingAlpha.Snap(0f);

        for (int i = 0; i < _TAB_COUNT; i++)
        {
            _TAB_ON[i].Snap(i == _tab ? 1f : 0f);
        }

        SetIdentity(false, now, true);
        _isNameStaggering = true;
        WriteTabHeading();
        ShowPage(now);
        RefreshValues(now);
        Paint(now);
        // 텍스트 발광의 재생성을 막기 위해 Canvas 갱신 전에 메시를 준비한다.
        if (_tabs[_tab].glow != null) _tabs[_tab].label.ForceMeshUpdate();
    }

    private void BeginTabChange(int previous, float now)
    {
        _numberTime = now;
        _sweepTime = now;
        _pillY.To(_tabs[_tab].rect.anchoredPosition.y, now, _settings.PillTime, 0f, _CURVE_PILL);
        _pageAlpha.To(0f, now, _settings.PageFadeTime, 0f, CCssEase.EASE);
        _isFillPending = true;
        _fillTime = now + _settings.PageSwapDelay;
        WriteTabHeading();

        if (previous == _HELPER_TAB) SetIdentity(false, now, false);
    }

    private void UpdatePage(float now)
    {
        if (!_isFillPending || now < _fillTime) return;

        _isFillPending = false;
        _isFirstFill = false;
        ShowPage(now);
        _pageAlpha.To(_tab == _HELPER_TAB ? 0f : 1f, now, _settings.PageFadeTime, 0f, CCssEase.EASE);
    }

    private void ShowPage(float now)
    {
        _shownPage = _tab;
        _fillTime = now;
        _fillOffset = _isFirstFill ? _settings.ContentDelay + _settings.FirstPageDelay : 0f;

        for (int i = 0; i < _pages.Length; i++)
        {
            _pages[i].SetActive(i == _shownPage);
        }

        ResetShownValues();
    }

    private void WriteTabHeading()
    {
        _BUFFER[0] = '0';
        _BUFFER[1] = (char)('1' + _tab);
        _numberText.SetCharArray(_BUFFER, 0, 2);
        _captionText.text = _settings.GetTabCaption(_tab);
        RectTransform caption = _captionText.rectTransform;
        caption.anchoredPosition = new Vector2(
            _numberBase.x + CHudNumberFormat.CssWidth(_numberText, _BUFFER, 2) + _captionGap,
            caption.anchoredPosition.y);
    }

    private void SetIdentity(bool isHelper, float now, bool isInstant)
    {
        if (!isInstant && _isHelperIdentity == isHelper) return;

        _isHelperIdentity = isHelper;
        CCharacterInfoSettingsSO.SIdentity identity = isHelper ? _settings.Helper : _settings.Player;
        CCharacterInfoSettingsSO.STheme theme = isHelper ? _settings.HelperTheme : _settings.PlayerTheme;
        _titleText.text = identity.title;
        _nameText.text = identity.name;
        _latinText.text = identity.latinName;
        _nameText.fontSharedMaterial = isHelper ? _nameHelperMaterial : _nameMaterial;
        _numberText.fontSharedMaterial = isHelper ? _numberHelperMaterial : _numberMaterial;
        if (_nameHelperGlow != null)
        {
            _nameHelperGlow.SetActive(isHelper);
            if (isHelper) _nameText.ForceMeshUpdate();
        }

        Color pillEnd = theme.pill;
        pillEnd.a = 0f;
        _pillBox.SetGradient(theme.pill, pillEnd, 90f);
        _isNameStaggering = false;

        if (isInstant)
        {
            _theme.Snap(isHelper ? 1f : 0f);
            _nameSwapTime = _NEVER;
        }
        else
        {
            _theme.To(isHelper ? 1f : 0f, now, _settings.ThemeTime, 0f, CCssEase.EASE);
            _nameSwapTime = now;
        }
    }

    private void Paint(float now)
    {
        float elapsed = now - _openTime;
        PaintFrame(elapsed);
        PaintSide(now, elapsed);
        PaintTabs(now);
        PaintPage(now, elapsed);
        PaintSweep(now);
        if (_isNameStaggering) PaintNameLetters(elapsed);
    }

    private void PaintFrame(float elapsed)
    {
        float fade = CCssEase.Evaluate(CCssEase.EASE, Progress(elapsed, 0f, _settings.FadeTime));
        _playerShade.alpha = _isHelperIdentity ? 0f : fade;
        _helperShade.alpha = _isHelperIdentity ? fade : 0f;

        _letterbox.alpha = CCssEase.Evaluate(CCssEase.EASE, Progress(elapsed, 0f, _settings.LetterboxFadeTime));
        float slide = 1f - CCssEase.Evaluate(_CURVE_SETTLE,
            Progress(elapsed, _settings.LetterboxDelay, _settings.LetterboxTime));
        _letterboxTop.anchoredPosition = new Vector2(0f, slide * _letterboxTop.rect.height);
        _letterboxBottom.anchoredPosition = new Vector2(0f, -slide * _letterboxBottom.rect.height);

        _overlay.alpha = CCssEase.Evaluate(CCssEase.EASE,
            Progress(elapsed, _settings.FadeDelay, _settings.FadeTime));
        _closeBox.Fill = _closeArea.IsHovered ? _settings.CloseHoverFill : _settings.CloseFill;

        bool isLoading = _tab == _HELPER_TAB && _stage != null && !_stage.IsHelperReady;
        float now = _openTime + elapsed;
        _loadingAlpha.To(isLoading ? _settings.LoadingAlpha : 0f, now, _settings.LoadingFadeTime, 0f,
            CCssEase.EASE);
        _loading.alpha = _loadingAlpha.Value(now);
    }

    private void PaintSide(float now, float elapsed)
    {
        Mouse mouse = Mouse.current;
        Vector2 target = Vector2.zero;
        if (mouse != null && Screen.width > 0 && Screen.height > 0)
        {
            Vector2 position = mouse.position.ReadValue();
            target = new Vector2(Mathf.Clamp(position.x / Screen.width * 2f - 1f, -1f, 1f),
                Mathf.Clamp(position.y / Screen.height * 2f - 1f, -1f, 1f));
        }

        float follow = 1f - Mathf.Pow(1f - _settings.ParallaxFollow, Time.unscaledDeltaTime * 60f);
        _pointer += (target - _pointer) * follow;

        float sideIn = CCssEase.Evaluate(_CURVE_SETTLE,
            Progress(elapsed, _settings.ContentDelay, _settings.SideInTime));
        _sideGroup.alpha = sideIn;
        _side.anchoredPosition = _sideBase + new Vector2(-(1f - sideIn) * _settings.SideInDistance, 0f) +
            Vector2.Scale(_pointer, _settings.Parallax);

        bool isHeadingHidden = _tab == _HELPER_TAB && !_isHelperIdentity;
        _numberAlpha.To(isHeadingHidden ? 0f : 1f, now, _settings.PageFadeTime, 0f, CCssEase.EASE);
        _numberGroup.alpha = _numberAlpha.Value(now);
        float numberIn = CCssEase.Evaluate(_CURVE_SETTLE, Progress(now, _numberTime, _settings.NumberInTime));
        _numberText.alpha = numberIn;
        _numberRect.anchoredPosition = _numberBase +
            new Vector2(-(1f - numberIn) * _settings.NumberInDistance, 0f);

        float nameIn = CCssEase.Evaluate(_CURVE_SETTLE, Progress(now, _nameSwapTime, _settings.NameSwapTime));
        _nameGroup.alpha = nameIn;
        _nameRect.anchoredPosition = _nameBase + new Vector2(-(1f - nameIn) * _settings.NameSwapDistance, 0f);

        float theme = _theme.Value(now);
        if (!Mathf.Approximately(theme, _appliedTheme)) ApplyTheme(theme);
    }

    private void ApplyTheme(float theme)
    {
        _appliedTheme = theme;
        CCharacterInfoSettingsSO.STheme from = _settings.PlayerTheme;
        CCharacterInfoSettingsSO.STheme to = _settings.HelperTheme;
        _nameText.color = Color.Lerp(from.cream, to.cream, theme);
        _titleText.color = Color.Lerp(from.soft, to.soft, theme);
        _hint.color = Color.Lerp(from.hint, to.hint, theme);

        Color accent = Color.Lerp(from.accent, to.accent, theme);
        SetColorKeepAlpha(_latinText, accent);
        SetColorKeepAlpha(_captionText, accent);
        for (int i = 0; i < _tabs.Length; i++)
        {
            SetColorKeepAlpha(_tabs[i].number, accent);
        }
    }

    private void PaintTabs(float now)
    {
        float theme = _theme.Value(now);
        Color idle = Color.Lerp(_settings.PlayerTheme.tabIdle, _settings.HelperTheme.tabIdle, theme);
        Color cream = Color.Lerp(_settings.PlayerTheme.cream, _settings.HelperTheme.cream, theme);

        for (int i = 0; i < _tabs.Length; i++)
        {
            bool isOn = i == _tab;
            bool isLit = isOn || (_tabs[i].area != null && _tabs[i].area.IsHovered);
            _TAB_ON[i].To(isLit ? 1f : 0f, now, _settings.TabColorTime, 0f, CCssEase.EASE);
            Color color = Color.Lerp(idle, cream, _TAB_ON[i].Value(now));
            bool hasChanged = _tabs[i].label.color != color;
            _tabs[i].label.color = color;
            if (_tabs[i].glow == null) continue;

            if (_tabs[i].glow.activeSelf != isOn)
            {
                _tabs[i].glow.SetActive(isOn);
                hasChanged = true;
            }

            if (isOn && hasChanged) _tabs[i].label.ForceMeshUpdate();
        }

        Vector2 pill = _pill.anchoredPosition;
        pill.y = _pillY.Value(now);
        _pill.anchoredPosition = pill;
    }

    private void PaintPage(float now, float elapsed)
    {
        float firstIn = CCssEase.Evaluate(_CURVE_SETTLE, Progress(elapsed,
            _settings.ContentDelay + _settings.FirstPageDelay, _settings.FirstPageTime));
        _pageGroup.alpha = _pageAlpha.Value(now) * firstIn;
        float shift = -(1f - firstIn) * _settings.SideInDistance;
        if (_isFillPending) shift += _settings.PageSlideDistance;
        _page.anchoredPosition = _pageBase + new Vector2(shift, 0f);

        float sinceFill = now - _fillTime;
        switch (_shownPage)
        {
            case 0:
                PaintReveals(_infoItems, sinceFill, _settings.ItemInTime, _settings.ItemStagger,
                    _settings.ItemInDistance);
                PaintBars(sinceFill);
                break;
            case 1:
                PaintReveals(_statItems, sinceFill, _settings.StatInTime, _settings.StatStagger,
                    _settings.StatInDistance);
                break;
            case 2:
                PaintReveals(_recordItems, sinceFill, _settings.ItemInTime, _settings.ItemStagger,
                    _settings.ItemInDistance);
                PaintReveals(_bossItems, sinceFill, _settings.ItemInTime, _settings.ItemStagger,
                    _settings.ItemInDistance);
                break;
        }
    }

    private static void PaintReveals(SReveal[] items, float sinceFill, float time, float stagger, float distance)
    {
        for (int i = 0; i < items.Length; i++)
        {
            float k = CCssEase.Evaluate(_CURVE_SETTLE, Progress(sinceFill, i * stagger, time));
            items[i].group.alpha = k;
            items[i].rect.anchoredPosition = items[i].basePosition - new Vector2(0f, (1f - k) * distance);
        }
    }

    private void PaintBars(float sinceFill)
    {
        float grow = CCssEase.Evaluate(_CURVE_SETTLE,
            Progress(sinceFill, _settings.BarFillDelay + _fillOffset, _settings.BarFillTime));
        float shineTime = sinceFill - _settings.BarShineDelay;
        float cycle = shineTime < 0f ? 1f : Mathf.Repeat(shineTime, _settings.BarShinePeriod) /
            _settings.BarShinePeriod;
        float travel = CCssEase.Evaluate(CCssEase.EASE_IN_OUT, Mathf.Clamp01(cycle / _settings.BarShineMoveRatio));

        for (int i = 0; i < _bars.Length; i++)
        {
            float width = _bars[i].track.rect.width * _bars[i].ratio * grow;
            _bars[i].fill.sizeDelta = new Vector2(width, _bars[i].fill.sizeDelta.y);
            _bars[i].shine.sizeDelta = new Vector2(width * _settings.BarShineWidthRatio, _bars[i].shine.sizeDelta.y);
            _bars[i].shine.anchoredPosition =
                new Vector2(width * Mathf.LerpUnclamped(
                    _settings.BarShineRange.x, _settings.BarShineRange.y, travel), 0f);
        }
    }

    private void PaintSweep(float now)
    {
        float k = Progress(now, _sweepTime, _settings.SweepTime);
        if (_sweepTime <= _NEVER || k >= 1f)
        {
            _sweepGroup.alpha = 0f;
            return;
        }

        float alpha = k < _settings.SweepPeak
            ? CCssEase.Evaluate(_CURVE_SWEEP, k / _settings.SweepPeak)
            : 1f - CCssEase.Evaluate(_CURVE_SWEEP, (k - _settings.SweepPeak) / (1f - _settings.SweepPeak));
        _sweepGroup.alpha = alpha;
        Vector2 position = _sweep.anchoredPosition;
        position.x = Mathf.LerpUnclamped(_sweepRange.x, _sweepRange.y, CCssEase.Evaluate(_CURVE_SWEEP, k));
        _sweep.anchoredPosition = position;
    }

    private void PaintNameLetters(float elapsed)
    {
        _nameText.ForceMeshUpdate();
        TMP_TextInfo info = _nameText.textInfo;
        float start = _settings.ContentDelay + _settings.NameLetterDelay;
        bool isDone = true;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo character = info.characterInfo[i];
            if (!character.isVisible) continue;

            float k = CCssEase.Evaluate(_CURVE_SETTLE,
                Progress(elapsed, start + i * _settings.NameLetterStagger, _settings.NameLetterTime));
            if (k >= 1f) continue;

            isDone = false;
            TMP_MeshInfo mesh = info.meshInfo[character.materialReferenceIndex];
            float drop = (1f - k) * _settings.NameLetterRise;
            for (int v = character.vertexIndex; v < character.vertexIndex + 4; v++)
            {
                mesh.vertices[v].y -= drop;
                mesh.colors32[v].a = (byte)(mesh.colors32[v].a * k);
            }
        }

        if (isDone)
        {
            _isNameStaggering = false;
            return;
        }

        _nameText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }

    private static float Progress(float value, float start, float duration)
    {
        if (duration <= 0f) return value >= start ? 1f : 0f;

        return Mathf.Clamp01((value - start) / duration);
    }

    private static void SetColorKeepAlpha(TMP_Text text, Color color)
    {
        color.a = text.color.a;
        text.color = color;
    }
}
