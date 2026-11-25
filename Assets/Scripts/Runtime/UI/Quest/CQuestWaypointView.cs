using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CQuestWaypointView : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private RectTransform _canvasRect;
    [SerializeField] private RectTransform _root;
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private RectTransform _diamond;
    [SerializeField] private RectTransform _arrow;
    [SerializeField] private TMP_Text _name;
    [SerializeField] private TMP_Text _distance;

    [SerializeField] private float _height = 1.2f;
    [SerializeField] private float _npcHeight = 1.6f;
    [SerializeField] private float _edgeMargin = 70f;
    [Tooltip("화면 위쪽에서 잰 표식의 최대 위치 비율")]
    [SerializeField, Range(0.3f, 1f)] private float _bottomLimit = 0.68f;
    [SerializeField, Min(0.01f)] private float _fadeTime = 0.25f;

    private readonly List<CQuestSO> _TRACKED = new List<CQuestSO>();
    private bool _hasOverride;
    private Vector3 _overridePoint;
    private string _overrideName;
    private float _overrideHeight;
    private CPlayer _player;
    private float _alpha;
    private int _distanceShown = -1;

    /// <summary>
    /// 기본 목적지 대신 표시할 위치와 이름을 지정합니다.
    /// </summary>
    public void SetOverride(Vector3 point, string displayName, float height)
    {
        _hasOverride = true;
        _overridePoint = point;
        _overrideName = displayName;
        _overrideHeight = height;
    }

    /// <summary>
    /// 표식을 기본 목적지로 되돌립니다.
    /// </summary>
    public void ClearOverride()
    {
        _hasOverride = false;
    }

    private void Start()
    {
        if (_camera == null) _camera = Camera.main;
        _player = FindFirstObjectByType<CPlayer>();
        _group.alpha = 0f;
    }

    private void LateUpdate()
    {
        bool hasTarget = TryGetTarget(out Vector3 point, out string displayName, out float height);
        bool isBlocked = _player == null || _player.IsInputLocked ||
            (CGameManager.Instance != null && CGameManager.Instance.GameState != EGameState.Idle);
        bool isOn = hasTarget && !isBlocked;

        _alpha = Mathf.MoveTowards(_alpha, isOn ? 1f : 0f, Time.unscaledDeltaTime / _fadeTime);
        _group.alpha = _alpha;
        if (_alpha <= 0f || !hasTarget || _player == null) return;

        Place(point + Vector3.up * height);

        if (_name.text != displayName) _name.text = displayName;

        Vector3 offset = point - _player.transform.position;
        offset.y = 0f;
        int meters = Mathf.RoundToInt(offset.magnitude);
        if (meters != _distanceShown)
        {
            _distanceShown = meters;
            _distance.text = meters + "m";
        }

        float bob = 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime / 1.4f * Mathf.PI * 2f);
        _diamond.anchoredPosition = new Vector2(0f, -bob * 4f);
    }

    private void Place(Vector3 world)
    {
        Rect rect = _canvasRect.rect;
        Vector3 viewport = _camera.WorldToViewportPoint(world);
        bool isBehind = viewport.z < 0f;
        float x = viewport.x * rect.width;
        float y = (1f - viewport.y) * rect.height;

        if (isBehind)
        {
            x = rect.width - x;
            y = rect.height - y;
        }

        float bottom = rect.height * _bottomLimit;
        bool isEdge = isBehind || x < _edgeMargin || x > rect.width - _edgeMargin || y < _edgeMargin || y > bottom;

        if (isEdge)
        {
            float cx = rect.width * 0.5f;
            float cy = rect.height * 0.5f;
            float dx = x - cx;
            float dy = y - cy;
            float kx = (cx - _edgeMargin) / Mathf.Max(1e-3f, Mathf.Abs(dx));
            float ky = (dy > 0f ? bottom - cy : cy - _edgeMargin) / Mathf.Max(1e-3f, Mathf.Abs(dy));
            float k = Mathf.Min(kx, ky);
            x = cx + dx * k;
            y = cy + dy * k;
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            _arrow.localRotation = Quaternion.Euler(0f, 0f, -(angle + 90f));
        }

        _arrow.gameObject.SetActive(isEdge);
        _diamond.gameObject.SetActive(!isEdge);

        _root.pivot = new Vector2(0.5f, isEdge ? 0.5f : 0f);
        _root.anchoredPosition = new Vector2(x, -y);
    }

    private bool TryGetTarget(out Vector3 point, out string displayName, out float height)
    {
        point = Vector3.zero;
        displayName = string.Empty;
        height = _height;

        if (_hasOverride)
        {
            point = _overridePoint;
            displayName = _overrideName;
            height = _overrideHeight;
            return true;
        }

        if (!CQuestManager.IsReady) return false;

        CQuestManager.GetTracked(_TRACKED);

        foreach (CQuestSO quest in _TRACKED)
        {
            EQuestState state = CQuestManager.GetState(quest);

            if (state == EQuestState.Reportable)
            {
                CTalkNpc reportNpc = CQuestManager.FindNpc(quest.ReportNpcId);
                if (reportNpc == null) continue;

                point = reportNpc.FeetPosition;
                displayName = reportNpc.DisplayName;
                height = _npcHeight;
                return true;
            }

            if (!CQuestManager.TryGetCurrent(quest, out int index, out _)) continue;

            CQuestObjective objective = quest.GetObjective(index);
            if (!CQuestManager.TryGetDestination(objective, out point, out CTalkNpc npc)) continue;

            displayName = objective.destinationName;
            if (npc != null)
            {
                height = _npcHeight;
                if (string.IsNullOrEmpty(displayName)) displayName = npc.DisplayName;
            }

            return true;
        }

        return false;
    }
}
