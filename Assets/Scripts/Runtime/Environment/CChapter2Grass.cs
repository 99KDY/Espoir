using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 2챕터 맵 들풀과 꽃. 플레이어 둘레 반경 안의 8m 칸만 만들어 인스턴스로 그린다.
/// </summary>
public class CChapter2Grass : MonoBehaviour
{
    private const float _CHUNK_SIZE = 8f;
    private const int _CLUMP_TYPES = 4;
    private const int _CHUNKS_PER_FRAME = 6;
    private const int _CACHE_LIMIT = 24;
    private static readonly float _WIND_YAW = Mathf.Atan2(0.6f, 0.8f);

    [Header("References")]
    [SerializeField] private Transform _target;
    [SerializeField] private TextAsset _fieldData;
    [SerializeField] private Mesh[] _clumpMeshes = new Mesh[_CLUMP_TYPES];
    [SerializeField] private Material _bladeMaterial;
    [SerializeField] private Mesh _stalkMesh;
    [SerializeField] private Mesh _glowFlowerMesh;
    [SerializeField] private Mesh _petalFlowerMesh;
    [SerializeField] private Material _stalkMaterial;
    [SerializeField] private Material[] _glowFlowerMaterials;
    [SerializeField] private Material[] _petalFlowerMaterials;

    [Header("Grass")]
    [SerializeField] private float _radius = 44f;
    [SerializeField] private float _density = 11f;
    [SerializeField] private float _height = 0.34f;
    [SerializeField] private float _heightVariation = 0.3f;
    [SerializeField] private float _wide = 1.25f;

    [Header("Ground Under Grass")]
    [SerializeField] private float _under = 0.86f;
    [SerializeField] private float _wave = 0.07f;
    [SerializeField] private float _rim = 0.3f;
    // x = 시작 거리, y = 끝 거리, z = 먼 곳 대비
    [SerializeField] private Vector3 _lod = new Vector3(24f, 80f, 0.3f);

    [Header("Moon Flowers")]
    [SerializeField] private float _glowPatch = 0.0055f;
    [SerializeField] private Vector2Int _glowCount = new Vector2Int(6, 11);
    [SerializeField] private float _glowSpread = 1f;
    [SerializeField] private float _glowSize = 0.13f;
    [SerializeField] private float _petalPatch = 0.006f;
    [SerializeField] private Vector2Int _petalCount = new Vector2Int(9, 18);
    [SerializeField] private float _petalSpread = 2f;
    [SerializeField] private float _petalSize = 0.11f;

    private class CChunk
    {
        public Matrix4x4[][] clumps = new Matrix4x4[_CLUMP_TYPES][];
        public Matrix4x4[] stalks;
        public Matrix4x4[] glowHeads;
        public int glowMaterial;
        public Matrix4x4[] petals;
        public int petalMaterial;
    }

    private struct SFootprint
    {
        public bool circle;
        public float x;
        public float z;
        public float rot;
        public double cosRotation;
        public double sinRotation;
        public float extent;
        public float w;
        public float d;
        public float r;
        public float top;
    }

    private readonly Dictionary<Vector2Int, CChunk> _CHUNKS = new Dictionary<Vector2Int, CChunk>();
    private readonly List<Vector2Int> _DROP_KEYS = new List<Vector2Int>();
    private readonly List<Matrix4x4>[] _CLUMP_LISTS = new List<Matrix4x4>[_CLUMP_TYPES];
    private readonly List<SFootprint> _NEAR_FOOTPRINTS = new List<SFootprint>();
    private readonly List<Matrix4x4> _STALKS = new List<Matrix4x4>();
    private readonly List<Matrix4x4> _HEADS = new List<Matrix4x4>();
    private readonly List<Matrix4x4> _PETALS = new List<Matrix4x4>();
    private int _gridSize;
    private float _gridHalf;
    private float _gridStep;
    private float _waterY;
    private float[] _heights;
    private float[] _paveMask;
    private SFootprint[] _footprints;
    private Vector4[] _plinths;
    private CSimplexNoise _noise;
    private bool _isReady;
    private Vector2Int _lastCell;
    private float _lastRadius;
    private bool _pendingChunks = true;
    private readonly Dictionary<Vector2Int, CChunk> _CACHED_CHUNKS = new Dictionary<Vector2Int, CChunk>();
    private readonly Queue<Vector2Int> _CACHE_ORDER = new Queue<Vector2Int>();

