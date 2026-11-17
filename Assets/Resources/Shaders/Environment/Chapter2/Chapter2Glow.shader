Shader "Espoir/Environment/Chapter2/Glow"
{
    // 바닥에 깐 빛 띠·빛 고리: uv.y = 0 쪽이 밝고 1 쪽으로 스러짐 (더하기)
    Properties
    {
        _Color ("Color", Color) = (1, 0.6, 0.9, 1)
        _Amount ("Amount", Float) = 0.12
        _Falloff ("Falloff Power", Float) = 2.4
        _FogNear ("Fog Near", Float) = 130
        _FogFar ("Fog Far", Float) = 520
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float3 _Color;
            float _Amount;
            float _Falloff;
            float _FogNear;
            float _FogFar;

            struct SVertexOutput
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float dist : TEXCOORD1;
            };

            SVertexOutput Vert(float4 vertex : POSITION, float2 uv : TEXCOORD0)
            {
                SVertexOutput o;
                o.pos = UnityObjectToClipPos(vertex);
                o.uv = uv;
                o.dist = length(UnityObjectToViewPos(vertex));
                return o;
            }

            float4 Frag(SVertexOutput i) : SV_Target
            {
                float a = pow(1.0 - saturate(i.uv.y), _Falloff) * smoothstep(0.0, 0.05, i.uv.x) *
                    smoothstep(1.0, 0.95, i.uv.x);
                a *= 1.0 - smoothstep(_FogNear, _FogFar, i.dist);
                return float4(_Color * a * _Amount, 1.0);
            }
            ENDCG
        }
    }
}
