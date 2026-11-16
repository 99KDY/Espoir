#ifndef ESPOIR_CHAPTER3_SAND_BODY_INCLUDED
#define ESPOIR_CHAPTER3_SAND_BODY_INCLUDED

// 챕터3 지형의 바닥 명암과 무늬를 계산합니다.
// 무늬 5는 넓은 대리석 판이며, 연마한 판석 설정을 함께 지원합니다.
// 풀 덮개는 챕터3에 없어서 뺐다(챕터2 전역값이 남아 있어도 영향 없음).
#include "../Chapter2/Chapter2Common.cginc"

float3 _SandLit;
float3 _SandMid;
float3 _SandShadow;
float3 _SandDeep;
float3 _PathColor;
float3 _GlintColor;
float _Crest;
float _LitGrad;
float _GrainTex;
float _GrainSize;
float _Glint;
float _GlintSize;
float _GlintDensity;
float _GoldGlow;
float _GlowBase;
float _GlowCap;
float3 _HardLit;
float3 _HardShadow;
float3 _PaveA;
float3 _PaveB;
float _Strata;
float _PaveSize;
float _PaveSand;
float _PaveStyle;
float _PaveClean;
float _PaveJoint;
float3 _PaveJointColor;
float _Pond;
float _WaterY;
float _Night;

float Chapter3FineGrain(float2 wp, float size, float seed)
{
    float2 p = wp / size;
    float2 c = floor(p);
    float2 f = p - c;
    float2 h = Hash22(c + seed);
    float k = Hash12(c + seed + 5.1);
    float aa = max(fwidth(p.x), fwidth(p.y)) * 0.7 + 1e-4;
    float d = length(f - (0.5 + (h - 0.5) * 0.35));
    float inG = 1.0 - smoothstep(0.4 - aa, 0.4 + aa, d);
    float v = (k - 0.5) * 2.0;
    v = sign(v) * pow(abs(v), 1.4);
    return lerp(-0.3, v, inG);
}

// 무늬 5: 줄눈이 이어지는 정사각 판입니다. x는 줄눈까지 거리, y는 판마다 다른 값입니다.
float2 Chapter3PaveMarble(float2 q)
{
    float2 p = q / 2.6;
    float2 n = floor(p);
    float2 f = frac(p);
    return float2(min(min(f.x, 1.0 - f.x), min(f.y, 1.0 - f.y)) * 2.6, 0.35 + Hash12(n) * 0.4);
}

