Shader "Espoir/Environment/Sand"
{
    // 정점 채널: color.r = 접촉 AO, uv1 = (pathMask, crest), uv2 = (hard, pave)
    Properties
    {
        _MapOrigin ("Map Origin", Vector) = (0, 0, 0, 0)
        _MapAxisX ("Map X Axis", Vector) = (1, 0, 0, 0)
        _MapAxisZ ("Map Z Axis", Vector) = (0, 0, 1, 0)

        [Header(Sand)]
        _SandLit ("Sand Lit", Color) = (0.957, 0.816, 0.604, 1)
        _SandMid ("Sand Mid", Color) = (0.878, 0.682, 0.494, 1)
        _SandShadow ("Sand Shadow", Color) = (0.659, 0.565, 0.62, 1)
        _SandDeep ("Sand Deep", Color) = (0.494, 0.486, 0.643, 1)
        _PathColor ("Path Color", Color) = (0.886, 0.776, 0.627, 1)
        _Crest ("Crest Highlight", Float) = 1
        _LitGrad ("Lit Gradient", Float) = 1
        _GrainTex ("Fine Grain", Float) = 0.16
        _GrainSize ("Grain Size", Float) = 0.025

        [Header(Glint and Glow)]
        _GlintColor ("Glint Color", Color) = (1, 0.941, 0.722, 1)
        _Glint ("Glint", Float) = 1.3
        _GlintSize ("Glint Size", Float) = 0.065
        _GlintDensity ("Glint Density", Float) = 0.32
        _GoldGlow ("Gold Glow", Float) = 0.7
        _GlowBase ("Gold Glow Base", Float) = 0.18
        _GlowCap ("Glow Cap", Float) = 99

        [Header(Hard Ground and Pave)]
        _HardLit ("Hard Lit", Color) = (0.824, 0.675, 0.525, 1)
        _HardShadow ("Hard Shadow", Color) = (0.549, 0.439, 0.439, 1)
        _Strata ("Strata", Float) = 1.3
        _PaveA ("Pave A", Color) = (0.863, 0.761, 0.635, 1)
        _PaveB ("Pave B", Color) = (0.737, 0.627, 0.518, 1)
        _PaveSize ("Pave Size", Float) = 0.9
        _PaveSand ("Pave Sand", Float) = 0.6

        [Header(Water Edge)]
        _Pond ("Lake Shading On", Float) = 1
        _WaterY ("Water Height (map)", Float) = -0.72

        [Header(Lighting)]
        _LightTint ("Light Color", Color) = (1, 0.902, 0.776, 1)
        _Ambient ("Ambient Color", Color) = (0.627, 0.69, 0.878, 1)
        _AmbientStrength ("Ambient Strength", Float) = 0.45
        _RampThreshold ("Ramp Threshold", Float) = 0.5
        _RampSoft ("Ramp Softness", Float) = 0.035
        _MidWidth ("Mid Band Width", Float) = 0.04
        _MidStrength ("Mid Band Strength", Float) = 0.35
        _CastShadowStrength ("Receive Cast Shadow", Float) = 1
        _Rim ("Rim", Float) = 0.7
        _RimColor ("Rim Color", Color) = (1, 0.941, 0.863, 1)

        [Header(Fog and Map)]
        _FogColor ("Fog Color", Color) = (0.918, 0.855, 0.8, 1)
        _FogNear ("Fog Near", Float) = 100
        _FogFar ("Fog Far", Float) = 360
        _FogHeight ("Fog Height", Float) = 0.22
        _MapHeight ("Map Root Height", Float) = 0
        _LampPos ("Lamp Position (w = radius)", Vector) = (0, -100, 0, 0)
        _LampColor ("Lamp Color", Color) = (0, 0, 0, 1)
        _LampFlicker ("Lamp Flicker", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fwdbase
            #pragma target 3.5
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include "EnvironmentCommon.cginc"

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

            struct SVertexInput
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
            };

            struct SVertexOutput
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float4 masks : TEXCOORD2;   // path, crest, hard, pave
                float ao : TEXCOORD3;
                SHADOW_COORDS(4)
            };

            SVertexOutput Vert(SVertexInput v)
            {
                SVertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.masks = float4(v.uv1, v.uv2);
                o.ao = v.color.r;
                TRANSFER_SHADOW(o);
                return o;
            }

            // 고운 모래알: 셀마다 밝기가 제각각인 작은 알갱이 + 알갱이 사이 틈
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

            // 월드 격자마다 무작위로 기운 모래알 면이 해를 반사하면 별처럼 반짝
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

            float4 Frag(SVertexOutput i) : SV_Target
            {
                float3 normalDirection = normalize(MapDirection(i.worldNormal));
                float3 viewDirection = normalize(MapDirection(_WorldSpaceCameraPos - i.worldPos));
                float3 lightDirection = normalize(MapDirection(_WorldSpaceLightPos0.xyz));
                float dist = length(_WorldSpaceCameraPos - i.worldPos);
                float3 wp = MapPosition(i.worldPos);
                float mapY = wp.y;
                float vPath = i.masks.x;
                float vCrest = i.masks.y;
                float vHard = i.masks.z;
                float vPave = i.masks.w;
                float pixG = max(fwidth(wp.x), fwidth(wp.z));

                float ndl = dot(normalDirection, lightDirection);
                float sh = lerp(1.0, SHADOW_ATTENUATION(i), _CastShadowStrength);
                float lf = ndl * 0.5 + 0.5;
                float lfS = lf * lerp(0.35, 1.0, sh);

                // 모래 색 램프: 그늘(보라빛) → 경계 띠 → 중간 금 → 밝은 금, 골짜기는 더 깊은 보라
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
                float3 lampEarly = LampLight(i.worldPos, normalize(i.worldNormal));

                // 고운 모래알 결: 화면에서 한 알이 약 2픽셀 아래로 작아지면 다음 단계로 넘어가며 섞음
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
                UNITY_BRANCH
                if (vPath != 0.0 || vPave > 0.001)
                {
                    path *= lerp(0.9, 1.05, FractalNoise(wp.xz * 1.6) * 0.5 + 0.5);
                }
                sand = lerp(sand, path, vPath);
                if (vHard > 0.001)
                {
                    float3 hardCol = lerp(_HardShadow, _HardLit, lerp(lit, 1.0, 0.15)) * lerp(0.95, 1.04,
                        FractalNoise(wp.xz * 0.7) * 0.5 + 0.5);
                    float steep = smoothstep(0.9, 0.55, normalDirection.y);
                    float stLine = 1.0;
                    UNITY_BRANCH
                    if (_Strata > 0.0)
                    {
                        float st = frac(mapY * _Strata + FractalNoise(wp.xz * 0.12) * 0.7);
                        stLine = smoothstep(0.0, 0.1, st) * smoothstep(1.0, 0.8, st);
                    }
                    hardCol = lerp(hardCol, hardCol * lerp(0.8, 0.95, stLine) * float3(0.96, 0.92, 0.98), steep);
                    sand = lerp(sand, lerp(hardCol, path, vPath * 0.6), vHard);
                }
                // 돌바닥: 판석마다 색이 조금씩 다르고 가장자리는 어둡게, 틈과 벽 밑에는 모래
                if (vPave > 0.001)
                {
                    float2 pp = wp.xz / _PaveSize;
                    pp += float2(SimplexNoise(pp * 0.6), SimplexNoise(pp * 0.6 + 7.3)) * 0.18;
                    float2 vc = PaveCell(pp);
                    float pxp = pixG / _PaveSize;
                    float detail = 1.0 - smoothstep(0.05, 0.16, pxp);
                    float crack = 0.045;
                    float edge = smoothstep(crack, crack + 0.03 + pxp * 1.5, vc.x);
                    float3 stoneLit = lerp(_PaveB, _PaveA, vc.y)
                        * lerp(0.95, 1.04, SimplexNoise(wp.xz * 2.1) * 0.5 + 0.5);
                    float3 stone = stoneLit * lerp(_SandShadow / max(_SandLit, 0.01), 1.0, lit * 0.85 + 0.15);
                    stone *= lerp(0.86, 1.0, smoothstep(crack, crack + 0.16, vc.x) * detail + (1.0 - detail));
                    float cov = smoothstep(0.5, 0.78,
                        FractalNoise(wp.xz * 0.26) * 0.5 + 0.5 + (1.0 - i.ao) * 1.2) * _PaveSand;
                    float fill = saturate(cov + (1.0 - edge) * detail * 0.9);
                    float3 paveCol = lerp(stone, path, lerp(fill, cov * 0.6 + 0.2 * _PaveSand, 1.0 - detail));
                    sand = lerp(sand, paveCol, vPave);
                }
                float under = 0.0;
                float wDepth = 0.0;
                if (_Pond > 0.5)
                {
                    wDepth = _WaterY - mapY;
                    under = smoothstep(-0.02, 0.04, wDepth);
                    float wet = (1.0 - smoothstep(0.0, 0.28, -wDepth)) * (1.0 - under);
                    sand *= 1.0 - wet * 0.22;
                    sand = lerp(sand, sand * float3(0.62, 1.0, 0.96) * 1.05, under * smoothstep(0.0, 0.5, wDepth));
                    sand = lerp(sand, float3(0.1, 0.48, 0.56), under * smoothstep(0.4, 1.5, wDepth) * 0.45);
                }

                float3 col = sand * _LightTint * lerp(0.62, 1.0, lit);
                col += sand * _Ambient * _AmbientStrength;
                col *= i.ao;

                // 금빛 발광 (바탕 금빛과 반사 하이라이트 중 큰 쪽)
                float3 halfDirection = normalize(lightDirection + viewDirection);
                float sheen = pow(max(dot(normalDirection, halfDirection), 0.0), 6.0);
                float grazing = pow(1.0 - max(dot(normalDirection, viewDirection), 0.0), 3.0);
                float sway = SimplexNoise(wp.xz * 0.025 + viewDirection.xz * 1.2) * 0.5 + 0.5;
                float glow = max(_GlowBase * (0.75 + sway * 0.5), sheen * 0.5 + grazing * 0.4);
                glow *= lit * sh * (1.0 + vCrest * _Crest) * (1.0 - vPath * 0.6) * (1.0 - vPave * 0.55) *
                    (1.0 - vHard * 0.75);
                float gAmt = glow * _GoldGlow;
                gAmt = _GlowCap * (1.0 - exp(-gAmt / _GlowCap));
                col += _GlintColor * gAmt * (1.0 - under);

                // 반짝임: 화면 크기에 맞춰 격자 단계를 고름
                if (_Glint > 0.0)
                {
                    float lvl = max(0.0, ceil(log2(pixG * 2.6 / _GlintSize)));
                    float size = _GlintSize * exp2(lvl);
                    float g = GlintLayer(wp.xz, size, normalDirection, halfDirection, 0.0);
                    UNITY_BRANCH
                    if (lvl < 1.0)
                    {
                        g += GlintLayer(wp.xz, size * 0.5, normalDirection, halfDirection, 31.7) * 0.6
                            * (1.0 - smoothstep(0.0, 1.0, lvl));
                    }
                    float mask = sh * smoothstep(0.35, 0.6,
                        lf) * (1.0 - vPath * 0.6) * (1.0 - vHard * 0.7) * (1.0 - vPave * 0.6) *
                                 (1.0 - smoothstep(_FogNear, _FogFar, dist));
                    col += lerp(_GlintColor, float3(1.0, 0.97, 0.85), 0.25) * g * mask * 5.0 * _Glint;
                }
                if (_Pond > 0.5 && under > 0.0)
                {
                    float ca = Caustic(wp.xz * 1.4, _Time.y);
                    col += float3(0.85, 1.0, 0.9) * _LightTint * ca * under * 0.32 * smoothstep(0.0, 0.25,
                        wDepth) * (1.0 - smoothstep(1.0, 1.8, wDepth));
                }
                float rim = pow(1.0 - max(dot(normalDirection, viewDirection), 0.0), 4.0) * _Rim * 0.35;
                col += _RimColor * rim * lit;
                col += sand * lampEarly;

                float fog = smoothstep(_FogNear, _FogFar, dist);
                fog = max(fog, (1.0 - smoothstep(0.0, 6.0,
                    mapY + 2.0)) * _FogHeight * smoothstep(_FogNear * 0.5, _FogFar, dist));
                col = lerp(col, _FogColor, saturate(fog));
                return float4(col, 1.0);
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
