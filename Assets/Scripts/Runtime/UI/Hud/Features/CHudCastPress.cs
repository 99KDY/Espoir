using UnityEngine;

/// <summary>
/// 스킬 사용으로 쿨타임이 시작되면 슬롯과 아이콘의 확대, 테두리 연출을 재생합니다.
/// </summary>
[DefaultExecutionOrder(260)]
public class CHudCastPress : MonoBehaviour
{
    [SerializeField] private CHudCooldownRise _cooldown;
    [Tooltip("함께 확대할 슬롯 배경, 아이콘, 쿨타임과 테두리")]
    [SerializeField] private RectTransform[] _bdParts;
    [SerializeField] private RectTransform _icon;
    [SerializeField] private CHudBox _box;
    [SerializeField] private CHudBox _ring;

    [Header("사용 연출")]
    [SerializeField] private float _pressTime = 0.45f;
    [SerializeField] private float _pressScale = 0.86f;
    [SerializeField] private float _pressBrightness = 2f;
    [SerializeField] private float _ringTime = 0.6f;
    [SerializeField] private float _ringOffset = 18f;
    [SerializeField] private float _iconTime = 0.5f;
    [SerializeField] private float _iconScale = 1.3f;

    private Vector3[] _basePos;
    private Color _baseBorder;
    private bool _wasCooling;
    private bool _hasBaseline;
    private float _start = -10f;
    private bool _isPlaying;

    private void Awake()
    {
        _basePos = new Vector3[_bdParts.Length];
        for (int i = 0; i < _bdParts.Length; i++)
        {
            if (_bdParts[i] != null) _basePos[i] = _bdParts[i].localPosition;
        }

        if (_box != null) _baseBorder = _box.BorderColor;
        if (_ring != null) _ring.gameObject.SetActive(false);
    }

    /// <summary>지금 누름 반응을 재생 중인지.</summary>
    public bool IsPlaying => _isPlaying;

    /// <summary>마우스 반응 배율. 사용 연출의 아이콘 배율에 곱합니다.</summary>
    public float IconHoverScale { get; set; } = 1f;

    /// <summary>누름 반응을 처음부터 재생합니다.</summary>
    public void Play()
    {
        _start = Time.unscaledTime;
        _isPlaying = true;
        if (_ring != null) _ring.gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        bool isCooling = _cooldown != null && _cooldown.IsCooling;
        if (_hasBaseline && isCooling && !_wasCooling) Play();
        _wasCooling = isCooling;
        _hasBaseline = true;

        if (!_isPlaying) return;

        float t = Time.unscaledTime - _start;
        if (t >= Mathf.Max(_pressTime, Mathf.Max(_ringTime, _iconTime)))
        {
            Stop();
            return;
        }

        float kp = CHudEase.Bezier(0.2f, 0.9f, 0.3f, 1.5f, Mathf.Clamp01(t / _pressTime));
        float s = Mathf.LerpUnclamped(_pressScale, 1f, kp);
        float ki = CHudEase.EaseOut(Mathf.Clamp01(t / _iconTime));
        float si = Mathf.Lerp(_iconScale, 1f, ki);
        RectTransform slot = (RectTransform)transform;
        Vector3 c = slot.rect.center;
        for (int i = 0; i < _bdParts.Length; i++)
        {
            RectTransform p = _bdParts[i];
            if (p == null) continue;

            p.localPosition = c + (_basePos[i] - c) * s;
            p.localScale = Vector3.one * (p == _icon ? s * si * IconHoverScale : s);
        }

        // 슬롯 색은 CHudSlotStyle이 먼저 갱신하므로 현재 프레임의 밝기만 곱한다.
        if (_box != null)
        {
            float b = Mathf.Max(0f, Mathf.LerpUnclamped(_pressBrightness, 1f, kp));
            Color f = _box.Fill;
            _box.Fill = new Color(Mathf.Clamp01(f.r * b), Mathf.Clamp01(f.g * b), Mathf.Clamp01(f.b * b), f.a);
            _box.BorderColor = new Color(Mathf.Clamp01(_baseBorder.r * b), Mathf.Clamp01(_baseBorder.g * b),
                Mathf.Clamp01(_baseBorder.b * b), _baseBorder.a);
        }

        if (_ring != null)
        {
            float kr = CHudEase.EaseOut(Mathf.Clamp01(t / _ringTime));
            float w = Mathf.Lerp(2f, 1f, kr);
            float off = Mathf.Lerp(0f, _ringOffset, kr) + w;
            RectTransform rr = _ring.rectTransform;
            rr.offsetMin = new Vector2(-off, -off);
            rr.offsetMax = new Vector2(off, off);
            rr.localScale = Vector3.one * s;
            _ring.BorderWidth = w;
            _ring.BorderColor = new Color(1f, 1f, 1f, Mathf.Lerp(0.9f, 0f, kr));
        }
    }

    private void Stop()
    {
        _isPlaying = false;
        for (int i = 0; i < _bdParts.Length; i++)
        {
            if (_bdParts[i] == null) continue;

            _bdParts[i].localPosition = _basePos[i];
            _bdParts[i].localScale = Vector3.one;
        }

        if (_box != null) _box.BorderColor = _baseBorder;
        if (_ring != null) _ring.gameObject.SetActive(false);
    }
}
