using System;
using UnityEngine;

/// <summary>
/// 손등 문양의 메시, 텍스처와 발광 설정입니다.
/// </summary>
[CreateAssetMenu(fileName = "CharacterInfoHandMark", menuName = "Espoir/Character Info Hand Mark")]
public class CCharacterInfoHandMarkSO : ScriptableObject
{
    /// <summary>
    /// 손 뼈에 부착하는 고리 효과. 크기는 손등 길이 기준입니다.
    /// </summary>
    [Serializable]
    public struct SRing
    {
        [Tooltip("켜면 초승달 그림, 끄면 마법진 그림")]
        public bool usesCrescent;
        [Tooltip("켜면 손을 감는 고리(손가락 방향이 축), 끄면 손등과 나란한 판")]
        public bool isBracelet;
        [Tooltip("판 한 변의 절반")]
        public float radius;
        [Tooltip("손등에서 띄우는 높이")]
        public float lift;
        [Tooltip("문양 칸 세로 자리 (0 손목 쪽 ~ 1 손가락 쪽)")]
        [Range(0f, 1f)] public float along;
        [Tooltip("기울임 (도)")]
        public float tilt;
        [Tooltip("초당 도는 바퀴 수 (음수 = 반대로)")]
        public float spin;
        [Tooltip("밝기 배율")]
        public float power;
        [Tooltip("흰 선의 밝기 (1 을 넘으면 화면의 빛 번짐이 받음)")]
        public float hot;
        [Tooltip("켜는 순간 한 번 퍼져 나가고 사라지는 판")]
        public bool isBurst;
        [Tooltip("퍼져 나가는 시간 (초)")]
        public float burstDuration;
        [Tooltip("퍼지기 시작할 때의 크기 비율")]
        [Range(0f, 1f)] public float burstFrom;
        [Tooltip("처음 돌아가 있는 각 (라디안)")]
        public float startAngle;
    }

    [Header("메시와 기준 좌표")]
    [Tooltip("문양 메시와 뼈 목록을 공유할 대상 렌더러 이름")]
    [SerializeField] private string _sourceRendererName = "PradonaUpperBody";
    [Tooltip("문양이 있는 손의 뼈 (이 뼈대는 이름의 L/R 이 몸 좌우와 반대라 오른손이 hand.L)")]
    [SerializeField] private string _handBoneName = "hand.L";
    [SerializeField] private string _middleBoneName = "middle_proximal.L";
    [Tooltip("손등 메시. UV는 문양 좌표, UV2.x는 손등 방향 가중치. 재질 슬롯 0은 문양, 1은 발광")]
    [SerializeField] private Mesh _markMesh;
    [Tooltip("손 테두리 발광에 사용하는 메시")]
    [SerializeField] private Mesh _rimMesh;
    [Tooltip("문양 한가운데 (손 뼈 기준)")]
    [SerializeField] private Vector3 _frameCenter;
    [Tooltip("문양 칸의 오른쪽 = 엄지 반대쪽 (손 뼈 기준 방향)")]
    [SerializeField] private Vector3 _frameRight = Vector3.right;
    [Tooltip("문양 칸의 위 = 손가락 쪽 (손 뼈 기준 방향)")]
    [SerializeField] private Vector3 _frameUp = Vector3.up;
    [Tooltip("손등 바깥쪽 (손 뼈 기준 방향)")]
    [SerializeField] private Vector3 _frameNormal = Vector3.forward;
    [Tooltip("손등 길이: 손목 → 가운뎃손가락 뿌리 (메쉬 단위)")]
    [SerializeField, Min(0.0001f)] private float _handLength = 0.07f;
    [Tooltip("문양 칸 한 변 (메쉬 단위)")]
    [SerializeField, Min(0.0001f)] private float _markSize = 0.06f;

    [Header("텍스처")]
    [SerializeField] private Texture2D _crestTexture;
    [SerializeField] private Texture2D _circleTexture;
    [SerializeField] private Texture2D _crescentTexture;

    [Header("셰이더")]
    [SerializeField] private Shader _inkShader;
    [SerializeField] private Shader _glowShader;
    [SerializeField] private Shader _rimShader;
    [SerializeField] private Shader _ringShader;

