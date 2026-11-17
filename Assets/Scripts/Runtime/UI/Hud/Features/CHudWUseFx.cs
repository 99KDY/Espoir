using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 왕관 사용 시 슬롯을 밀고 빛을 퍼뜨리는 연출을 재생합니다.
/// CPlayerSkill.CrownUsed에서 사용 후 중첩을 받아 최대 중첩의 추가 섬광을 구분합니다.
/// </summary>
[DefaultExecutionOrder(250)]
public class CHudWUseFx : MonoBehaviour
{
    private sealed class CTarget
    {
        public RectTransform center;
        public RectTransform[] moved;
        public CanvasGroup flash;
        public Vector2 push;
        public float delay;
        public Vector2[] applied;
    }

    [SerializeField] private CPlayerHudView _hudView;
    [SerializeField] private CHudGroupFx _groupFx;
    [SerializeField] private RectTransform _wSlot;
    [Tooltip("W 슬롯을 제외하고 함께 밀어낼 HUD 요소")]
    [SerializeField] private RectTransform[] _inner;
    [Tooltip("Inner와 같은 순서로 연결한 최대 중첩 섬광")]
    [SerializeField] private CanvasGroup[] _innerFlash;
    [Tooltip("함께 움직일 이름판과 버프 목록. 첫 요소가 중심입니다.")]
    [SerializeField] private RectTransform[] _plate;
    [Tooltip("함께 움직일 메뉴 버튼")]
    [SerializeField] private RectTransform _menu;
    [SerializeField] private int _fullStack = 3;

    [Header("왕관 사용 연출")]
    [SerializeField] private float _speed = 2000f;
    [SerializeField] private float _amp = 14f;
    [SerializeField] private float _pushTime = 0.6f;
    [SerializeField] private float _flashAt = 0.04f;
    [SerializeField] private float _flashTime = 0.45f;
    [SerializeField] private float _flashOpacity = 0.55f;
    [SerializeField] private float _shake = 6f;
    [SerializeField] private float _shakeTime = 0.5f;
    [SerializeField] private float _bloomTime = 0.7f;
    [SerializeField] private float _bloomAmp = 1.1f;

    private readonly List<CTarget> _TARGETS = new List<CTarget>();
    private readonly Vector3[] _CORNERS = new Vector3[4];
    private CPlayerSkill _skill;
    private float _start = -10f;
    private bool _isFull;
    private bool _isActive;

    /// <summary>마지막 연출을 시작한 시각. 시간 배율의 영향을 받지 않습니다.</summary>
    public float LastStart => _start;

    private void Awake()
    {
        for (int i = 0; i < _innerFlash.Length; i++)
        {
            if (_innerFlash[i] != null) _innerFlash[i].gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_skill != null) _skill.CrownUsed -= Play;
    }

    /// <summary>
    /// W 사용 순간 연출을 재생합니다. stack = 사용 뒤 왕관 중첩(3 이상이면 3스택 연출).
    /// </summary>
    public void Play(int stack)
    {
        Restore();
        _TARGETS.Clear();
        _start = Time.unscaledTime;
        _isFull = stack >= _fullStack;
        _isActive = true;

        Vector2 w = CssCenter(_wSlot);
        for (int i = 0; i < _inner.Length; i++)
        {
            if (_inner[i] == null) continue;

            AddTarget(w, _inner[i], new[] { _inner[i] }, i < _innerFlash.Length ? _innerFlash[i] : null);
        }

        if (_plate != null && _plate.Length > 0 && _plate[0] != null) AddTarget(w, _plate[0], _plate, null);
        if (_menu != null) AddTarget(w, _menu, new[] { _menu }, null);

        CMinimapHud map = FindFirstObjectByType<CMinimapHud>();
        RectTransform mapBox = map != null ? map.transform.Find("Minimap") as RectTransform : null;
        if (mapBox != null) AddTarget(w, mapBox, new[] { mapBox }, null);
    }

    private void AddTarget(Vector2 w, RectTransform center, RectTransform[] moved, CanvasGroup flash)
    {
        // W 슬롯에서 멀수록 늦게 시작하며 바깥 방향으로 밀어낸다.
        Vector2 c = CssCenter(center);
        Vector2 d = c - w;
        float n = d.magnitude;
        if (n < 1e-3f) n = 1f;
        CTarget t = new CTarget
        {
            center = center,
            moved = moved,
            flash = _isFull ? flash : null,
            push = new Vector2(d.x / n * _amp, d.y / n * _amp * 0.6f),
            delay = d.magnitude / _speed,
            applied = new Vector2[moved.Length],
        };
        _TARGETS.Add(t);
    }

