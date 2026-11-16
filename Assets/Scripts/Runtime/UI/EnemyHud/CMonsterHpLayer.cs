using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스를 제외한 CEnemy의 머리 위에 체력 표시를 배치하고 추적합니다.
/// 막대는 미리 만든 풀에서 꺼내 쓰고, 몬스터 사망 표시가 끝나거나 · 사라지거나 · 꺼지면 바로 풀로 돌린다.
/// 화면 좌표는 카메라가 움직인 뒤(LateUpdate, 실행 순서 100)에 계산한다.
/// </summary>
[DefaultExecutionOrder(100)]
public class CMonsterHpLayer : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private CPlayer _player;
    [SerializeField] private CMonsterHpView _template;
    [SerializeField] private CHudGlowFx _fx;
    [SerializeField] private int _poolSize = 12;

    [Header("표시")]
    [Tooltip("체력 표시 위에 출력할 몬스터 이름")]
    [SerializeField] private string _monsterName = "모래 떠돌이";
    [Tooltip("콜라이더 꼭대기에서 체력 표시까지의 높이 (m)")]
    [SerializeField] private float _heightAboveTop = 0.3f;
    [SerializeField] private float _defaultHeight = 2f;
    [Tooltip("화면 가장자리를 1로 보는 표시 범위")]
    [SerializeField] private float _screenMargin = 1.15f;
    [Tooltip("새 몬스터를 찾는 간격 (s)")]
    [SerializeField] private float _scanInterval = 0.25f;

    [Header("피격 효과")]
    [SerializeField] private Vector3 _sparkColor = new Vector3(1.3f, 1.02f, 0.74f);

    private readonly List<CMonsterHpView> _ACTIVE = new List<CMonsterHpView>();
    private readonly Stack<CMonsterHpView> _POOL = new Stack<CMonsterHpView>();
    private readonly Dictionary<CEnemy, CMonsterHpView> _BY_TARGET = new Dictionary<CEnemy, CMonsterHpView>();
    private RectTransform _rect;
    private Canvas _canvas;
    private float _nextScan;

    /// <summary>
    /// 지금 붙어 있는 막대 수.
    /// </summary>
    public int ActiveCount => _ACTIVE.Count;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _canvas = GetComponentInParent<Canvas>();
        if (_camera == null) _camera = Camera.main;
        _template.gameObject.SetActive(false);
        for (int i = 0; i < _poolSize; i++) _POOL.Push(CreateView(i));
    }

    private void OnEnable()
    {
        CUnit.OnDamageResolved += HandleDamageResolved;
    }

    private void OnDisable()
    {
        CUnit.OnDamageResolved -= HandleDamageResolved;
        for (int i = _ACTIVE.Count - 1; i >= 0; i--) ReleaseAt(i);
    }

    private CMonsterHpView CreateView(int index)
    {
        CMonsterHpView view = Instantiate(_template, _template.transform.parent);
        view.name = "MonsterHp" + index;
        view.gameObject.SetActive(false);
        return view;
    }

    private void HandleDamageResolved(SDamageResult result)
    {
        if (result.hpLost <= 0f || !(result.target is CEnemy enemy) || enemy is CSlothBoss) return;

        CMonsterHpView view = Track(enemy);
        if (view == null) return;

        Vector2 tip = view.Hit(result.isCritical, Time.time);
        if (_fx == null || !view.gameObject.activeInHierarchy) return;

        Vector2 position = (Vector2)view.transform.localPosition + tip + _rect.rect.size * _rect.pivot;
        if (result.isCritical) _fx.Flare(position, 10f, _sparkColor, 0.32f, 0.3f);
        else _fx.Spark(position, _sparkColor, 60f, -Mathf.PI * 0.5f, Mathf.PI * 2f, 1.2f, 0.28f, 3f);
    }

    private void LateUpdate()
    {
        float now = Time.time;
        if (now >= _nextScan)
        {
            _nextScan = now + _scanInterval;
            Scan();
        }

        if (_player == null) _player = FindFirstObjectByType<CPlayer>();
        bool isHidden = IsTalking() || _camera == null;
        float scale = _canvas != null ? _canvas.scaleFactor : 1f;

        for (int i = _ACTIVE.Count - 1; i >= 0; i--)
        {
            CMonsterHpView view = _ACTIVE[i];
            CEnemy target = view.Target;
            if (target == null || !target.isActiveAndEnabled || view.IsFinished(now))
            {
                ReleaseAt(i);
                continue;
            }

            Vector3 anchor = target.transform.position + Vector3.up * view.AnchorHeight;
            Vector3 viewport = isHidden ? Vector3.zero : _camera.WorldToViewportPoint(anchor);
            float nx = viewport.x * 2f - 1f;
            float ny = viewport.y * 2f - 1f;
            bool isVisible = !isHidden && viewport.z > 0f && Mathf.Abs(nx) < _screenMargin
                && Mathf.Abs(ny) < _screenMargin;
            if (view.gameObject.activeSelf != isVisible) view.gameObject.SetActive(isVisible);

            if (isVisible)
            {
                Vector3 screen = _camera.WorldToScreenPoint(anchor);
                view.transform.localPosition = new Vector3(screen.x / scale, screen.y / scale,
                    0f) - (Vector3)(_rect.rect.size * _rect.pivot);
            }

            view.Tick(now, Time.deltaTime);
        }
    }

    // 대화와 소환 연출 중에는 체력 UI를 숨깁니다.
    private bool IsTalking()
    {
        if (_player != null && _player.IsInputLocked) return true;
        CGameManager gm = CGameManager.Instance;
        return gm != null && gm.GameState == EGameState.Story;
    }

    private void Scan()
    {
        CEnemy[] enemies = FindObjectsByType<CEnemy>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            CEnemy enemy = enemies[i];
            if (enemy is CSlothBoss || enemy.HpCurrent <= 0f || _BY_TARGET.ContainsKey(enemy)) continue;
            Track(enemy);
        }
    }

    private CMonsterHpView Track(CEnemy enemy)
    {
        if (_BY_TARGET.TryGetValue(enemy, out CMonsterHpView existing)) return existing;
        if (!enemy.isActiveAndEnabled) return null;

        CMonsterHpView view = _POOL.Count > 0 ? _POOL.Pop() : CreateView(_ACTIVE.Count + _POOL.Count);
        float height = _defaultHeight;
        if (enemy.TryGetComponent(out Collider body) && body.enabled)
        {
            height = body.bounds.max.y - enemy.transform.position.y;
        }

        view.Bind(enemy, _monsterName, height + _heightAboveTop, Time.time);
        _ACTIVE.Add(view);
        _BY_TARGET.Add(enemy, view);
        return view;
    }

    private void ReleaseAt(int index)
    {
        CMonsterHpView view = _ACTIVE[index];
        CEnemy target = view.Target;
        _ACTIVE.RemoveAt(index);
        // 파괴된 몬스터도 instance id 로 찾아 지워진다
        if (!ReferenceEquals(target, null)) _BY_TARGET.Remove(target);

        if (view == null) return; // 씬을 닫는 중
        view.Release();
        _POOL.Push(view);
    }
}
