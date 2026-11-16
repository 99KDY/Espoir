using UnityEngine;

/// <summary>
/// 씬 하나가 어떤 맵인지(씬 ID · 영역 이름 · 장소 이름 · 시작 위치) 알려 주는 맵 정보입니다.
/// 맵 입장 띠와 씬 이동이 이 값을 읽습니다.
/// </summary>
public class CMapInfo : MonoBehaviour
{
    [SerializeField] private ESceneId _sceneId = ESceneId.Desert;
    [Tooltip("'ㅁㅁ의 영역' — 장소 이름과 항상 같이 표시")]
    [SerializeField] private string _regionName = "나태의 영역";
    [SerializeField] private string _placeName = "벨페고르 사막";
    [Tooltip("이 맵에 들어왔을 때 플레이어가 서는 자리 (지형 위)")]
    [SerializeField] private Transform _startPoint;

    private static CMapInfo _current;

    /// <summary>지금 열린 맵의 정보 (없으면 null).</summary>
    public static CMapInfo Current => _current;

    /// <summary>이 맵의 씬 ID.</summary>
    public ESceneId SceneId => _sceneId;

    /// <summary>영역 이름 (예: 색욕의 영역).</summary>
    public string RegionName => _regionName;

    /// <summary>장소 이름 (예: 달빛 장미 정원).</summary>
    public string PlaceName => _placeName;

    /// <summary>시작 자리 (없으면 null).</summary>
    public Transform StartPoint => _startPoint;

    private void Awake()
    {
        _current = this;
    }

    private void OnDestroy()
    {
        if (_current == this) _current = null;
    }
}
