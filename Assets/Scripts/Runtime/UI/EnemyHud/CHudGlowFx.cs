using UnityEngine;

/// <summary>
/// 체력 막대 끝에 피격 불티와 원형 섬광을 표시합니다.
/// 미리 만든 사각형을 돌려 쓰며, 좌표는 이 오브젝트의 RectTransform 기준(1080 높이 캔버스 단위)이다.
/// </summary>
public class CHudGlowFx : MonoBehaviour
{
    private const float _SPARK_GRAVITY = 160f;

    private struct SGlowItem
    {
        public CCssBoxGraphic graphic;
        public bool isActive;
        public bool isSpark;
        public float startTime;
        public float life;
        public Vector2 origin;
        public Vector2 velocity;
        public float size;
        public float drag;
        public float peak;
        public Vector3 color;
    }

    [SerializeField] private Material _material;
    [SerializeField] private int _poolSize = 24;

    private SGlowItem[] _items;
    private int _next;

    private void Awake()
    {
        _items = new SGlowItem[Mathf.Max(1, _poolSize)];
        for (int i = 0; i < _items.Length; i++)
        {
            GameObject go = new GameObject("Glow" + i, typeof(RectTransform));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            CCssBoxGraphic g = go.AddComponent<CCssBoxGraphic>();
            g.material = _material;
            g.raycastTarget = false;
            go.SetActive(false);
            _items[i].graphic = g;
        }
    }

    private void OnDisable()
    {
        if (_items == null) return;
        for (int i = 0; i < _items.Length; i++)
        {
            Release(i);
        }
    }

    /// <summary>
    /// 부드러운 원광 (테두리 없음). 크기는 반지름 r × 2 × (0.7 → 1.2), 알파 = peak · sin(min(1, 3k)·π/2) · (1 − k).
    /// </summary>
    public void Flare(Vector2 position, float radius, Vector3 color, float peak, float life)
    {
        int i = Take();
        _items[i].isSpark = false;
        _items[i].origin = position;
        _items[i].size = radius;
        _items[i].color = color;
        _items[i].peak = peak;
        _items[i].life = life;
        Begin(i);
    }

    /// <summary>
    /// 불티의 속도, 방향, 크기와 수명을 지정합니다. angle은 화면 위쪽이 -π/2인 좌표계 기준입니다.
    /// </summary>
    public void Spark(Vector2 position, Vector3 color, float speed, float angle, float spread, float size,
        float life, float drag)
    {
        int i = Take();
        float a = angle + (Random.value - 0.5f) * spread;
        float v = speed * (0.35f + Random.value * 0.65f);
        _items[i].isSpark = true;
        _items[i].origin = position + new Vector2((Random.value - 0.5f) * 4f, -(Random.value - 0.5f) * 4f);
        // 아래쪽으로 증가하는 y를 Unity 캔버스의 위쪽으로 증가하는 y로 변환합니다.
        _items[i].velocity = new Vector2(Mathf.Cos(a) * v, -Mathf.Sin(a) * v);
        _items[i].size = size * (0.6f + Random.value * 0.8f);
        _items[i].life = life * (0.7f + Random.value * 0.6f);
        _items[i].drag = drag;
        _items[i].color = color;
        Begin(i);
    }

    private int Take()
    {
        for (int n = 0; n < _items.Length; n++)
        {
            int i = (_next + n) % _items.Length;
            if (!_items[i].isActive)
            {
                _next = i + 1;
                return i;
            }
        }

        int oldest = _next % _items.Length;
        _next = oldest + 1;
        return oldest;
    }

    private void Begin(int i)
    {
        _items[i].isActive = true;
        _items[i].startTime = Time.time;
        _items[i].graphic.gameObject.SetActive(true);
        UpdateItem(i, Time.time);
    }

    private void LateUpdate()
    {
        float now = Time.time;
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i].isActive) UpdateItem(i, now);
        }
    }

    private void UpdateItem(int i, float now)
    {
        SGlowItem item = _items[i];
        float age = now - item.startTime;
        float k = age / item.life;
        if (k >= 1f)
        {
            Release(i);
            return;
        }

        RectTransform rt = item.graphic.rectTransform;
        if (!item.isSpark)
        {
            float side = item.size * 2f * (0.7f + k * 0.5f);
            rt.anchoredPosition = item.origin;
            rt.sizeDelta = new Vector2(side, side);
            rt.localRotation = Quaternion.identity;
            float alpha = item.peak * Mathf.Sin(Mathf.Min(1f, k * 3f) * Mathf.PI * 0.5f) * (1f - k);
            item.graphic.SetDynamic(new Vector4(item.color.x, item.color.y, item.color.z, alpha), Vector4.zero,
                Vector3.zero, Vector4.zero);
            return;
        }

        // 속도 감쇠와 아래쪽 중력을 적용합니다.
        float decay = Mathf.Exp(-item.drag * age);
        float e = (1f - decay) / Mathf.Max(item.drag, 0.001f);
        Vector2 pos = item.origin + item.velocity * e + new Vector2(0f, -0.5f * _SPARK_GRAVITY * age * age);
        Vector2 vel = item.velocity * decay + new Vector2(0f, -_SPARK_GRAVITY * age);
        float speed = vel.magnitude;
        float stretch = Mathf.Clamp(speed * 0.03f, 1f, 5f);
        float vA = (1f - Mathf.SmoothStep(0f, 1f, (k - 0.55f) / 0.45f)) * Mathf.SmoothStep(0f, 1f, k / 0.06f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(item.size * 2f * stretch, item.size * 2f);
        rt.localRotation = Quaternion.Euler(0f, 0f, speed > 1f ? Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg : 0f);
        item.graphic.SetDynamic(new Vector4(item.color.x, item.color.y, item.color.z, vA),
            new Vector4(1f, 0f, 0f, 0f), Vector3.zero, Vector4.zero);
    }

    private void Release(int i)
    {
        _items[i].isActive = false;
        if (_items[i].graphic != null && _items[i].graphic.gameObject.activeSelf)
        {
            _items[i].graphic.gameObject.SetActive(false);
        }
    }
}
