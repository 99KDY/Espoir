using UnityEngine;

public class CTutorialScarecrow : CEnemy
{
    [SerializeField] private string _displayName = "허수아비";

    [Header("피격 반응")]
    [SerializeField] private Transform _visual;
    [SerializeField] private float _swingAngle = 7f;
    [SerializeField, Min(0.01f)] private float _swingDuration = 0.5f;

    private Quaternion _restRotation;
    private Vector3 _swingAxis = Vector3.right;
    private float _swingTimeRemaining;
    private bool _restoreHp;

    public override string DisplayName => _displayName;

    protected override void Awake()
    {
        base.Awake();
        if (_visual != null) _restRotation = _visual.localRotation;
    }

    private void LateUpdate()
    {
        if (_restoreHp)
        {
            RestoreHp(HpMax);
            _restoreHp = false;
        }

        if (_visual == null || _swingTimeRemaining <= 0f) return;

        _swingTimeRemaining = Mathf.Max(0f, _swingTimeRemaining - Time.deltaTime);
        if (_swingTimeRemaining <= 0f)
        {
            _visual.localRotation = _restRotation;
            return;
        }

        float remaining = _swingTimeRemaining / _swingDuration;
        float angle = _swingAngle * Mathf.Sin((1f - remaining) * Mathf.PI * 4f) * remaining * remaining;
        _visual.localRotation = _restRotation * Quaternion.AngleAxis(angle, _swingAxis);
    }

    protected override void AfterHit(CUnit attacker, CUnit defender)
    {
        base.AfterHit(attacker, defender);
        if (_visual == null || attacker == null) return;

        Vector3 push = transform.position - attacker.transform.position;
        push.y = 0f;
        if (push.sqrMagnitude < 0.0001f) push = transform.forward;
        Vector3 axis = Vector3.Cross(Vector3.up, push.normalized);
        _swingAxis = _visual.parent != null ? _visual.parent.InverseTransformDirection(axis) : axis;
        _swingTimeRemaining = _swingDuration;
    }

    protected override void Die()
    {
        // 공격 측에서 적중 직후의 체력으로 피해를 판정하므로 회복은 LateUpdate에서 처리한다.
        _restoreHp = true;
    }
}
