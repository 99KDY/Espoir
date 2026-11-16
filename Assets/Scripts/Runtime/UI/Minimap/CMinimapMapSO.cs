using UnityEngine;

/// <summary>
/// 맵 하나의 미니맵 데이터. 씬마다 하나 만들어 그 씬의 CMinimapHud 에 연결한다 (영역 · 장소 이름은 씬의 CMapInfo 가 가짐).
/// 지도는 기본으로 미니맵 카메라가 실시간으로 찍는다. 구운 이미지는 예비 방식(_useBakedImage)일 때만 쓰며,
/// 에디터 메뉴 Espoir/Minimap 으로 굽는다.
/// </summary>
[CreateAssetMenu(fileName = "Minimap", menuName = "Espoir/Minimap Map")]
public class CMinimapMapSO : ScriptableObject
{
    [Header("예비: 구운 지도 이미지 (u = 월드 +X, v = 월드 +Z)")]
    [Tooltip("끄면(기본) 실시간 미니맵 카메라. 켜면 아래 구운 이미지를 씀")]
    [SerializeField] private bool _useBakedImage;
    [SerializeField] private Texture2D _mapTexture;
    [Tooltip("이미지 가운데의 월드 X · Z")]
    [SerializeField] private Vector2 _worldCenter;
    [Tooltip("이미지가 덮는 월드 크기 X · Z (m)")]
    [SerializeField] private Vector2 _worldSize = new Vector2(384f, 384f);

    [Header("지도 굽기 (에디터 도구가 읽음)")]
    [Tooltip("이 이름의 씬 루트 아래 렌더러만 찍는다 (플레이어 · 몬스터 · UI 제외)")]
    [SerializeField] private string _bakeRootName = "DesertMap";
    [SerializeField, Min(256)] private int _bakeResolution = 3072;
    [Tooltip("한 변을 몇 칸으로 나눠 찍을지. 시선 방향을 쓰는 모래 셰이더는 칸 경계가 보일 수 있어 1 권장")]
    [SerializeField, Min(1)] private int _bakeTiles = 1;
    [Tooltip("지형 밖으로 더 찍을 여백 (m). 가장 멀리 본 반 변(34 x 2)보다 커야 가장자리가 늘어나 보이지 않음")]
    [SerializeField, Min(0f)] private float _bakeMargin = 72f;
    [Tooltip("이보다 큰 렌더러는 범위 계산에서 뺀다 (하늘 · 먼 배경 산)")]
    [SerializeField, Min(1f)] private float _bakeIgnoreLargerThan = 300f;

    public bool UseBakedImage => _useBakedImage && _mapTexture != null;
    public Texture2D MapTexture => _mapTexture;
    public Vector2 WorldCenter => _worldCenter;
    public Vector2 WorldSize => _worldSize;
    public string BakeRootName => _bakeRootName;
    public int BakeResolution => _bakeResolution;
    public int BakeTiles => _bakeTiles;
    public float BakeMargin => _bakeMargin;
    public float BakeIgnoreLargerThan => _bakeIgnoreLargerThan;

    /// <summary>월드 위치(XZ)를 지도 이미지 UV 로 바꾼다.</summary>
    public Vector2 WorldToUv(Vector3 world)
    {
        float u = (world.x - _worldCenter.x) / _worldSize.x + 0.5f;
        float v = (world.z - _worldCenter.y) / _worldSize.y + 0.5f;
        return new Vector2(u, v);
    }
}
