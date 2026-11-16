Shader "Espoir/Environment/Chapter2/Ground"
{
    // 정점 채널: color.r = 접촉 AO, uv1 = (pathMask, crest), uv2 = (hard, pave)
    Properties
    {
        _MapOrigin ("Map Origin", Vector) = (0, 0, 0, 0)
        _MapAxisX ("Map X Axis", Vector) = (-1, 0, 0, 0)
        _MapAxisZ ("Map Z Axis", Vector) = (0, 0, 1, 0)

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

        [Header(Glint)]
        _GlintColor ("Glint Color", Color) = (1, 0.847, 0.627, 1)
        _Glint ("Glint", Float) = 0.4
        _GlintSize ("Glint Size", Float) = 0.065
        _GlintDensity ("Glint Density", Float) = 0.32
        _GoldGlow ("Gold Glow", Float) = 0
        _GlowBase ("Gold Glow Base", Float) = 0
        _GlowCap ("Glow Cap", Float) = 99

        [Header(Hard Ground and Pave)]
        _HardLit ("Hard Lit", Color) = (0.65, 0.56, 0.7, 1)
        _HardShadow ("Hard Shadow", Color) = (0.29, 0.24, 0.45, 1)
        _Strata ("Strata", Float) = 1.3
        _PaveA ("Pave A", Color) = (0.9, 0.74, 0.8, 1)
        _PaveB ("Pave B", Color) = (0.77, 0.61, 0.69, 1)
        _PaveSize ("Pave Size", Float) = 0.9
        _PaveSand ("Pave Sand", Float) = 0.3

        [Header(Water Edge)]
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

        [Header(Fog)]
        _FogColor ("Fog Color", Color) = (0.35, 0.28, 0.5, 1)
        _FogNear ("Fog Near", Float) = 130
        _FogFar ("Fog Far", Float) = 520
        _FogHeight ("Fog Height", Float) = 0.2
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
            #include "Chapter2SandBody.cginc"

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
                float4 masks : TEXCOORD2;
                float ao : TEXCOORD3;
                SHADOW_COORDS(4)
            };

            SVertexOutput Vert(SVertexInput v)
            {
                SVertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                float pave = 1.0 - smoothstep(-0.35, 0.35, (0.5 - v.uv2.y) * 2.0);
                o.masks = float4(v.uv1, v.uv2.x, pave);
                o.ao = v.color.r;
                TRANSFER_SHADOW(o);
                return o;
            }

            float4 Frag(SVertexOutput i) : SV_Target
            {
                float3 col = Chapter2SandColor(i.worldPos, i.worldNormal, i.masks, i.ao.xxx, SHADOW_ATTENUATION(i),
                    false);
                return float4(col, 1.0);
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
