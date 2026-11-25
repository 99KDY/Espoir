using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum ETutorialKeyIcon
{
    Key = 0,
    LeftMouse = 1,
    RightMouse = 2,
    Wheel = 3,
}

public struct STutorialKeyStep
{
    public string key;
    public ETutorialKeyIcon icon;
    public string plusKey;
    public ETutorialKeyIcon plusIcon;
    public string altKey;
    public string caption;
    public bool isDone;
    public bool isNow;
}

/// <summary>
/// 튜토리얼 입력 안내와 자막, HUD 슬롯 강조를 표시합니다.
/// </summary>
public class CTutorialGuideView : MonoBehaviour
{
    private class CCap
    {
        public RectTransform root;
        public CHudBox box;
        public CHudBox ring;
        public RectTransform content;
    }

    private class CStep
    {
        public readonly List<CCap> caps = new List<CCap>();
        public bool isCurrent;
        public bool isNow;
    }

    [Header("키 안내")]
    [SerializeField] private RectTransform _keyRoot;
    [SerializeField] private CanvasGroup _keyGroup;
    [SerializeField] private GameObject _stepTemplate;
    [SerializeField] private GameObject _capTemplate;
    [SerializeField] private GameObject _mouseTemplate;
    [SerializeField] private GameObject _rightMouseTemplate;
    [SerializeField] private GameObject _wheelTemplate;
    [SerializeField] private GameObject _joinTemplate;
    [SerializeField] private GameObject _arrowTemplate;

    [Header("자막")]
    [SerializeField] private CanvasGroup _subtitleGroup;
    [SerializeField] private RectTransform _subtitleRoot;
    [SerializeField] private TMP_Text _subtitleName;
    [SerializeField] private TMP_Text _subtitleText;
    [SerializeField, Min(1f)] private float _typingSpeed = 30f;

    [Header("알림")]
    [SerializeField] private CanvasGroup _toastGroup;
    [SerializeField] private RectTransform _toastRoot;
    [SerializeField] private TMP_Text _toastText;
    [SerializeField, Min(0.1f)] private float _toastTime = 2.6f;

    [Header("강조")]
    [SerializeField] private CHudBox _highlightTemplate;
    [SerializeField] private CHudBox _highlightRoundTemplate;

    private static readonly Color _CAP_FILL = new Color(14f / 255f, 14f / 255f, 26f / 255f, 0.55f);
    private static readonly Color _CAP_FILL_NOW = new Color(120f / 255f, 70f / 255f, 30f / 255f, 0.55f);
    private static readonly Color _CAP_BORDER = new Color(1f, 240f / 255f, 222f / 255f, 0.8f);
    private static readonly Color _CAP_BORDER_NOW = new Color(1f, 217f / 255f, 160f / 255f);
    private static readonly Color _CAPTION = new Color(1f, 240f / 255f, 220f / 255f);
    private static readonly Color _CAPTION_NOW = new Color(1f, 217f / 255f, 160f / 255f);
    private static readonly Color _AMBER = new Color(1f, 192f / 255f, 112f / 255f);
    private const float _PRESS_PERIOD = 1.3f;
    private const float _PRESS_PERIOD_NOW = 0.8f;
    private const float _HIGHLIGHT_PERIOD = 1.6f;

    private readonly List<CStep> _STEPS = new List<CStep>();
    private readonly List<GameObject> _PARTS = new List<GameObject>();
    private readonly List<STutorialKeyStep> _SHOWN_KEYS = new List<STutorialKeyStep>();
    private int _currentKey = -1;
    private bool _isKeyOn;
    private float _keyAlpha;
    private float _keyShownAt;
    private string _subtitle;
    private float _subtitleTime;
    private float _subtitleDuration;
    private int _subtitleShown = -1;
    private float _subtitleAlpha;
    private float _toastAt = -99f;
    private bool _isHidden;
    private CHudBox _highlight;
    private CHudBox _highlightRound;
    private CHudBox _highlightActive;
    private RectTransform _highlightTarget;
    private RectTransform _avoid;
    private float _keyLift;
    private static readonly Vector3[] _CORNERS = new Vector3[4];

