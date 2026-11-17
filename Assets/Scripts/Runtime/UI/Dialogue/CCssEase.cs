using UnityEngine;

/// <summary>
/// cubic-bezier 이징과 시간 기반 전환을 계산합니다.
/// </summary>
public static class CCssEase
{
    public static readonly Vector4 EASE = new Vector4(0.25f, 0.1f, 0.25f, 1f);
    public static readonly Vector4 EASE_IN = new Vector4(0.42f, 0f, 1f, 1f);
    public static readonly Vector4 EASE_OUT = new Vector4(0f, 0f, 0.58f, 1f);
    public static readonly Vector4 EASE_IN_OUT = new Vector4(0.42f, 0f, 0.58f, 1f);
    public static readonly Vector4 LINEAR = new Vector4(0f, 0f, 1f, 1f);

    /// <summary>
    /// CSS cubic-bezier(x1, y1, x2, y2) 의 진행도 x(0~1)에 대한 값을 반환합니다.
    /// </summary>
    public static float Evaluate(Vector4 curve, float x)
    {
        if (x <= 0f) return 0f;
        if (x >= 1f) return 1f;

        // x(t) = x 를 뉴턴법 + 이분법으로 풀고 y(t) 를 구함
        float t = x;

        for (int i = 0; i < 8; i++)
        {
            float error = Bezier(curve.x, curve.z, t) - x;

            if (Mathf.Abs(error) < 1e-5f) return Bezier(curve.y, curve.w, t);

            float slope = BezierSlope(curve.x, curve.z, t);

            if (Mathf.Abs(slope) < 1e-6f) break;

            t -= error / slope;
        }

        float low = 0f;
        float high = 1f;
        t = x;

        for (int i = 0; i < 24; i++)
        {
            float value = Bezier(curve.x, curve.z, t);

            if (Mathf.Abs(value - x) < 1e-5f) break;

            if (value < x)
            {
                low = t;
            }

            else
            {
                high = t;
            }

            t = (low + high) * 0.5f;
        }

        return Bezier(curve.y, curve.w, t);
    }

    /// <summary>
    /// 0~1 진행률에 smoothstep 곡선을 적용합니다.
    /// </summary>
    public static float Smooth(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    private static float Bezier(float p1, float p2, float t)
    {
        float u = 1f - t;
        return 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t;
    }

    private static float BezierSlope(float p1, float p2, float t)
    {
        float u = 1f - t;
        return 3f * u * u * p1 + 6f * u * t * (p2 - p1) + 3f * t * t * (1f - p2);
    }
}

/// <summary>
/// CSS transition 한 속성. 목표가 바뀌면 지금 값에서 새 목표로 duration · delay · 이징대로 옮겨 간다.
/// </summary>
public struct SCssTween
{
    private float _from;
    private float _to;
    private float _start;
    private float _duration;
    private float _delay;
    private Vector4 _curve;

    public float Target => _to;

    /// <summary>
    /// 전환 없이 바로 값을 정합니다.
    /// </summary>
    public void Snap(float value)
    {
        _from = value;
        _to = value;
        _duration = 0f;
        _delay = 0f;
        _start = 0f;
        _curve = CCssEase.LINEAR;
    }

    /// <summary>
    /// 목표를 바꿉니다. 같은 목표면 아무것도 하지 않습니다.
    /// </summary>
    public void To(float target, float now, float duration, float delay, Vector4 curve)
    {
        if (Mathf.Approximately(target, _to)) return;

        _from = Value(now);
        _to = target;
        _start = now;
        _duration = duration;
        _delay = delay;
        _curve = curve;
    }

    /// <summary>
    /// now 시각의 값.
    /// </summary>
    public float Value(float now)
    {
        if (_duration <= 0f) return now - _start >= _delay ? _to : _from;

        float k = (now - _start - _delay) / _duration;

        if (k <= 0f) return _from;
        if (k >= 1f) return _to;

        return Mathf.LerpUnclamped(_from, _to, CCssEase.Evaluate(_curve, k));
    }
}
