Shader "Espoir/UI/CharacterInfo/Halo"
{
    Properties
    {
        _ColorTop ("Color A (linear)", Vector) = (1, 0.33, 0.24, 1)
        _ColorBottom ("Color B (linear)", Vector) = (0.32, 0.37, 1, 1)
        _Amount ("Amount", Float) = 0
        _Phase ("Phase", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent-1" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        ZTest LEqual
        Cull Off
        ColorMask RGB

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            float4 _ColorTop;
            float4 _ColorBottom;
            float _Amount;
            float _Phase;

            struct SVaryings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            SVaryings Vert(appdata_base v)
            {
                SVaryings o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                return o;
            }

            fixed4 Frag(SVaryings i) : SV_Target
            {
                float2 q = (i.uv - 0.5) * 2.0;
                float d = length(q * float2(1.0, 0.86));
                float g = exp(-d * d * 3.4) * (1.0 - smoothstep(0.8, 1.0, d));
                float mixing = smoothstep(-0.5, 0.7, q.y + sin(_Phase) * 0.25 + q.x * 0.3);
                float3 c = lerp(_ColorTop.rgb, _ColorBottom.rgb, mixing);
                return fixed4(c * g * _Amount, 1.0);
            }
            ENDCG
        }
    }
}
