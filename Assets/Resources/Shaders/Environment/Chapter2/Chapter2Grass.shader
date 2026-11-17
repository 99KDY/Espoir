Shader "Espoir/Environment/Chapter2/Grass"
{
    // 풀 포기 (CChapter2Grass 가 인스턴스로 그림). uv.y = 잎 높이 비율.
    // 색은 바닥 셰이더와 같은 식으로 그 자리 바닥색을 구하고 끝으로 갈수록만 밝힌다.
    // 드리운 그림자는 화면 그림자(바닥 깊이 기준)를 그대로 받는다 — 풀 키가 낮아 바닥 그림자와 같다.
    Properties
    {
        _MapOrigin ("Map Origin", Vector) = (0, 0, 0, 0)
        _MapAxisX ("Map X Axis", Vector) = (-1, 0, 0, 0)
        _MapAxisZ ("Map Z Axis", Vector) = (0, 0, 1, 0)

        [Header(Blade)]
        _Tip ("Tip Brightness", Float) = 1.2
        _TipTint ("Tip Tint", Vector) = (0.95, 1.05, 0.98, 0)
        _WaveTip ("Wave Tip", Float) = 0.22
        _Wind ("Wind", Float) = 0.2

        [Header(Ground)]
        _SandLit ("Lit", Color) = (0.5, 0.6, 0.5, 1)
        _SandMid ("Mid", Color) = (0.45, 0.5, 0.45, 1)
        _SandShadow ("Shadow", Color) = (0.4, 0.38, 0.5, 1)
        _SandDeep ("Deep", Color) = (0.3, 0.28, 0.45, 1)
        _PathColor ("Path Color", Color) = (0.6, 0.55, 0.6, 1)
        _Crest ("Crest Highlight", Float) = 0
        _LitGrad ("Lit Gradient", Float) = 1
        _GrainTex ("Fine Grain", Float) = 0
        _GrainSize ("Grain Size", Float) = 0.025
        _GlintColor ("Glint Color", Color) = (1, 0.847, 0.627, 1)
        _Glint ("Glint", Float) = 0.4
        _GlintSize ("Glint Size", Float) = 0.065
        _GlintDensity ("Glint Density", Float) = 0.32
        _GoldGlow ("Gold Glow", Float) = 0
        _GlowBase ("Gold Glow Base", Float) = 0
        _GlowCap ("Glow Cap", Float) = 99
        _HardLit ("Hard Lit", Color) = (0.65, 0.56, 0.7, 1)
        _HardShadow ("Hard Shadow", Color) = (0.29, 0.24, 0.45, 1)
        _Strata ("Strata", Float) = 1.3
        _PaveA ("Pave A", Color) = (0.9, 0.74, 0.8, 1)
        _PaveB ("Pave B", Color) = (0.77, 0.61, 0.69, 1)
        _PaveSize ("Pave Size", Float) = 0.9
        _PaveSand ("Pave Sand", Float) = 0.3
        _Pond ("Lake Shading On", Float) = 1
        _WaterY ("Water Height (map)", Float) = -0.72

        [Header(Lighting)]
        _LightTint ("Light Color", Color) = (1, 1, 1, 1)
        _Ambient ("Ambient Color", Color) = (0.5, 0.5, 0.7, 1)
        _AmbientStrength ("Ambient Strength", Float) = 0.9
        _ShadowTint ("Shadow Tint", Color) = (0.5, 0.4, 0.7, 1)
        _MidTint ("Mid Band Tint", Color) = (0.7, 0.55, 0.9, 1)
        _RampThreshold ("Ramp Threshold", Float) = 0.5
        _RampSoft ("Ramp Softness", Float) = 0.035
        _MidWidth ("Mid Band Width", Float) = 0.04
        _MidStrength ("Mid Band Strength", Float) = 0.35
        _CastShadowStrength ("Receive Cast Shadow", Float) = 1
        _Rim ("Rim", Float) = 0.6
        _RimColor ("Rim Color", Color) = (0.83, 0.74, 1, 1)
        _Night ("Night", Float) = 1
        _FogColor ("Fog Color", Color) = (0.35, 0.28, 0.5, 1)
        _FogNear ("Fog Near", Float) = 130
        _FogFar ("Fog Far", Float) = 520
        _FogHeight ("Fog Height", Float) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        Cull Off

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing
            #pragma target 3.5
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include "Chapter2SandBody.cginc"

            float _Tip;
            float4 _TipTint;
            float _WaveTip;
            float _Wind;

            struct SVertexInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct SVertexOutput
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 tint : TEXCOORD1;
                SHADOW_COORDS(2)
            };

            SVertexOutput Vert(SVertexInput v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                SVertexOutput o;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 rootW = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0)).xyz;
                float3 rootM = MapPosition(rootW);
                float hgt = v.uv.y;
                // 반경 가장자리: 포기 전체가 뿌리 한 점으로 줄어들어 사라짐
                float gNear = Chapter2GrassNear(rootW);
                wp = lerp(rootW, wp, gNear);
                float t = _Time.y;
                float gw = Chapter2GrassWave(rootM.xz, t);
                float hh = wp.y - rootW.y;
                float sway = (0.3 + 0.7 * gw) * _Wind + sin(t * 2.1 + rootM.x * 1.7 + rootM.z * 1.3) * 0.03;
                float2 offset = float2(0.6, 0.8) * sway * hh * hgt;
                wp += MapDirection(float3(offset.x, 0.0, offset.y));
                wp.y -= abs(sway) * hh * hgt * 0.35;
                // 색: 뿌리 = 풀 아래 바닥색 → 끝으로 갈수록 밝게, 멀어지면 평균색
                float viewDist = length(_WorldSpaceCameraPos - wp);
                float gDet = gNear * lerp(1.0, _Chapter2GrassLod.z,
                    smoothstep(_Chapter2GrassLod.x, _Chapter2GrassLod.y, viewDist));
                float curve = hgt * hgt * (3.0 - 2.0 * hgt);
                float rnd = frac(sin(dot(rootM.xz, float2(12.9898, 78.233))) * 43758.5453);
                float3 tipColor = _TipTint.xyz * (_Tip + _WaveTip * gw) * lerp(0.94, 1.06, rnd);
                o.tint = lerp((1.0 + _Chapter2GrassWave * gw).xxx, lerp(_Chapter2GrassUnder.xxx, tipColor, curve),
                    gDet);
                o.worldPos = wp;
                o.pos = UnityWorldToClipPos(wp);
                v.vertex = mul(unity_WorldToObject, float4(wp, 1.0));
                TRANSFER_SHADOW(o);
                return o;
            }

            float4 Frag(SVertexOutput i) : SV_Target
            {
                float3 col = Chapter2SandColor(i.worldPos, float3(0.0, 1.0, 0.0), 0.0, i.tint,
                    SHADOW_ATTENUATION(i), true);
                return float4(col, 1.0);
            }
            ENDCG
        }
    }
}
