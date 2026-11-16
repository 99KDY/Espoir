using TMPro;
using UnityEngine;

/// <summary>
/// 몬스터 이름과 체력 비율, 체력 막대를 표시합니다. CMonsterHpLayer가 풀에서 꺼내 사용합니다.
/// 오브젝트 위치는 머리 위 기준점이며, 자식 위치는 해당 점 기준 px 단위입니다.
/// </summary>
public class CMonsterHpView : MonoBehaviour
{
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private RectTransform _box;
    [SerializeField] private CCssBoxGraphic _bar;
    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _pct;

    [Header("요소 배치 (기준점 위쪽이 양수)")]
    [SerializeField] private float _barWidth = 92f;
    [SerializeField] private float _barHeight = 7f;
    [SerializeField] private float _barBottom = 8f;
    [SerializeField] private float _rowBaseline = 22f;
    [SerializeField] private float _rowGap = 8f;
    [SerializeField] private float _lowThreshold = 0.3f;
    [SerializeField] private float _trailDelay = 0.35f;
    [SerializeField] private float _trailSpeed = 0.9f;
    [SerializeField] private float _textDeadAlpha = 0.35f;

    // 일반 피격과 치명타 흔들림의 정규화 키프레임입니다.
    private static readonly float[] _SHAKE_T = { 0f, 0.3f, 0.65f, 1f };
    private static readonly Vector2[] _SHAKE_V = { Vector2.zero, new Vector2(1f, 0f), new Vector2(-1f, 0f),
        Vector2.zero };
    private static readonly float[] _SHAKE_CRIT_T = { 0f, 0.2f, 0.5f, 0.75f, 1f };
    private static readonly Vector2[] _SHAKE_CRIT_V = { Vector2.zero, new Vector2(1.5f, -0.5f),
        new Vector2(-1.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero };
    private const float _SHAKE_TIME = 0.2f;
    private const float _SHAKE_CRIT_TIME = 0.26f;
    private const float _FLASH_TIME = 0.32f;
    private const float _FLASH_CRIT_TIME = 0.5f;
    private const float _WHITE_TIME = 0.3f;
    private const float _DEAD_GONE = 1.8f;

    private readonly char[] _PCT_CHARS = new char[8];
    private int _pctCount;
    private float _lastPct = -1f;
    private string _displayName;

    private CEnemy _target;
    private float _anchorHeight;
    private float _h = 1f;
    private float _hd = 1f;
    private float _hitAt = -99f;
    private bool _isCritHit;
    private bool _isDead;
    private float _deadAt;
    private float _spawnAt;
    private float _phase;

    private SCssTransition _fill;
    private SCssTransition _trailAlpha;
    private SCssTransition _textAlpha;
    private SCssTransition _boxAlpha;
    private SCssTransition _low;

    /// <summary>
    /// 붙어 있는 몬스터.
    /// </summary>
    public CEnemy Target => _target;

    /// <summary>
    /// 머리 위 기준점의 발 기준 높이 (m).
    /// </summary>
    public float AnchorHeight => _anchorHeight;

    /// <summary>
    /// 사망 표시가 끝나 풀로 돌려도 되는지.
    /// </summary>
    public bool IsFinished(float now)
    {
        return _isDead && now - _deadAt > _DEAD_GONE;
    }

    /// <summary>
    /// 대상을 연결하고 등장 연출을 시작합니다.
    /// </summary>
    public void Bind(CEnemy target, string displayName, float anchorHeight, float now)
    {
        _target = target;
        _anchorHeight = anchorHeight;
        _phase = Random.value * 4.6f;
        if (_displayName != displayName)
        {
            _displayName = displayName;
            _name.text = displayName;
        }

        _h = Ratio();
        _hd = _h;
        _hitAt = -99f;
        _isDead = _h <= 0f;
        _deadAt = now;
        _spawnAt = now;
        _lastPct = -1f;
        _fill.Snap(_h);
        _trailAlpha.Snap(1f);
        _textAlpha.Snap(1f);
        _boxAlpha.Snap(1f);
        _low.Snap(_h < _lowThreshold ? 1f : 0f);
        gameObject.SetActive(true);
        Tick(now, 0f);
    }

    /// <summary>
    /// 풀로 돌린다.
    /// </summary>
    public void Release()
    {
        _target = null;
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    /// <summary>
    /// 피격 연출을 시작하고 이 오브젝트 기준 체력 막대 끝 위치를 반환합니다.
    /// </summary>
    public Vector2 Hit(bool isCritical, float now)
    {
        _h = Ratio();
        _hitAt = now;
        _isCritHit = isCritical;
        return new Vector2(-_barWidth * 0.5f + _barWidth * _h, _barBottom + _barHeight * 0.5f);
    }

    /// <summary>
    /// 매 프레임 상태 · 모양 갱신.
    /// </summary>
    public void Tick(float now, float deltaTime)
    {
        if (_target == null) return;

        _h = Ratio();
        if (!_isDead && _h <= 0f)
        {
            _isDead = true;
            _deadAt = now;
            _fill.Set(0f, now, 0.55f, 0f, CCssEasing.EASE_IN);
            _trailAlpha.Set(0f, now, 0.5f, 0.35f, CCssEasing.EASE);
            _textAlpha.Set(_textDeadAlpha, now, 0.4f, 0.15f, CCssEasing.EASE);
            _boxAlpha.Set(0f, now, 0.5f, 1.3f, CCssEasing.EASE);
        }

        if (!_isDead) _fill.Set(_h, now, 0.14f, 0f, CCssEasing.EASE_OUT);

        // 늦게 따라오는 막대: 마지막으로 맞고 0.35초 뒤부터 초당 0.9
        if (now - _hitAt > _trailDelay && _hd > _h) _hd = Mathf.Max(_h, _hd - deltaTime * _trailSpeed);
        else if (_hd < _h) _hd = _h;

        _low.Set(!_isDead && _h < _lowThreshold ? 1f : 0f, now, 0.4f, 0f, CCssEasing.EASE);

        if (!gameObject.activeInHierarchy) return;

        float pct = (float)System.Math.Ceiling((double)_target.HpCurrent / _target.HpMax * 100.0);
        if (!Mathf.Approximately(pct, _lastPct))
        {
            _lastPct = pct;
            _pctCount = CHudNumberFormat.Write(_PCT_CHARS, Mathf.Max(0, (int)pct));
            _PCT_CHARS[_pctCount++] = '%';
            _pct.SetCharArray(_PCT_CHARS, 0, _pctCount);
        }

        Paint(now);
    }

    private void Paint(float now)
    {
        // 글자 줄: 폭 = max(막대 폭, 이름 + 간격 + %), 가운데 정렬. 이름 margin-right -자간 반영
        float nameW = CHudNumberFormat.CssWidth(_name, _displayName) - _name.characterSpacing * 0.01f * _name.fontSize;
        float pctW = CHudNumberFormat.CssWidth(_pct, _PCT_CHARS, _pctCount);
        float rowW = Mathf.Max(_barWidth, nameW + _rowGap + pctW);
        _name.rectTransform.anchoredPosition = new Vector2(-rowW * 0.5f, _rowBaseline);
        _pct.rectTransform.anchoredPosition = new Vector2(rowW * 0.5f - pctW, _rowBaseline);

        Vector2 shake = Vector2.zero;
        float flashAlpha = 0f;
        float white = 0f;
        float age = now - _hitAt;
        if (_isCritHit)
        {
            if (age < _SHAKE_CRIT_TIME) shake = CCssEasing.Keyframes(_SHAKE_CRIT_T, _SHAKE_CRIT_V,
                CCssEasing.EASE_OUT, age / _SHAKE_CRIT_TIME);
            if (age < _FLASH_CRIT_TIME) flashAlpha = 1f - CCssEasing.Evaluate(CCssEasing.EASE_OUT,
                age / _FLASH_CRIT_TIME);
            if (age < _WHITE_TIME) white = 1f - CCssEasing.Evaluate(CCssEasing.EASE_OUT, age / _WHITE_TIME);
        }

        else
        {
            if (age < _SHAKE_TIME) shake = CCssEasing.Keyframes(_SHAKE_T, _SHAKE_V, CCssEasing.EASE_OUT,
                age / _SHAKE_TIME);
            if (age < _FLASH_TIME) flashAlpha = 1f - CCssEasing.Evaluate(CCssEasing.EASE_OUT, age / _FLASH_TIME);
        }

        // 다시 나옴: 채움 0 → 값 .55s cubic-bezier(.2,.7,.3,1), 상자 opacity 0 · 3px 아래에서 .4s ease-out
        float spawnAge = now - _spawnAt;
        float fillIn = CCssEasing.Evaluate(new Vector4(0.2f, 0.7f, 0.3f, 1f), spawnAge / 0.55f);
        float up = CCssEasing.Evaluate(CCssEasing.EASE_OUT, spawnAge / 0.4f);
        _box.anchoredPosition = new Vector2(0f, -3f * (1f - up));
        _group.alpha = _boxAlpha.Evaluate(now) * up;

        // 체력 막대에만 흔들림을 적용하며 y 방향은 캔버스 좌표에 맞춰 반전합니다.
        _bar.rectTransform.anchoredPosition = new Vector2(-_barWidth * 0.5f + shake.x, _barBottom +
            _barHeight - shake.y);
        _bar.SetDynamic(new Vector4(_fill.Evaluate(now) * fillIn, _hd, _h, _hd),
            new Vector4(flashAlpha, 1f, white, _isCritHit ? 1f : 0f),
            new Vector3(_phase, 1f, _low.Evaluate(now)),
            new Vector4(_trailAlpha.Evaluate(now), 0f, 0f, 0f));

        float textAlpha = _textAlpha.Evaluate(now);
        _name.alpha = textAlpha;
        _pct.alpha = textAlpha;
    }

    private float Ratio()
    {
        if (_target == null || _target.HpMax <= 0f) return 0f;
        return Mathf.Clamp01(_target.HpCurrent / _target.HpMax);
    }
}
