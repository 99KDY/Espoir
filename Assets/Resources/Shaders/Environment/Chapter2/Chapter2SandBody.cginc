#ifndef ESPOIR_CHAPTER2_SAND_BODY_INCLUDED
#define ESPOIR_CHAPTER2_SAND_BODY_INCLUDED

// 바닥과 풀잎(Chapter2Grass)이 같은 식을 쓴다 — 풀잎 뿌리색이 그 자리 바닥색과 같아야 틈이 안 보인다.
#include "Chapter2Common.cginc"

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
float _Pond;
float _WaterY;
float _Night;

float FineGrain(float2 wp, float size, float seed)
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

float Caustic(float2 p, float t)
{
    float2 q = p + float2(SimplexNoise(p * 0.5 + t * 0.25), SimplexNoise(p * 0.5 - t * 0.2 + 7.0)) * 0.7;
    float a = 1.0 - smoothstep(0.0, 0.06, abs(SimplexNoise(q * 1.1 + t * 0.18)));
    float b = 1.0 - smoothstep(0.0, 0.05, abs(SimplexNoise(q * 1.8 - t * 0.14 + 3.0)));
    return max(a * 0.85, b * 0.6);
}

// 월드 격자마다 무작위로 기운 모래알 면이 빛을 반사하면 별처럼 반짝
float GlintLayer(float2 wp, float size, float3 normalDirection, float3 halfDirection, float seed)
{
    float2 cell = floor(wp / size);
    float2 f = frac(wp / size);
    float sum = 0.0;
    for (int j = -1; j <= 1; j++)
    {
        for (int i = -1; i <= 1; i++)
        {
            float2 c = cell + float2(i, j);
            float2 rnd = Hash22(c + seed);
            if (Hash12(c + 7.3 + seed) > _GlintDensity) continue;
            float2 ctr = float2(i, j) + 0.2 + rnd * 0.6;
            float2 dd = f - ctr;
            float d = length(dd);
            float3 gn = normalize(normalDirection + float3(rnd.x - 0.5, 0.0, rnd.y - 0.5) * 1.9);
            float spec = smoothstep(0.9, 0.97, dot(gn, halfDirection));
            float tw = 0.6 + 0.4 * sin(_Time.y * (1.5 + rnd.x * 3.0) + rnd.y * 40.0);
            float core = smoothstep(0.22, 0.0, d);
            float crs = smoothstep(0.05, 0.0, abs(dd.x) * abs(dd.y) * 8.0) * smoothstep(0.55, 0.0, d);
            sum += spec * (core + crs * 0.7) * tw;
        }
    }
    return sum;
}

// worldPos·worldNormal = Unity 월드. masks = (길, 능선, 단단한 땅, 판석). tint = 정점 AO(바닥) 또는 풀잎 색(풀)
// isBlade = 풀잎 (풀 아래 바닥 가라앉힘은 바닥에서만)
float3 Chapter2SandColor(float3 worldPos, float3 worldNormal, float4 masks, float3 tint, float shadow, bool isBlade)
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

    // 모래 색 램프: 그늘 → 경계 띠 → 중간 → 밝은 면, 골짜기는 더 깊은 그늘색
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
    // 풀 덮개의 바닥 쪽: 풀잎 아래는 뿌리색으로 가라앉고, 먼 곳은 풀밭 평균색 + 바람결
    if (!isBlade && _Chapter2GrassCenter.z > 0.0)
    {
        float gDet = Chapter2GrassNear(worldPos) * lerp(1.0, _Chapter2GrassLod.z,
            smoothstep(_Chapter2GrassLod.x, _Chapter2GrassLod.y, dist));
        sand *= lerp(1.0 + _Chapter2GrassWave * Chapter2GrassWave(wp.xz, _Time.y), _Chapter2GrassUnder, gDet);
    }
    float3 lampEarly = Chapter2LampLight(worldPos, normalize(worldNormal));

    if (_GrainTex > 0.0)
    {
        float glv = max(0.0, log2(pixG * 2.0 / _GrainSize));
        float gl0 = floor(glv);
        float glt = smoothstep(0.2, 0.8, frac(glv));
        float gs0 = _GrainSize * exp2(gl0);
        float g1 = lerp(FineGrain(wp.xz, gs0, 1.0), FineGrain(wp.xz, gs0 * 2.0, 1.0), glt);
        float g2 = lerp(FineGrain(wp.xz, gs0 * 2.6, 7.0), FineGrain(wp.xz, gs0 * 5.2, 7.0), glt);
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
    // 돌바닥: 판석마다 색이 조금씩 다르고, 가장자리는 깎인 듯 어둡게, 틈에는 흙
    if (vPave > 0.001)
    {
        float2 pp = wp.xz / _PaveSize;
        pp += float2(SimplexNoise(pp * 0.6), SimplexNoise(pp * 0.6 + 7.3)) * 0.18;
        float2 vc = PaveCell(pp);
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

    // 반짝임: 화면 크기에 맞춰 격자 단계를 고름
    if (_Glint > 0.0)
    {
        float lvl = max(0.0, ceil(log2(pixG * 2.6 / _GlintSize)));
        float size = _GlintSize * exp2(lvl);
        float g = GlintLayer(wp.xz, size, normalDirection, halfDirection, 0.0);
        g += GlintLayer(wp.xz, size * 0.5, normalDirection, halfDirection, 31.7) * 0.6 *
            (1.0 - smoothstep(0.0, 1.0, lvl));
        float mask = sh * smoothstep(0.35, 0.6, lf) * (1.0 - vPath * 0.6) * (1.0 - vHard * 0.7) * (1.0 - vPave * 0.6) *
            (1.0 - smoothstep(_FogNear, _FogFar, dist));
        col += lerp(_GlintColor, float3(1.0, 0.97, 0.85), 0.25) * g * mask * 5.0 * _Glint * (1.0 - _Night * 0.7);
        // 밤: 등불 쪽을 향한 모래알이 금빛으로 반짝
        if (_Night > 0.0)
        {
            float3 lampDirection;
            float li = Chapter2LampDominant(worldPos, lampDirection);
            if (li > 0.005)
            {
                float3 halfLamp = normalize(MapDirection(lampDirection) + viewDirection);
                float g2 = GlintLayer(wp.xz, size, normalDirection, halfLamp, 57.0) +
                    GlintLayer(wp.xz, size * 0.5, normalDirection, halfLamp, 91.0) * 0.6;
                col += float3(1.0, 0.78, 0.4) * g2 * min(li, 1.2) * 7.0 * _Night * (1.0 - vPath * 0.4);
            }
        }
    }
    if (_Pond > 0.5 && under > 0.0)
    {
        float ca = Caustic(wp.xz * 1.4, _Time.y);
        col += float3(0.85, 1.0, 0.9) * _LightTint * ca * under * 0.32 * smoothstep(0.0, 0.25, wDepth) *
            (1.0 - smoothstep(1.0, 1.8, wDepth));
    }
    float rim = pow(1.0 - max(dot(normalDirection, viewDirection), 0.0), 4.0) * _Rim * 0.35;
    if (_Chapter2GrassCenter.z > 0.0) rim *= lerp(1.0, _Chapter2GrassRim, 1.0 - vPave);
    col += _RimColor * rim * lit;

    // 등불 (물속 바닥은 거의 받지 않음 — 수로가 뿌옇게 뜨지 않게)
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
