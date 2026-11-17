using UnityEngine;

/// <summary>
/// CSS 타이밍 함수(cubic-bezier · ease · ease-in-out · ease-out)를 그대로 계산한다.
/// </summary>
public static class CHudEase
{
    /// <summary>CSS ease = cubic-bezier(.25, .1, .25, 1).</summary>
    public static float Ease(float t) => Bezier(0.25f, 0.1f, 0.25f, 1f, t);

    /// <summary>CSS ease-in-out = cubic-bezier(.42, 0, .58, 1).</summary>
    public static float EaseInOut(float t) => Bezier(0.42f, 0f, 0.58f, 1f, t);

    /// <summary>CSS ease-out = cubic-bezier(0, 0, .58, 1).</summary>
    public static float EaseOut(float t) => Bezier(0f, 0f, 0.58f, 1f, t);

    /// <summary>CSS ease-in = cubic-bezier(.42, 0, 1, 1).</summary>
    public static float EaseIn(float t) => Bezier(0.42f, 0f, 1f, 1f, t);

    /// <summary>
    /// CSS cubic-bezier(x1, y1, x2, y2) 의 진행값 t(0~1)에 대한 출력값.
    /// </summary>
    public static float Bezier(float x1, float y1, float x2, float y2, float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;

        // x(s) = t 인 s 를 뉴턴법 + 이분법으로 찾는다.
        float s = t;
        for (int i = 0; i < 8; i++)
        {
            float x = Curve(x1, x2, s) - t;
            float dx = Slope(x1, x2, s);
            if (Mathf.Abs(x) < 1e-5f) break;
            if (Mathf.Abs(dx) < 1e-6f) break;
            s -= x / dx;
        }

        if (s < 0f || s > 1f || Mathf.Abs(Curve(x1, x2, s) - t) > 1e-3f)
        {
            float lo = 0f;
            float hi = 1f;
            s = t;
            for (int i = 0; i < 30; i++)
            {
                float x = Curve(x1, x2, s);
                if (Mathf.Abs(x - t) < 1e-5f) break;
                if (x < t) lo = s;
                else hi = s;
                s = (lo + hi) * 0.5f;
            }
        }

        return Curve(y1, y2, s);
    }

    private static float Curve(float a, float b, float s)
    {
        float u = 1f - s;
        return 3f * u * u * s * a + 3f * u * s * s * b + s * s * s;
    }

    private static float Slope(float a, float b, float s)
    {
        float u = 1f - s;
        return 3f * u * u * a + 6f * u * s * (b - a) + 3f * s * s * (1f - b);
    }
}
