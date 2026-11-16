using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 지도와 대상 표식, 확대 단계를 표시합니다.
/// 직교 카메라가 설정된 간격으로 지도를 촬영하고,
/// 플레이어 · NPC · 몬스터 · 보스 표식을 그 촬영 기준의 실제 월드 위치에 놓는다. 확대는 단추 · 휠로 5단계.
/// 맵 데이터가 구운 이미지 예비 방식이면 카메라 없이 그 이미지를 잘라 쓴다.
/// 미니맵 카메라는 후처리 · 오디오 · 이펙트 렌더러 없이 이 컴포넌트가 직접 Render 하고, MainCamera 태그를 쓰지 않는다.
/// </summary>
[DefaultExecutionOrder(100)]
public class CMinimapHud : MonoBehaviour, IScrollHandler
{
    private class CMarkerSlot
    {
        public GameObject gameObject;
        public RectTransform root;
        public RectTransform icon;
        public Image iconImage;
        public bool isFar;
    }

    private static readonly int _UV_CENTER_ID = Shader.PropertyToID("_UvCenter");
    private static readonly int _UV_AXIS_X_ID = Shader.PropertyToID("_UvAxisX");
    private static readonly int _UV_AXIS_Y_ID = Shader.PropertyToID("_UvAxisY");
    private static readonly int _TONE_ID = Shader.PropertyToID("_Tone");

    [SerializeField] private CPlayer _player;
    [Tooltip("화면 방향 기준 카메라. 비우면 Camera.main")]
    [SerializeField] private Camera _camera;
    [SerializeField] private CMinimapMapSO _map;

    [Header("지도")]
    [SerializeField] private RawImage _mapImage;
    [Tooltip("×1.0 에서 지도 가운데부터 가장자리까지의 월드 거리 (m)")]
    [SerializeField] private float _halfRange = 34f;
    [Tooltip("보이는 범위 배수. 앞일수록 확대")]
    [SerializeField] private float[] _zoomSteps = { 0.5f, 0.7f, 1f, 1.4f, 2f };
    [SerializeField] private int _startZoomIndex = 2;
    [Tooltip("60Hz 기준 한 프레임에 남은 확대 차이를 따라가는 비율")]
    [SerializeField, Range(0.01f, 1f)] private float _zoomFollowPerFrame = 0.16f;
    [Tooltip("카메라가 없을 때 화면 위쪽 방향의 Y 회전각")]
    [SerializeField] private float _fallbackYaw = 225f;

    [Header("실시간 지도 카메라")]
    [SerializeField, Min(64)] private int _liveResolution = 384;
    [SerializeField, Min(1)] private int _liveInterval = 3;
    [SerializeField] private float _liveHeight = 200f;
    [SerializeField] private float _liveNearClip = 1f;
    [SerializeField] private float _liveFarClip = 600f;
    [Tooltip("지도 카메라에 표시할 레이어")]
    [SerializeField] private LayerMask _liveCullingMask = ~(1 << 5);
    [Tooltip("미니맵 카메라를 그리는 동안만 쓸 그림자 거리. 카메라가 땅에서 200m 라 기본(150)이면 그림자가 안 찍힘. 0 = 바꾸지 않음")]
    [SerializeField, Min(0f)] private float _liveShadowDistance = 260f;

    [Header("영역과 장소 이름")]
    [SerializeField] private TMP_Text _areaText;
    [SerializeField] private RectTransform _areaTag;
    [Tooltip("영역 이름 배경의 왼쪽 · 오른쪽 여백")]
    [SerializeField] private Vector2 _areaTagPadding = new Vector2(7f, 7f);
    [SerializeField] private TMP_Text _placeText;

    [Header("확대 단추")]
    [SerializeField] private Button _zoomInButton;
    [SerializeField] private Button _zoomOutButton;
    [SerializeField] private CanvasGroup _zoomInGroup;
    [SerializeField] private CanvasGroup _zoomOutGroup;
    [Tooltip("확대 단계 점. 0 번 = 가장 확대(+ 쪽)")]
    [SerializeField] private Image[] _zoomDots;
    [SerializeField] private Image[] _zoomDotHalos;
    [SerializeField] private Color _dotColor = new Color(1f, 1f, 1f, 0.42f);
    [SerializeField] private Color _dotOnColor = Color.white;
    [SerializeField] private float _dotHaloAlpha = 0.18f;
    [SerializeField] private float _dotTransition = 0.2f;
    [Tooltip("끝 단계에서 더 못 누르는 단추의 불투명도")]
    [SerializeField] private float _limitAlpha = 0.32f;
    [SerializeField] private float _limitTransition = 0.2f;