    private static readonly int _CENTER_ID = Shader.PropertyToID("_Chapter2GrassCenter");
    private static readonly int _UNDER_ID = Shader.PropertyToID("_Chapter2GrassUnder");
    private static readonly int _WAVE_ID = Shader.PropertyToID("_Chapter2GrassWave");
    private static readonly int _RIM_ID = Shader.PropertyToID("_Chapter2GrassRim");
    private static readonly int _LOD_ID = Shader.PropertyToID("_Chapter2GrassLod");

    private void Awake()
    {
        for (int i = 0; i < _CLUMP_TYPES; i++)
        {
            _CLUMP_LISTS[i] = new List<Matrix4x4>(512);
        }
        _noise = new CSimplexNoise(911);
        ReadField(_fieldData.bytes);
    }

    private void OnValidate()
    {
        _CHUNKS.Clear();
        _CACHED_CHUNKS.Clear();
        _CACHE_ORDER.Clear();
        _isReady = false;
        _pendingChunks = true;
    }

    private void OnDisable()
    {
        Shader.SetGlobalVector(_CENTER_ID, Vector4.zero);
    }

    private void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }
        Vector3 fieldPosition = new Vector3(-_target.position.x, _target.position.y, _target.position.z);
        UpdateChunks(fieldPosition);
        _isReady |= !_pendingChunks;
        Shader.SetGlobalVector(_CENTER_ID, _isReady
            ? new Vector4(_target.position.x, _target.position.z, _radius, 0f)
            : Vector4.zero);
        Shader.SetGlobalFloat(_UNDER_ID, _under);
        Shader.SetGlobalFloat(_WAVE_ID, _wave);
        Shader.SetGlobalFloat(_RIM_ID, _rim);
        Shader.SetGlobalVector(_LOD_ID, _lod);

        if (_isReady)
        {
            DrawChunks();
        }
    }

    private void UpdateChunks(Vector3 fieldPosition)
    {
        int cx = Mathf.FloorToInt(fieldPosition.x / _CHUNK_SIZE);
        int cz = Mathf.FloorToInt(fieldPosition.z / _CHUNK_SIZE);
        Vector2Int center = new Vector2Int(cx, cz);
        if (!_pendingChunks && center == _lastCell && _radius == _lastRadius)
        {
            return;
        }
        _lastCell = center;
        _lastRadius = _radius;
        int rc = Mathf.CeilToInt(_radius / _CHUNK_SIZE);
        int limit = _CHUNKS_PER_FRAME;
        int made = 0;
        for (int dz = -rc; dz <= rc && made < limit; dz++)
        {
            for (int dx = -rc; dx <= rc && made < limit; dx++)
            {
                float loadRadius = _radius / _CHUNK_SIZE + 0.5f;
                if (dx * dx + dz * dz > loadRadius * loadRadius)
                {
                    continue;
                }
                Vector2Int key = new Vector2Int(cx + dx, cz + dz);
                if (_CHUNKS.ContainsKey(key))
                {
                    continue;
                }
                if (_CACHED_CHUNKS.TryGetValue(key, out CChunk cached))
                {
                    _CHUNKS.Add(key, cached);
                }

                else
                {
                    _CHUNKS.Add(key, MakeChunk(key.x, key.y));
                }
                made++;
            }
        }
        _pendingChunks = made == limit;
        _DROP_KEYS.Clear();
        foreach (Vector2Int key in _CHUNKS.Keys)
        {
            int dx = key.x - cx;
            int dz = key.y - cz;
            float dropRadius = _radius / _CHUNK_SIZE + 1.5f;
            if (dx * dx + dz * dz > dropRadius * dropRadius)
            {
                _DROP_KEYS.Add(key);
            }
        }
        foreach (Vector2Int key in _DROP_KEYS)
        {
            if (!_CACHED_CHUNKS.ContainsKey(key))
            {
                if (_CACHED_CHUNKS.Count == _CACHE_LIMIT)
                {
                    _CACHED_CHUNKS.Remove(_CACHE_ORDER.Dequeue());
                }
                _CACHED_CHUNKS.Add(key, _CHUNKS[key]);
                _CACHE_ORDER.Enqueue(key);
            }
            _CHUNKS.Remove(key);
        }
    }

    private void DrawChunks()
    {
        int layer = gameObject.layer;
        foreach (CChunk chunk in _CHUNKS.Values)
        {
            for (int k = 0; k < _CLUMP_TYPES; k++)
            {
                Matrix4x4[] m = chunk.clumps[k];
                if (m.Length == 0)
                {
                    continue;
                }
                Graphics.DrawMeshInstanced(_clumpMeshes[k], 0, _bladeMaterial, m, m.Length, null,
                    ShadowCastingMode.Off, true, layer);
            }
            if (chunk.stalks != null && chunk.stalks.Length > 0)
            {
                Graphics.DrawMeshInstanced(_stalkMesh, 0, _stalkMaterial, chunk.stalks, chunk.stalks.Length, null,
                    ShadowCastingMode.Off, true, layer);
                Graphics.DrawMeshInstanced(_glowFlowerMesh, 0, _glowFlowerMaterials[chunk.glowMaterial],
                    chunk.glowHeads,
                    chunk.glowHeads.Length, null, ShadowCastingMode.Off, true, layer);
            }
            if (chunk.petals != null && chunk.petals.Length > 0)
            {
                Graphics.DrawMeshInstanced(_petalFlowerMesh, 0,
                    _petalFlowerMaterials[chunk.petalMaterial], chunk.petals,
                    chunk.petals.Length, null, ShadowCastingMode.Off, true, layer);
            }
        }
    }

    // 난수 호출 순서는 동일한 배치를 재생성하기 위해 유지합니다.
    private CChunk MakeChunk(int ix, int iz)
    {
        CRandom random = new CRandom((uint)(((ix + 500) * 1013 + (iz + 500) * 37) * 7 + 3));
        CollectNearFootprints(ix, iz);
        CChunk chunk = new CChunk();
        double cell = 1.0 / System.Math.Sqrt(_density);
        int nx = (int)System.Math.Ceiling(_CHUNK_SIZE / cell);
        for (int k = 0; k < _CLUMP_TYPES; k++)
        {
            _CLUMP_LISTS[k].Clear();
        }

        for (int j = 0; j < nx; j++)
        {
            for (int i = 0; i < nx; i++)
            {
                double x = ix * _CHUNK_SIZE + (i + 0.5 + (random.Next() - 0.5) * 0.9) * cell;
                double z = iz * _CHUNK_SIZE + (j + 0.5 + (random.Next() - 0.5) * 0.9) * cell;
                double r1 = random.Next();
                double r2 = random.Next();
                double r3 = random.Next();
                double r4 = random.Next();
                if (x >= (ix + 1) * _CHUNK_SIZE || z >= (iz + 1) * _CHUNK_SIZE)
                {
                    continue;
                }
                double y = Spot(x, z);
                if (y < -0.5)
                {
                    continue;
                }
                double h = _height * (0.86 + 0.28 * (_noise.Sample(x * 0.05, z * 0.05) * 0.5 + 0.5))
                    * (1.0 - _heightVariation / 2.0 + _heightVariation * r1);
                h *= 1.0 - System.Math.Min(0.55, PaveAt(x, z) * 2.0);
                Quaternion q = EulerYXZ((r2 - 0.5) * 0.16, _WIND_YAW + (r3 - 0.5) * 1.1, (r4 - 0.5) * 0.16);
                double wd = h * _wide * (0.9 + 0.3 * r2);
                Matrix4x4 m = CreatePlacementMatrix(x, y - 0.015, z, q, new Vector3((float)wd, (float)h, (float)wd));
                _CLUMP_LISTS[(int)System.Math.Floor(r1 * 3.999)].Add(m);
            }
        }
        for (int k = 0; k < _CLUMP_TYPES; k++)
        {
            chunk.clumps[k] = _CLUMP_LISTS[k].ToArray();
        }

        // 달빛꽃 무리: 가는 줄기 끝에 은은히 빛나는 꽃, 드문 자리에 몇 송이씩
        if (random.Next() < _CHUNK_SIZE * _CHUNK_SIZE * _glowPatch)
        {
            double px = ix * _CHUNK_SIZE + random.Next() * _CHUNK_SIZE;
            double pz = iz * _CHUNK_SIZE + random.Next() * _CHUNK_SIZE;
            int count = _glowCount.x + (int)System.Math.Floor(random.Next() * (_glowCount.y - _glowCount.x + 1));
            chunk.glowMaterial = (int)System.Math.Floor(random.Next() * _glowFlowerMaterials.Length);
            _STALKS.Clear();
            _HEADS.Clear();
            for (int f = 0; f < count; f++)
            {
                double a = random.Next() * 6.283;
                double rr = System.Math.Sqrt(random.Next()) * _glowSpread;
                double x = px + System.Math.Cos(a) * rr;
                double z = pz + System.Math.Sin(a) * rr;
                double y = Spot(x, z);
                double hh = _height * (1.15 + 0.6 * random.Next());
                double tx = (random.Next() - 0.5) * 0.25;
                double tz = (random.Next() - 0.5) * 0.25;
                double ry = random.Next() * 6.283;
                double s = _glowSize * (0.75 + 0.5 * random.Next());
                if (y < -0.5)
                {
                    continue;
                }
                Quaternion q = EulerYXZ(tx, ry, tz);
                _STALKS.Add(CreatePlacementMatrix(x, y - 0.01, z, q, new Vector3(1f, (float)hh, 1f)));
                Vector3 top = q * new Vector3(0f, (float)hh, 0f);
                Quaternion headRotation = EulerYXZ(tx * 2.0 + 0.25, ry, tz * 2.0);
                _HEADS.Add(CreatePlacementMatrix(x + top.x, y - 0.01 + top.y, z + top.z, headRotation,
                    Vector3.one * (float)s));
            }
            chunk.stalks = _STALKS.ToArray();
            chunk.glowHeads = _HEADS.ToArray();
        }
        // 작은 들꽃 무리: 풀 끝 높이에 납작한 다섯 꽃잎
        if (random.Next() < _CHUNK_SIZE * _CHUNK_SIZE * _petalPatch)
        {
            double px = ix * _CHUNK_SIZE + random.Next() * _CHUNK_SIZE;
            double pz = iz * _CHUNK_SIZE + random.Next() * _CHUNK_SIZE;
            int count = _petalCount.x + (int)System.Math.Floor(random.Next() * (_petalCount.y - _petalCount.x + 1));
            chunk.petalMaterial = (int)System.Math.Floor(random.Next() * _petalFlowerMaterials.Length);
            _PETALS.Clear();
            for (int f = 0; f < count; f++)
            {
                double a = random.Next() * 6.283;
                double rr = System.Math.Sqrt(random.Next()) * _petalSpread;
                double x = px + System.Math.Cos(a) * rr;
                double z = pz + System.Math.Sin(a) * rr;
                double y = Spot(x, z);
                double s = _petalSize * (0.7 + 0.6 * random.Next());
                if (y < -0.5)
                {
                    continue;
                }
                double ex = (random.Next() - 0.5) * 0.5;
                double ey = random.Next() * 6.283;
                double ez = (random.Next() - 0.5) * 0.5;
                Quaternion q = EulerYXZ(ex, ey, ez);
                double py = y + _height * (0.75 + 0.3 * random.Next());
                _PETALS.Add(CreatePlacementMatrix(x, py, z, q, Vector3.one * (float)s));
            }
            chunk.petals = _PETALS.ToArray();
        }
        return chunk;
    }

    // 배치 데이터의 오른손 좌표계를 Unity 좌표계로 변환합니다.
    private static Matrix4x4 CreatePlacementMatrix(double x, double y, double z, Quaternion rotation, Vector3 scale)
    {
        Quaternion q = new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);
        return Matrix4x4.TRS(new Vector3((float)-x, (float)y, (float)z), q, scale);
    }

    private static Quaternion EulerYXZ(double ex, double ey, double ez)
    {
        double c1 = System.Math.Cos(ex / 2), c2 = System.Math.Cos(ey / 2), c3 = System.Math.Cos(ez / 2);
        double s1 = System.Math.Sin(ex / 2), s2 = System.Math.Sin(ey / 2), s3 = System.Math.Sin(ez / 2);
        return new Quaternion(
            (float)(s1 * c2 * c3 + c1 * s2 * s3),
            (float)(c1 * s2 * c3 - s1 * c2 * s3),
            (float)(c1 * c2 * s3 - s1 * s2 * c3),
            (float)(c1 * c2 * c3 + s1 * s2 * s3));
    }

    private double Spot(double x, double z)
    {
        double u = (x - z) * 0.7071067811865476;
        double v = (-x - z) * 0.7071067811865476;
        if (System.Math.Abs(u) > 110 || System.Math.Abs(v) > 110)
        {
            return -1;
        }
        foreach (Vector4 p in _plinths)
        {
            double du = u - p.x, dv = v - p.y;
            if (System.Math.Sqrt(du * du + dv * dv) < p.z + p.w + 0.5)
            {
                return -1;
            }
        }
        if (PaveAt(x, z) > 0.45)
        {
            return -1;
        }
        double y = HeightAt(x, z);
        if (y < _waterY + 0.06 || IsBlocked(x, z, 0.06, y + 0.05))
        {
            return -1;
        }
        return y;
    }

    private double PaveAt(double x, double z)
    {
        int i = (int)System.Math.Round((x + _gridHalf) / _gridStep);
        int j = (int)System.Math.Round((z + _gridHalf) / _gridStep);
        if (i < 0 || j < 0 || i >= _gridSize || j >= _gridSize)
        {
            return 1;
        }
        return _paveMask[j * _gridSize + i];
    }

    private double HeightAt(double x, double z)
    {
        int seg = _gridSize - 1;
        double fx = System.Math.Min(System.Math.Max((x + _gridHalf) / _gridStep, 0), seg - 1e-3);
        double fz = System.Math.Min(System.Math.Max((z + _gridHalf) / _gridStep, 0), seg - 1e-3);
        int i = (int)System.Math.Floor(fx), j = (int)System.Math.Floor(fz);
        double tx = fx - i, tz = fz - j;
        double h00 = _heights[j * _gridSize + i], h10 = _heights[j * _gridSize + i + 1];
        double h01 = _heights[(j + 1) * _gridSize + i], h11 = _heights[(j + 1) * _gridSize + i + 1];
        double a = h00 + (h10 - h00) * tx;
        double b = h01 + (h11 - h01) * tx;
        return a + (b - a) * tz;
    }

    private void CollectNearFootprints(int ix, int iz)
    {
        _NEAR_FOOTPRINTS.Clear();
        float minX = ix * _CHUNK_SIZE - 4f, maxX = (ix + 1) * _CHUNK_SIZE + 4f;
        float minZ = iz * _CHUNK_SIZE - 4f, maxZ = (iz + 1) * _CHUNK_SIZE + 4f;
        foreach (SFootprint f in _footprints)
        {
            float ext = f.extent;
            if (f.x + ext < minX || f.x - ext > maxX || f.z + ext < minZ || f.z - ext > maxZ)
            {
                continue;
            }
            _NEAR_FOOTPRINTS.Add(f);
        }
    }

    private bool IsBlocked(double x, double z, double rad, double y)
    {
        foreach (SFootprint f in _NEAR_FOOTPRINTS)
        {
            if (y > f.top)
            {
                continue;
            }
            if (f.circle)
            {
                double dx = x - f.x, dz = z - f.z;
                if (System.Math.Sqrt(dx * dx + dz * dz) < f.r + rad)
                {
                    return true;
                }
                continue;
            }
            double c = f.cosRotation, s = f.sinRotation;
            double lx = (x - f.x) * c - (z - f.z) * s;
            double lz = (x - f.x) * s + (z - f.z) * c;
            if (System.Math.Abs(lx) < f.w / 2 + rad && System.Math.Abs(lz) < f.d / 2 + rad)
            {
                return true;
            }
        }
        return false;
    }

    private void ReadField(byte[] bytes)
    {
        using (BinaryReader reader = new BinaryReader(new MemoryStream(bytes)))
        {
            reader.ReadInt32();
            _gridSize = reader.ReadInt32();
            _gridHalf = reader.ReadSingle();
            _gridStep = reader.ReadSingle();
            _waterY = reader.ReadSingle();
            int cells = _gridSize * _gridSize;
            _heights = new float[cells];
            for (int i = 0; i < cells; i++)
            {
                _heights[i] = reader.ReadSingle();
            }
            _paveMask = new float[cells];
            for (int i = 0; i < cells; i++)
            {
                _paveMask[i] = reader.ReadSingle();
            }
            _footprints = new SFootprint[reader.ReadInt32()];
            for (int i = 0; i < _footprints.Length; i++)
            {
                SFootprint f = new SFootprint();
                f.circle = reader.ReadInt32() == 1;
                f.x = reader.ReadSingle();
                f.z = reader.ReadSingle();
                f.rot = reader.ReadSingle();
                f.w = reader.ReadSingle();
                f.d = reader.ReadSingle();
                f.r = reader.ReadSingle();
                f.top = reader.ReadSingle();
                f.cosRotation = System.Math.Cos(f.rot);
                f.sinRotation = System.Math.Sin(f.rot);
                f.extent = f.circle ? f.r : Mathf.Sqrt(f.w * f.w + f.d * f.d) / 2f;
                _footprints[i] = f;
            }
            _plinths = new Vector4[reader.ReadInt32()];
            for (int i = 0; i < _plinths.Length; i++)
            {
                _plinths[i] = new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                    reader.ReadSingle());
            }
        }
    }

    private class CRandom
    {
        private uint _state;

        public CRandom(uint seed)
        {
            _state = seed == 0 ? 1u : seed;
        }

        public double Next()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return (_state % 100000) / 100000.0;
        }
    }

    private class CSimplexNoise
    {
        private static readonly int[,] _GRAD = { { 1, 1 }, { -1, 1 }, { 1, -1 }, { -1, -1 }, { 1, 0 },
            { -1, 0 }, { 0, 1 }, { 0, -1 } };
        private static readonly double _F2 = 0.5 * (System.Math.Sqrt(3) - 1);
        private static readonly double _G2 = (3 - System.Math.Sqrt(3)) / 6;
        private readonly byte[] _PERM = new byte[512];

        public CSimplexNoise(int seed)
        {
            double s = seed * 9301 + 49297;
            byte[] p = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                p[i] = (byte)i;
            }
            for (int i = 255; i > 0; i--)
            {
                s = (s * 9301 + 49297) % 233280;
                int j = (int)System.Math.Floor(s / 233280 * (i + 1));
                byte t = p[i];
                p[i] = p[j];
                p[j] = t;
            }
            for (int i = 0; i < 512; i++)
            {
                _PERM[i] = p[i & 255];
            }
        }

        public double Sample(double xin, double yin)
        {
            double sk = (xin + yin) * _F2;
            int i = (int)System.Math.Floor(xin + sk), j = (int)System.Math.Floor(yin + sk);
            double t = (i + j) * _G2;
            double x0 = xin - (i - t), y0 = yin - (j - t);
            int i1 = x0 > y0 ? 1 : 0, j1 = x0 > y0 ? 0 : 1;
            double x1 = x0 - i1 + _G2, y1 = y0 - j1 + _G2, x2 = x0 - 1 + 2 * _G2, y2 = y0 - 1 + 2 * _G2;
            int ii = i & 255, jj = j & 255;
            double n = Corner(x0, y0, _PERM[ii + _PERM[jj]]);
            n += Corner(x1, y1, _PERM[ii + i1 + _PERM[jj + j1]]);
            n += Corner(x2, y2, _PERM[ii + 1 + _PERM[jj + 1]]);
            return 70 * n;
        }

        private static double Corner(double x, double y, int g)
        {
            double tt = 0.5 - x * x - y * y;
            if (tt < 0)
            {
                return 0;
            }
            tt *= tt;
            return tt * tt * (_GRAD[g % 8, 0] * x + _GRAD[g % 8, 1] * y);
        }
    }
}
