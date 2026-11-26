using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CBossArena : MonoBehaviour
{
    private BoxCollider _box;

    private void Awake()
    {
        _box = GetComponent<BoxCollider>();
    }

    public bool Contains(Vector3 worldPosition)
    {
        Vector3 offset = transform.InverseTransformPoint(worldPosition) - _box.center;
        Vector3 halfSize = _box.size * 0.5f;

        return Mathf.Abs(offset.x) <= halfSize.x
            && Mathf.Abs(offset.y) <= halfSize.y
            && Mathf.Abs(offset.z) <= halfSize.z;
    }
}