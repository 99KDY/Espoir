using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 코트, 목도리, 머리카락 본의 흔들림을 계산합니다.
/// 뼈 끝점을 관성 · 복원력 · 중력으로 움직이고, 몸에 붙인 구 · 캡슐 충돌체 밖으로 밀어낸 뒤 뼈를 그 방향으로 돌린다.
/// 체인, 충돌체와 따라가기 설정은 Inspector에서 지정합니다.
/// </summary>
public class CNpcSpringBones : MonoBehaviour
{
    /// <summary>
    /// 몸 충돌체는 b가 비면 구, 있으면 캡슐입니다. 오프셋은 Blender 좌표(m): x 왼쪽, y 뒤, z 위.
    /// </summary>
    [Serializable]
    public struct SSpringCollider
    {
        public string name;
        public string boneA;
        public Vector3 offsetA;
        public string boneB;
        public Vector3 offsetB;
        public float radius;
    }

    /// <summary>
    /// 흔들림 체인: 이름이 pattern(정규식)에 맞는 뼈 전부.
    /// </summary>
    [Serializable]
    public struct SSpringChain
    {
        public string pattern;
        public float stiffness;
        public float drag;
        public float gravity;
        public float hit;
        public string[] colliders;
    }

    /// <summary>
    /// 위치 따라가기 (블렌더 Copy Location). axis 가 0 이 아니면 그 방향 성분만(skip 이면 그 방향만 빼고).
    /// </summary>
    [Serializable]
    public struct SSpringFollow
    {
        public string bone;
        public string source;
        public Vector3 axis;
        public bool skip;
    }

    private class CCollider
    {
        public Transform boneA;
        public Vector3 localA;
        public Vector3 worldA;
        public Transform boneB;
        public Vector3 localB;
        public Vector3 worldB;
        public float radius;
    }

    private class CJoint
    {
        public Transform bone;
        public Vector3 axisLocal;
        public Quaternion restRotation;
        public Vector3 current;
        public Vector3 previous;
        public float length;
        public float stiffness;
        public float drag;
        public float gravity;
        public float hit;
        public CCollider[] colliders;
    }

    private class CFollow
    {
        public Transform bone;
        public Transform source;
        public Vector3 rest;
        public Vector3 direction;
        public bool hasDirection;
        public bool skip;
    }

    [Tooltip("모델 정면 기준 (충돌체 오프셋을 옮길 때 씀). 비면 부모")]
    [SerializeField] private Transform _basis;
    [Tooltip("모델 단위 ↔ 화면 단위 환산: 두 뼈 사이의 실제 거리(m)")]
    [SerializeField] private string _referenceBoneA = "hips";
    [SerializeField] private string _referenceBoneB = "head";
    [SerializeField, Min(0.001f)] private float _referenceLength = 0.565f;
    [SerializeField] private SSpringCollider[] _colliders = Array.Empty<SSpringCollider>();
    [SerializeField] private SSpringChain[] _chains = Array.Empty<SSpringChain>();
    [SerializeField] private SSpringFollow[] _follows = Array.Empty<SSpringFollow>();

    private const float _STEP = 1f / 90f;
    private const int _MAX_STEPS = 4;
    private const float _TELEPORT = 0.12f;

    private readonly List<CJoint> _JOINTS = new List<CJoint>();
    private readonly List<CCollider> _COLLIDER_LIST = new List<CCollider>();
    private readonly List<CFollow> _FOLLOW_LIST = new List<CFollow>();
    private float _scale = 1f;
    private bool _hasLast;
    private Vector3 _last;

    /// <summary>
    /// 흔들리는 뼈 수 (확인용).
    /// </summary>
    public int JointCount => _JOINTS.Count;

    /// <summary>
    /// 만들어진 충돌체 수 (확인용).
    /// </summary>
    public int ColliderCount => _COLLIDER_LIST.Count;

    private void Awake()
    {
        Build();
        ResetPose();
    }

