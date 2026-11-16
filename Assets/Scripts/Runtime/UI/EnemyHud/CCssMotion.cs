using UnityEngine;

/// <summary>
/// cubic-bezier 이징 곡선과 시간별 키프레임을 계산합니다.
/// </summary>
public static class CCssEasing
{
    public static readonly Vector4 EASE = new Vector4(0.25f, 0.1f, 0.25f, 1f);
    public static readonly Vector4 EASE_IN = new Vector4(0.42f, 0f, 1f, 1f);
    public static readonly Vector4 EASE_OUT = new Vector4(0f, 0f, 0.58f, 1f);
    public static readonly Vector4 EASE_IN_OUT = new Vector4(0.42f, 0f, 0.58f, 1f);
    public static readonly Vector4 LINEAR = new Vector4(0f, 0f, 1f, 1f);

    /// <summary>
    /// cubic-bezier(x1, y1, x2, y2) 로 0~1 진행값을 바꾼다.
    /// </summary>
    public static float Evaluate(Vector4 curve, float t)
    {
        t = Mathf.Clamp01(t);
        if (curve == LINEAR) return t;

        // x(u) = t 를 뉴턴법으로 풀고 y(u) 를 돌려준다.
        float u = t;
        for (int i = 0; i < 8; i++)
        {
            float x = BezierAxis(u, curve.x, curve.z) - t;
            float dx = BezierSlope(u, curve.x, curve.z);
            if (Mathf.Abs(x) < 1e-5f || Mathf.Abs(dx) < 1e-6f) break;
            u = Mathf.Clamp01(u - x / dx);
        }

        return BezierAxis(u, curve.y, curve.w);
    }

    /// <summary>
    /// @keyframes 값 (구간마다 같은 이징). times 는 0~1 오름차순, 첫 값 0 · 마지막 값 1 을 포함해야 한다.
    /// </summary>
    public static Vector2 Keyframes(float[] times, Vector2[] values, Vector4 curve, float k)
    {
        if (k <= times[0]) return values[0];

        for (int i = 1; i < times.Length; i++)
        {
            if (k <= times[i])
            {
                float span = times[i] - times[i - 1];
                float local = span > 0f ? (k - times[i - 1]) / span : 1f;
                return Vector2.LerpUnclamped(values[i - 1], values[i], Evaluate(curve, local));
            }
        }

        return values[values.Length - 1];
    }

    private static float BezierAxis(float u, float p1, float p2)
    {
        float iu = 1f - u;
        return 3f * iu * iu * u * p1 + 3f * iu * u * u * p2 + u * u * u;
    }

    private static float BezierSlope(float u, float p1, float p2)
    {
        float iu = 1f - u;
        return 3f * iu * iu * p1 + 6f * iu * u * (p2 - p1) + 3f * u * u * (1f - p2);
    }
}

/// <summary>
/// CSS transition 한 개. 목표가 바뀌면 지금 보이는 값에서 새 목표로 처음부터 다시 간다.
/// </summary>
public struct SCssTransition
{
    private float _from;
    private float _to;
    private float _startTime;
    private float _duration;
    private float _delay;
    private Vector4 _curve;

    /// <summary>
    /// 지금 목표값.
    /// </summary>
    public float Target => _to;

    /// <summary>
    /// 전환 없이 바로 값을 정한다.
    /// </summary>
    public void Snap(float value)
    {
        _from = value;
        _to = value;
        _duration = 0f;
        _delay = 0f;
    }

    /// <summary>
    /// 목표가 바뀌었으면 지금 보이는 값에서 시작하는 전환을 건다.
    /// </summary>
    public void Set(float target, float now, float duration, float delay, Vector4 curve)
    {
        if (Mathf.Approximately(target, _to)) return;

        _from = Evaluate(now);
        _to = target;
        _startTime = now;
        _duration = duration;
        _delay = delay;
        _curve = curve;
    }

    /// <summary>
    /// 시각 now 의 보이는 값.
    /// </summary>
    public float Evaluate(float now)
    {
        if (_duration <= 0f) return _to;

        float k = (now - _startTime - _delay) / _duration;
        if (k <= 0f) return _from;
        if (k >= 1f) return _to;
        return Mathf.LerpUnclamped(_from, _to, CCssEasing.Evaluate(_curve, k));
    }

    /// <summary>
    /// 전환이 진행 중인지.
    /// </summary>
    public bool IsRunning(float now)
    {
        return _duration > 0f && now - _startTime - _delay < _duration;
    }
}
