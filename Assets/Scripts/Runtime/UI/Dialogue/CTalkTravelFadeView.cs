using TMPro;
using UnityEngine;

/// <summary>
/// 맵 이동 시 화면을 암전시키고 목적지 이름과 이동 중 문구를 표시합니다.
/// </summary>
public class CTalkTravelFadeView : MonoBehaviour
{
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private TMP_Text _destination;
    [SerializeField, Min(0.01f)] private float _duration = 0.6f;

    private SCssTween _opacity;
    private bool _isOn;

    private void Awake()
    {
        _opacity.Snap(0f);
        Apply(0f);
    }

    /// <summary>
    /// 암전을 켜거나 끕니다. 켤 때 갈 맵 이름을 넣습니다.
    /// </summary>
    public void Set(bool isOn, string destination, float now)
    {
        _isOn = isOn;
        if (isOn && destination != null) _destination.text = destination;
        _opacity.To(isOn ? 1f : 0f, now, _duration, 0f, CCssEase.EASE);
    }

    private void Update()
    {
        Apply(Time.unscaledTime);
    }

    private void Apply(float now)
    {
        float opacity = _opacity.Value(now);
        _group.alpha = opacity;
        // 켜진 동안만 클릭을 막음 (pointer-events: auto)
        _group.blocksRaycasts = _isOn;
    }
}
