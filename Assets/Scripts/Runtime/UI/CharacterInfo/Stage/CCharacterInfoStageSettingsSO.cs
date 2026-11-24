using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// 캐릭터 정보창의 카메라, 동작과 연출 설정입니다.
/// </summary>
[CreateAssetMenu(fileName = "CharacterInfoStageSettings", menuName = "Espoir/UI/Character Info Stage Settings")]
public class CCharacterInfoStageSettingsSO : ScriptableObject
{
    /// <summary>
    /// 캐릭터 발 위치를 기준으로 한 탭별 카메라 설정.
    /// </summary>
    [Serializable]
    public struct SShot
    {
        [Tooltip("기준 방향의 회전각 (라디안, 양수는 캐릭터 왼쪽)")]
        public float azimuth;
        public float distance;
        public float height;
        public float lookHeight;
        [Tooltip("세로 화각 (도)")]
        public float fieldOfView;
        [Tooltip("캐릭터의 화면 가로 위치 (-1 왼쪽 ~ 1 오른쪽)")]
        public float screenX;
        [Tooltip("캐릭터 회전각 (라디안)")]
        public float face;
        [Tooltip("머무는 동안 카메라가 도는 속도 (라디안/초)")]
        public float drift;
    }

    /// <summary>
    /// 권능 탭의 카메라 키. 무대 좌표는 X: 왼쪽, Y: 위, Z: 카메라 방향입니다.
    /// </summary>
    [Serializable]
    public struct SSwapKey
    {
        [Tooltip("클립 시간 (초)")]
        public float time;
        [Tooltip("이전 키에서 보간하지 않고 전환")]
        public bool isCut;
        public Vector3 lookAt;
        [Tooltip("정면에서 화면 오른쪽으로 돈 각 (도)")]
        public float azimuth;
        public float distance;
        [Tooltip("바라보는 점보다 카메라가 얼마나 위에 있는지")]
        public float heightOffset;
        public float fieldOfView;
        [Tooltip("카메라 기울기 (라디안)")]
        public float roll;
        [Tooltip("배경 흐림 배율")]
        public float blur;
    }

    [Serializable]
    public struct STheme
    {
        public Color light;
        [Tooltip("배경에 곱하는 색 (1 = 그대로)")]
        public Vector3 backgroundTint;
        public Color word;
        public Color haloTop;
        public Color haloBottom;
    }

    [Serializable]
    public struct SAssets
    {
        public GameObject playerVisualPrefab;
        public GameObject helperPrefab;
        public CCharacterInfoHandMarkSO handMark;
        [Tooltip("서 있는 동작: 정보, 능력치, 기록")]
        public AnimationClip[] holdClips;
        [Tooltip("넘어가는 동작: 정보→능력치, 정보→기록, 능력치→정보, 능력치→기록, 기록→정보, 기록→능력치")]
        public AnimationClip[] transitionClips;
        public AnimationClip swapOutClip;
        public AnimationClip helperSwapClip;
        public AnimationClip helperHoldClip;
        [Tooltip("배경 글자 그림: LEVIA, STATS, RECORDS, PRADONA")]
        public Texture2D[] wordTextures;
        public Shader wordShader;
        public Shader haloShader;
        public Shader glintShader;
        [Tooltip("검 모델의 양 끝 (검 오브젝트 기준 좌표). 손에서 먼 쪽을 칼끝으로 본다")]
        public Vector3 swordEndA;
        public Vector3 swordEndB;
        [Tooltip("무대 카메라의 후처리 레이어가 쓰는 Post Processing 기본 자원")]
        public PostProcessResources postProcessResources;
    }

