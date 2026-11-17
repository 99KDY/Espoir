using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

/// <summary>
/// TMP 글자의 크기, 줄 높이와 자간을 설정합니다.
/// </summary>
public static class CCssText
{
    /// <summary>
    /// font-size(px) · line-height(px) · letter-spacing(em) 을 넣습니다.
    /// 줄 높이는 글꼴 기본 줄 높이와의 차이를 lineSpacing 으로, CSS 의 위쪽 반 여백(half-leading)을 위 여백으로 맞춥니다.
    /// </summary>
    public static void Apply(TMP_Text text, float fontSizePx, float lineHeightPx, float letterSpacingEm)
    {
        text.fontSize = fontSizePx;
        text.characterSpacing = letterSpacingEm * 100f;

        if (text.font == null || lineHeightPx <= 0f) return;

        FaceInfo face = text.font.faceInfo;
        float scale = fontSizePx / face.pointSize;
        float fontLine = face.lineHeight * scale;
        float content = (face.ascentLine - face.descentLine) * scale;
        text.lineSpacing = (lineHeightPx - fontLine) / fontSizePx * 100f;
        Vector4 margin = text.margin;
        margin.y = (lineHeightPx - content) * 0.5f;
        text.margin = margin;
    }
}
