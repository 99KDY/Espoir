using TMPro;
using UnityEngine;

/// <summary>
/// 맵 진입 시 화면 상단에 영역 이름과 장소 이름을 표시합니다.
/// </summary>
public class CMapEntryBanner : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private CMapInfo _mapInfo;
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private RectTransform _band;
    [SerializeField] private TMP_Text _regionText;
    [SerializeField] private TMP_Text _placeText;

    [Header("자리 (1920x1080 기준 px)")]
    [SerializeField] private float _top = 160f;
    [SerializeField] private Vector4 _padding = new Vector4(18f, 150f, 20f, 150f);   // 위 · 오른쪽 · 아래 · 왼쪽
    [SerializeField] private float _gap = 14f;

    [Header("영역 줄 (.gpb i)")]
    [SerializeField] private float _regionSize = 15f;
    [SerializeField] private float _regionLetterSpacingEm = 0.34f;
    [SerializeField] private float _regionMarginRightEm = -0.5f;

    [Header("장소 줄 (.gpb b)")]
    [SerializeField] private float _placeSize = 38f;
    [SerializeField] private float _placeLetterSpacingEm = 0.4f;
    [SerializeField] private float _placeMarginRightEm = -0.4f;

    [Tooltip("줄 높이(line-height 1) 위에서 기준선까지 거리 / 글자 크기 — Noto Serif KR hhea (1 - (1.151 + 0.286)) / 2 + 1.151")]
    [SerializeField] private float _baselineRatio = 0.9325f;

    [Header("움직임 (@keyframes ee-fade, 4.4s ease both)")]
    [SerializeField] private float _duration = 4.4f;
    [SerializeField, Range(0f, 1f)] private float _inEnd = 0.14f;
    [SerializeField, Range(0f, 1f)] private float _outStart = 0.78f;
    [Tooltip("translateY(8px) → none (화면 아래쪽이 +)")]
    [SerializeField] private float _rise = 8f;
    [SerializeField] private bool _playOnStart = true;

    [Tooltip("한 프레임에 진행하는 최대 시간 (초) — 로딩 직후 긴 프레임에서 띠가 건너뛰어지지 않게")]
    [SerializeField] private float _maxFrameStep = 0.1f;

    private float _time = -1f;
    private bool _waiting;

    /// <summary>재생 중이면 true.</summary>
    public bool IsPlaying => _time >= 0f || _waiting;

    private void Start()
    {
        _group.alpha = 0f;
        _band.gameObject.SetActive(false);
        if (_playOnStart && _mapInfo != null) Play(_mapInfo.RegionName, _mapInfo.PlaceName);
    }

    /// <summary>
    /// 영역 · 장소 이름으로 입장 띠를 처음부터 다시 재생합니다. 씬 이동 페이드가 끝난 뒤 시작합니다.
    /// </summary>
    public void Play(string regionName, string placeName)
    {
        _regionText.text = regionName;
        _placeText.text = placeName;
        _band.gameObject.SetActive(true);
        Layout();
        _group.alpha = 0f;
        _time = -1f;
        _waiting = true;
    }

    private void Update()
    {
        if (_waiting)
        {
            if (IsScreenCovered()) return;
            _waiting = false;
            _time = 0f;
        }

        if (_time < 0f) return;
        // 씬을 막 불러온 첫 프레임은 unscaledDeltaTime 에 로딩 시간(측정 2.8초)이 들어가 띠가 나타나기도 전에 거의 끝나 버림 → 한 프레임 최대 0.1초
        _time += Mathf.Min(Time.unscaledDeltaTime, _maxFrameStep);
        float p = Mathf.Clamp01(_time / _duration);
        Apply(p);
        if (p >= 1f)
        {
            _time = -1f;
            _band.gameObject.SetActive(false);
        }
    }

    // 씬 이동 중(검은 화면이 덮여 있는 동안)에는 기다림
    private static bool IsScreenCovered()
    {
        CSceneFlowManager flow = CSceneFlowManager.Instance;
        return flow != null && (flow.IsTraveling || flow.IsScreenCovered);
    }

    // 키프레임: 0% { opacity 0; translateY(8px) } 14% { opacity 1; none } 78% { opacity 1 } 100% { opacity 0 }
    // CSS animation-timing-function 은 구간마다 따로 걸림 → 구간마다 ease
    private void Apply(float p)
    {
        float alpha;
        float rise = 0f;
        if (p < _inEnd)
        {
            float k = Ease(p / _inEnd);
            alpha = k;
            rise = _rise * (1f - k);
        }

        else if (p < _outStart)
        {
            alpha = 1f;
        }

        else
        {
            alpha = 1f - Ease((p - _outStart) / (1f - _outStart));
        }

        _group.alpha = alpha;
        _band.anchoredPosition = new Vector2(0f, -_top - rise);
    }

    // CSS 'ease' = cubic-bezier(0.25, 0.1, 0.25, 1)
    private static float Ease(float x)
    {
        return CubicBezier(0.25f, 0.1f, 0.25f, 1f, x);
    }

    private static float CubicBezier(float x1, float y1, float x2, float y2, float x)
    {
        if (x <= 0f) return 0f;
        if (x >= 1f) return 1f;
        float t = x;
        for (int i = 0; i < 8; i++)
        {
            float cx = Bezier(x1, x2, t) - x;
            float dx = BezierSlope(x1, x2, t);
            if (Mathf.Abs(cx) < 1e-6f) break;
            if (Mathf.Abs(dx) < 1e-6f) break;
            t -= cx / dx;
        }

        t = Mathf.Clamp01(t);
        return Bezier(y1, y2, t);
    }

    private static float Bezier(float p1, float p2, float t)
    {
        float u = 1f - t;
        return 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t;
    }

    private static float BezierSlope(float p1, float p2, float t)
    {
        float u = 1f - t;
        return 3f * u * u * p1 + 6f * u * t * (p2 - p1) + 3f * t * t * (1f - p2);
    }

    // 글자 폭과 자간으로 각 줄의 폭을 계산하고 여백을 반영해 중앙에 정렬합니다.
    private void Layout()
    {
        float regionWidth = LineWidth(_regionText, _regionSize, _regionLetterSpacingEm);
        float placeWidth = LineWidth(_placeText, _placeSize, _placeLetterSpacingEm);
        float regionBox = regionWidth + _regionMarginRightEm * _regionSize;
        float placeBox = placeWidth + _placeMarginRightEm * _placeSize;
        float inner = Mathf.Max(regionBox, placeBox);
        float height = _padding.x + _regionSize + _gap + _placeSize + _padding.z;
        _band.sizeDelta = new Vector2(inner + _padding.y + _padding.w, height);

        PlaceLine(_regionText, regionBox, _padding.x + _regionSize * _baselineRatio);
        PlaceLine(_placeText, placeBox, _padding.x + _regionSize + _gap + _placeSize * _baselineRatio);
    }

    private static float LineWidth(TMP_Text text, float size, float spacingEm)
    {
        text.fontSize = size;
        text.characterSpacing = spacingEm * 100f;   // TMP 자간 단위 = 0.01em
        text.ForceMeshUpdate();
        TMP_TextInfo info = text.textInfo;
        if (info.characterCount == 0) return 0f;
        TMP_CharacterInfo first = info.characterInfo[0];
        TMP_CharacterInfo last = info.characterInfo[info.characterCount - 1];
        return last.xAdvance - first.origin;
    }

    // 줄 상자 왼쪽 = 띠 가운데 - 여백 상자 폭 / 2, 기준선 = 띠 위에서 baseline px 아래
    private static void PlaceLine(TMP_Text text, float marginBox, float baseline)
    {
        RectTransform rt = text.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(Mathf.Max(marginBox, 1f) + 200f, 0f);
        rt.anchoredPosition = new Vector2(-marginBox * 0.5f, -baseline);
    }
}
