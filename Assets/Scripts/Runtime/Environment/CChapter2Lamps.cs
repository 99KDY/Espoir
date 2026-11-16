using UnityEngine;

/// <summary>
/// 2챕터 맵 등불. 기준 위치(플레이어)에 가까운 등불부터 맵 셰이더의 등불 칸에 채운다.
/// </summary>
public class CChapter2Lamps : MonoBehaviour
{
    // 맵 셰이더 Chapter2Common.cginc 의 CHAPTER2_LAMP_SLOTS 와 같아야 한다
    private const int _SLOT_COUNT = 40;

    [System.Serializable]
    public struct SLamp
    {
        public Vector3 position;
        public float radius;
        public Vector3 color;
        public bool flicker;
    }

    [SerializeField] private Transform _target;
    [SerializeField, Range(1, _SLOT_COUNT)] private int _maxLamps = _SLOT_COUNT;
    // 1 = 가운데가 환하고 멀리 길게 풀리는 빛, 0 = 사막 맵과 같은 2단 원
    [SerializeField, Range(0f, 1f)] private float _softness = 1f;
    [SerializeField] private SLamp[] _lamps;

    private readonly Vector4[] _POSITIONS = new Vector4[_SLOT_COUNT];
    private readonly Vector4[] _COLORS = new Vector4[_SLOT_COUNT];
    private float[] _distances;
    private int[] _order;
    private Vector3 _lastCenter;
    private bool _hasOrder;

    private static readonly int _POSITION_ID = Shader.PropertyToID("_Chapter2LampPos");
    private static readonly int _COLOR_ID = Shader.PropertyToID("_Chapter2LampColor");
    private static readonly int _COUNT_ID = Shader.PropertyToID("_Chapter2LampCount");
    private static readonly int _SOFT_ID = Shader.PropertyToID("_Chapter2LampSoft");

    private void Awake()
    {
        _distances = new float[_lamps.Length];
        _order = new int[_lamps.Length];
    }

    private void OnValidate()
    {
        _hasOrder = false;
    }

    private void OnDisable()
    {
        Shader.SetGlobalFloat(_COUNT_ID, 0f);
    }

    private void LateUpdate()
    {
        Vector3 center = _target != null ? _target.position : transform.position;
        if (!_hasOrder || !center.Equals(_lastCenter))
        {
            for (int i = 0; i < _lamps.Length; i++)
            {
                _distances[i] = (_lamps[i].position - center).sqrMagnitude;
                _order[i] = i;
            }
            System.Array.Sort(_distances, _order);
            _lastCenter = center;
            _hasOrder = true;
        }

        float t = Time.time;
        int count = Mathf.Min(_maxLamps, _lamps.Length);
        for (int i = 0; i < count; i++)
        {
            SLamp lamp = _lamps[_order[i]];
            float flicker = lamp.flicker
                ? 0.85f + 0.15f * Mathf.Sin(t * 17f + i) * Mathf.Sin(t * 7.3f)
                : 0.97f + 0.03f * Mathf.Sin(t * 5f + i * 2f);
            _POSITIONS[i] = new Vector4(lamp.position.x, lamp.position.y, lamp.position.z, lamp.radius);
            _COLORS[i] = lamp.color * flicker;
        }
        Shader.SetGlobalVectorArray(_POSITION_ID, _POSITIONS);
        Shader.SetGlobalVectorArray(_COLOR_ID, _COLORS);
        Shader.SetGlobalFloat(_COUNT_ID, count);
        Shader.SetGlobalFloat(_SOFT_ID, _softness);
    }
}