    private const float _AVOID_GAP = 14f;

    /// <summary>
    /// 지금 자막이 떠 있거나 타이핑 중이면 true.
    /// </summary>
    public bool IsSubtitleOn => _subtitle != null;

    private void Awake()
    {
        _stepTemplate.SetActive(false);
        _capTemplate.SetActive(false);
        _mouseTemplate.SetActive(false);
        _rightMouseTemplate.SetActive(false);
        _wheelTemplate.SetActive(false);
        _joinTemplate.SetActive(false);
        _arrowTemplate.SetActive(false);
        _keyGroup.alpha = 0f;
        _subtitleGroup.alpha = 0f;
        _toastGroup.alpha = 0f;
    }

    /// <summary>
    /// 안내 표시를 일시적으로 숨깁니다.
    /// </summary>
    public void SetHidden(bool isHidden)
    {
        _isHidden = isHidden;
    }

    /// <summary>
    /// 키 순서를 보여 줍니다. 빈 목록이면 숨깁니다. 같은 내용이면 다시 만들지 않습니다.
    /// </summary>
    public void ShowKeys(IReadOnlyList<STutorialKeyStep> steps, int currentIndex)
    {
        if (currentIndex == _currentKey && HasSameKeys(steps)) return;

        bool wasEmpty = _SHOWN_KEYS.Count == 0;
        _currentKey = currentIndex;
        _SHOWN_KEYS.Clear();
        for (int i = 0; i < steps.Count; i++) _SHOWN_KEYS.Add(steps[i]);
        _isKeyOn = steps.Count > 0;
        if (!_isKeyOn) return;

        BuildSteps(steps, currentIndex);
        if (wasEmpty) _keyShownAt = Time.unscaledTime;
    }

    /// <summary>
    /// 화자 이름과 자막을 표시합니다.
    /// </summary>
    public void Say(string speaker, string text)
    {
        _subtitle = text ?? string.Empty;
        _subtitleTime = 0f;
        _subtitleDuration = Mathf.Max(2.6f, 1.2f + _subtitle.Length * 0.075f);
        _subtitleShown = -1;
        if (_subtitleName.text != speaker) _subtitleName.text = speaker;
    }

    /// <summary>
    /// 자막을 바로 지웁니다.
    /// </summary>
    public void ClearSubtitle()
    {
        _subtitle = null;
    }

    /// <summary>
    /// 실패 이유 같은 알림을 잠깐 띄웁니다.
    /// </summary>
    public void Toast(string text)
    {
        _toastText.text = text;
        _toastAt = Time.unscaledTime;
    }

    /// <summary>
    /// 지정한 HUD 슬롯을 강조합니다. null이면 강조를 해제합니다.
    /// </summary>
    public void Highlight(RectTransform slot)
    {
        if (slot == _highlightTarget) return;

        _highlightTarget = slot;
        if (_highlight != null) _highlight.gameObject.SetActive(false);
        if (_highlightRound != null) _highlightRound.gameObject.SetActive(false);
        _highlightActive = null;
        if (slot == null) return;

        RectTransform frame = slot.Find("Box") as RectTransform;
        CHudBox frameBox = frame != null ? frame.GetComponent<CHudBox>() : null;
        bool isRound = frameBox != null && frameBox.IsCircle;
        CHudBox template = isRound ? _highlightRoundTemplate : _highlightTemplate;
        if (template == null) return;

        CHudBox glow = isRound ? _highlightRound : _highlight;
        if (glow == null)
        {
            glow = Instantiate(template);
            glow.name = "TutorialHighlight";
            glow.raycastTarget = false;
            if (isRound) _highlightRound = glow;
            else _highlight = glow;
        }

        CHudBox source = frameBox != null ? frameBox : slot.GetComponentInChildren<CHudBox>(true);
        glow.material = source != null ? source.material : template.material;

        RectTransform rect = glow.rectTransform;
        rect.SetParent(slot, false);
        if (frame != null)
        {
            rect.anchorMin = frame.anchorMin;
            rect.anchorMax = frame.anchorMax;
            rect.pivot = frame.pivot;
            rect.anchoredPosition = frame.anchoredPosition;
            rect.sizeDelta = frame.sizeDelta;
        }
        else
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        rect.SetAsLastSibling();
        glow.gameObject.SetActive(true);
        _highlightActive = glow;
    }

