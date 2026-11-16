// sRGB 합성 구간 0 끝: 합성 결과를 Linear 값으로 되돌립니다. UiSrgbComposite.cginc 참조.
Shader "Espoir/UI/UiSrgbEnd0"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always

        GrabPass { "_UiSrgbAfter0" }

        Pass
        {
            Blend Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UiSrgbComposite.cginc"

            sampler2D _UiSrgbAfter0;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 grab : TEXCOORD0;
            };

            v2f vert (float4 vertex : POSITION)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.grab = ComputeGrabScreenPos(o.pos);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 srgb = tex2D(_UiSrgbAfter0, i.grab.xy / i.grab.w).rgb;
                return float4(UiSrgbToLin(srgb), 1.0);
            }
            ENDCG
        }
    }
}
