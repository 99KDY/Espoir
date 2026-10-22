Shader "Espoir/Environment/Rock"
{
    // 정점 채널: color.r = AO, uv1 = (zone, cav)
    Properties
    {
        [Header(Colors)]
        _Talus ("Talus", Color) = (0.863, 0.722, 0.573, 1)
        _Talus2 ("Talus 2", Color) = (0.784, 0.631, 0.486, 1)
        _Cliff ("Cliff", Color) = (0.886, 0.784, 0.659, 1)
        _Cliff2 ("Cliff 2", Color) = (0.8, 0.682, 0.557, 1)
        _Upper ("Upper", Color) = (0.925, 0.839, 0.729, 1)
        _Upper2 ("Upper 2", Color) = (0.839, 0.737, 0.612, 1)
        _Edge ("Edge", Color) = (0.965, 0.886, 0.753, 1)
        _Shade ("Shade", Color) = (0.541, 0.478, 0.627, 1)
        _Spec ("Spec Color", Color) = (1, 0.957, 0.886, 1)
        _SkyHi ("Sky Reflection", Color) = (0.624, 0.722, 0.886, 1)

        [Header(Style)]
        _Bands ("Bands", Float) = 2
        _Soft ("Softness", Float) = 0.06
        _Grad ("Height Gradient", Float) = 0.85
        _GradTop ("Gradient Top", Float) = 20
        _Blotch ("Blotch", Float) = 0
        _RimAmount ("Rim", Float) = 0.8
        _Cavity ("Cavity", Float) = 0.3
        _ShadowAmount ("Shadow", Float) = 1
        _Thresh ("Threshold", Float) = 0.5
        _Flat ("Flat Faces", Float) = 1
        _FaceJit ("Face Jitter", Float) = 0.16
        _ShadeMix ("Shade Mix", Float) = 0
        _EdgeHi ("Edge Highlight", Float) = 0
        _GoldGlowRock ("Gold Glow", Float) = 0.5
        _GlowBaseRock ("Gold Glow Base", Float) = 0
        _SpecAmount ("Spec", Float) = 0.5
        _SpecPow ("Spec Power", Float) = 64
        _SpecHard ("Spec Hard", Float) = 0.5
        _SpecSmooth ("Spec Smooth", Float) = 0
        _Fres ("Sky Fresnel", Float) = 0.18
        _GlintColor ("Gold Color", Color) = (1, 0.941, 0.722, 1)

        [Header(Lighting)]
        _LightTint ("Light Color", Color) = (1, 0.902, 0.776, 1)
        _Ambient ("Ambient Color", Color) = (0.627, 0.69, 0.878, 1)
        _AmbientStrength ("Ambient Strength", Float) = 0.45
        _ShadowTint ("Shadow Tint", Color) = (0.604, 0.596, 0.8, 1)
        _CastShadowStrength ("Receive Cast Shadow", Float) = 0
        _FogColor ("Fog Color", Color) = (0.918, 0.855, 0.8, 1)
        _FogNear ("Fog Near", Float) = 200
        _FogFar ("Fog Far", Float) = 820
        _MapHeight ("Map Root Height", Float) = 0
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
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include "EnvironmentCommon.cginc"

            float3 _Talus;
            float3 _Talus2;
            float3 _Cliff;
            float3 _Cliff2;
            float3 _Upper;
            float3 _Upper2;
            float3 _Edge;
            float3 _Shade;
            float3 _Spec;
            float3 _SkyHi;
            float3 _GlintColor;
            float _Bands, _Soft, _Grad, _GradTop, _Blotch, _RimAmount, _Cavity, _ShadowAmount, _Thresh, _Flat,
                _FaceJit, _ShadeMix;
            float _EdgeHi;
            float _GoldGlowRock;
            float _GlowBaseRock;
            float _SpecAmount;
            float _SpecPow;
            float _SpecHard;
            float _SpecSmooth;
            float _Fres;

            struct SVertexInput
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 uv1 : TEXCOORD1;
            };

            struct SVertexOutput
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 zoneCav : TEXCOORD2;
                float ao : TEXCOORD3;
                SHADOW_COORDS(4)
            };

            SVertexOutput Vert(SVertexInput v)
            {
                SVertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.zoneCav = v.uv1;
                o.ao = v.color.r;
                TRANSFER_SHADOW(o);
                return o;
            }

            float4 Frag(SVertexOutput i, float facing : VFACE) : SV_Target
            {
                float3 wp = i.worldPos;
                float3 p = wp - float3(0.0, _MapHeight, 0.0);
                float3 surfaceNormal = normalize(i.worldNormal);
                if (facing < 0.0) surfaceNormal = -surfaceNormal;
                if (_Flat > 0.5)
                {
                    // 면마다 평평한 명암 (저폴리)
                    surfaceNormal = normalize(cross(ddx(wp), ddy(wp)));
                    if (dot(surfaceNormal, _WorldSpaceCameraPos - wp) < 0.0) surfaceNormal = -surfaceNormal;
                }
                float3 normalDirection = surfaceNormal;
                float3 viewDirection = normalize(_WorldSpaceCameraPos - wp);
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float dist = length(_WorldSpaceCameraPos - wp);

                float ndl = dot(normalDirection, lightDirection);
                float sh = lerp(1.0, SHADOW_ATTENUATION(i), _CastShadowStrength);
                float lf = (ndl * 0.5 + 0.5) * lerp(0.3, 1.0, sh);
                float lit;
                if (_Bands < 2.5) lit = smoothstep(_Thresh - _Soft, _Thresh + _Soft, lf);
                else lit = smoothstep(_Thresh - 0.08 - _Soft, _Thresh - 0.08 + _Soft,
                    lf) * 0.55 + smoothstep(_Thresh + 0.16 - _Soft, _Thresh + 0.16 + _Soft, lf) * 0.45;

                // 바탕색: 너덜 비탈 / 절벽 띠 / 위 몸체
                float z = i.zoneCav.x;
                float blot = FractalNoise(p.xz * 0.022 + p.y * 0.018) * 0.5 + 0.5;
                float bm = lerp(0.5, smoothstep(0.2, 0.8, blot), _Blotch);
                float3 talus = lerp(_Talus2, _Talus, bm), cliff = lerp(_Cliff2, _Cliff, bm),
                    upper = lerp(_Upper2, _Upper, bm);
                float3 alb = z < 1.0 ? lerp(talus, cliff, smoothstep(0.35, 1.0, z)) : lerp(cliff, upper,
                    smoothstep(1.0, 1.7, z));
                if (_FaceJit > 0.0)
                {
                    float fh = Hash12(floor(surfaceNormal.xz * 23.0 + surfaceNormal.y * 11.0) +
                        floor(p.xz / 40.0) * 3.7);
                    alb *= 1.0 + (fh - 0.5) * _FaceJit;
                }
                alb *= lerp(1.0, lerp(0.84, 1.1, smoothstep(0.0, _GradTop, p.y)), _Grad);
                float ao = i.ao;
                alb *= lerp(1.0, ao, _Cavity);
                float hi = smoothstep(0.1, 0.45, i.zoneCav.y) * smoothstep(-0.1, 0.6, surfaceNormal.y + 0.25);
                alb = lerp(alb, _Edge, hi * _EdgeHi);

                float3 shade = lerp(lerp(_ShadowTint * _ShadowAmount, _Shade, _ShadeMix), 1.0, lit);
                float3 col = alb * shade * _LightTint + alb * _Ambient * _AmbientStrength * ao;
                float rim = smoothstep(0.62, 0.7, 1.0 - max(dot(normalDirection, viewDirection), 0.0)) *
                    smoothstep(-0.1, 0.4, ndl) * _RimAmount;
                col += _LightTint * rim * 0.22;

                float3 halfDirection = normalize(lightDirection + viewDirection);
                float sheen = pow(max(dot(normalDirection, halfDirection), 0.0), 6.0);
                float grazing = pow(1.0 - max(dot(surfaceNormal, viewDirection), 0.0), 3.0);
                if (_GoldGlowRock > 0.0)
                {
                    float broad = pow(max(dot(surfaceNormal, halfDirection), 0.0), 2.5);
                    float sway = SimplexNoise(p.xz * 0.025 + viewDirection.xz * 1.2) * 0.5 + 0.5;
                    float glow = max(_GlowBaseRock * (0.75 + sway * 0.5) * (0.5 + 0.5 * broad),
                        sheen * 0.5 + grazing * 0.4) * lit;
                    float gAmt = glow * _GoldGlowRock;
                    gAmt = 0.9 * (1.0 - exp(-gAmt / 0.9));
                    col += _GlintColor * gAmt;
                }
                // 돌의 광택: 각진 면 법선과 매끈한 정점 법선을 섞은 법선으로 반사
                float3 vn = normalize(i.worldNormal) * (facing < 0.0 ? -1.0 : 1.0);
                float3 specularNormal = normalize(lerp(surfaceNormal, vn, _SpecSmooth));
                if (_SpecAmount > 0.0)
                {
                    float sp = pow(max(dot(specularNormal, halfDirection), 0.0), _SpecPow);
                    sp = lerp(sp, smoothstep(0.42, 0.5, sp), _SpecHard);
                    col += _Spec * _LightTint * sp * _SpecAmount * lerp(0.2, 1.0, sh);
                }
                if (_Fres > 0.0)
                {
                    float fr = pow(1.0 - max(dot(specularNormal, viewDirection), 0.0), 4.0);
                    float3 reflectedDirection = reflect(-viewDirection, specularNormal);
                    float3 sky = lerp(_FogColor, _SkyHi, smoothstep(-0.1, 0.6, reflectedDirection.y));
                    col = lerp(col, sky, saturate(min(fr * _Fres, 0.85)));
                }
                col = lerp(col, _FogColor, smoothstep(_FogNear, _FogFar, dist));
                return float4(col, 1.0);
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
