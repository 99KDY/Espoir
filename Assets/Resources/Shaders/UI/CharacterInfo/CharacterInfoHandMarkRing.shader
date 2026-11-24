Shader "Espoir/UI/CharacterInfo/HandMarkRing"
{
    Properties
    {
        [NoScaleOffset] _MarkTex ("Ring (R line, G narrow, B wide)", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+6" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "RING"
            Tags { "LightMode" = "Always" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off
            ColorMask RGB

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MarkTex;
            float4 _RingColorA;
            float4 _RingColorB;
            float4 _RingValues;     // x 돌린 각(라디안), y 크기 비율, z 밝기, w 흰 선의 밝기

            struct SVaryings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            SVaryings Vert(float4 vertex : POSITION, float2 uv : TEXCOORD0)
            {
                SVaryings o;
                o.pos = UnityObjectToClipPos(vertex);
                o.uv = uv;
                return o;
            }

            float4 Frag(SVaryings i) : SV_Target
            {
                float2 p = i.uv - 0.5;
                float c = cos(_RingValues.x);
                float s = sin(_RingValues.x);
                p = float2(c * p.x - s * p.y, s * p.x + c * p.y) / max(_RingValues.y, 0.01);
                float2 q = p + 0.5;
                float2 inside = step(0.0, q) * step(q, 1.0);

                float3 ring = tex2Dgrad(_MarkTex, q, ddx(q), ddy(q)).rgb * inside.x * inside.y;
                float lineAmount = smoothstep(0.42, 0.58, ring.r);
                float inner = smoothstep(0.28, 0.36, ring.g) * (1.0 - lineAmount);
                float3 color = lerp(float3(1.0, 1.0, 1.0), _RingColorB.rgb, 0.45) * lineAmount * _RingValues.w
                    + _RingColorB.rgb * inner * 0.9 + _RingColorA.rgb * ring.b * 0.22;
                return float4(color * _RingValues.z, 1.0);
            }
            ENDCG
        }

        // 가산 효과는 캐릭터의 환경 색 보정 마스크에서 제외한다.
        Pass
        {
            Name "ENVIRONMENT_MASK"
            Tags { "LightMode" = "EnvironmentMask" }
            ZWrite Off
            ZTest Always
            ColorMask 0

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            float4 Vert(float4 vertex : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(vertex);
            }

            float4 Frag() : SV_Target
            {
                return 0;
            }
            ENDCG
        }
    }
}
