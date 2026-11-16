using UnityEngine;

/// <summary>
/// 하단 HUD 전체에 적용할 색상, 빛 번짐과 변형 효과를 관리합니다.
/// brightness · 색수차 · 진홍 그림자 · 흔들림 · punch · 복제 잔상 · 글리치 복제 · 'bl' 빛 번짐 값을 담는다.
/// </summary>
public struct SHudGroupState
{
    public float brightness;
    public float ca;
    public bool isRed;
    public float punch;
    public Vector2 shake;
    public bool hasGhost;
    public Vector3 ghostScale;
    public Vector3 ghostAlpha;
    public Vector4 glitch0;
    public Vector4 glitch1;
    public float bloomAmp;
    public float bloomTime;

    /// <summary>아무 효과도 없는 상태.</summary>
    public static SHudGroupState Idle => new SHudGroupState { brightness = 1f, ghostScale = Vector3.one };
}

/// <summary>
/// 하단 HUD 묶음 합성기. 시작 사각형(장면을 잡고 알파 비움)과 끝 사각형(묶음을 되살려 효과를 걸고 sRGB 로 합성) 재질에 값을 넣는다.
/// 묶음 안 요소 위치(.cb 상자 · R 중심)는 RectTransform 에서 읽는다.
/// </summary>
[DefaultExecutionOrder(300)]
public class CHudGroupFx : MonoBehaviour
{
    private static readonly int _FX = Shader.PropertyToID("_Fx");
    private static readonly int _SHAKE = Shader.PropertyToID("_Shake");
    private static readonly int _CB = Shader.PropertyToID("_Cb");
    private static readonly int _R_CENTER = Shader.PropertyToID("_RCenter");
    private static readonly int _GHOST_S = Shader.PropertyToID("_GhostS");
    private static readonly int _GHOST_A = Shader.PropertyToID("_GhostA");
    private static readonly int _GL0 = Shader.PropertyToID("_Gl0");
    private static readonly int _GL1 = Shader.PropertyToID("_Gl1");
    private static readonly int _BLOOM = Shader.PropertyToID("_Bloom");
    private static readonly int _C1 = Shader.PropertyToID("_C1");
    private static readonly int _C2 = Shader.PropertyToID("_C2");
    private static readonly int _BACKDROP_ON = Shader.PropertyToID("_HudBackdropOn");
    private static readonly int _CLIP = Shader.PropertyToID("_Clip");
    private static readonly int _BLOOM2 = Shader.PropertyToID("_Bloom2");
    private static readonly int _BLOOM2_C = Shader.PropertyToID("_Bloom2C");

    [SerializeField] private CHudGroupQuad _begin;
    [SerializeField] private CHudGroupQuad _end;
    [SerializeField] private RectTransform _cbBox;
    [SerializeField] private RectTransform _rSlot;
    [SerializeField] private Color _crimson = new Color(1f, 61f / 255f, 82f / 255f);
    [SerializeField] private Color _gold = new Color(1f, 210f / 255f, 122f / 255f);

    private SHudGroupState _state = SHudGroupState.Idle;
    private readonly Vector3[] _CORNERS = new Vector3[4];
    private Vector2 _extraShake;
    private RectTransform _bloom2Center;
    private Vector2 _bloom2;

    /// <summary>
    /// 이번 프레임에 .cb 흔들림(translate, px)을 더합니다(W 사용 순간 등 궁 연출 밖의 트랙). 이 컴포넌트보다 먼저 실행되는 쪽이 매 프레임 호출.
    /// </summary>
    public void AddShake(Vector2 offset)
    {
        _extraShake += offset;
    }

    /// <summary>
    /// 궁극기 이외 슬롯을 중심으로 한 빛 번짐의 강도와 진행 시간을 설정합니다.
    /// </summary>
    public void SetBloom2(RectTransform center, float amp, float time)
    {
        _bloom2Center = center;
        _bloom2 = new Vector2(amp, time);
    }

    /// <summary>
    /// 이번 프레임의 묶음 효과 값을 넣습니다(궁 연출 뷰가 매 프레임 호출).
    /// </summary>
    public void SetState(SHudGroupState state)
    {
        _state = state;
    }