    [Header("표식")]
    [SerializeField] private RectTransform _markerArea;
    [Tooltip("모양별 표식 원본 (EMinimapMarkerKind 순서: Npc · Monster · Boss)")]
    [SerializeField] private RectTransform[] _markerTemplates = new RectTransform[3];
    [SerializeField] private RectTransform _meMarker;
    [SerializeField] private RectTransform _meRing;
    [SerializeField] private Image _meRingImage;
    [Tooltip("지도 밖 표식을 붙여 둘 테두리 안쪽 비율")]
    [SerializeField] private float _edgeLimit = 0.655f;
    [SerializeField] private float _farScale = 0.7f;
    [SerializeField] private float _farAlpha = 0.75f;
    [Tooltip("내 표식 바깥 원: 주기 · 가장 클 때 배율 · 가장 옅을 때 불투명도")]
    [SerializeField] private Vector3 _meRingPulse = new Vector3(2.4f, 1.25f, 0.35f);
    [Tooltip("보스 표식 깜빡임: 주기 · 가장 옅을 때 불투명도")]
    [SerializeField] private Vector2 _bossPulse = new Vector2(1.8f, 0.35f);
    [Tooltip("새로 생긴 적 · 대화 NPC 를 찾아 표식을 붙이는 간격 (초)")]
    [SerializeField, Min(0.05f)] private float _scanInterval = 0.25f;
    [Tooltip("미니맵에 띄우지 않는 대화 NPC ID")]
    [SerializeField] private string[] _hiddenNpcIds = { "gamori" };

    [Header("나타남 · 숨김")]
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private float _fadeDuration = 0.35f;

    private readonly List<CMarkerSlot>[] _SLOTS =
    {
        new List<CMarkerSlot>(), new List<CMarkerSlot>(), new List<CMarkerSlot>()
    };
    private readonly int[] _USED_SLOTS = new int[3];
    private Material _mapMaterial;
    private int _zoomIndex;
    private float _zoomFactor = 1f;
    private float[] _dotProgress;
    private float _zoomInAlpha = 1f;
    private float _zoomOutAlpha = 1f;
    private bool _isVisible = true;
    private float _fadeFrom;
    private float _fadeTo = 1f;
    private float _fadeTime;
    private Camera _liveCamera;
    private RenderTexture _liveTexture;
    private int _liveFrame;
    private bool _hasShot;
    private Vector3 _shotCenter;
    private Vector3 _shotUp;
    private Vector3 _shotRight;
    private float _shotRange;
    private float _nextScanTime;
    private readonly List<GameObject> _SCENE_ROOTS = new List<GameObject>();
    private readonly List<CEnemy> _ENEMIES = new List<CEnemy>();

    /// <summary>지금 확대 단계 (0 = 가장 확대).</summary>
    public int ZoomIndex => _zoomIndex;
    public int ZoomStepCount => _zoomSteps.Length;
    /// <summary>지금 지도 가운데에서 가장자리까지의 월드 거리 (보간 중인 값).</summary>
    public float CurrentRange => _halfRange * _zoomFactor;
    public CMinimapMapSO Map => _map;
    /// <summary>실시간 카메라로 그리는 중이면 true (구운 이미지 예비 방식이면 false).</summary>
    public bool IsLive => _map != null && !_map.UseBakedImage;
    /// <summary>실시간 미니맵 카메라 (실시간 방식일 때만, 직접 Render 하므로 꺼져 있음).</summary>
    public Camera LiveCamera => _liveCamera;