    [Serializable]
    public struct SCamera
    {
        [Tooltip("탭 사이 이동 시간")]
        public float tabTime;
        [Tooltip("탭 전환 감속 계수")]
        public float whipPower;
        [Tooltip("마우스를 따라 카메라가 움직이는 정도")]
        public float parallax;
        [Tooltip("마우스 위치에 따른 수평 회전각 (라디안)")]
        public float parallaxAzimuth;
        public float parallaxHeight;
        public float mouseFollow;
        [Tooltip("도착한 뒤 천천히 다가가는 비율과 걸리는 시간")]
        public float push;
        public float pushTime;
        [Tooltip("열기 시작 시 추가할 회전각 (라디안)")]
        public float introAzimuth;
        public float introDistance;
        public float introHeight;
        public float introTime;
        [Tooltip("게임 카메라에서 무대 카메라로 넘어오는 시간")]
        public float openBlendTime;
        [Tooltip("닫을 때 무대 카메라가 게임 카메라 자리로 돌아가는 시간. 0 이면 바로 끊는다")]
        public float closeBlendTime;
        [Tooltip("드래그 회전량 (화면 높이 1080 기준 라디안/px)")]
        public float dragAngle;
        [UnityEngine.Min(0f)] public float doubleClickTime;
        [UnityEngine.Min(0f)] public float shakeDecay;
        [Tooltip("드래그 관성 배율")]
        public float dragThrow;
        [Tooltip("드래그 관성의 최대 회전 속도 (라디안/초)")]
        public float dragMaxSpeed;
        public float dragDamping;
        [Tooltip("이 화면 높이 비율보다 위, 아래(탭, 닫기 단추 띠)에서 시작한 끌기는 받지 않음")]
        public float dragBand;
        [Tooltip("게임 카메라보다 얼마나 뒤에 그릴지")]
        public float depthOffset;
        [Tooltip("정보창 전용 화면 효과 볼륨을 둘 레이어 (게임 카메라의 볼륨 레이어에 없는 것)")]
        public int volumeLayer;
    }

    [Serializable]
    public struct SOcclusion
    {
        public LayerMask mask;
        [Tooltip("가림 검사 방향의 회전 간격 (라디안)")]
        public float baseStep;
        public int baseTries;
        [Tooltip("가림을 보는 몸 높이 (발 기준)")]
        public Vector3 bodyHeights;
        public float cameraRadius;
    }

    [Serializable]
    public struct SLook
    {
        [Tooltip("배경 흐림 반지름 (1080 높이 기준 px)")]
        public float blur;
        public float dim;
        public Vector3 tint;
        public float saturation;
        public float vignette;
        [Tooltip("화면에서 캐릭터와 먼 곳도 배경처럼 다루는 정도")]
        public float side;
        public float follow;
        [Tooltip("초점 범위와 초점, 화면 중심을 잡는 높이 (발 기준)")]
        public float focusRange;
        public float focusHeight;
        public float centerHeight;
        [Header("순간 효과")]
        public float flash;
        public float zoom;
        [Tooltip("자세가 잡히는 순간의 효과 세기")]
        public float moment;
        public float landFlash;
        [UnityEngine.Min(0f)] public float flashDecay;
        [UnityEngine.Min(0f)] public float zoomDecay;
        [UnityEngine.Min(0f)] public float haloDecay;
        [Header("배경 글자")]
        public float wordOpacity;
        [Tooltip("화면 가로 위치 (-1~1)와 너비 비율")]
        public float wordX;
        public float wordWidth;
        public float wordDistance;
        public Color wordColor;
        public float wipeOutTime;
        public float wipeInTime;
        public float sweepTime;
        public float sweepDelay;
        [Header("후광")]
        public float halo;
        public Color haloTop;
        public Color haloBottom;
        public Vector2 haloSize;
        public float haloDistance;
        public float haloHeight;
        [Header("검 발광")]
        [Tooltip("밝기 배율. 0 이면 끔")]
        public float glint;
        public Color glintColorA;
        public Color glintColorB;
        [Tooltip("가드에서 칼끝까지 흐르는 시간, 창을 연 뒤 처음 흐르기까지의 시간")]
        public float glintTime;
        public float glintOpenDelay;
        [Header("정보 패널 배경")]
        [Tooltip("정보 패널 배경 흐림 반지름 (1080 높이 기준 px)")]
        public float panelBlur;
        [Tooltip("정보 패널의 오른쪽 경계 (0~1)")]
        public float panelEdge;
        public float panelFeather;
    }

