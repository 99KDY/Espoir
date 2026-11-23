using System;
using UnityEngine;

/// <summary>
/// 캐릭터 정보창의 문구, 색상과 연출 설정입니다.
/// </summary>
[CreateAssetMenu(fileName = "CharacterInfoSettings", menuName = "Espoir/Character Info Settings")]
public class CCharacterInfoSettingsSO : ScriptableObject
{
    [Serializable]
    public struct SIdentity
    {
        public string title;
        public string name;
        public string latinName;
    }

    [Serializable]
    public struct STheme
    {
        [Tooltip("이름, 켜진 탭 글자")]
        public Color cream;
        [Tooltip("영문 이름, 탭 번호, 머리말 글자")]
        public Color accent;
        [Tooltip("칭호")]
        public Color soft;
        [Tooltip("꺼진 탭 글자")]
        public Color tabIdle;
        [Tooltip("켜진 탭 바탕의 왼쪽 끝 색 (오른쪽으로 갈수록 투명)")]
        public Color pill;
        [Tooltip("왼쪽 아래 조작 안내")]
        public Color hint;
    }

    [Serializable]
    public struct SBossStatusStyle
    {
        public string label;
        public Color tagFill;
        public Color tagText;
        [Tooltip("군주 칭호, 이름 글자의 투명도")]
        [Range(0f, 1f)] public float textAlpha;
        [Tooltip("영역, 장소 글자의 투명도")]
        [Range(0f, 1f)] public float placeAlpha;
    }

    [Header("이름표")]
    [SerializeField] private SIdentity _player = new SIdentity
    {
        title = "질투의 군주", name = "레비아", latinName = "LEVIA",
    };
    [SerializeField] private SIdentity _helper = new SIdentity
    {
        title = "조력자", name = "프라도나", latinName = "PRADONA",
    };

    [Header("탭 머리말")]
    [SerializeField] private string[] _tabCaptions = { "PROFILE", "STATS", "RECORDS", "PRADONA" };

    [Header("기록")]
    [SerializeField] private string _bestTimePrefix = "최단 ";
    [SerializeField] private SBossStatusStyle _clearedStyle = new SBossStatusStyle
    {
        label = "처치", tagFill = new Color(1f, 240f / 255f, 220f / 255f, 1f),
        tagText = new Color(24f / 255f, 26f / 255f, 44f / 255f, 1f), textAlpha = 1f, placeAlpha = 0.8f,
    };
    [SerializeField] private SBossStatusStyle _challengingStyle = new SBossStatusStyle
    {
        label = "도전 중", tagFill = new Color(1f, 192f / 255f, 112f / 255f, 0.2f),
        tagText = new Color(1f, 175f / 255f, 123f / 255f, 1f), textAlpha = 1f, placeAlpha = 0.8f,
    };
    [SerializeField] private SBossStatusStyle _unknownStyle = new SBossStatusStyle
    {
        label = "미발견", tagFill = new Color(1f, 1f, 1f, 0.08f),
        tagText = new Color(1f, 1f, 1f, 0.5f), textAlpha = 0.45f, placeAlpha = 0.45f,
    };

    [Header("색상")]
    [SerializeField] private STheme _playerTheme = new STheme
    {
        cream = new Color(1f, 240f / 255f, 220f / 255f, 1f),
        accent = new Color(1f, 175f / 255f, 123f / 255f, 1f),
        soft = new Color(1f, 244f / 255f, 232f / 255f, 0.78f),
        tabIdle = new Color(1f, 244f / 255f, 232f / 255f, 0.62f),
        pill = new Color(1f, 240f / 255f, 222f / 255f, 0.24f),
        hint = new Color(1f, 244f / 255f, 232f / 255f, 0.55f),
    };
    [SerializeField] private STheme _helperTheme = new STheme
    {
        cream = new Color(243f / 255f, 236f / 255f, 1f, 1f),
        accent = new Color(214f / 255f, 196f / 255f, 1f, 1f),
        soft = new Color(228f / 255f, 216f / 255f, 1f, 0.84f),
        tabIdle = new Color(224f / 255f, 210f / 255f, 1f, 0.62f),
        pill = new Color(196f / 255f, 170f / 255f, 1f, 0.28f),
        hint = new Color(224f / 255f, 210f / 255f, 1f, 0.55f),
    };
    [SerializeField, Range(0f, 1f)] private float _loadingAlpha = 0.85f;
    [SerializeField] private Color _closeFill = new Color(14f / 255f, 14f / 255f, 26f / 255f, 0.4f);
    [SerializeField] private Color _closeHoverFill = new Color(1f, 240f / 255f, 222f / 255f, 0.18f);

