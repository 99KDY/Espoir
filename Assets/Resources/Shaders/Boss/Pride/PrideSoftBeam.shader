Shader "Espoir/Pride/SoftBeam"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color", Color) = (1, 0.96, 0.82, 1)
        [HDR] _GlowColor ("Glow Color", Color) = (1, 0.43, 0.055, 0.65)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            half4 _CoreColor;
            half4 _GlowColor;

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                half alpha : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.alpha = input.color.a;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half across = abs(input.uv.y * 2.0h - 1.0h);
                half edges = 1.0h - smoothstep(0.58h, 1.0h, across);
                half core = 1.0h - smoothstep(0.14h, 0.24h, across);
                core *= smoothstep(0.25h, 0.70h, input.alpha);

                half4 beam = lerp(_GlowColor, _CoreColor, core);
                beam.a *= edges * input.alpha;
                return beam;
            }
            ENDCG
        }
    }
}