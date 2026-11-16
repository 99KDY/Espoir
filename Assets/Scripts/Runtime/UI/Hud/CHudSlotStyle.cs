using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 슬롯 상태에 따라 대기 중 강조, 쿨타임 그림자와 사용 준비 섬광을 갱신합니다.
/// 궁극기 상태의 진홍 테마를 적용합니다. 쿨타임 여부는 CHudCooldownRise 에서, 궁 상태 가중치는 CLeviathanHudView 에서 받는다.
/// </summary>
[DefaultExecutionOrder(150)]
public class CHudSlotStyle : MonoBehaviour
{
    [SerializeField] private CHudBox _box;
    [SerializeField] private CHudBox _sheen;
    [SerializeField] private CHudIconShadow _iconShadow;
    [SerializeField] private TMP_Text _key;
    [SerializeField] private CHudCooldownRise _cooldown;

    [Header("빈 퀵슬롯 (.qs.empty) — 아이콘 이미지가 꺼지면 적용")]
    [SerializeField] private Image _emptyProbe;
    [SerializeField] private Color _emptyFill = new Color(8f / 255f, 10f / 255f, 18f / 255f, 0.16f);
    [SerializeField, Range(0f, 1f)] private float _emptyKeyAlpha = 0.5f;

    [Header("준비 숨결 kh-rdy (0 = 없음)")]
    [SerializeField, Range(0f, 1f)] private float _readyAlpha = 0.32f;
    [SerializeField] private float _readyPeriod = 3.4f;

    [Header("아이콘 그림자")]
    [SerializeField] private bool _hasReadyIconGlow = true;
    [SerializeField] private bool _hasCooldownIconShadow = true;

    [Header("흐름 가득 .ps[data-p=\"1\"].full .bd: box-shadow 0 0 14px color-mix(ac 55%) (transition .2s)")]
    [SerializeField] private Color _fullShadow = new Color(1f, 192f / 255f, 112f / 255f, 0.55f);
    [SerializeField] private float _fullShadowBlur = 14f;

    [Header("아이콘 글자색 (웹 svg 아이콘의 color · 궁 상태 color #fff — img 아이콘은 흰색 그대로)")]
    [SerializeField] private Color _iconTint = Color.white;
    [SerializeField] private Color _iconTintCrimson = Color.white;

    [Header("색 (CSS 값)")]
    [SerializeField] private Color _amber = new Color(1f, 192f / 255f, 112f / 255f);
    [SerializeField] private Color _crimson = new Color(1f, 61f / 255f, 82f / 255f);
    [SerializeField] private Color _crimsonFill = new Color(0.358642f, 0.0952374f, 0.133088f, 0.601569f);
    [SerializeField] private Color _keyColor = new Color(1f, 228f / 255f, 196f / 255f);
    [SerializeField] private Color _keyCrimson = new Color(1f, 158f / 255f, 168f / 255f);

    private const float _COOL_TRANSITION = 0.2f;
    private const float _SHEEN_TIME = 0.8f;

    private Color _baseFill;
    private bool _hasBase;
    private float _readyWeight = 1f;
    private float _crimsonWeight;
    private bool _wasCooling;
    private float _sheenAge = -1f;
    private bool _isFull;
    private float _fullWeight;

    /// <summary>궁 상태(진홍) 가중치 0~1. CSS transition 곡선은 호출하는 쪽이 적용한다.</summary>
    public float CrimsonWeight
    {
        get { return _crimsonWeight; }
        set { _crimsonWeight = Mathf.Clamp01(value); }
    }

    /// <summary>흐름 가득(.full) 여부. 패시브1 칸에만 씀.</summary>
    public bool IsFull
    {
        get { return _isFull; }
        set { _isFull = value; }
    }

