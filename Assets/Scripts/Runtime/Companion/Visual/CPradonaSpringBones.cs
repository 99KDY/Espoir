using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 프라도나의 옷과 머리카락 흔들림을 계산합니다.
/// </summary>
[DefaultExecutionOrder(100)]
public class CPradonaSpringBones : MonoBehaviour
{
    /// <summary>
    /// boneB가 없으면 구, 있으면 캡슐입니다. 오프셋은 Blender 좌표(m)입니다.
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

    [Serializable]
    public struct SSpringJoint
    {
        public string pattern;
        public float stiffness;
        public float drag;
        public float gravity;
        public float hit;
    }

    [Serializable]
    public struct SSpringChain
    {
        public string pattern;
        public float stiffness;
        public float drag;
        public float gravity;
        public float hit;
        public float wind;
        public string[] colliders;
        public SSpringJoint[] joints;
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
        public float wind;
        public CCollider[] colliders;
    }

    [Tooltip("충돌체와 바람의 기준 Transform")]
    [SerializeField] private Transform _basis;
    [Tooltip("이동 상태를 확인할 Animator")]
    [SerializeField] private Animator _animator;
    [Tooltip("모델 크기 보정에 사용할 기준 본")]
    [SerializeField] private string _referenceBoneA = "hips";
    [SerializeField] private string _referenceBoneB = "head";
    [SerializeField, Min(0.001f)] private float _referenceLength = 0.445f;
    [Tooltip("이동 중 바람 세기")]
    [SerializeField, Min(0f)] private float _runWind = 0.8f;
    [SerializeField] private SSpringCollider[] _colliders = Array.Empty<SSpringCollider>();
    [SerializeField] private SSpringChain[] _chains = Array.Empty<SSpringChain>();

    // 프레임률에 따른 흔들림 차이를 줄이기 위해 120Hz 고정 간격으로 계산한다.
    private const float _STEP = 1f / 120f;
    private const int _MAX_STEPS = 4;
    private const float _TELEPORT = 0.12f;

    private static readonly int _IS_MOVING_HASH = Animator.StringToHash("IsMoving");

    private readonly List<CJoint> _JOINTS = new List<CJoint>();
    private readonly List<CCollider> _COLLIDER_LIST = new List<CCollider>();
    private float _scale = 1f;
    private float _time;
    private bool _hasLast;
    private Vector3 _last;
    private Vector3 _lastBasisPosition;
    private Vector3 _flow;

    /// <summary>
    /// 외부에서 Simulate를 호출해 흔들림을 갱신하는지 여부입니다.
    /// </summary>
    public bool IsManual { get; set; }

    /// <summary>
    /// 월드 좌표 기준으로 추가할 바람입니다.
    /// </summary>
    public Vector3 ExternalFlow { get; set; }

    private void Awake()
    {
        Build();
    }

    private void OnEnable()
    {
        ResetPose();
    }

