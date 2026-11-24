Shader "Espoir/UI/CharacterInfo/Word"
{
    Properties
    {
        _MainTex ("Word (R)", 2D) = "black" {}
        _Color ("Color (linear)", Vector) = (1, 1, 1, 1)
        _Opacity ("Opacity", Float) = 0
        _Wipe ("Wipe", Range(0, 1)) = 1
        _Sweep ("Sweep (-1 = off)", Float) = -1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
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

            sampler2D _MainTex;
            float4 _Color;
            float _Opacity;
            float _Wipe;
            float _Sweep;

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
                float a = tex2D(_MainTex, i.uv).r;
                float s = smoothstep(_Wipe * 1.25 - 0.25, _Wipe * 1.25, i.uv.x);

                float lead = 4.0 * s * (1.0 - s);
                float sx = i.uv.x - (_Sweep * 1.7 - 0.35) + (i.uv.y - 0.5) * 0.3;
                float sweep = _Sweep < 0.0 ? 0.0 : exp(-sx * sx / 0.006);
                float3 col = _Color.rgb * a * (1.0 - s) * _Opacity * (1.0 + sweep * 2.4 + lead * 1.6);
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}