    [Serializable]
    public struct SThemeLook
    {
        public float tint;
        public float vignette;
        [Tooltip("후광, 배경 글자 색을 테마 색에 맞추는 정도")]
        public float match;
        public float follow;
        public float colorFollow;
        public STheme player;
        public STheme helper;
    }

    [Serializable]
    public struct SSwap
    {
        [Tooltip("하이파이브 타격 시점 (클립 시간, 초)")]
        public float hitTime;
        public float glowTime;
        public float hideTime;
        public float holdTime;
        [Tooltip("재생 속도 키 (X: 클립 시간, Y: 배율)")]
        public Vector2[] rate;
        [Tooltip("재생 속도 계산 간격 (클립 시간, 초)")]
        [UnityEngine.Min(0.0001f)] public float rateStep;
        public SSwapKey[] keys;
        [Tooltip("카메라가 가릴 때 바꿔 보는 값 (x = 더하는 각(도), y = 거리 배율)")]
        public Vector2[] fitTries;
        public float mouseAzimuth;
        public float mouseHeight;
        [Tooltip("프라도나 조명 방향 (X: 방위각, Y: 고도각, 도)")]
        public Vector2 helperLightAngles;
        [Header("하이파이브")]
        public float startFlash;
        public float hitFlash;
        public float hitZoom;
        public float hitShake;
        public float hitShakeDelay;
        [Header("탭 종료")]
        public float leaveFlash;
        public float leaveZoom;
        [Header("배경 글자")]
        [Tooltip("화면 위치 (-1~1)")]
        public Vector2 wordPosition;
        public float wordWidth;
        public float wordAppearTime;
        public float wordAppearLength;
        public float wordDepth;
        public float wordMinDepth;
        [Header("문양 발광 시 바람")]
        public float gustLength;
        public float gustStrength;
        public float gustDecay;
        public float gustBack;
        public float gustUp;
        [Tooltip("문양 손 둘레를 화면 효과에서 빼는 반지름")]
        public float protectRadius;
    }

    [Serializable]
    public struct SMotion
    {
        public float transitionBlend;
        public float settleBlend;
        public float holdBlend;
        public float swapBlend;
        [Tooltip("넘어가는 동작에서 자세가 잡히는 때 (transitionClips 와 같은 순서)")]
        public float[] arriveTimes;
    }

    [SerializeField] private SAssets _assets;

    [SerializeField] private SShot[] _shots =
    {
        new SShot
        {
            azimuth = -0.28f, distance = 2.1f, height = 1.52f, lookHeight = 1.36f, fieldOfView = 26f,
            screenX = 0.3f, face = -0.2f, drift = 0.012f
        },
        new SShot
        {
            azimuth = 0.42f, distance = 5.2f, height = 1.15f, lookHeight = 1.2f, fieldOfView = 29f,
            screenX = 0.36f, face = 0.1f, drift = 0.01f
        },
        new SShot
        {
            azimuth = 2.75f, distance = 3.4f, height = 1.75f, lookHeight = 1.45f, fieldOfView = 34f,
            screenX = 0.32f, face = 0f, drift = 0.008f
        },
    };

    [SerializeField] private SCamera _camera = new SCamera
    {
        tabTime = 0.9f, whipPower = 11f, parallax = 1.2f, parallaxAzimuth = 0.05f, parallaxHeight = 0.07f,
        mouseFollow = 3f, push = 0.05f, pushTime = 9f, introAzimuth = 2.4f, introDistance = 1.5f, introHeight = 2.8f,
        introTime = 2.3f, openBlendTime = 0.85f, closeBlendTime = 0.6f, dragAngle = 0.006f,
        dragThrow = 0.6f, dragMaxSpeed = 6f,
        dragDamping = 4f, dragBand = 0.07f, depthOffset = 10f, volumeLayer = 1,
        doubleClickTime = 0.3f, shakeDecay = 7f,
    };

