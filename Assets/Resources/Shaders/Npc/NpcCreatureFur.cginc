// 눈과 갈기 위치는 스키닝 전 좌표(TEXCOORD2)를 사용한다.
#ifndef NPC_CREATURE_FUR_INCLUDED
#define NPC_CREATURE_FUR_INCLUDED

float4 _FurCut;
float4 _FurCutA;
float4 _FurCutB;
float4 _Sheen;
float4 _SheenColor;
float4 _FurForm;
float4 _FurFormColor;
float _FurFormSoft;
float _BackRim;
float4 _BackRimColor;
float4 _BackRimShape;
float _Under;
float4 _UnderColor;
float4 _CEyeL;
float4 _CEyeR;
float4 _CEyeRight;
float4 _CEyeUp;
float4 _CEyeFx;
float4 _CEyeHl;
float4 _JigA;
float4 _JigB;
float4 _Jig;

float FurLuma(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

// 모델의 정면은 -Y, 위쪽은 +Z다.
float JigMask(float3 p)
{
    if (_JigB.w < 0.5) return 0.0;
    return smoothstep(_JigA.x, _JigA.y, p.y) * smoothstep(_JigA.z, _JigA.w, p.z) * smoothstep(_JigB.x, _JigB.y, p.z) *
        (1.0 - smoothstep(_JigB.z * 0.7, _JigB.z, abs(p.x)));
}

float3 FurCutAlbedo(float3 albedo)
{
    if (_FurCut.x <= 0.0) return albedo;

    float3 a = _FurCutA.rgb;
    float3 ab = _FurCutB.rgb - a;
    float colorPosition = dot(albedo - a, ab) / max(dot(ab, ab), 1e-5);
    float off = length(albedo - (a + ab * clamp(colorPosition, -0.25, 1.25)));
    float blendMask = (1.0 - smoothstep(_FurCut.z, _FurCut.z * 1.6, off)) * smoothstep(-0.3, -0.12, colorPosition) *
        (1.0 - smoothstep(1.12, 1.3, colorPosition));
    float edgeWidth = max(fwidth(colorPosition) * 1.2, _FurCut.y);
    float k = smoothstep(_FurCut.w - edgeWidth, _FurCut.w + edgeWidth, colorPosition);
    float3 mid = lerp(a, _FurCutB.rgb, clamp(colorPosition, 0.0, 1.0));
    float3 flatColor = lerp(a, _FurCutB.rgb, k) * lerp(1.0, FurLuma(albedo) / max(FurLuma(mid), 1e-4), 0.3);
    return lerp(albedo, flatColor, blendMask * _FurCut.x);
}

void CreatureEyeMask(float3 rest, float3 rawAlbedo, out float eyeMask, out float irisMask,
    out float eyeArea, out float2 eyeOffset)
{
    eyeMask = 0.0;
    irisMask = 0.0;
    eyeArea = 0.0;
    eyeOffset = float2(0.0, 0.0);

    float3 leftDelta = rest - _CEyeL.xyz;
    float3 rightDelta = rest - _CEyeR.xyz;
    bool isLeft = dot(leftDelta, leftDelta) < dot(rightDelta, rightDelta);
    float3 eyeDelta = isLeft ? leftDelta : rightDelta;
    float eyeRadius = isLeft ? _CEyeL.w : _CEyeR.w;
    if (eyeRadius <= 0.0) return;
    eyeOffset = float2(dot(eyeDelta, _CEyeRight.xyz), dot(eyeDelta, _CEyeUp.xyz)) / eyeRadius;
    eyeArea = (1.0 - smoothstep(5.0, 6.0, length(eyeOffset))) *
        (1.0 - smoothstep(3.0, 4.0, abs(dot(eyeDelta, cross(_CEyeRight.xyz, _CEyeUp.xyz))) / eyeRadius));
    float3 gammaColor = pow(max(rawAlbedo, 0.0), 1.0 / 2.2);
    float maxChannel = max(max(gammaColor.r, gammaColor.g), gammaColor.b);
    float minChannel = min(min(gammaColor.r, gammaColor.g), gammaColor.b);
    irisMask = smoothstep(0.62, 0.74, (maxChannel - minChannel) / max(maxChannel, 1e-4)) *
        smoothstep(0.5, 0.65, maxChannel) * eyeArea;
    eyeMask = max(irisMask, (1.0 - smoothstep(0.12, 0.22, maxChannel)) *
        (1.0 - smoothstep(1.4, 1.7, length(eyeOffset))) * eyeArea);
}

float3 CreatureFurShade(float3 col, float3 albedo, int part, float3 N, float3 geoN, float3 V, float NdotH,
    float litMask, float eyeMask, float rimFace)
{
    if (_Under > 0.0 && part != 1)
    {
        col = lerp(col, col * _UnderColor.rgb, smoothstep(-0.15, -0.85, N.y) * _Under * (1.0 - eyeMask));
    }

    if (part == 3 && (_FurForm.x > 0.0 || _FurForm.z > 0.0))
    {
        float3 Nr = normalize(lerp(geoN, N, _FurFormSoft));
        float ndv = max(dot(Nr, V), 0.0);
        float formWidth = fwidth(ndv) * 1.2 + 0.015;
        float side = 1.0 - smoothstep(_FurForm.y - formWidth, _FurForm.y + formWidth, ndv);
        col = lerp(col, col * _FurFormColor.rgb, side * _FurForm.x * (1.0 - eyeMask));
        float mid = smoothstep(_FurForm.w - formWidth, _FurForm.w + formWidth, ndv);
        col *= 1.0 + mid * _FurForm.z * lerp(0.5, 1.0, litMask) * (1.0 - eyeMask);
    }

    if (_Sheen.x > 0.0 && part != 1)
    {
        float sheenWidth = fwidth(NdotH) * 1.2 + _Sheen.z;
        float shine = smoothstep(_Sheen.y - sheenWidth, _Sheen.y + sheenWidth, NdotH) * litMask * (1.0 - eyeMask);
        col *= 1.0 + _SheenColor.rgb * shine * _Sheen.x;
    }

    if (_BackRim > 0.0)
    {
        float br = smoothstep(_BackRimShape.x, _BackRimShape.x + _BackRimShape.y, 1.0 - max(dot(N, V), 0.0)) *
            (1.0 - litMask);
        col += br * _BackRimColor.rgb * _BackRim * lerp(float3(1.0, 1.0, 1.0), albedo * 2.5, 0.5) *
            (part == 1 ? rimFace : 1.0);
    }

    return col;
}

float3 CreatureEyeShade(float3 col, float irisMask, float eyeArea, float2 eyeOffset, float3 lightColor)
{
    col = lerp(col, max(lerp(FurLuma(col).xxx, col, 1.25), 0.0) * (1.0 + 0.3 * smoothstep(0.3, -2.2, eyeOffset.y)),
        irisMask * _CEyeFx.y);
    col = lerp(col, col * float3(0.66, 0.5, 0.46), irisMask * smoothstep(-0.6, 1.0, eyeOffset.y) * _CEyeFx.z);
    float highlightWidth = fwidth(length(eyeOffset)) * 1.2 + 1e-4;
    float highlightRadius = _CEyeHl.x;
    float primaryHighlight = 1.0 - smoothstep(highlightRadius - highlightWidth, highlightRadius + highlightWidth,
        length((eyeOffset - _CEyeHl.yz) * float2(1.0, 1.3)));
    float secondaryHighlight = (1.0 - smoothstep(
        highlightRadius * 0.42 - highlightWidth, highlightRadius * 0.42 + highlightWidth,
        length(eyeOffset - float2(-_CEyeHl.y * 0.95, _CEyeHl.z - 1.25)))) * 0.9;
    return lerp(col, float3(1.0, 0.98, 0.94) * lightColor,
        max(primaryHighlight, secondaryHighlight) * _CEyeFx.w * eyeArea *
        (1.0 - smoothstep(2.2, 2.6, length(eyeOffset))));
}

#endif
