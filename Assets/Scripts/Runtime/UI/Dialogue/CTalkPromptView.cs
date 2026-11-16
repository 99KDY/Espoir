using UnityEngine;

/// <summary>
/// 상호작용할 수 있는 NPC 위에 펼쳐지는 마름모 표시를 그립니다.
/// NPC 머리 위 기준점에 설정된 오프셋을 적용합니다.
/// </summary>
public class CTalkPromptView : MonoBehaviour
{
    private static readonly Vector4 _MOVE = new Vector4(0.2f, 0.9f, 0.3f, 1.2f);
    private static readonly Vector4 _POP = new Vector4(0.2f, 0.9f, 0.3f, 1.5f);

    [SerializeField] private RectTransform _root;
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private RectTransform _line;
    [SerializeField] private RectTransform _key;
    [SerializeField] private RectTransform _wave;
    [SerializeField] private CanvasGroup _waveGroup;

    [Header("웹 값 (px, 1080 기준)")]
    [SerializeField] private float _liftAboveHead = 66f;
    [SerializeField] private float _hiddenOffset = 8f;
    [SerializeField] private float _lineWidth = 90f;
    [SerializeField] private float _wavePeriod = 1.8f;

    private SCssTween _opacity;
    private SCssTween _offsetY;
    private SCssTween _width;
    private SCssTween _keyScale;
    private bool _isOn;
    private bool _isReady;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        if (_isReady) return;

        _isReady = true;
        _opacity.Snap(0f);
        _offsetY.Snap(_hiddenOffset);
        _width.Snap(0f);
        _keyScale.Snap(0f);
    }

    /// <summary>
    /// 머리 위 화면 자리(캔버스 좌표, 왼쪽 위 기준 px)와 켜짐 여부를 넣습니다.
    /// </summary>
    public void Tick(Vector2 headCss, bool isOn, float now)
    {
        Init();

        if (isOn != _isOn)
        {
            _isOn = isOn;
            float target = isOn ? 1f : 0f;
            _opacity.To(target, now, 0.2f, 0f, CCssEase.EASE);
            _offsetY.To(isOn ? 0f : _hiddenOffset, now, 0.25f, 0f, _MOVE);
            _width.To(isOn ? _lineWidth : 0f, now, 0.3f, 0f, CCssEase.EASE);
            _keyScale.To(target, now, 0.35f, 0.25f, _POP);
        }

        _group.alpha = _opacity.Value(now);

        // .pr: top = ny - 66, translate(-50%, -100%) (+8px 아래로 숨김) → 30x30 상자의 아래 가운데가 (nx, ny - 66)
        RectTransform parent = (RectTransform)_root.parent;
        float bottomCss = headCss.y - _liftAboveHead + _offsetY.Value(now);
        _root.anchoredPosition = new Vector2(headCss.x, parent.rect.height - bottomCss + _root.sizeDelta.y * 0.5f);

        float width = _width.Value(now);
        _line.sizeDelta = new Vector2(width, 1f);
        float scale = Mathf.Max(0f, _keyScale.Value(now));
        _key.localScale = new Vector3(scale, scale, 1f);

        // ::after py-wave 1.8s ease-out 무한: scale 1 → 1.9, opacity .9 → 0
        float phase = Mathf.Repeat(now, _wavePeriod) / _wavePeriod;
        float k = CCssEase.Evaluate(CCssEase.EASE_OUT, phase);
        float waveScale = Mathf.Lerp(1f, 1.9f, k);
        _wave.localScale = new Vector3(waveScale, waveScale, 1f);
        _waveGroup.alpha = Mathf.Lerp(0.9f, 0f, k);
    }
}