    // 화면 좌표를 높이 1080 기준 좌표로 변환한다. Y는 아래쪽이 양수다.
    private Vector2 CssCenter(RectTransform rt)
    {
        rt.GetWorldCorners(_CORNERS);
        Canvas cv = rt.GetComponentInParent<Canvas>();
        Camera cam = cv != null && cv.renderMode != RenderMode.ScreenSpaceOverlay ? cv.worldCamera : null;
        Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, _CORNERS[0]);
        Vector2 b = RectTransformUtility.WorldToScreenPoint(cam, _CORNERS[2]);
        Vector2 s = (a + b) * 0.5f;
        float k = 1080f / Mathf.Max(1, Screen.height);
        return new Vector2(s.x * k, (Screen.height - s.y) * k);
    }

    private void Update()
    {
        // 지난 프레임에 더한 밀림을 빼 둠 (다른 뷰가 쓰는 위치와 섞이지 않게)
        Restore();
    }

    private void Restore()
    {
        for (int i = 0; i < _TARGETS.Count; i++)
        {
            CTarget t = _TARGETS[i];
            for (int j = 0; j < t.moved.Length; j++)
            {
                if (t.moved[j] == null || t.applied[j] == Vector2.zero) continue;

                t.moved[j].anchoredPosition -= t.applied[j];
                t.applied[j] = Vector2.zero;
            }
        }
    }

    private void LateUpdate()
    {
        if (_skill == null && _hudView != null && _hudView.Player != null)
        {
            _skill = _hudView.Player.GetComponent<CPlayerSkill>();
            if (_skill != null) _skill.CrownUsed += Play;
        }

        if (!_isActive) return;

        float tt = Time.unscaledTime - _start;
        bool alive = false;

        for (int i = 0; i < _TARGETS.Count; i++)
        {
            CTarget t = _TARGETS[i];

            float p = (tt - t.delay) / _pushTime;
            float v = 0f;
            if (p > 0f && p < 1f)
            {
                v = p < 0.22f
                    ? CHudEase.Bezier(0.2f, 0.8f, 0.3f, 1f, p / 0.22f)
                    : 1f - CHudEase.Bezier(0.2f, 0.8f, 0.3f, 1f, (p - 0.22f) / 0.78f);
            }

            if (p < 1f) alive = true;
            for (int j = 0; j < t.moved.Length; j++)
            {
                RectTransform m = t.moved[j];
                if (m == null) continue;

                Canvas cv = m.GetComponentInParent<Canvas>();
                float unit = cv != null ? Screen.height / 1080f / Mathf.Max(1e-4f, cv.rootCanvas.scaleFactor) : 1f;
                Vector2 off = new Vector2(t.push.x, -t.push.y) * v * unit;
                m.anchoredPosition += off;
                t.applied[j] = off;
            }

            if (t.flash != null)
            {
                float q = (tt - _flashAt - t.delay) / _flashTime;
                bool on = q >= 0f && q < 1f;
                if (q < 1f) alive = true;
                if (t.flash.gameObject.activeSelf != on) t.flash.gameObject.SetActive(on);
                if (on) t.flash.alpha = _flashOpacity * (1f - CHudEase.EaseOut(q));
            }
        }

        if (_groupFx != null)
        {
            // 시작 충격이 빠르게 줄어들도록 시간에 따라 흔들림을 감쇠한다.
            if (tt <= _shakeTime)
            {
                alive = true;
                float env = Mathf.Min(1f, tt / 0.12f) * (1f - Mathf.Clamp01((tt - (_shakeTime - 0.5f)) / 0.5f));
                float sh = _shake * (0.25f * env + Mathf.Min(1.4f, Mathf.Exp(-13f * tt)));
                if (sh > 0.05f)
                {
                    _groupFx.AddShake(new Vector2(Random.Range(-1f, 1f) * sh, Random.Range(-1f, 1f) * sh * 0.7f));
                }
            }

            // W 슬롯을 중심으로 빛 번짐을 재생한다.
            if (tt <= _bloomTime)
            {
                alive = true;
                float r = Mathf.Min(0.35f, _bloomTime * 0.4f);
                float e = _bloomAmp * Mathf.Min(1f, tt / 0.1f) * (1f - Mathf.Clamp01((tt - (_bloomTime - r)) / r));
                _groupFx.SetBloom2(_wSlot, e, tt);
            }
        }

        if (!alive)
        {
            _isActive = false;
            for (int i = 0; i < _TARGETS.Count; i++)
            {
                if (_TARGETS[i].flash != null) _TARGETS[i].flash.gameObject.SetActive(false);
            }
        }
    }
}
