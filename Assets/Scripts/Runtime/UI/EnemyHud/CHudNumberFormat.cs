using TMPro;
using UnityEngine;

/// <summary>
/// 체력 UI 글자 도우미: 문자열을 만들지 않는 천 단위 쉼표 숫자 쓰기, CSS 와 같은 방식의 글자 상자 폭 계산.
/// </summary>
public static class CHudNumberFormat
{
    /// <summary>
    /// buffer[start] 부터 value 를 천 단위 쉼표와 함께 쓴다(en-US). 반환값 = 쓴 뒤 끝 위치.
    /// </summary>
    public static int Write(char[] buffer, int value, int start = 0)
    {
        if (value < 0) value = 0;

        int digits = 1;
        for (int v = value; v >= 10; v /= 10) digits++;
        int length = digits + (digits - 1) / 3;
        int position = start + length;
        int written = 0;
        do
        {
            if (written > 0 && written % 3 == 0) buffer[--position] = ',';
            buffer[--position] = (char)('0' + value % 10);
            value /= 10;
            written++;
        }
        while (value > 0);

        return start + length;
    }

    /// <summary>
    /// CSS 글자 상자 폭 = 글자 전진 폭 합 + 글자마다 letter-spacing (마지막 글자 뒤 포함). 커닝은 넣지 않음.
    /// </summary>
    public static float CssWidth(TMP_Text text, char[] chars, int count)
    {
        float width = 0f;
        for (int i = 0; i < count; i++) width += Advance(text.font, chars[i], text.fontSize);
        return width + count * text.characterSpacing * 0.01f * text.fontSize;
    }

    /// <summary>
    /// 문자열 버전.
    /// </summary>
    public static float CssWidth(TMP_Text text, string s)
    {
        float width = 0f;
        for (int i = 0; i < s.Length; i++) width += Advance(text.font, s[i], text.fontSize);
        return width + s.Length * text.characterSpacing * 0.01f * text.fontSize;
    }

    private static float Advance(TMP_FontAsset font, char c, float fontSize)
    {
        TMP_FontAsset f = font;
        for (int depth = 0; f != null && depth < 4; depth++)
        {
            if (f.characterLookupTable.TryGetValue(c, out TMP_Character ch) && ch.glyph != null)
            {
                return ch.glyph.metrics.horizontalAdvance * fontSize / f.faceInfo.pointSize * f.faceInfo.scale;
            }

            f = f.fallbackFontAssetTable != null
                && f.fallbackFontAssetTable.Count > 0 ? f.fallbackFontAssetTable[0] : null;
        }

        return fontSize * 0.5f;
    }
}
