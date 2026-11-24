using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CLinePreview : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _heightOffset = 0.05f;

    // 연결할 
    private static readonly int[] Triangles =
        { 0, 2, 1, // 삼각형 1
        1, 2, 3 }; // 삼각형 2

    private static readonly Vector2[] UVs =
    {
        new Vector2(0f, 0f),
        new Vector2(1f, 0f),
        new Vector2(0f, 1f),
        new Vector2(1f, 1f)
    };

    private readonly Vector3[] _vertices = new Vector3[4];
    private Mesh _mesh;

    private void Awake()
    {
        _mesh = new Mesh { name = "Line Preview" };
        GetComponent<MeshFilter>().sharedMesh = _mesh;
    }

    public void Show(Vector3 start, Vector3 end, float width)
    {
        Vector3 direction = end - start;
        direction.y = 0f;
        float length = direction.magnitude;

        if (length < 0.001f || width <= 0f)
        {
            ClearPreview();
            return;
        }

        transform.SetPositionAndRotation(
            start + Vector3.up * _heightOffset,
            Quaternion.LookRotation(direction / length, Vector3.up));

        float halfWidth = width * 0.5f;

        _vertices[0] = new Vector3(-halfWidth, 0f, 0f);
        _vertices[1] = new Vector3(halfWidth, 0f, 0f);
        _vertices[2] = new Vector3(-halfWidth, 0f, length);
        _vertices[3] = new Vector3(halfWidth, 0f, length);

        _mesh.Clear();
        _mesh.vertices = _vertices;
        _mesh.uv = UVs;
        _mesh.triangles = Triangles;
        _mesh.RecalculateNormals();
    }

    public void ClearPreview()
    {
        if (_mesh != null) _mesh.Clear();
    }

    private void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
    }
}