    private void Awake()
    {
        _zoomIndex = Mathf.Clamp(_startZoomIndex, 0, _zoomSteps.Length - 1);
        _zoomFactor = _zoomSteps[_zoomIndex];
        _dotProgress = new float[_zoomDots.Length];
        for (int i = 0; i < _dotProgress.Length; i++) _dotProgress[i] = i == _zoomIndex ? 1f : 0f;
        _zoomInAlpha = _zoomIndex == 0 ? _limitAlpha : 1f;
        _zoomOutAlpha = _zoomIndex == _zoomSteps.Length - 1 ? _limitAlpha : 1f;

        if (_mapImage != null && _mapImage.material != null)
        {
            _mapMaterial = new Material(_mapImage.material);
            _mapImage.material = _mapMaterial;
        }

        foreach (RectTransform template in _markerTemplates)
        {
            if (template != null) template.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (_player == null) _player = FindFirstObjectByType<CPlayer>();
        if (_player == null) CLogger.Warn($"{name}: 미니맵에 플레이어가 없습니다.");
        if (_zoomInButton != null) _zoomInButton.onClick.AddListener(ZoomIn);
        if (_zoomOutButton != null) _zoomOutButton.onClick.AddListener(ZoomOut);

        SetMap(_map);
        ApplyZoomState(true);
        _fadeFrom = 0f;
        _fadeTo = 1f;
        _fadeTime = 0f;
        if (_group != null) _group.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (_zoomInButton != null) _zoomInButton.onClick.RemoveListener(ZoomIn);
        if (_zoomOutButton != null) _zoomOutButton.onClick.RemoveListener(ZoomOut);
        if (_mapMaterial != null) Destroy(_mapMaterial);
        ReleaseLiveCamera();
    }

    /// <summary>맵 데이터(이름 · 지도 이미지 · 월드 범위)를 바꾼다. 씬 이동 뒤 HUD 를 다시 쓸 때 부른다.</summary>
    public void SetMap(CMinimapMapSO map)
    {
        _map = map;
        if (_map == null)
        {
            if (_mapImage != null) _mapImage.enabled = false;
            ReleaseLiveCamera();
            _hasShot = false;
            return;
        }

        _hasShot = false;
        if (IsLive) CreateLiveCamera();
        else ReleaseLiveCamera();

        if (_mapImage != null)
        {
            _mapImage.texture = IsLive ? _liveTexture : _map.MapTexture;
            _mapImage.enabled = false;   // 첫 촬영(또는 구운 이미지 UV 계산) 뒤에 켬
        }

        if (_mapMaterial != null)
        {
            // 실시간: 렌더 텍스처를 칸에 그대로(UV = 칸 UV), 톤 곡선은 셰이더에서. 예비: 톤은 구울 때 적용됨
            _mapMaterial.SetFloat(_TONE_ID, IsLive ? 1f : 0f);
            _mapMaterial.SetVector(_UV_CENTER_ID, new Vector4(0.5f, 0.5f, 0f, 0f));
            _mapMaterial.SetVector(_UV_AXIS_X_ID, new Vector4(0.5f, 0f, 0f, 0f));
            _mapMaterial.SetVector(_UV_AXIS_Y_ID, new Vector4(0f, 0.5f, 0f, 0f));
        }

        // 영역 · 장소 이름은 씬의 맵 정보(CMapInfo)에서 읽는다 (입장 띠와 같은 값)
        CMapInfo info = CMapInfo.Current;
        string areaName = info != null ? info.RegionName : string.Empty;
        string placeName = info != null ? info.PlaceName : string.Empty;
        if (info == null) CLogger.Warn($"{name}: 씬에 CMapInfo 가 없어 미니맵 이름을 비웁니다.");

        if (_areaText != null)
        {
            _areaText.text = areaName;
            _areaText.ForceMeshUpdate();
            if (_areaTag != null)
            {
                // CSS 자간은 마지막 글자 뒤에도 붙어 딱지 폭에 들어감 (TMP 선호 폭에는 없음)
                float trailingSpacing = _areaText.characterSpacing * 0.01f * _areaText.fontSize;
                Vector2 size = _areaTag.sizeDelta;
                size.x = _areaTagPadding.x + _areaText.preferredWidth + trailingSpacing + _areaTagPadding.y;
                _areaTag.sizeDelta = size;
            }
        }

        if (_placeText != null) _placeText.text = placeName;
    }

    /// <summary>설정된 전환 시간에 맞춰 미니맵을 보이거나 숨깁니다.</summary>
    public void SetVisible(bool isVisible)
    {
        if (_isVisible == isVisible) return;
        _isVisible = isVisible;
        _fadeFrom = _group != null ? _group.alpha : 1f;
        _fadeTo = isVisible ? 1f : 0f;
        _fadeTime = 0f;
        if (_group != null) _group.blocksRaycasts = isVisible;
    }

    /// <summary>한 단계 확대 (+ 단추).</summary>
    public void ZoomIn()
    {
        SetZoomIndex(_zoomIndex - 1);
        ReleaseButtonSelection();
    }

    /// <summary>한 단계 축소 (− 단추).</summary>
    public void ZoomOut()
    {
        SetZoomIndex(_zoomIndex + 1);
        ReleaseButtonSelection();
    }

    // 클릭 후 선택 강조가 남지 않도록 선택을 해제합니다.
    private static void ReleaseButtonSelection()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || eventSystem.currentSelectedGameObject == null) return;
        eventSystem.SetSelectedGameObject(null);
    }

    /// <summary>확대 단계를 정한다 (0 = 가장 확대). 범위를 넘으면 끝 단계로 자른다.</summary>
    public void SetZoomIndex(int index)
    {
        int next = Mathf.Clamp(index, 0, _zoomSteps.Length - 1);
        if (next == _zoomIndex) return;
        _zoomIndex = next;
        ApplyZoomState(false);
    }

    /// <summary>미니맵 위 휠: 위로 굴리면 확대, 아래로 굴리면 축소.</summary>
    public void OnScroll(PointerEventData eventData)
    {
        if (Mathf.Approximately(eventData.scrollDelta.y, 0f)) return;
        SetZoomIndex(_zoomIndex - (eventData.scrollDelta.y > 0f ? 1 : -1));
    }

    private void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        UpdateFade(dt);
        UpdateZoomVisuals(dt);
        if (!_isVisible && (_group == null || _group.alpha <= 0f)) return;

        if (Time.unscaledTime >= _nextScanTime) AttachMarkers();

        if (_player == null) return;

        float follow = 1f - Mathf.Pow(1f - _zoomFollowPerFrame, dt * 60f);
        _zoomFactor += (_zoomSteps[_zoomIndex] - _zoomFactor) * follow;
        float range = CurrentRange;

        Vector3 up;
        Vector3 right;
        GetScreenAxes(out up, out right);
        Vector3 center = _player.transform.position;

        if (IsLive)
        {
            // 촬영 사이에도 표식은 마지막 촬영 위치를 기준으로 갱신해 지도와 정렬합니다.
            bool isDrawn = _isVisible || (_group != null && _group.alpha > 0f);
            if (isDrawn && (!_hasShot || _liveFrame % _liveInterval == 0)) RenderLive(center, up, right, range);
            _liveFrame++;
            if (!_hasShot) return;
            UpdateMarkers(center, _shotCenter, _shotUp, _shotRight, _shotRange);
        }

        else
        {
            UpdateMapImage(center, up, right, range);
            UpdateMarkers(center, center, up, right, range);
        }

        UpdateMeRing();
    }

    // 실행 중 생긴 적(소환된 보스 포함) · 대화 NPC 에도 표식을 붙인다. 모양은 CMinimapMarker 가 컴포넌트로 정함
    private void AttachMarkers()
    {
        _nextScanTime = Time.unscaledTime + _scanInterval;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            scene.GetRootGameObjects(_SCENE_ROOTS);
            foreach (GameObject root in _SCENE_ROOTS)
            {
                if (!root.activeInHierarchy) continue;
                root.GetComponentsInChildren(false, _ENEMIES);
                foreach (CEnemy enemy in _ENEMIES)
                {
                    if (!enemy.TryGetComponent(out CMinimapMarker _))
                    {
                        enemy.gameObject.AddComponent<CMinimapMarker>();
                    }
                }
            }
        }
        _SCENE_ROOTS.Clear();
        _ENEMIES.Clear();

        IReadOnlyList<CTalkNpc> npcs = CTalkNpc.All;
        for (int i = 0; i < npcs.Count; i++)
        {
            CTalkNpc npc = npcs[i];
            if (System.Array.IndexOf(_hiddenNpcIds, npc.NpcId) >= 0) continue;
            if (!npc.TryGetComponent(out CMinimapMarker _)) npc.gameObject.AddComponent<CMinimapMarker>();
        }
    }

    private void CreateLiveCamera()
    {
        if (_liveCamera != null) return;

        _liveTexture = new RenderTexture(_liveResolution, _liveResolution, 24, RenderTextureFormat.ARGBHalf,
            RenderTextureReadWrite.Linear)
        {
            name = "MinimapLive",
            antiAliasing = 4
        };
        _liveTexture.Create();

        // 씬에 저장되지 않는 맨 카메라: 후처리(PostProcessLayer) · 이펙트 렌더러 · AudioListener 없음, MainCamera 태그 아님
        GameObject go = new GameObject("MinimapLiveCamera") { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(transform, false);
        _liveCamera = go.AddComponent<Camera>();
        _liveCamera.enabled = false;
        _liveCamera.orthographic = true;
        _liveCamera.aspect = 1f;
        _liveCamera.allowHDR = true;
        _liveCamera.allowMSAA = true;
        _liveCamera.clearFlags = CameraClearFlags.SolidColor;
        _liveCamera.backgroundColor = Color.black;
        _liveCamera.nearClipPlane = _liveNearClip;
        _liveCamera.farClipPlane = _liveFarClip;
        _liveCamera.cullingMask = _liveCullingMask;
        _liveCamera.useOcclusionCulling = false;
        _liveCamera.targetTexture = _liveTexture;
    }

    private void ReleaseLiveCamera()
    {
        if (_liveCamera != null) Destroy(_liveCamera.gameObject);
        _liveCamera = null;
        if (_liveTexture == null) return;
        _liveTexture.Release();
        Destroy(_liveTexture);
        _liveTexture = null;
    }

    private void RenderLive(Vector3 center, Vector3 up, Vector3 right, float range)
    {
        if (_liveCamera == null) return;

        _liveCamera.orthographicSize = range;
        _liveCamera.transform.SetPositionAndRotation(center + Vector3.up * _liveHeight,
            Quaternion.LookRotation(Vector3.down, up));   // 화면 위 = 본 화면의 위쪽 방향

        float shadowDistance = QualitySettings.shadowDistance;
        if (_liveShadowDistance > 0f) QualitySettings.shadowDistance = _liveShadowDistance;
        try
        {
            _liveCamera.Render();
        }
        finally
        {
            QualitySettings.shadowDistance = shadowDistance;
        }

        _shotCenter = center;
        _shotUp = up;
        _shotRight = right;
        _shotRange = range;
        if (!_hasShot && _mapImage != null) _mapImage.enabled = true;
        _hasShot = true;
    }

    private void GetScreenAxes(out Vector3 up, out Vector3 right)
    {
        Camera cam = _camera != null ? _camera : Camera.main;
        up = Vector3.zero;
        if (cam != null)
        {
            up = cam.transform.forward;
            up.y = 0f;
            if (up.sqrMagnitude < 0.0001f)
            {
                up = cam.transform.up;
                up.y = 0f;
            }
        }

        if (up.sqrMagnitude < 0.0001f) up = Quaternion.Euler(0f, _fallbackYaw, 0f) * Vector3.forward;
        up.Normalize();
        right = new Vector3(up.z, 0f, -up.x);
    }

    private void UpdateMapImage(Vector3 center, Vector3 up, Vector3 right, float range)
    {
        if (_mapMaterial == null || _map == null) return;

        if (_mapImage != null && !_mapImage.enabled) _mapImage.enabled = true;
        Vector2 size = _map.WorldSize;
        Vector2 uv = _map.WorldToUv(center);
        _mapMaterial.SetVector(_UV_CENTER_ID, new Vector4(uv.x, uv.y, 0f, 0f));
        _mapMaterial.SetVector(_UV_AXIS_X_ID, new Vector4(right.x * range / size.x, right.z * range / size.y, 0f, 0f));
        _mapMaterial.SetVector(_UV_AXIS_Y_ID, new Vector4(up.x * range / size.x, up.z * range / size.y, 0f, 0f));
    }

    // center는 마지막 촬영 위치입니다. 플레이어 표식도 이 위치를 기준으로 움직입니다.
    private void UpdateMarkers(Vector3 player, Vector3 center, Vector3 up, Vector3 right, float range)
    {
        for (int k = 0; k < _USED_SLOTS.Length; k++) _USED_SLOTS[k] = 0;

        float half = _markerArea.rect.width * 0.5f;
        if (_meMarker != null)
        {
            Vector3 me = player - center;
            float meX = Vector3.Dot(me, right) / range * half;
            float meY = Vector3.Dot(me, up) / range * half;
            _meMarker.anchoredPosition = new Vector2(meX, meY);
        }

        float bossAlpha = PulseAlpha(Time.unscaledTime, _bossPulse.x, _bossPulse.y);
        IReadOnlyList<CMinimapMarker> markers = CMinimapMarker.Active;
        for (int i = 0; i < markers.Count; i++)
        {
            CMinimapMarker marker = markers[i];
            if (!marker.IsShown) continue;

            int kind = (int)marker.Kind;
            CMarkerSlot slot = GetSlot(kind, _USED_SLOTS[kind]);
            if (slot == null) continue;
            _USED_SLOTS[kind]++;

            // 지도 좌표는 x 오른쪽 · y 아래쪽입니다. max(|x|,|y|)로 사각형 가장자리를 판정합니다.
            Vector3 delta = marker.WorldPosition - center;
            float x = Vector3.Dot(delta, right) / range;
            float y = -Vector3.Dot(delta, up) / range;
            float distance = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
            bool isFar = distance > _edgeLimit;
            if (isFar)
            {
                x *= _edgeLimit / distance;
                y *= _edgeLimit / distance;
            }

            if (!slot.gameObject.activeSelf) slot.gameObject.SetActive(true);
            slot.root.anchoredPosition = new Vector2(x * half, -y * half);
            if (slot.isFar != isFar)
            {
                slot.isFar = isFar;
                slot.icon.localScale = Vector3.one * (isFar ? _farScale : 1f);
            }

            float alpha = marker.Kind == EMinimapMarkerKind.Boss ? bossAlpha : (isFar ? _farAlpha : 1f);
            Color color = slot.iconImage.color;
            if (!Mathf.Approximately(color.a, alpha))
            {
                color.a = alpha;
                slot.iconImage.color = color;
            }
        }

        for (int k = 0; k < _SLOTS.Length; k++)
        {
            List<CMarkerSlot> list = _SLOTS[k];
            for (int s = _USED_SLOTS[k]; s < list.Count; s++)
            {
                if (list[s].gameObject.activeSelf) list[s].gameObject.SetActive(false);
            }
        }
    }

    private CMarkerSlot GetSlot(int kind, int index)
    {
        List<CMarkerSlot> list = _SLOTS[kind];
        if (index < list.Count) return list[index];

        RectTransform template = kind < _markerTemplates.Length ? _markerTemplates[kind] : null;
        if (template == null) return null;

        RectTransform root = Instantiate(template, template.parent);
        root.name = template.name + " " + index;
        Image image = root.GetComponentInChildren<Image>(true);
        CMarkerSlot slot = new CMarkerSlot
        {
            gameObject = root.gameObject,
            root = root,
            icon = image.rectTransform,
            iconImage = image
        };
        list.Add(slot);
        return slot;
    }

    private void UpdateMeRing()
    {
        if (_meRing == null) return;

        // 주기 중간에 가장 크고 옅어졌다가 원래 상태로 돌아옵니다.
        float period = _meRingPulse.x;
        float phase = Mathf.Repeat(Time.unscaledTime, period) / period;
        float k = phase < 0.5f ? EaseInOut(phase * 2f) : 1f - EaseInOut((phase - 0.5f) * 2f);
        _meRing.localScale = Vector3.one * Mathf.Lerp(1f, _meRingPulse.y, k);
        if (_meRingImage != null)
        {
            Color color = _meRingImage.color;
            color.a = Mathf.Lerp(1f, _meRingPulse.z, k);
            _meRingImage.color = color;
        }
    }

    private void ApplyZoomState(bool isInstant)
    {
        if (_zoomInButton != null) _zoomInButton.interactable = _zoomIndex > 0;
        if (_zoomOutButton != null) _zoomOutButton.interactable = _zoomIndex < _zoomSteps.Length - 1;
        if (!isInstant) return;

        for (int i = 0; i < _dotProgress.Length; i++) _dotProgress[i] = i == _zoomIndex ? 1f : 0f;
        _zoomInAlpha = _zoomIndex == 0 ? _limitAlpha : 1f;
        _zoomOutAlpha = _zoomIndex == _zoomSteps.Length - 1 ? _limitAlpha : 1f;
        UpdateZoomVisuals(0f);
    }

    private void UpdateZoomVisuals(float dt)
    {
        float dotStep = _dotTransition > 0f ? dt / _dotTransition : 1f;
        for (int i = 0; i < _zoomDots.Length; i++)
        {
            float target = i == _zoomIndex ? 1f : 0f;
            _dotProgress[i] = Mathf.MoveTowards(_dotProgress[i], target, dotStep);
            float k = Ease(_dotProgress[i]);
            if (_zoomDots[i] != null) _zoomDots[i].color = Color.Lerp(_dotColor, _dotOnColor, k);
            if (_zoomDotHalos != null && i < _zoomDotHalos.Length && _zoomDotHalos[i] != null)
            {
                Color halo = _zoomDotHalos[i].color;
                halo.a = _dotHaloAlpha * k;
                _zoomDotHalos[i].color = halo;
            }
        }

        float limitStep = _limitTransition > 0f ? dt * (1f - _limitAlpha) / _limitTransition : 1f;
        float zoomInTarget = _zoomIndex == 0 ? _limitAlpha : 1f;
        float zoomOutTarget = _zoomIndex == _zoomSteps.Length - 1 ? _limitAlpha : 1f;
        _zoomInAlpha = Mathf.MoveTowards(_zoomInAlpha, zoomInTarget, limitStep);
        _zoomOutAlpha = Mathf.MoveTowards(_zoomOutAlpha, zoomOutTarget, limitStep);
        if (_zoomInGroup != null) _zoomInGroup.alpha = EasedLimitAlpha(_zoomInAlpha);
        if (_zoomOutGroup != null) _zoomOutGroup.alpha = EasedLimitAlpha(_zoomOutAlpha);
    }

    private float EasedLimitAlpha(float linearAlpha)
    {
        float k = Mathf.InverseLerp(_limitAlpha, 1f, linearAlpha);
        return Mathf.Lerp(_limitAlpha, 1f, Ease(k));
    }

    private void UpdateFade(float dt)
    {
        if (_group == null) return;
        if (_fadeTime >= _fadeDuration && Mathf.Approximately(_group.alpha, _fadeTo)) return;

        _fadeTime += dt;
        float k = _fadeDuration > 0f ? Mathf.Clamp01(_fadeTime / _fadeDuration) : 1f;
        _group.alpha = Mathf.Lerp(_fadeFrom, _fadeTo, Ease(k));
    }

    private static float PulseAlpha(float time, float period, float minAlpha)
    {
        // 주기 중간에 가장 밝아졌다가 최소 불투명도로 돌아옵니다.
        float phase = Mathf.Repeat(time, period) / period;
        float k = phase < 0.5f ? Ease(phase * 2f) : 1f - Ease((phase - 0.5f) * 2f);
        return Mathf.Lerp(minAlpha, 1f, k);
    }

    // CSS 'ease' = cubic-bezier(.25, .1, .25, 1)
    private static float Ease(float x)
    {
        return CubicBezier(0.25f, 0.1f, 0.25f, 1f, x);
    }

    // CSS 'ease-in-out' = cubic-bezier(.42, 0, .58, 1)
    private static float EaseInOut(float x)
    {
        return CubicBezier(0.42f, 0f, 0.58f, 1f, x);
    }

    private static float CubicBezier(float x1, float y1, float x2, float y2, float x)
    {
        if (x <= 0f) return 0f;
        if (x >= 1f) return 1f;

        // x(t) = x 인 t 를 뉴턴법으로 찾고 y(t) 를 돌려준다
        float t = x;
        for (int i = 0; i < 8; i++)
        {
            float error = Bezier(x1, x2, t) - x;
            if (Mathf.Abs(error) < 0.00001f) break;
            float slope = BezierSlope(x1, x2, t);
            if (Mathf.Abs(slope) < 0.000001f) break;
            t = Mathf.Clamp01(t - error / slope);
        }

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
}