    [Header("문양 색상")]
    [SerializeField, ColorUsage(false)] private Color _inkColor = new Color32(0xb9, 0x8b, 0xe0, 0xff);
    [Tooltip("문양 색상의 흰색 혼합 비율")]
    [SerializeField, Range(0f, 1f)] private float _inkWhiten = 0.3f;
    [Tooltip("문양 불투명도")]
    [SerializeField, Range(0f, 1f)] private float _inkAmount = 0.448f;

    [Header("문양 발광")]
    [SerializeField, ColorUsage(false)] private Color _glowColor = new Color32(0xdc, 0xcf, 0xff, 0xff);
    [Tooltip("선의 빛을 흰색 쪽으로 섞는 정도")]
    [SerializeField, Range(0f, 1f)] private float _coreWhite = 0.7f;
    [Tooltip("선 밝기")]
    [SerializeField, Min(0f)] private float _core = 1.5f;
    [Tooltip("선 둘레에 더하는 좁은 빛")]
    [SerializeField, Min(0f)] private float _sheen = 0.28f;
    [Tooltip("선 둘레에 더하는 넓은 빛")]
    [SerializeField, Min(0f)] private float _aura = 0.07f;
    [Tooltip("선을 타고 가운데에서 바깥으로 흐르는 빛")]
    [SerializeField, Min(0f)] private float _flow = 0.15f;
    [Tooltip("꺼져 있을 때도 선에 남기는 빛")]
    [SerializeField, Min(0f)] private float _idle;

    [Header("주변 피부 색상")]
    [SerializeField, ColorUsage(false)] private Color _glowTint = new Color32(0x8d, 0x78, 0xdc, 0xff);
    [Tooltip("주변 색상의 흰색 혼합 비율")]
    [SerializeField, Range(0f, 1f)] private float _glowTintWhiten = 0.4f;
    [Tooltip("좁은 범위의 색상 강도")]
    [SerializeField, Min(0f)] private float _halo = 0.5f;
    [Tooltip("넓은 범위의 색상 강도")]
    [SerializeField, Min(0f)] private float _wide = 0.112f;

    [Header("발광 색상 보정")]
    [Tooltip("빛이 센 자리는 아래 피부를 그만큼 덜어 내 하얗게 날아가지 않게 하는 정도 (0 = 순수 더하기)")]
    [SerializeField, Range(0f, 1f)] private float _keepHue = 0.9f;
    [Tooltip("피부 밝기 보정 기준 (선형 색상)")]
    [SerializeField, Range(0.1f, 1f)] private float _skinPeak = 0.9f;

    [Header("손 가장자리 발광")]
    [SerializeField, Min(0f)] private float _rimPower = 0.6f;
    [SerializeField, ColorUsage(false)] private Color _rimColorA = new Color32(0x9d, 0x5c, 0xff, 0xff);
    [SerializeField, ColorUsage(false)] private Color _rimColorB = new Color32(0xd7, 0xa8, 0xff, 0xff);

    [Header("고리 효과")]
    [SerializeField, ColorUsage(false)] private Color _ringColorA = new Color32(0x8a, 0x4d, 0xff, 0xff);
    [SerializeField, ColorUsage(false)] private Color _ringColorB = new Color32(0xd7, 0xa8, 0xff, 0xff);
    [SerializeField] private SRing[] _rings =
    {
        new SRing
        {
            radius = 1.05f, lift = 0.6f, along = 0.5f, spin = 0.1f, power = 0.8f, hot = 2.2f, startAngle = 3.04f,
        },
        new SRing
        {
            usesCrescent = true, isBracelet = true, radius = 0.7f, lift = 0.1f, along = 0.42f, tilt = 24f,
            spin = 0.85f, power = 0.7f, hot = 2.2f, startAngle = 1.74f,
        },
        new SRing
        {
            usesCrescent = true, radius = 2.3f, lift = 0.3f, along = 0.5f, spin = 0.5f, power = 1f, hot = 3f,
            isBurst = true, burstDuration = 0.5f, burstFrom = 0.2f, startAngle = 1.68f,
        },
    };