    /// <summary>
    /// 입력 안내와 겹치지 않게 배치할 창을 지정합니다.
    /// </summary>
    public void AvoidRect(RectTransform rect)
    {
        _avoid = rect;
    }

    private float GetAvoidLift()
    {
        if (_avoid == null || !_avoid.gameObject.activeInHierarchy || !_isKeyOn) return 0f;

        RectTransform parent = (RectTransform)_keyRoot.parent;
        Bounds keys = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, _keyRoot);
        float keyBottom = keys.min.y - _keyRoot.anchoredPosition.y;

        _avoid.GetWorldCorners(_CORNERS);
        Canvas avoidCanvas = _avoid.GetComponentInParent<Canvas>();
        Camera avoidCamera = avoidCanvas != null && avoidCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? avoidCanvas.worldCamera : null;
        Vector2 screenTop = RectTransformUtility.WorldToScreenPoint(avoidCamera, _CORNERS[1]);
        Vector2 screenBottom = RectTransformUtility.WorldToScreenPoint(avoidCamera, _CORNERS[0]);
        Canvas keyCanvas = _keyRoot.GetComponentInParent<Canvas>();
        Camera keyCamera = keyCanvas != null && keyCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? keyCanvas.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenTop, keyCamera, out Vector2 top) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, screenBottom, keyCamera, out Vector2 bottom))
        {
            return 0f;
        }

        float keyTop = keys.max.y - _keyRoot.anchoredPosition.y;
        if (keyBottom >= top.y + _AVOID_GAP || keyTop <= bottom.y - _AVOID_GAP) return 0f;

        RectTransform screen = keyCanvas != null ? (RectTransform)keyCanvas.rootCanvas.transform : parent;
        screen.GetWorldCorners(_CORNERS);
        float canvasBottom = parent.InverseTransformPoint(_CORNERS[0]).y;
        float canvasTop = parent.InverseTransformPoint(_CORNERS[1]).y;

        float up = top.y + _AVOID_GAP - keyBottom;
        float maxUp = canvasTop - _AVOID_GAP - keyTop;
        if (up <= maxUp) return up;

        float down = bottom.y - _AVOID_GAP - keyTop;
        float maxDown = canvasBottom + _AVOID_GAP - keyBottom;
        if (down >= maxDown) return down;

        return Mathf.Max(0f, maxUp);
    }

    private void OnDestroy()
    {
        if (_highlight != null) Destroy(_highlight.gameObject);
        if (_highlightRound != null) Destroy(_highlightRound.gameObject);
    }

    private void LateUpdate()
    {
        float now = Time.unscaledTime;
        float dt = Time.unscaledDeltaTime;

        UpdateSubtitle(dt);
        UpdateToast(now);

        _keyAlpha = Mathf.MoveTowards(_keyAlpha, _isKeyOn && !_isHidden ? 1f : 0f, dt / 0.3f);
        _keyGroup.alpha = _keyAlpha;
        float rise = CHudEase.Ease(Mathf.Clamp01((now - _keyShownAt) / 0.3f));
        _keyLift = Mathf.Lerp(_keyLift, GetAvoidLift(), 1f - Mathf.Exp(-dt * 18f));
        _keyRoot.anchoredPosition = new Vector2(0f, -8f * (1f - rise) + _keyLift);
        if (_keyAlpha > 0f) AnimateKeys(now);

        if (_highlightActive != null && _highlightActive.gameObject.activeSelf)
        {
            float pulse = 0.5f - 0.5f * Mathf.Cos(now / _HIGHLIGHT_PERIOD * Mathf.PI * 2f);
            float alpha = _isHidden ? 0f : 1f;
            Color shadow = _AMBER;
            shadow.a = (0.25f + 0.7f * pulse) * alpha;
            _highlightActive.SetShadow(4f + 12f * pulse, shadow);
            _highlightActive.SetInset(10f * pulse, new Color(_AMBER.r, _AMBER.g, _AMBER.b, 0.35f * pulse * alpha));
        }
    }

    private void UpdateSubtitle(float dt)
    {
        bool isOn = _subtitle != null && !_isHidden;
        _subtitleAlpha = Mathf.MoveTowards(_subtitleAlpha, isOn ? 1f : 0f, dt / 0.25f);
        _subtitleGroup.alpha = _subtitleAlpha;
        _subtitleRoot.anchoredPosition = new Vector2(0f, -6f * (1f - _subtitleAlpha));
        if (_subtitle == null || _isHidden) return;

        _subtitleTime += dt;
        int count = Mathf.Min(_subtitle.Length, Mathf.FloorToInt(_subtitleTime * _typingSpeed));
        if (count != _subtitleShown)
        {
            _subtitleShown = count;
            _subtitleText.text = _subtitle.Substring(0, count);
        }

        if (_subtitleTime > _subtitleDuration) _subtitle = null;
    }

    private void UpdateToast(float now)
    {
        float age = now - _toastAt;
        float alpha = age < 0.2f ? age / 0.2f : age < _toastTime ? 1f : 1f - (age - _toastTime) / 0.2f;
        alpha = _isHidden ? 0f : Mathf.Clamp01(alpha);
        _toastGroup.alpha = alpha;
        _toastRoot.anchoredPosition = new Vector2(0f, 6f * (1f - Mathf.Clamp01(age / 0.2f)));
    }

    private bool HasSameKeys(IReadOnlyList<STutorialKeyStep> steps)
    {
        if (_SHOWN_KEYS.Count != steps.Count) return false;

        for (int i = 0; i < steps.Count; i++)
        {
            STutorialKeyStep shown = _SHOWN_KEYS[i];
            STutorialKeyStep step = steps[i];
            if (shown.key != step.key || shown.icon != step.icon || shown.plusKey != step.plusKey ||
                shown.plusIcon != step.plusIcon || shown.altKey != step.altKey || shown.caption != step.caption ||
                shown.isDone != step.isDone || shown.isNow != step.isNow) return false;
        }

        return true;
    }

    private void BuildSteps(IReadOnlyList<STutorialKeyStep> steps, int currentIndex)
    {
        foreach (GameObject part in _PARTS)
        {
            Destroy(part);
        }

        _PARTS.Clear();
        _STEPS.Clear();

        for (int i = 0; i < steps.Count; i++)
        {
            STutorialKeyStep data = steps[i];
            if (i > 0) _PARTS.Add(Instantiate(_arrowTemplate, _keyRoot));

            GameObject root = Instantiate(_stepTemplate, _keyRoot);
            _PARTS.Add(root);

            CStep step = new CStep
            {
                isCurrent = !data.isDone && i == currentIndex,
                isNow = !data.isDone && i == currentIndex && data.isNow,
            };

            Transform caps = root.transform.Find("Caps");
            step.caps.Add(MakeCap(caps, data.key, data.icon));
            if (!string.IsNullOrEmpty(data.plusKey))
            {
                MakeJoin(caps, "+");
                step.caps.Add(MakeCap(caps, data.plusKey, data.plusIcon));
            }

            if (!string.IsNullOrEmpty(data.altKey))
            {
                MakeJoin(caps, "/");
                step.caps.Add(MakeCap(caps, data.altKey, ETutorialKeyIcon.Key));
            }

            TMP_Text caption = root.transform.Find("Caption").GetComponent<TMP_Text>();
            caption.text = data.caption;
            caption.color = step.isNow ? _CAPTION_NOW : _CAPTION;
            bool isPast = data.isDone || i < currentIndex;
            root.GetComponent<CanvasGroup>().alpha = step.isCurrent ? 1f : isPast ? 0.3f : 0.45f;

            foreach (CCap cap in step.caps)
            {
                cap.box.BorderColor = step.isNow ? _CAP_BORDER_NOW : _CAP_BORDER;
            }

            _STEPS.Add(step);
        }

        foreach (GameObject part in _PARTS)
        {
            part.SetActive(true);
        }
    }

    private CCap MakeCap(Transform parent, string key, ETutorialKeyIcon icon)
    {
        GameObject template = icon == ETutorialKeyIcon.Key ? _capTemplate
            : icon == ETutorialKeyIcon.Wheel ? _wheelTemplate
            : icon == ETutorialKeyIcon.RightMouse ? _rightMouseTemplate : _mouseTemplate;
        GameObject root = Instantiate(template, parent);
        root.SetActive(true);

        CCap cap = new CCap
        {
            root = (RectTransform)root.transform,
            box = root.GetComponent<CHudBox>(),
            ring = root.transform.Find("Ring").GetComponent<CHudBox>(),
            content = (RectTransform)root.transform.Find("Content"),
        };

        if (icon == ETutorialKeyIcon.Key)
        {
            TMP_Text label = cap.content.GetComponent<TMP_Text>();
            label.text = key;
            if (!string.IsNullOrEmpty(key) && key.Length > 2) label.fontSize = 19f;
        }

        return cap;
    }

    private void MakeJoin(Transform parent, string text)
    {
        GameObject join = Instantiate(_joinTemplate, parent);
        join.SetActive(true);
        join.GetComponent<TMP_Text>().text = text;
    }

    private void AnimateKeys(float now)
    {
        foreach (CStep step in _STEPS)
        {
            if (!step.isCurrent) continue;

            float period = step.isNow ? _PRESS_PERIOD_NOW : _PRESS_PERIOD;
            float t = Mathf.Repeat(now, period) / period;
            float scale;
            float bright;

            if (t < 0.07f)
            {
                float k = CHudEase.EaseIn(t / 0.07f);
                scale = Mathf.Lerp(1f, 0.86f, k);
                bright = Mathf.Lerp(0f, 1f, k);
            }
            else
            {
                float k = Mathf.Clamp01((t - 0.07f) / 0.35f);
                scale = Mathf.LerpUnclamped(0.86f, 1f, CHudEase.Bezier(0.2f, 0.9f, 0.3f, 1.5f, k));
                bright = 1f - CHudEase.EaseOut(Mathf.Clamp01((t - 0.07f) / 0.33f));
            }

            float ringK = Mathf.Clamp01((t - 0.08f) / (step.isNow ? 0.52f : 0.47f));
            float ringE = CHudEase.EaseOut(ringK);
            bool isRingOn = t >= 0.06f;
            float ringSpread = (step.isNow ? 20f : 18f) * ringE;
            Color ringColor = step.isNow ? _CAP_BORDER_NOW : Color.white;
            ringColor.a = isRingOn ? (step.isNow ? 0.95f : 0.9f) * (1f - ringE) : 0f;

            float glow = step.isNow ? 0.5f - 0.5f * Mathf.Cos(now / _PRESS_PERIOD_NOW * Mathf.PI * 2f) : 0f;

            foreach (CCap cap in step.caps)
            {
                cap.root.localScale = Vector3.one * scale;
                cap.content.localScale = Vector3.one * (1f + 0.3f * bright);
                cap.box.Fill = Color.Lerp(Color.Lerp(_CAP_FILL, _CAP_FILL_NOW, glow), Color.white, 0.25f * bright);
                Color shadow = Color.Lerp(new Color(0f, 0f, 0f, 0.45f),
                    new Color(_AMBER.r, _AMBER.g, _AMBER.b, 0.95f), glow);
                cap.box.SetShadow(14f + 8f * glow, shadow, 4f * (1f - glow));
                cap.ring.rectTransform.sizeDelta = Vector2.one * ringSpread * 2f;
                cap.ring.BorderColor = ringColor;
                cap.ring.BorderWidth = Mathf.Lerp(2f, 1f, ringE);
            }
        }
    }
}