    private void OnEnable()
    {
        Shader.SetGlobalFloat(_BACKDROP_ON, 1f);
    }

    private void OnDisable()
    {
        Shader.SetGlobalFloat(_BACKDROP_ON, 0f);
    }

    private void LateUpdate()
    {
        if (_end == null) return;

        Material m = _end.RuntimeMaterial;
        if (m == null) return;

        RectTransform space = _end.rectTransform;
        Vector4 cb = LocalRect(_cbBox, space);
        Vector4 rr = LocalRect(_rSlot, space);
        Vector2 rc = new Vector2((rr.x + rr.z) * 0.5f, (rr.y + rr.w) * 0.5f);
        float radius = (rr.z - rr.x) * 0.5f;

        m.SetVector(_FX, new Vector4(_state.brightness, _state.ca, _state.isRed ? 1f : 0f, _state.punch));
        m.SetVector(_SHAKE, new Vector4(_state.shake.x, _state.shake.y, 0f, 0f));
        m.SetVector(_CB, cb);
        m.SetVector(_R_CENTER, new Vector4(rc.x, rc.y, radius, 0f));
        m.SetVector(_GHOST_S, new Vector4(_state.ghostScale.x, _state.ghostScale.y, _state.ghostScale.z,
            _state.hasGhost ? 1f : 0f));
        m.SetVector(_GHOST_A, new Vector4(_state.ghostAlpha.x, _state.ghostAlpha.y, _state.ghostAlpha.z, 0f));
        m.SetVector(_GL0, _state.glitch0);
        m.SetVector(_GL1, _state.glitch1);
        m.SetVector(_BLOOM, new Vector4(_state.bloomAmp, _state.bloomTime, 0f, 0f));
        m.SetVector(_C1, new Vector4(_crimson.r, _crimson.g, _crimson.b, 1f));
        m.SetVector(_C2, new Vector4(_gold.r, _gold.g, _gold.b, 1f));

        // 묶음 내용은 GroupBegin 사각형(알파를 비운 곳) 안에만 있음 — 밖을 읽으면 Overlay 화면 알파(1)가 덮임으로 읽혀 선이 생김
        Vector4 clip = _begin != null ? LocalRect(_begin.rectTransform, space) : new Vector4(-1e6f, -1e6f, 1e6f, 1e6f);
        m.SetVector(_CLIP, new Vector4(clip.x + 0.5f, clip.y + 0.5f, clip.z - 0.5f, clip.w - 0.5f));
        Vector2 shake = _state.shake + _extraShake;
        m.SetVector(_SHAKE, new Vector4(shake.x, shake.y, 0f, 0f));
        _extraShake = Vector2.zero;
        if (_bloom2Center != null && _bloom2.x > 0f)
        {
            Vector4 wr = LocalRect(_bloom2Center, space);
            m.SetVector(_BLOOM2, new Vector4(_bloom2.x, _bloom2.y, 0f, 0f));
            m.SetVector(_BLOOM2_C, new Vector4((wr.x + wr.z) * 0.5f, (wr.y + wr.w) * 0.5f, (wr.z - wr.x) * 0.5f, 0f));
        }

        else
        {
            m.SetVector(_BLOOM2, Vector4.zero);
        }

        _bloom2 = Vector2.zero;

        // 그룹 필터를 적용할 때 슬롯 배경 흐림을 끄고 슬롯 색상만 합성합니다.
        bool hasFilter = _state.ca > 0.2f || _state.brightness > 1.01f || (_state.isRed && _state.ca > 0.5f);
        Shader.SetGlobalFloat(_BACKDROP_ON, hasFilter ? 0f : 1f);
    }

    private Vector4 LocalRect(RectTransform target, RectTransform space)
    {
        if (target == null) return new Vector4(0f, 0f, 1f, 1f);

        target.GetWorldCorners(_CORNERS);
        Vector3 a = space.InverseTransformPoint(_CORNERS[0]);
        Vector3 b = space.InverseTransformPoint(_CORNERS[2]);
        return new Vector4(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }
}