    [Header("움직임")]
    [Tooltip("켤 때 문양 한가운데에서 끝까지 퍼지는 시간 (초)")]
    [SerializeField, Min(0.01f)] private float _revealTime = 0.32f;
    [Tooltip("발광이 켜지는 속도")]
    [SerializeField, Min(0f)] private float _glowRise = 9f;
    [SerializeField, Min(0f)] private float _glowFall = 3.5f;
    [Tooltip("발광 맥동의 진폭")]
    [SerializeField, Range(0f, 1f)] private float _pulse = 0.06f;
    [SerializeField, Min(0f)] private float _pulseSpeed = 1.4f;
    [Tooltip("배경 흐림에서 빼 줄 자리: 손 뼈 → 가운뎃손가락 뿌리 사이 비율")]
    [SerializeField, Range(0f, 1f)] private float _protectAlong = 0.6f;

    [Header("세기 조절 (1 = 그대로)")]
    [SerializeField, Min(0f)] private float _glowScale = 1f;
    [SerializeField, Min(0f)] private float _ringScale = 1f;
    [SerializeField, Min(0f)] private float _rimScale = 1f;

    public string SourceRendererName => _sourceRendererName;
    public string HandBoneName => _handBoneName;
    public string MiddleBoneName => _middleBoneName;
    public Mesh MarkMesh => _markMesh;
    public Mesh RimMesh => _rimMesh;
    public Vector3 FrameCenter => _frameCenter;
    public Vector3 FrameRight => _frameRight;
    public Vector3 FrameUp => _frameUp;
    public Vector3 FrameNormal => _frameNormal;
    public float HandLength => _handLength;
    public float MarkSize => _markSize;
    public Texture2D CrestTexture => _crestTexture;
    public Texture2D CircleTexture => _circleTexture;
    public Texture2D CrescentTexture => _crescentTexture;
    public Shader InkShader => _inkShader;
    public Shader GlowShader => _glowShader;
    public Shader RimShader => _rimShader;
    public Shader RingShader => _ringShader;
    public float InkAmount => _inkAmount;
    public float CoreWhite => _coreWhite;
    public float Core => _core;
    public float Sheen => _sheen;
    public float Aura => _aura;
    public float Flow => _flow;
    public float Idle => _idle;
    public float Halo => _halo;
    public float Wide => _wide;
    public float KeepHue => _keepHue;
    public float SkinPeak => _skinPeak;
    public float RimPower => _rimPower;
    public SRing[] Rings => _rings;
    public float RevealTime => _revealTime;
    public float GlowRise => _glowRise;
    public float GlowFall => _glowFall;
    public float Pulse => _pulse;
    public float PulseSpeed => _pulseSpeed;
    public float ProtectAlong => _protectAlong;
    public float GlowScale => _glowScale;
    public float RingScale => _ringScale;
    public float RimScale => _rimScale;

    /// <summary>
    /// 무늬 색 (선형, 흰색 쪽으로 섞은 뒤).
    /// </summary>
    public Vector4 InkLinear => Whiten(_inkColor, _inkWhiten);

    /// <summary>
    /// 선에 더하는 빛의 색 (선형).
    /// </summary>
    public Vector4 GlowLinear => Whiten(_glowColor, 0f);

    /// <summary>
    /// 선 둘레 피부를 물들이는 색 (선형, 흰색 쪽으로 섞은 뒤).
    /// </summary>
    public Vector4 GlowTintLinear => Whiten(_glowTint, _glowTintWhiten);

    /// <summary>
    /// 손 가장자리 빛의 바깥 띠 색 (선형).
    /// </summary>
    public Vector4 RimLinearA => Whiten(_rimColorA, 0f);

    /// <summary>
    /// 손 가장자리 빛의 맨 가장자리 색 (선형).
    /// </summary>
    public Vector4 RimLinearB => Whiten(_rimColorB, 0f);

    /// <summary>
    /// 고리의 넓은 번짐 색 (선형).
    /// </summary>
    public Vector4 RingLinearA => Whiten(_ringColorA, 0f);

    /// <summary>
    /// 고리의 선, 띠 색 (선형).
    /// </summary>
    public Vector4 RingLinearB => Whiten(_ringColorB, 0f);

    private static Vector4 Whiten(Color color, float amount)
    {
        Color linear = Color.Lerp(color.linear, Color.white, amount);
        return new Vector4(linear.r, linear.g, linear.b, 1f);
    }
}