    [Header("열릴 때")]
    [Tooltip("무대 카메라가 자리를 잡은 뒤 왼쪽 글자가 들어오기 시작하는 시간")]
    [SerializeField, Min(0f)] private float _contentDelay = 1.65f;
    [SerializeField, Min(0f)] private float _fadeTime = 0.5f;
    [SerializeField, Min(0f)] private float _fadeDelay = 0.18f;
    [SerializeField, Min(0f)] private float _letterboxFadeTime = 0.3f;
    [SerializeField, Min(0f)] private float _letterboxTime = 1.1f;
    [SerializeField, Min(0f)] private float _letterboxDelay = 0.1f;
    [SerializeField, Min(0f)] private float _sideInTime = 0.9f;
    [SerializeField, Min(0f)] private float _sideInDistance = 34f;
    [SerializeField, Min(0f)] private float _nameLetterDelay = 0.15f;
    [SerializeField, Min(0f)] private float _nameLetterTime = 1f;
    [SerializeField, Min(0f)] private float _nameLetterStagger = 0.11f;
    [SerializeField, Min(0f)] private float _nameLetterRise = 16f;
    [SerializeField, Min(0f)] private float _firstPageDelay = 0.35f;
    [SerializeField, Min(0f)] private float _firstPageTime = 0.7f;

    [Header("탭을 바꿀 때")]
    [Tooltip("내용이 빠진 뒤 새 내용이 들어오기까지")]
    [SerializeField, Min(0f)] private float _pageSwapDelay = 0.15f;
    [Tooltip("내용, 머리말 번호가 사라지고 나타나는 시간")]
    [SerializeField, Min(0f)] private float _pageFadeTime = 0.35f;
    [Tooltip("내용이 빠져 있는 동안 오른쪽으로 밀려 있는 거리 (px)")]
    [SerializeField, Min(0f)] private float _pageSlideDistance = 12f;
    [SerializeField, Min(0f)] private float _pillTime = 0.5f;
    [SerializeField, Min(0f)] private float _tabColorTime = 0.3f;
    [SerializeField, Min(0f)] private float _numberInTime = 0.6f;
    [SerializeField, Min(0f)] private float _numberInDistance = 26f;
    [SerializeField, Min(0f)] private float _sweepTime = 0.8f;
    [Tooltip("빛 띠가 가장 밝아지는 진행률")]
    [SerializeField, Range(0.01f, 0.99f)] private float _sweepPeak = 0.15f;
    [SerializeField, Min(0f)] private float _loadingFadeTime = 0.3f;

    [Header("내용이 들어올 때")]
    [SerializeField, Min(0f)] private float _itemInTime = 0.5f;
    [SerializeField, Min(0f)] private float _itemInDistance = 10f;
    [SerializeField, Min(0f)] private float _itemStagger = 0.05f;
    [SerializeField, Min(0f)] private float _statInTime = 0.7f;
    [SerializeField, Min(0f)] private float _statInDistance = 12f;
    [SerializeField, Min(0f)] private float _statStagger = 0.055f;
    [SerializeField, Min(0f)] private float _barFillTime = 1.15f;
    [SerializeField, Min(0f)] private float _barFillDelay = 0.2f;
    [SerializeField, Min(0f)] private float _countUpTime = 0.82f;
    [SerializeField, Min(0f)] private float _countUpDelay = 0.14f;
    [SerializeField, Min(0f)] private float _countUpStagger = 0.055f;
    [Tooltip("막대 위로 빛이 지나가는 주기와 처음 기다리는 시간")]
    [SerializeField, Min(0.1f)] private float _barShinePeriod = 3.8f;
    [SerializeField, Min(0f)] private float _barShineDelay = 1.3f;
    [Tooltip("광택 주기 중 이동 시간의 비율")]
    [SerializeField, Range(0.01f, 1f)] private float _barShineMoveRatio = 0.45f;
    [Tooltip("채워진 막대 폭에 대한 광택 폭 비율")]
    [SerializeField, Min(0f)] private float _barShineWidthRatio = 0.6f;
    [Tooltip("채워진 막대 폭 기준 광택 시작과 종료 위치")]
    [SerializeField] private Vector2 _barShineRange = new Vector2(-0.6f, 1f);

