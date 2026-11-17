using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 소환 시 화면 가장자리를 어둡게 하고 왼쪽에 이름판을 표시합니다.
/// 왼쪽 7% · 위 30% 자리에 옅은 베일 → '나태의 군주' → '벨페고르' 가 왼쪽에서 오른쪽으로 걷히며 나타나고, 금선이 오른쪽으로 사라지며 그어짐.
/// 종료 구간에는 이름판을 왼쪽으로 이동시키며 숨깁니다.
/// </summary>
public class CBossSummonTitleView : MonoBehaviour
{
    private static readonly Vector4 _LINE_EASE = new Vector4(0.2f, 0.7f, 0.2f, 1f);

    [SerializeField] private CanvasGroup _vignette;
    [SerializeField] private RectTransform _card;
    [SerializeField] private CanvasGroup _cardGroup;
    [SerializeField] private CanvasGroup _veil;
    [SerializeField] private RectTransform _veilRect;
    [SerializeField] private RectMask2D _titleMask;
    [SerializeField] private RectMask2D _nameMask;
    [SerializeField] private RectTransform _line;
    [SerializeField] private CanvasGroup _lineGroup;

    [Header("웹 값 (초)")]
    [SerializeField] private float _totalTime = 5.8f;
    [SerializeField] private float _vignetteTime = 5.6f;
    [SerializeField] private float _veilDelay = 2f;
    [SerializeField] private float _veilDuration = 0.8f;
    [SerializeField] private float _titleDelay = 2.15f;
    [SerializeField] private float _titleDuration = 1f;
    [SerializeField] private float _nameDelay = 2.4f;
    [SerializeField] private float _nameDuration = 1.3f;
    [SerializeField] private float _lineDelay = 2.8f;
    [SerializeField] private float _lineDuration = 1.2f;
    [SerializeField] private float _outDelay = 5f;
    [SerializeField] private float _outDuration = 0.6f;
    [SerializeField] private float _outOffset = 14f;

    [Header("자리 (화면 비율 · px)")]
    [SerializeField] private float _left = 0.07f;
    [SerializeField] private float _top = 0.3f;

    private float _startTime = -1f;

    /// <summary>
    /// 소환 연출 중 HUD, 이름표와 상호작용 표시를 숨길 때 사용합니다.
    /// </summary>
    public bool IsPlaying => _startTime >= 0f;

    /// <summary>
    /// 지금 재생 시각 (확인용, 재생 중이 아니면 -1).
    /// </summary>
    public float Elapsed => _startTime >= 0f ? Time.unscaledTime - _startTime : -1f;

    private void Awake()
    {
        if (_startTime < 0f) gameObject.SetActive(false);
    }

    /// <summary>
    /// 처음부터 다시 재생합니다.
    /// </summary>
    public void Play()
    {
        _startTime = Time.unscaledTime;
        gameObject.SetActive(true);
        Apply(0f);
    }

    private void Update()
    {
        if (_startTime < 0f) return;

        float t = Time.unscaledTime - _startTime;

        if (t >= _totalTime)
        {
            _startTime = -1f;
            gameObject.SetActive(false);
            return;
        }

        Apply(t);
    }

    private void Apply(float t)
    {
        // sm-vg: 0% 0 → 15% 1 → 80% 1 → 100% 0 (구간마다 ease)
        float v = t / _vignetteTime;
        float vignette;

        if (v < 0.15f) vignette = CCssEase.Evaluate(CCssEase.EASE, v / 0.15f);
        else if (v < 0.8f) vignette = 1f;
        else vignette = 1f - CCssEase.Evaluate(CCssEase.EASE, Mathf.Clamp01((v - 0.8f) / 0.2f));

        _vignette.alpha = vignette;

        RectTransform parent = (RectTransform)_card.parent;
        float outK = CCssEase.Evaluate(CCssEase.EASE_IN, Mathf.Clamp01((t - _outDelay) / _outDuration));
        _cardGroup.alpha = 1f - outK;
        float cardLeft = parent.rect.width * _left;
        _card.anchoredPosition = new Vector2(cardLeft - outK * _outOffset, -parent.rect.height * _top);
        // 베일: left -7vw (= 카드 왼쪽만큼 되돌아가 화면 왼쪽 끝에서 시작), top -40
        _veilRect.anchoredPosition = new Vector2(-cardLeft, 40f);

        _veil.alpha = CCssEase.Evaluate(CCssEase.EASE_OUT, Mathf.Clamp01((t - _veilDelay) / _veilDuration));
        Wipe(_titleMask, CCssEase.Evaluate(CCssEase.EASE_OUT, Mathf.Clamp01((t - _titleDelay) / _titleDuration)));
        Wipe(_nameMask, CCssEase.Evaluate(CCssEase.EASE_OUT, Mathf.Clamp01((t - _nameDelay) / _nameDuration)));

        float lineK = CCssEase.Evaluate(_LINE_EASE, Mathf.Clamp01((t - _lineDelay) / _lineDuration));
        _lineGroup.alpha = lineK;
        _line.localScale = new Vector3(lineK, 1f, 1f);
    }

    // sm3-w: mask-image linear-gradient(90deg, #000 45%, transparent 55%) · mask-size 260% · position 100% → 0
    //  글자 상자(안쪽 여백 포함) 폭 W 기준으로 보이는 경계 = 1.43W - 1.6W·p, 흐린 폭 0.26W → RectMask2D 오른쪽 여백 + 부드러움으로 그대로 재현
    private static void Wipe(RectMask2D mask, float progress)
    {
        float width = mask.rectTransform.rect.width;
        float position = 1f - progress;
        float edge = 1.43f * width - 1.6f * width * position;
        int softness = Mathf.Max(1, Mathf.RoundToInt(0.26f * width));
        mask.softness = new Vector2Int(softness, 0);
        mask.padding = new Vector4(-softness - 8f, -40f, width - edge, -40f);
    }
}
