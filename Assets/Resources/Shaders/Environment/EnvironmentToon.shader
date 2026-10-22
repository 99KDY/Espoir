Shader "Espoir/Environment/Toon"
{
    Properties
    {
        _MapOrigin ("Map Origin", Vector) = (0, 0, 0, 0)
        _MapAxisX ("Map X Axis", Vector) = (1, 0, 0, 0)
        _MapAxisZ ("Map Z Axis", Vector) = (0, 0, 1, 0)

        [Header(Surface)]
        _BaseColor ("Base Color", Color) = (0.91, 0.788, 0.627, 1)
        _BaseColor2 ("Base Color 2", Color) = (0.851, 0.706, 0.541, 1)
        // 0 Plaster, 1 Stripes, 2 Wood, 3 Tile, 4 Stone, 5 Plain, 6 Leaf, 7 Metal
        // 8 Strata, 9 Earth, 10 Foliage, 11 Rock, 12 Flag, 13 WoodClean, 14 Cloth
        [IntRange] _Pattern ("Pattern", Range(0, 14)) = 5
        _PatternScale ("Pattern Scale", Float) = 1
        _Emissive ("Emissive", Float) = 0
        _Gloss ("Gloss", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2

        [Header(Pave)]
        _PaveA ("Pave A", Color) = (0.851, 0.706, 0.549, 1)
        _PaveB ("Pave B", Color) = (0.722, 0.565, 0.439, 1)
        _PathColor ("Path Color", Color) = (0.788, 0.604, 0.384, 1)
        _PaveSize ("Pave Size", Float) = 0.9

        [Header(Lighting)]
        _LightTint ("Light Color", Color) = (1, 0.96, 0.88, 1)
        _Ambient ("Ambient Color", Color) = (0.55, 0.62, 0.8, 1)
        _AmbientStrength ("Ambient Strength", Float) = 0.18
        _ShadowTint ("Shadow Tint", Color) = (0.62, 0.5, 0.68, 1)
        _MidTint ("Mid Band Tint", Color) = (1, 0.75, 0.6, 1)
        _RampThreshold ("Ramp Threshold", Float) = 0.5
        _RampSoft ("Ramp Softness", Float) = 0.03
        _MidWidth ("Mid Band Width", Float) = 0.04
        _MidStrength ("Mid Band Strength", Float) = 0.25
        _CastShadowStrength ("Receive Cast Shadow", Float) = 1
        _Rim ("Rim", Float) = 0.35
        _RimColor ("Rim Color", Color) = (1, 0.92, 0.75, 1)

        [Header(Fog and Map)]
        _FogColor ("Fog Color", Color) = (0.95, 0.8, 0.6, 1)
        _FogNear ("Fog Near", Float) = 40
        _FogFar ("Fog Far", Float) = 140
        _MapHeight ("Map Root Height", Float) = 0
        _LampPos ("Lamp Position (w = radius)", Vector) = (0, -100, 0, 0)
        _LampColor ("Lamp Color", Color) = (0, 0, 0, 1)
        _LampFlicker ("Lamp Flicker", Float) = 0
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "EnvironmentCommon.cginc"

    float3 _BaseColor;
    float3 _BaseColor2;
    float _Pattern;
    float _PatternScale;
    float _Emissive;
    float _Gloss;
    float3 _PaveA;
    float3 _PaveB;
    float3 _PathColor;
    float _PaveSize;

    // 월드 위치를 기준으로 잎의 흔들림을 계산한다.
    float3 WindOffset(float3 worldPos, float3 worldNormal, float2 uv)
    {
        float t = _Time.y;
        int pattern = (int)round(_Pattern);
        if (pattern == 10)
        {
            return float3(sin(t * 1.2 + worldPos.x * 0.8 + worldPos.y * 1.3), 0.4 * sin(t * 1.7 + worldPos.z),
                          cos(t * 1.0 + worldPos.z * 0.7 + worldPos.y)) * 0.03;
        }
        if (pattern == 14)
        {
            float wave = sin(t * 3.4 - uv.x * 5.5 + uv.y * 1.2) * 0.75
                + sin(t * 5.1 - uv.x * 9.0) * 0.25;
            return worldNormal * wave * 0.26 * uv.x + float3(0.0, -0.06 * uv.x * uv.x, 0.0);
        }
        if (pattern == 6)
        {
            float k = uv.x * uv.x;
            return float3(sin(t * 1.3 + worldPos.x * 0.4), 0.6 * sin(t * 1.7 + worldPos.z * 0.5),
                          cos(t * 1.1 + worldPos.z * 0.3)) * 0.07 * k;
        }
        return 0.0;
    }
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull [_Cull]

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing
            #pragma target 3.0
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct SVertexInput
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct SVertexOutput
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float ao : TEXCOORD3;
                SHADOW_COORDS(4)
            };

            SVertexOutput Vert(SVertexInput v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                SVertexOutput o;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                wp += WindOffset(wp, normalize(UnityObjectToWorldNormal(v.normal)), v.uv);
                v.vertex = mul(unity_WorldToObject, float4(wp, 1.0));
                o.pos = UnityWorldToClipPos(wp);
                o.worldPos = wp;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                o.ao = v.color.r;
                TRANSFER_SHADOW(o);
                return o;
            }

            float3 SurfaceColor(float3 p, float3 normalDirection, float2 uv)
            {
                float3 an = abs(normalDirection);
                float2 tp = an.y > max(an.x, an.z) ? p.xz : (an.x > an.z ? p.zy : p.xy);
                int pattern = (int)round(_Pattern);
                float3 result;
                if (pattern == 0)
                {
                    float m = FractalNoise(tp * 0.45 * _PatternScale) * 0.5 + 0.5;
                    float fine = SimplexNoise(tp * 7.0 * _PatternScale) * 0.5 + 0.5;
                    float3 c = lerp(_BaseColor2, _BaseColor, 0.5 + 0.5 * smoothstep(0.3, 0.7, m));
                    c *= lerp(0.94, 1.03, fine);
                    float dirt = 1.0 - smoothstep(0.0, 0.9, p.y + FractalNoise(tp * 2.0) * 0.25);
                    result = lerp(c, c * float3(0.86, 0.74, 0.62), dirt * 0.6);
                }
                else if (pattern == 1)
                {
                    float s = step(0.5, frac(uv.x * _PatternScale));
                    float thin = step(0.92, frac(uv.x * _PatternScale * 2.0 + 0.25));
                    float3 c = lerp(_BaseColor, _BaseColor2, s);
                    c = lerp(c, float3(1.0, 0.92, 0.75), thin * 0.6);
                    result = c * lerp(0.93, 1.03, SimplexNoise(uv * 40.0) * 0.5 + 0.5);
                }
                else if (pattern == 2)
                {
                    float plank = frac(uv.y * _PatternScale);
                    float seam = smoothstep(0.0, 0.05, plank) * smoothstep(1.0, 0.95, plank);
                    float grain = SimplexNoise(float2(uv.x * 3.0,
                        uv.y * _PatternScale * 8.0 + floor(uv.y * _PatternScale) * 3.7)) * 0.5 + 0.5;
                    float3 c = lerp(_BaseColor2, _BaseColor,
                        grain * 0.6 + 0.4 * Hash12(floor(float2(uv.y * _PatternScale, uv.y * _PatternScale))));
                    result = c * lerp(0.6, 1.0, seam);
                }
                else if (pattern == 3)
                {
                    float2 g = uv * _PatternScale;
                    float2 cell = floor(g);
                    float2 f = frac(g);
                    float edge = smoothstep(0.0, 0.08, f.x) * smoothstep(1.0, 0.92, f.x) * smoothstep(0.0,
                        0.08, f.y) * smoothstep(1.0, 0.92, f.y);
                    float3 c = lerp(_BaseColor, _BaseColor2, Hash12(cell) * 0.7);
                    result = lerp(c * 0.55, c, edge);
                }
                else if (pattern == 4)
                {
                    float2 g = tp * float2(1.6, 3.2) * _PatternScale;
                    g.x += step(1.0, PositiveModulo(floor(g.y), 2.0)) * 0.5;
                    float2 f = frac(g);
                    float mortar = smoothstep(0.0, 0.07, f.x) * smoothstep(1.0, 0.93, f.x) * smoothstep(0.0,
                        0.1, f.y) * smoothstep(1.0, 0.9, f.y);
                    float3 c = lerp(_BaseColor2, _BaseColor, Hash12(floor(g)) * 0.6 + 0.4);
                    result = lerp(c * 0.62, c, mortar);
                }
                else if (pattern == 8)
                {
                    float band = sin(p.y * 6.0 * _PatternScale + FractalNoise(p.xz * 0.7) * 1.2) * 0.5 + 0.5;
                    float3 c = lerp(_BaseColor2, _BaseColor, 0.45 + 0.55 * smoothstep(0.3, 0.7, band));
                    result = c * lerp(0.93, 1.04, SimplexNoise(tp * 5.0) * 0.5 + 0.5);
                }
                else if (pattern == 9)
                {
                    float rowY = p.y / 0.62 + FractalNoise(tp * 0.25) * 0.06;
                    float row = frac(rowY);
                    float fadeL = 1.0 - smoothstep(0.03, 0.08, fwidth(rowY) * 0.62);
                    float seam = lerp(1.0, smoothstep(0.0, 0.05, row) * smoothstep(1.0, 0.95, row), fadeL);
                    float3 c = lerp(_BaseColor2, _BaseColor, 0.5 + 0.5 * smoothstep(0.25, 0.75,
                        FractalNoise(tp * 0.3 * _PatternScale) * 0.5 + 0.5));
                    float patchM = smoothstep(0.47, 0.5, FractalNoise(tp * 0.42 + 7.0) * 0.5 + 0.5);
                    c = lerp(c, c * 1.06 + 0.015, patchM * 0.85);
                    c *= lerp(0.92, 1.0, seam);
                    float2 hp = float2(tp.x / 1.3, rowY + 0.5);
                    float2 hf = frac(hp) - 0.5;
                    float hole = step(abs(hf.x), 0.035) * step(abs(hf.y), 0.06) * step(0.6, Hash12(floor(hp))) * fadeL;
                    result = c * (1.0 - hole * 0.4);
                }
                else if (pattern == 10)
                {
                    float n = SimplexNoise(tp * 0.7 + p.y * 0.3) * 0.5 + 0.5;
                    float3 c = lerp(_BaseColor, _BaseColor2, smoothstep(0.55, 0.8, n) * 0.35);
                    result = c * lerp(0.94, 1.05, Hash12(floor(tp * 7.0)));
                }
                else if (pattern == 11)
                {
                    float m = FractalNoise(p.xz * 0.03 + p.y * 0.015) * 0.5 + 0.5;
                    float3 c = lerp(_BaseColor2, _BaseColor, 0.4 + 0.6 * smoothstep(0.3, 0.7, m));
                    float2 sp = an.y > max(an.x, an.z) ? p.xz : (an.x > an.z ? float2(p.z, p.y) : float2(p.x, p.y));
                    float streak = SimplexNoise(float2(sp.x * 0.7, sp.y * 0.05 * _PatternScale));
                    c *= lerp(1.0, lerp(0.97, 1.01, smoothstep(-0.5, 0.5, streak)), smoothstep(0.55, 0.25,
                        abs(normalDirection.y)));
                    float top = smoothstep(0.5, 0.8, normalDirection.y + SimplexNoise(p.xz * 0.35) * 0.1);
                    result = lerp(c, c * float3(1.1, 1.08, 1.04) + float3(0.03, 0.025, 0.015), top * 0.75);
                }
                else if (pattern == 12)
                {
                    if (normalDirection.y > 0.6)
                    {
                        float2 pp = p.xz / _PaveSize;
                        pp += float2(SimplexNoise(pp * 0.6), SimplexNoise(pp * 0.6 + 7.3)) * 0.18;
                        float2 vc = PaveCell(pp);
                        float3 c = lerp(_PaveB, _PaveA, vc.y) * lerp(0.95, 1.04, SimplexNoise(p.xz * 2.1) * 0.5 + 0.5);
                        c *= lerp(0.86, 1.0, smoothstep(0.045, 0.2, vc.x));
                        result = lerp(_PathColor, c, smoothstep(0.045, 0.075, vc.x));
                    }
                    else result = lerp(_PaveB, _PaveA, 0.3) * lerp(0.9, 0.97, SimplexNoise(tp * 1.3) * 0.5 + 0.5);
                }
                else if (pattern == 13)
                {
                    float pl = uv.x * _PatternScale;
                    float plank = frac(pl);
                    float seam = smoothstep(0.0, 0.035, plank) * smoothstep(1.0, 0.965, plank);
                    float id = Hash12(float2(floor(pl), 3.1));
                    float grain = SimplexNoise(float2(pl * 5.0, uv.y * 1.2 + id * 9.0)) * 0.5 + 0.5;
                    float3 c = lerp(_BaseColor2, _BaseColor, 0.5 + 0.3 * id + 0.2 * grain);
                    result = c * lerp(0.84, 1.0, seam);
                }
                else if (pattern == 6)
                {
                    float vein = smoothstep(0.08, 0.0, abs(uv.y - 0.5));
                    result = lerp(_BaseColor2, _BaseColor, uv.x) + vein * 0.08;
                }
                else
                {
                    result = lerp(_BaseColor, _BaseColor2, FractalNoise(tp * _PatternScale) * 0.5 + 0.5);
                }
                return result;
            }

            float4 Frag(SVertexOutput i, float facing : VFACE) : SV_Target
            {
                float3 normalDirection = normalize(i.worldNormal);
                if (facing < 0.0) normalDirection = -normalDirection;
                float3 viewDirection = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float dist = length(_WorldSpaceCameraPos - i.worldPos);
                float3 p = MapPosition(i.worldPos);
                float3 albedo = SurfaceColor(p, MapDirection(normalDirection), i.uv);
                int pattern = (int)round(_Pattern);

                float ndl = dot(normalDirection, lightDirection);
                float sh = lerp(1.0, SHADOW_ATTENUATION(i), _CastShadowStrength);
                float lf = (ndl * 0.5 + 0.5) * lerp(0.3, 1.0, sh);
                float3 ramp = ToonRamp(lf);
                float ao = i.ao;
                float3 col = albedo * ramp * _LightTint * lerp(0.55, 1.0, ao);
                col += albedo * _Ambient * _AmbientStrength * ao;

                if (_Gloss > 0.0)
                {
                    float3 halfDirection = normalize(lightDirection + viewDirection);
                    float sp = smoothstep(0.975, 0.985, dot(normalDirection, halfDirection)) * sh;
                    col += _LightTint * sp * _Gloss;
                    if (pattern == 7) col += albedo * smoothstep(0.45, 0.5, normalDirection.y * 0.5 + 0.5 +
                        dot(normalDirection, viewDirection) * 0.2) * 0.35;
                }
                if (pattern == 10) col += albedo * _LightTint * pow(max(dot(-viewDirection, lightDirection), 0.0),
                    3.0) * 0.5 * (1.0 - max(ndl, 0.0)) * sh;
                float rim = smoothstep(0.62, 0.66, 1.0 - max(dot(normalDirection, viewDirection), 0.0)) *
                    smoothstep(-0.2, 0.4, ndl) * _Rim;
                col += _RimColor * rim * 0.45 * sh;

                col += albedo * LampLight(i.worldPos, normalDirection);
                col += albedo * _Emissive;
                col = lerp(col, _FogColor, DistanceFog(dist));
                return float4(col, 1.0);
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode"="ShadowCaster" }

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #pragma target 3.0

            struct SVertexInput
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct SVertexOutput
            {
                V2F_SHADOW_CASTER;
            };

            SVertexOutput Vert(SVertexInput v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                SVertexOutput o;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                wp += WindOffset(wp, normalize(UnityObjectToWorldNormal(v.normal)), v.uv);
                v.vertex = mul(unity_WorldToObject, float4(wp, 1.0));
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 Frag(SVertexOutput i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
