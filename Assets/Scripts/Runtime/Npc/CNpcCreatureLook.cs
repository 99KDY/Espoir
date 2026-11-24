using UnityEngine;

public class CNpcCreatureLook : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private Animator _animator;
    [SerializeField] private SkinnedMeshRenderer[] _renderers;
    [SerializeField] private string _blinkLeftShape = "Blink_L";
    [SerializeField] private string _blinkRightShape = "Blink_R";

    [Header("눈 깜빡임 (초)")]
    [SerializeField, Min(0.01f)] private float _closeTime = 0.07f;
    [SerializeField, Min(0f)] private float _holdTime = 0.04f;
    [SerializeField, Min(0.01f)] private float _openTime = 0.16f;
    [SerializeField] private Vector2 _blinkInterval = new Vector2(2.5f, 5.5f);
    [SerializeField] private Vector2 _firstBlink = new Vector2(1.5f, 4.5f);

    [Header("대사 동작 중 눈")]
    [SerializeField] private string _gestureState = "Attack";
    [SerializeField] private AnimationCurve _gestureEyes = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.5f, 0f), new Keyframe(0.58f, 0.55f),
        new Keyframe(0.72f, 0.55f), new Keyframe(0.9f, 0f), new Keyframe(1.4f, 0f));

    [Header("가슴 갈기 흔들림")]
    [SerializeField] private Transform _jiggleBone;
    [SerializeField, Min(0f)] private float _stiffness = 420f;
    [SerializeField, Min(0f)] private float _damping = 27f;
    [SerializeField, Min(0f)] private float _gain = 1f;
    [SerializeField, Min(0f)] private float _maxOffset = 0.0176f;

    private static readonly int _JIG_ID = Shader.PropertyToID("_Jig");

    private const float _JIGGLE_RESET_DISTANCE = 0.6f;
    private const float _JIGGLE_STEP = 0.008f;

    private int[] _leftIndices;
    private int[] _rightIndices;
    private MaterialPropertyBlock _block;
    private int _gestureHash;
    private float _blinkTime = -1f;
    private float _nextBlink;
    private Vector3 _springPosition;
    private Vector3 _springVelocity;
    private bool _hasSpring;

    private void Awake()
    {
        _block = new MaterialPropertyBlock();
        _gestureHash = Animator.StringToHash(_gestureState);
        _leftIndices = new int[_renderers.Length];
        _rightIndices = new int[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            Mesh mesh = _renderers[i] != null ? _renderers[i].sharedMesh : null;
            _leftIndices[i] = mesh != null ? mesh.GetBlendShapeIndex(_blinkLeftShape) : -1;
            _rightIndices[i] = mesh != null ? mesh.GetBlendShapeIndex(_blinkRightShape) : -1;
        }

        _nextBlink = Random.Range(_firstBlink.x, _firstBlink.y);
    }

    private void OnEnable()
    {
        _hasSpring = false;
    }

    private void OnDisable()
    {
        SetEyes(0f);
        SetJiggle(Vector3.zero);
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        SetEyes(Mathf.Max(UpdateBlink(dt), GetGestureEyes()));
        if (_jiggleBone != null) SetJiggle(UpdateJiggle(dt));
    }

    private float UpdateBlink(float dt)
    {
        if (_blinkTime < 0f)
        {
            _nextBlink -= dt;
            if (_nextBlink <= 0f) _blinkTime = 0f;
            return 0f;
        }

        _blinkTime += dt;
        float closeEnd = _closeTime + _holdTime;
        float openEnd = closeEnd + _openTime;

        if (_blinkTime >= openEnd)
        {
            _blinkTime = -1f;
            _nextBlink = Random.Range(_blinkInterval.x, _blinkInterval.y);
            return 0f;
        }

        if (_blinkTime < _closeTime) return _blinkTime / _closeTime;
        if (_blinkTime < closeEnd) return 1f;
        return 1f - (_blinkTime - closeEnd) / _openTime;
    }

    private float GetGestureEyes()
    {
        if (_animator == null) return 0f;

        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        if (_animator.IsInTransition(0))
        {
            AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(0);
            if (next.shortNameHash == _gestureHash) state = next;
        }

        if (state.shortNameHash != _gestureHash) return 0f;

        float time = Mathf.Min(state.normalizedTime, 1f) * state.length;
        return _gestureEyes.Evaluate(time);
    }

    private Vector3 UpdateJiggle(float dt)
    {
        Vector3 bone = _jiggleBone.position;

        if (!_hasSpring || Vector3.Distance(bone, _springPosition) > _JIGGLE_RESET_DISTANCE)
        {
            _springPosition = bone;
            _springVelocity = Vector3.zero;
            _hasSpring = true;
        }

        int steps = Mathf.Max(1, Mathf.CeilToInt(dt / _JIGGLE_STEP));
        float stepTime = dt / steps;

        for (int i = 0; i < steps; i++)
        {
            Vector3 acceleration = (bone - _springPosition) * _stiffness - _springVelocity * _damping;
            _springVelocity += acceleration * stepTime;
            _springPosition += _springVelocity * stepTime;
        }

        return Vector3.ClampMagnitude((_springPosition - bone) * _gain, _maxOffset);
    }

    private void SetEyes(float amount)
    {
        float weight = amount * 100f;

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_leftIndices[i] >= 0) _renderers[i].SetBlendShapeWeight(_leftIndices[i], weight);
            if (_rightIndices[i] >= 0) _renderers[i].SetBlendShapeWeight(_rightIndices[i], weight);
        }
    }

    private void SetJiggle(Vector3 offset)
    {
        foreach (SkinnedMeshRenderer target in _renderers)
        {
            if (target == null) continue;
            target.GetPropertyBlock(_block);
            _block.SetVector(_JIG_ID, offset);
            target.SetPropertyBlock(_block);
        }
    }
}