// worldPos·worldNormal = Unity 월드. masks = (길, 능선, 단단한 땅, 판석). tint = 정점 AO
float3 Chapter3SandColor(float3 worldPos, float3 worldNormal, float4 masks, float3 tint, float shadow)
{
    float3 normalDirection = normalize(MapDirection(worldNormal));
    float3 viewDirection = normalize(MapDirection(_WorldSpaceCameraPos - worldPos));
    float3 lightDirection = normalize(MapDirection(_WorldSpaceLightPos0.xyz));
    float dist = length(_WorldSpaceCameraPos - worldPos);
    float3 wp = MapPosition(worldPos);
    float vPath = masks.x;
    float vCrest = masks.y;
    float vHard = masks.z;
    float vPave = masks.w;
    float pixG = max(fwidth(wp.x), fwidth(wp.z));

    float ndl = dot(normalDirection, lightDirection);
    float sh = lerp(1.0, shadow, _CastShadowStrength);
    float lf = ndl * 0.5 + 0.5;
    float lfS = lf * lerp(0.35, 1.0, sh);

    float lit = smoothstep(_RampThreshold - _RampSoft, _RampThreshold + _RampSoft, lfS);
    float mk = (lfS - _RampThreshold) / max(_MidWidth * 1.6, 1e-3);
    float midBand = exp(-mk * mk);
    float deep = smoothstep(0.42, 0.1, lfS) * 0.6 + (1.0 - sh) * 0.15;
    float litGrad = lerp(1.0, smoothstep(0.52, 0.85, lfS), _LitGrad);
    float3 litCol = lerp(_SandMid, _SandLit, litGrad);
    float3 sand = lerp(_SandShadow, litCol, lit);
    sand = lerp(sand, _SandMid * float3(1.06, 0.92, 0.85), midBand * _MidStrength * 2.0 * (1.0 - vPath));
    sand = lerp(sand, _SandDeep, saturate(deep) * 0.55);
    sand = lerp(sand, sand * 1.18 + _GlintColor * 0.08, vCrest * _Crest * lit);
    float3 lampEarly = Chapter2LampLight(worldPos, normalize(worldNormal));

    if (_GrainTex > 0.0)
    {
        float glv = max(0.0, log2(pixG * 2.0 / _GrainSize));
        float gl0 = floor(glv);
        float glt = smoothstep(0.2, 0.8, frac(glv));
        float gs0 = _GrainSize * exp2(gl0);
        float g1 = lerp(Chapter3FineGrain(wp.xz, gs0, 1.0), Chapter3FineGrain(wp.xz, gs0 * 2.0, 1.0), glt);
        float g2 = lerp(Chapter3FineGrain(wp.xz, gs0 * 2.6, 7.0), Chapter3FineGrain(wp.xz, gs0 * 5.2, 7.0), glt);
        sand *= 1.0 + (g1 * 0.8 + g2 * 0.12) * _GrainTex * lerp(1.0, 0.6, vPath);
    }
    float3 path = _PathColor * lerp(_SandShadow / max(_SandLit, 0.01), 1.0, lit * 0.85 + 0.15);
    path *= lerp(0.9, 1.05, FractalNoise(wp.xz * 1.6) * 0.5 + 0.5);
    sand = lerp(sand, path, vPath);
    if (vHard > 0.001)
    {
        float3 hardCol = lerp(_HardShadow, _HardLit, lerp(lit, 1.0, 0.15)) *
            lerp(0.95, 1.04, FractalNoise(wp.xz * 0.7) * 0.5 + 0.5);
        float steep = smoothstep(0.9, 0.55, normalDirection.y);
        float st = frac(wp.y * _Strata + FractalNoise(wp.xz * 0.12) * 0.7);
        float stLine = _Strata > 0.0 ? smoothstep(0.0, 0.1, st) * smoothstep(1.0, 0.8, st) : 1.0;
        hardCol = lerp(hardCol, hardCol * lerp(0.8, 0.95, stLine) * float3(0.96, 0.92, 0.98), steep);
        sand = lerp(sand, lerp(hardCol, path, vPath * 0.6), vHard);
    }
    if (vPave > 0.001)
    {
        float2 pp = wp.xz / _PaveSize;
        float2 vc;
        if (_PaveStyle < 0.5)
        {
            pp += float2(SimplexNoise(pp * 0.6), SimplexNoise(pp * 0.6 + 7.3)) * 0.18;
            vc = PaveCell(pp);
        }
        else
        {
            // 판석 무늬를 정원 축(u·v)에 맞춥니다.
            vc = Chapter3PaveMarble(float2(pp.x - pp.y, -pp.x - pp.y) * 0.7071);
        }
        float pxp = pixG / _PaveSize;
        float detail = 1.0 - smoothstep(0.05, 0.16, pxp);
        float crack = 0.045;
        float edge = smoothstep(crack, crack + 0.03 + pxp * 1.5, vc.x);
        float3 stoneLit = lerp(_PaveB, _PaveA, vc.y) * lerp(0.95, 1.04, SimplexNoise(wp.xz * 2.1) * 0.5 + 0.5);
        float3 stone = stoneLit * lerp(_SandShadow / max(_SandLit, 0.01), 1.0, lit * 0.85 + 0.15);
        stone *= lerp(0.86, 1.0, smoothstep(crack, crack + 0.16, vc.x) * detail + (1.0 - detail));
        float cov = smoothstep(0.5, 0.78, FractalNoise(wp.xz * 0.26) * 0.5 + 0.5 + (1.0 - tint.r) * 1.2) * _PaveSand;
        float fill = saturate(cov + (1.0 - edge) * detail * 0.9);
        float3 paveCol = lerp(stone, path, lerp(fill, cov * 0.6 + 0.2 * _PaveSand, 1.0 - detail));
        if (_PaveClean > 0.5)
        {
            // 연마한 판석: 얼룩·깎인 가장자리·틈 모래 없이 단색 판 + 가늘고 또렷한 줄눈 (화면 픽셀 크기로 다듬음)
            float aa = max(pixG * 1.1, 0.003);
            float jointLine = (1.0 - smoothstep(_PaveJoint, _PaveJoint + aa, vc.x)) *
                clamp(_PaveJoint * 2.4 / aa, 0.35, 1.0);
            float3 shadeRatio = lerp(_SandShadow / max(_SandLit, 0.01), 1.0, lit * 0.85 + 0.15);
            paveCol = lerp(lerp(_PaveB, _PaveA, vc.y), _PaveJointColor, jointLine) * shadeRatio;
        }
        sand = lerp(sand, paveCol, vPave);
    }
    float under = 0.0;
    float wDepth = 0.0;
    if (_Pond > 0.5)
    {
        wDepth = _WaterY - wp.y;
        under = smoothstep(-0.02, 0.04, wDepth);
        float wet = (1.0 - smoothstep(0.0, 0.28, -wDepth)) * (1.0 - under);
        sand *= 1.0 - wet * 0.22;
        sand = lerp(sand, sand * float3(0.62, 1.0, 0.96) * 1.05, under * smoothstep(0.0, 0.5, wDepth));
        sand = lerp(sand, float3(0.1, 0.48, 0.56), under * smoothstep(0.4, 1.5, wDepth) * 0.45);
    }

    float3 col = sand * _LightTint * lerp(0.62, 1.0, lit);
    col += sand * _Ambient * _AmbientStrength;
    col *= tint;

    float3 halfDirection = normalize(lightDirection + viewDirection);
    float sheen = pow(max(dot(normalDirection, halfDirection), 0.0), 6.0);
    float grazing = pow(1.0 - max(dot(normalDirection, viewDirection), 0.0), 3.0);
    float sway = SimplexNoise(wp.xz * 0.025 + viewDirection.xz * 1.2) * 0.5 + 0.5;
    float glow = max(_GlowBase * (0.75 + sway * 0.5), sheen * 0.5 + grazing * 0.4);
    glow *= lit * sh * (1.0 + vCrest * _Crest) * (1.0 - vPath * 0.6) * (1.0 - vPave * 0.55) * (1.0 - vHard * 0.75);
    float gAmt = glow * _GoldGlow;
    gAmt = _GlowCap * (1.0 - exp(-gAmt / _GlowCap));
    col += _GlintColor * gAmt * (1.0 - under);

    float rim = pow(1.0 - max(dot(normalDirection, viewDirection), 0.0), 4.0) * _Rim * 0.35;
    col += _RimColor * rim * lit;

    float3 lamp = lampEarly * (1.0 - under * _Chapter2LampSoft * 0.88);
    col += sand * lamp;
    col += _GlintColor * lamp * pow(max(dot(normalDirection, normalize(viewDirection + float3(0.0, 1.0, 0.0))),
        0.0), 8.0)
        * 0.4 * _Night;

    float fog = smoothstep(_FogNear, _FogFar, dist);
    fog = max(fog, (1.0 - smoothstep(0.0, 6.0, wp.y + 2.0)) * _FogHeight * smoothstep(_FogNear * 0.5, _FogFar, dist));
    return lerp(col, _FogColor, saturate(fog));
}

#endif
