#ifndef ESPOIR_ENVIRONMENT_COMMON_INCLUDED
#define ESPOIR_ENVIRONMENT_COMMON_INCLUDED

float3 Mod289(float3 x)
{
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}

float2 Mod289(float2 x)
{
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}

float3 Permute(float3 x)
{
    return Mod289(((x * 34.0) + 10.0) * x);
}

float SimplexNoise(float2 v)
{
    const float4 C = float4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);
    float2 i = floor(v + dot(v, C.yy));
    float2 x0 = v - i + dot(i, C.xx);
    float2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
    float4 x12 = x0.xyxy + C.xxzz;
    x12.xy -= i1;
    i = Mod289(i);
    float3 p = Permute(Permute(i.y + float3(0.0, i1.y, 1.0)) + i.x + float3(0.0, i1.x, 1.0));
    float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
    m = m * m;
    m = m * m;
    float3 x = 2.0 * frac(p * C.www) - 1.0;
    float3 h = abs(x) - 0.5;
    float3 ox = floor(x + 0.5);
    float3 a0 = x - ox;
    m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
    float3 g;
    g.x = a0.x * x0.x + h.x * x0.y;
    g.yz = a0.yz * x12.xz + h.yz * x12.yw;
    return 130.0 * dot(m, g);
}

float FractalNoise(float2 p)
{
    float a = 0.5;
    float s = 0.0;
    for (int i = 0; i < 4; i++)
    {
        s += a * SimplexNoise(p);
        p = p * 2.03 + 17.1;
        a *= 0.5;
    }
    return s;
}

float Hash12(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float2 Hash22(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

// 양수 y에 대해 음수 입력도 0~y 범위로 나머지를 계산한다.
float PositiveModulo(float x, float y)
{
    return x - y * floor(x / y);
}

// 판석: 보로노이 셀 경계까지 거리(x) + 셀 고유값(y)
float2 PaveCell(float2 x)
{
    float2 n = floor(x);
    float2 f = frac(x);
    float2 mg = 0.0;
    float2 mr = 0.0;
    float md = 8.0;
    for (int j = -1; j <= 1; j++)
    {
        for (int i = -1; i <= 1; i++)
        {
            float2 g = float2(i, j);
            float2 r = g + Hash22(n + g) * 0.75 + 0.125 - f;
            float d = dot(r, r);
            if (d < md)
            {
                md = d;
                mr = r;
                mg = g;
            }
        }
    }
    md = 8.0;
    for (int jj = -2; jj <= 2; jj++)
    {
        for (int ii = -2; ii <= 2; ii++)
        {
            float2 g = mg + float2(ii, jj);
            float2 r = g + Hash22(n + g) * 0.75 + 0.125 - f;
            if (dot(mr - r, mr - r) > 0.00001) md = min(md, dot(0.5 * (mr + r), normalize(r - mr)));
        }
    }
    return float2(md, Hash12(n + mg));
}

// 조명 방향과 그림자는 씬의 Directional Light를 사용한다.
float3 _LightTint;
float3 _Ambient;
float _AmbientStrength;
float3 _ShadowTint;
float3 _MidTint;
float _RampThreshold;
float _RampSoft;
float _MidWidth;
float _MidStrength;
float _CastShadowStrength;
float _Rim;
float3 _RimColor;
float3 _FogColor;
float _FogNear;
float _FogFar;
float _FogHeight;
// 맵 루트의 월드 높이
float _MapHeight;
float4 _MapOrigin;
float4 _MapAxisX;
float4 _MapAxisZ;

float3 MapDirection(float3 direction)
{
    return float3(dot(direction, _MapAxisX.xyz), direction.y, dot(direction, _MapAxisZ.xyz));
}

float3 MapPosition(float3 position)
{
    return MapDirection(position - _MapOrigin.xyz);
}
// xyz = 월드 위치, w = 반경
float4 _LampPos;
float3 _LampColor;
float _LampFlicker;

// 그늘 색 → 경계 띠 → 밝은 면
float3 ToonRamp(float lf)
{
    float lit = smoothstep(_RampThreshold - _RampSoft, _RampThreshold + _RampSoft, lf);
    float k = (lf - _RampThreshold) / max(_MidWidth, 1e-3);
    float band = exp(-k * k) * _MidStrength;
    float3 c = lerp(_ShadowTint, 1.0, lit);
    return lerp(c, _MidTint, band);
}

float3 LampColor()
{
    float t = _Time.y;
    float fl = _LampFlicker > 0.5 ? 0.85 + 0.15 * sin(t * 17.0) * sin(t * 7.3) : 0.97 + 0.03 * sin(t * 5.0);
    return _LampColor * fl;
}

// 등불: 거리 감쇠 + 툰 2단계
float3 LampLight(float3 p, float3 normalDirection)
{
    if (_LampPos.w <= 0.0) return 0.0;
    float3 d = _LampPos.xyz - p;
    float dist = length(d);
    float r = _LampPos.w;
    float att = 1.0 - smoothstep(r * 0.15, r, dist);
    att *= att;
    float ndl = max(dot(normalDirection, d / max(dist, 1e-4)), 0.0) * 0.7 + 0.3;
    float stepAtt = smoothstep(0.08, 0.12, att) * 0.55 + smoothstep(0.45, 0.5, att) * 0.45;
    return LampColor() * lerp(att, stepAtt, 0.6) * ndl;
}

float DistanceFog(float dist)
{
    return smoothstep(_FogNear, _FogFar, dist);
}

#endif
