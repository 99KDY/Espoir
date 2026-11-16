Shader "Espoir/Environment/Chapter3/LightShaft"
{
    // 창에서 바닥까지 내려오는 빛을 가산 합성합니다. 정점 색은 유리 색이며 바닥으로 갈수록 옅어집니다.
    Properties
    {
        _Color ("Strength", Color) = (0.28, 0.28, 0.28, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+6" "IgnoreProjector"="True" }
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

            struct SVertexInput
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct SVertexOutput
            {
                float4 pos : SV_POSITION;
                float3 color : TEXCOORD0;
            };

            SVertexOutput Vert(SVertexInput v)
            {
                SVertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color.rgb;
                return o;
            }

            float4 Frag(SVertexOutput i) : SV_Target
            {
                return float4(i.color * _Color, 1.0);
            }
            ENDCG
        }
    }
}