    [Header("이름표가 바뀔 때")]
    [SerializeField, Min(0f)] private float _themeTime = 0.5f;
    [SerializeField, Min(0f)] private float _nameSwapTime = 0.55f;
    [SerializeField, Min(0f)] private float _nameSwapDistance = 18f;

    [Header("마우스 시차 효과")]
    [SerializeField] private Vector2 _parallax = new Vector2(8f, 4.8f);
    [Tooltip("60분의 1초마다 목표에 다가가는 비율")]
    [SerializeField, Range(0f, 1f)] private float _parallaxFollow = 0.08f;

    public SIdentity Player => _player;
    public SIdentity Helper => _helper;
    public string BestTimePrefix => _bestTimePrefix;
    public SBossStatusStyle ClearedStyle => _clearedStyle;
    public SBossStatusStyle ChallengingStyle => _challengingStyle;
    public SBossStatusStyle UnknownStyle => _unknownStyle;
    public STheme PlayerTheme => _playerTheme;
    public STheme HelperTheme => _helperTheme;
    public float LoadingAlpha => _loadingAlpha;
    public Color CloseFill => _closeFill;
    public Color CloseHoverFill => _closeHoverFill;
    public float ContentDelay => _contentDelay;
    public float FadeTime => _fadeTime;
    public float FadeDelay => _fadeDelay;
    public float LetterboxFadeTime => _letterboxFadeTime;
    public float LetterboxTime => _letterboxTime;
    public float LetterboxDelay => _letterboxDelay;
    public float SideInTime => _sideInTime;
    public float SideInDistance => _sideInDistance;
    public float NameLetterDelay => _nameLetterDelay;
    public float NameLetterTime => _nameLetterTime;
    public float NameLetterStagger => _nameLetterStagger;
    public float NameLetterRise => _nameLetterRise;
    public float FirstPageDelay => _firstPageDelay;
    public float FirstPageTime => _firstPageTime;
    public float PageSwapDelay => _pageSwapDelay;
    public float PageFadeTime => _pageFadeTime;
    public float PageSlideDistance => _pageSlideDistance;
    public float PillTime => _pillTime;
    public float TabColorTime => _tabColorTime;
    public float NumberInTime => _numberInTime;
    public float NumberInDistance => _numberInDistance;
    public float SweepTime => _sweepTime;
    public float SweepPeak => Mathf.Clamp(_sweepPeak, 0.01f, 0.99f);
    public float LoadingFadeTime => _loadingFadeTime;
    public float ItemInTime => _itemInTime;
    public float ItemInDistance => _itemInDistance;
    public float ItemStagger => _itemStagger;
    public float StatInTime => _statInTime;
    public float StatInDistance => _statInDistance;
    public float StatStagger => _statStagger;
    public float BarFillTime => _barFillTime;
    public float BarFillDelay => _barFillDelay;
    public float CountUpTime => _countUpTime;
    public float CountUpDelay => _countUpDelay;
    public float CountUpStagger => _countUpStagger;
    public float BarShinePeriod => Mathf.Max(0.1f, _barShinePeriod);
    public float BarShineDelay => _barShineDelay;
    public float BarShineMoveRatio => Mathf.Clamp(_barShineMoveRatio, 0.01f, 1f);
    public float BarShineWidthRatio => Mathf.Max(0f, _barShineWidthRatio);
    public Vector2 BarShineRange => _barShineRange;
    public float ThemeTime => _themeTime;
    public float NameSwapTime => _nameSwapTime;
    public float NameSwapDistance => _nameSwapDistance;
    public Vector2 Parallax => _parallax;
    public float ParallaxFollow => _parallaxFollow;

    /// <summary>
    /// 탭 번호에 해당하는 머리말을 반환합니다.
    /// </summary>
    public string GetTabCaption(int tab)
    {
        if (_tabCaptions == null || tab < 0 || tab >= _tabCaptions.Length) return string.Empty;

        return _tabCaptions[tab];
    }
}
