using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(1000)]
public class CHerbalistHeadLighting : MonoBehaviour
{
    [SerializeField] private Transform _head;
    [SerializeField] private Vector3 _centerLocal;
    [SerializeField] private Vector3 _forwardLocal = Vector3.forward;
    [SerializeField] private Vector3 _upLocal = Vector3.up;
    [SerializeField] private float _radius = 0.1f;

    private static readonly int _CENTER = Shader.PropertyToID("uHeadCenter");
    private static readonly int _UP = Shader.PropertyToID("uHeadUp");
    private static readonly int _FORWARD = Shader.PropertyToID("uHeadForward");
    private static readonly int _RIGHT = Shader.PropertyToID("uHeadRight");
    private static readonly int _RADIUS = Shader.PropertyToID("uHeadRadius");

    private Renderer[] _renderers;
    private MaterialPropertyBlock _properties;

    private void OnEnable()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _properties = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        if (_head == null)
        {
            return;
        }

        Vector3 up = _head.TransformDirection(_upLocal).normalized;
        Vector3 forward = _head.TransformDirection(_forwardLocal).normalized;
        Vector3 right = Vector3.Cross(up, forward).normalized;
        Vector3 center = _head.TransformPoint(_centerLocal);
        float radius = _radius * transform.lossyScale.x;

        foreach (Renderer target in _renderers)
        {
            if (target == null)
            {
                continue;
            }

            target.GetPropertyBlock(_properties);
            _properties.SetVector(_CENTER, center);
            _properties.SetVector(_UP, up);
            _properties.SetVector(_FORWARD, forward);
            _properties.SetVector(_RIGHT, right);
            _properties.SetFloat(_RADIUS, radius);
            target.SetPropertyBlock(_properties);
        }
    }
}
