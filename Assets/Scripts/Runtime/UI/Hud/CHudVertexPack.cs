using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 셰이더에 CSS 값을 정점 데이터로 넘기는 도우미. 색 RGB 는 0~255 정수 세 개를 float 하나로 묶는다(셰이더에서 nointerpolation 으로 그대로 받음).
/// </summary>
public static class CHudVertexPack
{
    private const AdditionalCanvasShaderChannels _CHANNELS = AdditionalCanvasShaderChannels.TexCoord1 |
        AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3 |
        AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

    /// <summary>
    /// sRGB 색의 RGB 를 r + g·256 + b·65536 (각 0~255) 로 묶습니다.
    /// </summary>
    public static float Rgb(Color c)
    {
        float r = Mathf.Round(Mathf.Clamp01(c.r) * 255f);
        float g = Mathf.Round(Mathf.Clamp01(c.g) * 255f);
        float b = Mathf.Round(Mathf.Clamp01(c.b) * 255f);
        return r + g * 256f + b * 65536f;
    }

    /// <summary>
    /// 캔버스가 추가 정점 채널(uv1~3 · normal · tangent)을 셰이더로 넘기게 합니다.
    /// </summary>
    public static void EnableChannels(Canvas canvas)
    {
        if (canvas == null) return;

        Canvas root = canvas.rootCanvas;
        if ((root.additionalShaderChannels & _CHANNELS) != _CHANNELS)
        {
            root.additionalShaderChannels |= _CHANNELS;
        }
    }

    /// <summary>
    /// 중심 기준 좌표(uv0.xy)와 반 크기(uv0.zw)를 가진 사각형 하나를 넣습니다.
    /// </summary>
    public static void AddQuad(VertexHelper vh, Vector2 center, Vector2 quadHalf, Vector2 shapeHalf, Color32 color,
        Vector4 uv1, Vector4 uv2, Vector4 uv3, Vector4 tangent, Vector3 normal)
    {
        int start = vh.currentVertCount;
        for (int i = 0; i < 4; i++)
        {
            float sx = i == 0 || i == 1 ? -1f : 1f;
            float sy = i == 0 || i == 3 ? -1f : 1f;
            Vector2 offset = new Vector2(sx * quadHalf.x, sy * quadHalf.y);
            UIVertex v = UIVertex.simpleVert;
            v.position = center + offset;
            v.color = color;
            v.uv0 = new Vector4(offset.x, offset.y, shapeHalf.x, shapeHalf.y);
            v.uv1 = uv1;
            v.uv2 = uv2;
            v.uv3 = uv3;
            v.tangent = tangent;
            v.normal = normal;
            vh.AddVert(v);
        }

        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start + 2, start + 3, start);
    }
}
