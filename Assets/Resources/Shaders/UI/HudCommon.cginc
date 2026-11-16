// 하단 HUD 셰이더 공용 함수 (CSS 값을 그대로 쓰기 위한 도우미)
#ifndef ESPOIR_HUD_COMMON_INCLUDED
#define ESPOIR_HUD_COMMON_INCLUDED

#include "UiSrgbComposite.cginc"

// CHudBox 등에서 RGB 를 r + g*256 + b*65536 (0~255 정수) 로 묶어 보낸 값을 푼다. 알파는 따로 0~1.
float4 HudUnpack(float packedRgb, float alpha)
{
    float v = round(packedRgb);
    float b = floor(v / 65536.0);
    float g = floor((v - b * 65536.0) / 256.0);
    float r = v - b * 65536.0 - g * 256.0;
    return float4(float3(r, g, b) / 255.0, alpha);
}

float3 HudSrgbToLinear(float3 c)
{
    return c <= 0.04045 ? c / 12.92 : pow((c + 0.055) / 1.055, 2.4);
}

float3 HudLinearToSrgb(float3 c)
{
    c = max(c, 0.0);
    return c <= 0.0031308 ? c * 12.92 : 1.055 * pow(c, 1.0 / 2.4) - 0.055;
}

// 프리멀티플라이드 sRGB → 프리멀티플라이드 선형 (화면 버퍼가 선형 공간이므로 마지막에 한 번 바꿈)
float4 HudPremulSrgbToLinear(float4 c)
{
    if (c.a <= 1e-5) return float4(0, 0, 0, 0);
    float3 s = saturate(c.rgb / c.a);
    return float4(HudSrgbToLinear(s) * c.a, c.a);
}

float4 HudPremulLinearToSrgb(float4 c)
{
    if (c.a <= 1e-5) return float4(0, 0, 0, 0);
    float3 s = saturate(c.rgb / c.a);
    return float4(HudLinearToSrgb(s) * c.a, c.a);
}

// 최종 출력: 프리멀티플라이드 sRGB → 화면. 전역 sRGB 합성 구간 안(_UiGammaOut 1)이면 sRGB 숫자 그대로, 아니면 선형으로 바꿈
float4 HudOut(float4 premulSrgb)
{
    return _UiGammaOut > 0.5 ? premulSrgb : HudPremulSrgbToLinear(premulSrgb);
}

// 잡은 화면(GrabPass) 색 → sRGB. 전역 sRGB 합성 구간 안이면 이미 sRGB 숫자
float3 HudGrabToSrgb(float3 c)
{
    return _UiGammaOut > 0.5 ? c : HudLinearToSrgb(c);
}

// src 를 dst 위에 (둘 다 프리멀티플라이드)
float4 HudOver(float4 src, float4 dst)
{
    return src + (1.0 - src.a) * dst;
}

// 둥근 사각형 SDF (p: 중심 기준, hs: 반 크기, r: 모서리 반지름). 안쪽이 음수.
float HudRoundRect(float2 p, float2 hs, float r)
{
    float2 q = abs(p) - hs + r;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}

float HudErf(float x)
{
    float s = sign(x);
    float a = abs(x);
    float t = 1.0 / (1.0 + 0.3275911 * a);
    float y = 1.0 - (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592) *
        t * exp(-a * a);
    return s * y;
}

// 가우시안으로 흐린 반평면의 덮임 정도: 경계까지 거리 d(바깥 +), 표준편차 sigma. CSS 그림자 blur 값 b → sigma = b / 2.
float HudShadowEdge(float d, float sigma)
{
    return 0.5 * (1.0 - HudErf(d / (sigma * 1.41421356)));
}

// CSS linear-gradient(<angle>) 의 진행값 0~1 (p: 중심 기준, y 위쪽 +)
float HudGradientT(float2 p, float2 hs, float angleDeg)
{
    float a = radians(angleDeg);
    float2 dir = float2(sin(a), cos(a));
    float len = abs(2.0 * hs.x * dir.x) + abs(2.0 * hs.y * dir.y);
    return dot(p, dir) / max(len, 1e-4) + 0.5;
}

// 흐린 배경: 화면 텍스처를 표준편차 sigmaPx(화면 px) 가우시안으로 (9x9 표본, 간격 sigma/2)
float3 HudBlurScene(sampler2D tex, float4 texel, float2 uv, float sigmaPx)
{
    float3 sum = 0;
    float wsum = 0;
    float stepPx = sigmaPx * 0.5;
    [unroll]
    for (int y = -4; y <= 4; y++)
    {
        [unroll]
        for (int x = -4; x <= 4; x++)
        {
            float2 o = float2(x, y) * stepPx;
            float w = exp(-dot(o, o) / (2.0 * sigmaPx * sigmaPx));
            sum += tex2Dlod(tex, float4(uv + o * texel.xy, 0, 0)).rgb * w;
            wsum += w;
        }
    }
    return sum / wsum;
}

#endif
