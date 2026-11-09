using TMPro;
using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(TextMeshProUGUI))]

public class CTestStackLabel : MonoBehaviour
{
    private enum EDisplay
    {
        Stack,
        Cooldown,
        Duration,
        CrownWindow
    }

    [SerializeField] private EDisplay _display;
    [SerializeField] private ESkillType _skillType;
    [SerializeField] private string _format = "{0}";
    [SerializeField] private CUnit _unit;
    [SerializeField] private CStatusSO _status;
    [SerializeField] private Camera _camera;
    [SerializeField] private float _height;
    [SerializeField] private Vector2 _offset;

    private TextMeshProUGUI _label;
    private int _previousStack = -1;
    private int _previousTime = -1;

    private void Awake()
    {
        _label = GetComponent<TextMeshProUGUI>();
    }

    private void LateUpdate()
    {
        if (_unit == null || !_unit.isActiveAndEnabled || _unit.HpCurrent <= 0f || _camera == null)
        {
            _label.enabled = false;
            
            return;
        }

        Vector3 anchor = _unit.transform.position + Vector3.up * _height;
        Vector3 screen = _camera.WorldToScreenPoint(anchor);
        _label.enabled = screen.z > 0f && _camera.pixelRect.Contains((Vector2)screen);

        if (!_label.enabled)
        {
            return;
        }

        _label.rectTransform.position = (Vector2)screen + _offset;
        if (_display == EDisplay.Cooldown && _unit is CPlayer player)
        {
            player.GetSkillCooldown(_skillType, out _, out float remaining);
            UpdateTime(remaining, 0);
            return;
        }

        if (_display == EDisplay.CrownWindow && _unit is CPlayer crownPlayer)
        {
            UpdateTime(crownPlayer.CrownWindowRemaining, 0);
            return;
        }

        if (_status == null)
        {
            _label.enabled = false;
            return;
        }

        CStatus status = _unit.FindStatus(_status.Id);
        int stack = status != null ? status.Stack : 0;
        if (_display == EDisplay.Duration)
        {
            UpdateTime(status != null ? status.Duration : 0f, stack);
            return;
        }

        if (stack != _previousStack)
        {
            _label.text = stack.ToString();
            _previousStack = stack;
        }
    }

    private void UpdateTime(float remaining, int stack)
    {
        int time = Mathf.CeilToInt(Mathf.Max(0f, remaining) * 10f);
        if (time == _previousTime && stack == _previousStack)
        {
            return;
        }

        _label.SetText(_format, time * 0.1f, stack);
        _previousTime = time;
        _previousStack = stack;
    }

}