    private void Build()
    {
        _JOINTS.Clear();
        _COLLIDER_LIST.Clear();

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

        Transform referenceBoneA = Find(_referenceBoneA);
        Transform referenceBoneB = Find(_referenceBoneB);

        if (_basis == null || referenceBoneA == null || referenceBoneB == null)
        {
            CLogger.Error($"{name}: 흔들림 뼈의 기준 Transform과 기준 뼈 이름을 확인해주세요.");
            enabled = false;
            return;
        }

        _scale = Vector3.Distance(referenceBoneA.position, referenceBoneB.position) / _referenceLength;
        Quaternion rootRotation = _basis.rotation;
        Dictionary<string, CCollider> byName = new Dictionary<string, CCollider>();

        foreach (SSpringCollider c in _colliders)
        {
            Transform a = Find(c.boneA);
            Transform b = Find(c.boneB);
            bool wantsB = !string.IsNullOrEmpty(c.boneB);
            if (a == null || (wantsB && b == null)) continue;

            CCollider collider = new CCollider
            {
                boneA = a,
                radius = c.radius * _scale
            };
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

        foreach (SSpringChain chain in _chains)
        {
            Regex match = new Regex(chain.pattern);
            List<Transform> list = new List<Transform>();

            foreach (Transform t in order)
            {
                if (match.IsMatch(t.name)) list.Add(t);
            }

            // 부모 본을 먼저 계산하고 같은 깊이에서는 원래 순서를 유지한다.
            List<(Transform bone, int depth, int index)> sorted = new List<(Transform, int, int)>();
            for (int i = 0; i < list.Count; i++) sorted.Add((list[i], Depth(list[i]), i));
            sorted.Sort((x, y) => x.depth != y.depth ? x.depth.CompareTo(y.depth) : x.index.CompareTo(y.index));

            List<CCollider> colliders = new List<CCollider>();
            foreach (string n in chain.colliders ?? Array.Empty<string>())
            {
                if (byName.TryGetValue(n, out CCollider c)) colliders.Add(c);
            }

            CCollider[] chainColliders = colliders.ToArray();
            SSpringJoint[] overrides = chain.joints ?? Array.Empty<SSpringJoint>();
            Regex[] overrideMatches = new Regex[overrides.Length];
            for (int i = 0; i < overrides.Length; i++) overrideMatches[i] = new Regex(overrides[i].pattern);

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

                // 마디별 값: 처음 맞는 것 하나만 적용
                SSpringJoint values = new SSpringJoint
                {
                    stiffness = chain.stiffness,
                    drag = chain.drag,
                    gravity = chain.gravity,
                    hit = chain.hit
                };

                for (int i = 0; i < overrides.Length; i++)
                {
                    if (overrideMatches[i].IsMatch(bone.name))
                    {
                        values = overrides[i];
                        break;
                    }
                }

                Vector3 tail = bone.TransformPoint(axisLocal);
                _JOINTS.Add(new CJoint
                {
                    bone = bone,
                    axisLocal = axisLocal,
                    restRotation = bone.localRotation,
                    current = tail,
                    previous = tail,
                    length = Vector3.Distance(tail, bone.position),
                    stiffness = values.stiffness * _scale,
                    drag = values.drag,
                    gravity = values.gravity * _scale,
                    hit = values.hit * _scale,
                    wind = chain.wind * _scale,
                    colliders = chainColliders
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
        if (!IsManual) Simulate(Time.deltaTime);
    }

    /// <summary>
    /// 애니메이션 자세를 적용한 뒤 전달받은 시간으로 흔들림을 갱신합니다.
    /// </summary>
    public void Simulate(float deltaTime)
    {
        if (_JOINTS.Count == 0) return;

        // 이동 바람과 관성이 중복되지 않도록 본 끝점에도 모델 이동량을 적용한다.
        Vector3 basisPosition = _basis.position;
        Vector3 move = basisPosition - _lastBasisPosition;
        _lastBasisPosition = basisPosition;

        foreach (CJoint j in _JOINTS)
        {
            j.current += move;
            j.previous += move;
        }

        float dt = deltaTime;
        if (dt <= 0f) return;

        // 순간 회전이면 관성으로 튀어 몸 반대편에 걸리므로 그 자리에서 다시 시작
        Vector3 first = _JOINTS[0].bone.position;
        if (_hasLast && Vector3.Distance(_last + move, first) > _TELEPORT * _scale) ResetPose();
        _last = first;
        _hasLast = true;

        // 메뉴 사본은 Animator 컨트롤러 없이 재생하므로 이동 파라미터를 조회하지 않는다.
        bool isMoving = !IsManual && _animator != null && _animator.GetBool(_IS_MOVING_HASH);
        _flow = (isMoving ? -_basis.forward * _runWind : Vector3.zero) + ExternalFlow;

        // 밀린 시간은 버려서 긴 프레임 뒤에도 한꺼번에 몰아 계산하지 않는다.
        _time = Mathf.Min(_time + dt, _STEP * _MAX_STEPS);

        if (_time >= _STEP)
        {
            // 보간된 표시 자세를 마지막 계산 결과로 복원한다.
            Pose(1f);

            while (_time >= _STEP)
            {
                Step(_STEP);
                _time -= _STEP;
            }
        }

        // 고정 계산 주기와 화면 주기 사이의 자세를 보간한다.
        Pose(_time / _STEP);
    }

    private void Pose(float blend)
    {
        foreach (CJoint j in _JOINTS)
        {
            Transform bone = j.bone;
            Quaternion parentRotation = bone.parent.rotation;
            Vector3 restDir = (parentRotation * j.restRotation * j.axisLocal).normalized;
            Vector3 dir = (Vector3.Lerp(j.previous, j.current, blend) - bone.position).normalized;
            bone.rotation = Quaternion.FromToRotation(restDir, dir) * (parentRotation * j.restRotation);
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
            Vector3 restDir = (parentRotation * j.restRotation * j.axisLocal).normalized;
            Vector3 next = j.current + (j.current - j.previous) * (1f - j.drag)
                + restDir * (j.stiffness * dt)
                + Vector3.down * (j.gravity * dt)
                + _flow * (j.wind * dt);
            next = head + (next - head).normalized * j.length;
            Collide(ref next, j);
            next = head + (next - head).normalized * j.length;
            j.previous = j.current;
            j.current = next;

            Vector3 dir = (next - head).normalized;
            Quaternion swing = Quaternion.FromToRotation(restDir, dir);
            bone.rotation = swing * (parentRotation * j.restRotation);
        }
    }

    private static void Collide(ref Vector3 position, CJoint joint)
    {
        foreach (CCollider collider in joint.colliders)
        {
            Vector3 closestPoint;

            if (collider.boneB != null)
            {
                Vector3 segment = collider.worldB - collider.worldA;
                float projection = Mathf.Clamp01(Vector3.Dot(position - collider.worldA, segment)
                    / Mathf.Max(segment.sqrMagnitude, 1e-12f));
                closestPoint = collider.worldA + segment * projection;
            }
            else
            {
                closestPoint = collider.worldA;
            }

            float radius = collider.radius + joint.hit;
            Vector3 offset = position - closestPoint;
            float distance = offset.magnitude;
            if (distance < radius && distance > 1e-9f) position = closestPoint + offset * (radius / distance);
        }
    }

    /// <summary>
    /// 현재 위치를 기준으로 흔들림을 초기화합니다.
    /// </summary>
    public void ResetPose()
    {
        foreach (CJoint j in _JOINTS)
        {
            j.bone.localRotation = j.restRotation;
            j.current = j.bone.TransformPoint(j.axisLocal);
            j.previous = j.current;
        }

        if (_basis != null) _lastBasisPosition = _basis.position;
        _time = 0f;
        _hasLast = false;
    }
}