    private void Build()
    {
        _JOINTS.Clear();
        _COLLIDER_LIST.Clear();
        _FOLLOW_LIST.Clear();

        Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        List<Transform> order = new List<Transform>();

        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t == transform || bones.ContainsKey(t.name)) continue;
            bones.Add(t.name, t);
            order.Add(t);
        }

        Transform Find(string boneName) => !string.IsNullOrEmpty(boneName)
            && bones.TryGetValue(boneName, out Transform t) ? t : null;

        Transform ra = Find(_referenceBoneA), rb = Find(_referenceBoneB);

        if (ra == null || rb == null)
        {
            enabled = false;
            return;
        }

        _scale = Vector3.Distance(ra.position, rb.position) / _referenceLength;
        Transform basis = _basis != null ? _basis : transform.parent != null ? transform.parent : transform;
        Quaternion rootRotation = basis.rotation;
        Dictionary<string, CCollider> byName = new Dictionary<string, CCollider>();

        foreach (SSpringCollider c in _colliders)
        {
            Transform a = Find(c.boneA);
            Transform b = Find(c.boneB);
            bool wantsB = !string.IsNullOrEmpty(c.boneB);
            if (a == null || (wantsB && b == null)) continue;

            CCollider collider = new CCollider { boneA = a, radius = c.radius * _scale };
            collider.localA = a.InverseTransformPoint(a.position + rootRotation * (FromBlender(c.offsetA) * _scale));

            if (b != null)
            {
                collider.boneB = b;
                collider.localB = b.InverseTransformPoint(b.position + rootRotation *
                    (FromBlender(c.offsetB) * _scale));
            }

            byName[c.name] = collider;
            _COLLIDER_LIST.Add(collider);
        }

        foreach (SSpringFollow f in _follows)
        {
            Transform bone = Find(f.bone), source = Find(f.source);
            if (bone == null || source == null || bone.parent == null) continue;

            CFollow follow = new CFollow { bone = bone, source = source, rest = bone.localPosition, skip = f.skip };

            if (f.axis.sqrMagnitude > 1e-12f)
            {
                follow.hasDirection = true;
                follow.direction = (Quaternion.Inverse(bone.parent.rotation) *
                    (rootRotation * FromBlender(f.axis))).normalized;
            }

            _FOLLOW_LIST.Add(follow);
        }

        foreach (SSpringChain chain in _chains)
        {
            Regex match = new Regex(chain.pattern);
            List<Transform> list = new List<Transform>();

            foreach (Transform t in order)
            {
                if (match.IsMatch(t.name)) list.Add(t);
            }

            // 부모가 먼저 오게 (깊이 순, 같은 깊이는 원래 순서)
            List<(Transform bone, int depth, int index)> sorted = new List<(Transform, int, int)>();
            for (int i = 0; i < list.Count; i++) sorted.Add((list[i], Depth(list[i]), i));
            sorted.Sort((x, y) => x.depth != y.depth ? x.depth.CompareTo(y.depth) : x.index.CompareTo(y.index));

            List<CCollider> colliders = new List<CCollider>();
            foreach (string n in chain.colliders ?? Array.Empty<string>())
            {
                if (byName.TryGetValue(n, out CCollider c)) colliders.Add(c);
            }

            foreach (var item in sorted)
            {
                Transform bone = item.bone;
                Transform child = null;

                foreach (Transform c in bone)
                {
                    if (c.name != bone.name && match.IsMatch(c.name))
                    {
                        child = c;
                        break;
                    }
                }

                // 끝점: 자식 뼈 위치, 끝 뼈는 부모 → 자기 방향으로 같은 길이만큼 연장
                Vector3 axisLocal = child != null ? child.localPosition : bone.localPosition;
                if (axisLocal.sqrMagnitude < 1e-12f) continue;

                Vector3 tail = bone.TransformPoint(axisLocal);
                _JOINTS.Add(new CJoint
                {
                    bone = bone,
                    axisLocal = axisLocal,
                    restRotation = bone.localRotation,
                    current = tail,
                    previous = tail,
                    length = Vector3.Distance(tail, bone.position),
                    stiffness = chain.stiffness * _scale,
                    drag = chain.drag,
                    gravity = chain.gravity * _scale,
                    hit = chain.hit * _scale,
                    colliders = colliders.ToArray(),
                });
            }
        }
    }

    // Blender(z 위, -y 앞, x 왼쪽) 좌표를 Unity 모델(y 위, +z 앞, -x 왼쪽) 좌표로 변환합니다.
    private static Vector3 FromBlender(Vector3 o) => new Vector3(-o.x, o.z, -o.y);

    private static int Depth(Transform t)
    {
        int d = 0;
        for (Transform p = t.parent; p != null; p = p.parent) d++;
        return d;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || _JOINTS.Count == 0) return;

        ApplyFollow();

        // 순간 이동이면 관성으로 튀어 몸 반대편에 걸리므로 그 자리에서 다시 시작
        Vector3 first = _JOINTS[0].bone.position;
        if (_hasLast && Vector3.Distance(_last, first) > _TELEPORT * _scale) ResetPose();
        _last = first;
        _hasLast = true;

        int n = Mathf.Min(_MAX_STEPS, Mathf.Max(1, Mathf.CeilToInt(dt / _STEP)));
        for (int i = 0; i < n; i++) Step(dt / n);
    }

    private void ApplyFollow()
    {
        foreach (CFollow f in _FOLLOW_LIST)
        {
            Vector3 local = f.bone.parent.InverseTransformPoint(f.source.position);

            if (!f.hasDirection)
            {
                f.bone.localPosition = local;
                continue;
            }

            float along = Vector3.Dot(local - f.rest, f.direction);
            f.bone.localPosition = f.skip ? local - f.direction * along : f.rest + f.direction * along;
        }
    }

    private void Step(float dt)
    {
        foreach (CCollider c in _COLLIDER_LIST)
        {
            c.worldA = c.boneA.TransformPoint(c.localA);
            if (c.boneB != null) c.worldB = c.boneB.TransformPoint(c.localB);
        }

        foreach (CJoint j in _JOINTS)
        {
            Transform bone = j.bone;
            Quaternion parentRotation = bone.parent.rotation;
            Vector3 head = bone.position;
            // 원래(애니메이션 없는) 방향
            Vector3 restDir = (parentRotation * j.restRotation * j.axisLocal).normalized;
            Vector3 next = j.current + (j.current - j.previous) * (1f - j.drag)
                + restDir * (j.stiffness * dt)
                + Vector3.down * (j.gravity * dt);
            next = head + (next - head).normalized * j.length;
            Collide(ref next, j);
            next = head + (next - head).normalized * j.length;
            j.previous = j.current;
            j.current = next;

            // 뼈 회전: 원래 방향 → 새 방향
            Vector3 dir = (next - head).normalized;
            Quaternion swing = Quaternion.FromToRotation(restDir, dir);
            bone.rotation = swing * (parentRotation * j.restRotation);
        }
    }

    private static void Collide(ref Vector3 p, CJoint j)
    {
        foreach (CCollider c in j.colliders)
        {
            Vector3 cp;

            if (c.boneB != null)
            {
                Vector3 seg = c.worldB - c.worldA;
                float t = Mathf.Clamp01(Vector3.Dot(p - c.worldA, seg) / Mathf.Max(seg.sqrMagnitude, 1e-12f));
                cp = c.worldA + seg * t;
            }

            else
            {
                cp = c.worldA;
            }

            float r = c.radius + j.hit;
            Vector3 d = p - cp;
            float l = d.magnitude;
            if (l < r && l > 1e-9f) p = cp + d * (r / l);
        }
    }

    /// <summary>
    /// 흔들림을 원래 모양으로 되돌리고 그 자리에서 다시 시작합니다 (순간 이동 뒤 등).
    /// </summary>
    public void ResetPose()
    {
        foreach (CJoint j in _JOINTS)
        {
            j.bone.localRotation = j.restRotation;
            j.current = j.bone.TransformPoint(j.axisLocal);
            j.previous = j.current;
        }

        _hasLast = false;
    }
}