    [SerializeField] private SOcclusion _occlusion = new SOcclusion
    {
        mask = 1 | (1 << 3), baseStep = 0.39f, baseTries = 8, bodyHeights = new Vector3(0.42f, 0.93f, 1.53f),
        cameraRadius = 0.25f,
    };

    [SerializeField] private SLook _look = new SLook
    {
        blur = 11f, dim = 0.4f, tint = new Vector3(0.9f, 0.9f, 1.03f), saturation = 0.5f, vignette = 0.34f,
        side = 0.9f, follow = 3.2f, focusRange = 1f, focusHeight = 1f, centerHeight = 1.05f,
        flash = 0.8f, zoom = 0.8f, moment = 1.5f, landFlash = 0.35f,
        flashDecay = 5f, zoomDecay = 9f, haloDecay = 2.2f,
        wordOpacity = 0.62f, wordX = 0.34f, wordWidth = 0.6f, wordDistance = 1.3f,
        wordColor = new Color32(0xff, 0xf0, 0xdc, 0xff), wipeOutTime = 0.16f, wipeInTime = 0.6f, sweepTime = 1.3f,
        sweepDelay = 0.55f,
        halo = 0.5f, haloTop = new Color32(0xff, 0x9c, 0x86, 0xff), haloBottom = new Color32(0x9a, 0xa4, 0xff, 0xff),
        haloSize = new Vector2(3.3f, 3.9f), haloDistance = 0.75f, haloHeight = 1.3f,
        glint = 1f, glintColorA = new Color32(0xff, 0xf2, 0xde, 0xff),
        glintColorB = new Color32(0xff, 0xd4, 0x9a, 0xff),
        glintTime = 0.9f, glintOpenDelay = 1.1f,
        panelBlur = 18f, panelEdge = 0.5417f, panelFeather = 0.2438f,
    };

    [SerializeField] private SThemeLook _theme = new SThemeLook
    {
        tint = 0.2925f, vignette = 0.1575f, match = 0.45f, follow = 5f, colorFollow = 6f,
        player = new STheme
        {
            light = new Color32(0xff, 0x6f, 0x66, 0xff), backgroundTint = new Vector3(1.18f, 0.9f, 0.88f),
            word = new Color32(0xff, 0xe3, 0xda, 0xff), haloTop = new Color32(0xff, 0x8d, 0x76, 0xff),
            haloBottom = new Color32(0xff, 0xb8, 0xa4, 0xff),
        },
        helper = new STheme
        {
            light = new Color32(0xa4, 0x88, 0xff, 0xff), backgroundTint = new Vector3(0.93f, 0.88f, 1.18f),
            word = new Color32(0xd9, 0xc9, 0xff, 0xff), haloTop = new Color32(0xb8, 0x9c, 0xff, 0xff),
            haloBottom = new Color32(0x8f, 0x9d, 0xff, 0xff),
        },
    };