    private void LateUpdate()
    {
        if (_box == null) return;

        if (!_hasBase)
        {
            _baseFill = _box.Fill;
            _hasBase = true;
        }

        float dt = Time.unscaledDeltaTime;
        bool isCooling = _cooldown != null && _cooldown.IsCooling;

        // .oncd 가 빠지는 순간 훑는 빛 1회 (fz-sheen .8s ease-out, translateX -125% → 125%)
        if (_wasCooling && !isCooling)
        {
            _sheenAge = 0f;
        }

        _wasCooling = isCooling;
        UpdateSheen(dt);

        // box-shadow transition .2s: 쿨타임이면 준비 숨결이 꺼짐
        float target = isCooling ? 0f : 1f;
        _readyWeight = Mathf.MoveTowards(_readyWeight, target, dt / _COOL_TRANSITION);
        float ready = CHudEase.Ease(_readyWeight);
        float w = _crimsonWeight;

        // 빈 퀵슬롯: 배경 rgba(8,10,18,.16) · 점선 테두리 · 키 글자 .5
        bool isEmpty = _emptyProbe != null && (!_emptyProbe.enabled || _emptyProbe.sprite == null);
        if (_emptyProbe != null)
        {
            _box.BorderStyle = isEmpty ? CHudBox.EBorderStyle.Dashed : CHudBox.EBorderStyle.Solid;
        }

        // 배경: 프리멀티플라이드 보간 (CSS 색 보간)
        _box.Fill = LerpPremul(isEmpty ? _emptyFill : _baseFill, _crimsonFill, w);

        // 바깥 그림자 · inset: kh-rdy(0%·100% 0 0 5px kg / 50% 0 0 16px kg, inset 0 0 10px kg) ↔ 진홍(0 0 14px c1 .75, inset 0
        // 0 12px c1 .4)
        float shadowBlur = 0f;
        Color shadow = Color.clear;
        float insetBlur = 0f;
        Color inset = Color.clear;
        if (_readyAlpha > 0f)
        {
            float k = Mathf.Repeat(Time.unscaledTime / _readyPeriod, 1f);
            float u = k < 0.5f ? CHudEase.EaseInOut(k / 0.5f) : 1f - CHudEase.EaseInOut((k - 0.5f) / 0.5f);
            Color kg = new Color(_amber.r, _amber.g, _amber.b, _readyAlpha * ready);
            shadowBlur = Mathf.Lerp(5f, 16f, u);
            shadow = kg;
            insetBlur = 10f * u;
            inset = new Color(kg.r, kg.g, kg.b, kg.a * u);
        }

        _fullWeight = Mathf.MoveTowards(_fullWeight, _isFull ? 1f : 0f, dt / _COOL_TRANSITION);
        if (_fullWeight > 0f)
        {
            float f = CHudEase.Ease(_fullWeight);
            shadowBlur = Mathf.Lerp(shadowBlur, _fullShadowBlur, f);
            shadow = LerpPremul(shadow, _fullShadow, f);
        }

        Color redShadow = new Color(_crimson.r, _crimson.g, _crimson.b, 0.75f);
        Color redInset = new Color(_crimson.r, _crimson.g, _crimson.b, 0.4f);
        _box.SetShadow(Mathf.Lerp(shadowBlur, 14f, w), LerpPremul(shadow, redShadow, w));
        _box.SetInset(Mathf.Lerp(insetBlur, 12f, w), LerpPremul(inset, redInset, w));

        if (_iconShadow != null)
        {
            Color normal;
            if (isCooling && _hasCooldownIconShadow) normal = new Color(0f, 0f, 0f, 0.55f);
            else if (_hasReadyIconGlow) normal = new Color(_amber.r, _amber.g, _amber.b, 0.4f);
            else normal = Color.clear;

            _iconShadow.SetShadow(LerpPremul(normal, _crimson, w), Mathf.Lerp(4f, 5f, w));
            _iconShadow.SetTint(Color.Lerp(_iconTint, _iconTintCrimson, w));
        }

        if (_key != null)
        {
            Color kc = Color.Lerp(_keyColor, _keyCrimson, w);
            kc.a = isEmpty ? _emptyKeyAlpha : 1f;
            _key.color = kc;
        }
    }

    private void UpdateSheen(float dt)
    {
        if (_sheen == null) return;

        if (_sheenAge < 0f)
        {
            if (_sheen.gameObject.activeSelf) _sheen.gameObject.SetActive(false);
            return;
        }

        _sheenAge += dt;
        float p = _sheenAge / _SHEEN_TIME;
        if (p >= 1f)
        {
            _sheenAge = -1f;
            _sheen.gameObject.SetActive(false);
            return;
        }

        if (!_sheen.gameObject.activeSelf) _sheen.gameObject.SetActive(true);
        _sheen.SheenOffset = Mathf.Lerp(-1.25f, 1.25f, CHudEase.EaseOut(p));
    }

    private static Color LerpPremul(Color a, Color b, float t)
    {
        float alpha = Mathf.Lerp(a.a, b.a, t);
        if (alpha <= 1e-5f) return new Color(b.r, b.g, b.b, 0f);

        float r = Mathf.Lerp(a.r * a.a, b.r * b.a, t) / alpha;
        float g = Mathf.Lerp(a.g * a.a, b.g * b.a, t) / alpha;
        float bl = Mathf.Lerp(a.b * a.a, b.b * b.a, t) / alpha;
        return new Color(r, g, bl, alpha);
    }
}