    [SerializeField] private SSwap _swap = new SSwap
    {
        hitTime = 0.9f, glowTime = 2.15f, hideTime = 2.45f, holdTime = 2.6f,
        rate = new[]
        {
            new Vector2(0f, 1f), new Vector2(0.76f, 1f), new Vector2(0.8f, 0.45f), new Vector2(0.862f, 0.45f),
            new Vector2(0.884f, 1f), new Vector2(0.8995f, 1f), new Vector2(0.9f, 0.03f), new Vector2(0.9035f, 0.03f),
            new Vector2(0.912f, 0.3f), new Vector2(0.975f, 0.3f), new Vector2(1.03f, 1f), new Vector2(99f, 1f),
        },
        rateStep = 0.0004f,
        keys = new[]
        {
            new SSwapKey
            {
                time = 0f, isCut = true, lookAt = new Vector3(0.25f, 1.22f, 0f), azimuth = -13f,
                distance = 4f, heightOffset = -0.05f, fieldOfView = 30f, roll = -0.03f, blur = 1f
            },
            new SSwapKey
            {
                time = 0.76f, lookAt = new Vector3(0.42f, 1.36f, 0f), azimuth = -4f, distance = 3f,
                heightOffset = -0.18f, fieldOfView = 30f, roll = 0.02f, blur = 1f
            },
            new SSwapKey
            {
                time = 0.76f, isCut = true, lookAt = new Vector3(0.465f, 1.56f, 0f), azimuth = 8f,
                distance = 2.25f, heightOffset = 0.04f, fieldOfView = 28f, roll = 0f, blur = 0.6f
            },
            new SSwapKey
            {
                time = 1f, lookAt = new Vector3(0.465f, 1.56f, 0f), azimuth = -3f, distance = 1.95f,
                heightOffset = 0.02f, fieldOfView = 28f, roll = 0f, blur = 0.6f
            },
            new SSwapKey
            {
                time = 1f, isCut = true, lookAt = new Vector3(0.52f, 1.24f, 0.14f), azimuth = 14f,
                distance = 3.3f, heightOffset = 0.04f, fieldOfView = 29f, roll = 0f, blur = 0.9f
            },
            new SSwapKey
            {
                time = 2.15f, lookAt = new Vector3(0.09f, 1.34f, 0.3f), azimuth = 10f, distance = 1.72f,
                heightOffset = 0.03f, fieldOfView = 28f, roll = 0f, blur = 0.85f
            },
        },
        fitTries = new[]
        {
            new Vector2(0f, 1f), new Vector2(14f, 1f), new Vector2(-14f, 1f), new Vector2(0f, 0.82f),
            new Vector2(28f, 1f), new Vector2(-28f, 1f), new Vector2(28f, 0.82f), new Vector2(-28f, 0.82f),
            new Vector2(45f, 0.9f), new Vector2(-45f, 0.9f), new Vector2(0f, 0.66f), new Vector2(65f, 0.9f),
            new Vector2(-65f, 0.9f), new Vector2(90f, 0.85f), new Vector2(-90f, 0.85f),
        },
        mouseAzimuth = 2.4f, mouseHeight = 0.05f, helperLightAngles = new Vector2(35f, 32f),
        startFlash = 0.5f, hitFlash = 0.85f, hitZoom = 0.8f, hitShake = 0.65f, hitShakeDelay = 0.12f,
        leaveFlash = 0.8f, leaveZoom = 0.8f,
        wordPosition = new Vector2(0.13f, 0.67f), wordWidth = 0.6f, wordAppearTime = 1.9f, wordAppearLength = 0.5f,
        wordDepth = 1.5f, wordMinDepth = 2.4f,
        gustLength = 1.2f, gustStrength = 1.1f, gustDecay = 0.22f, gustBack = 0.8f, gustUp = 0.42f,
        protectRadius = 0.3f,
    };

    [SerializeField] private SMotion _motion = new SMotion
    {
        transitionBlend = 0.12f, settleBlend = 0.14f, holdBlend = 0.45f, swapBlend = 0.26f,
        arriveTimes = new[] { 0.62f, 0.8f, 0.75f, 0.79f, 0.96f, 0.9f },
    };

    public SAssets Assets => _assets;
    public SShot[] Shots => _shots;
    public SCamera Camera => _camera;
    public SOcclusion Occlusion => _occlusion;
    public SLook Look => _look;
    public SThemeLook Theme => _theme;
    public SSwap Swap => _swap;
    public SMotion Motion => _motion;
}
