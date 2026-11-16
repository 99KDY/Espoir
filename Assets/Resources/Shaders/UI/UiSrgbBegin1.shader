// sRGB 합성 구간 1 시작: 장면 색을 sRGB 값으로 기록합니다. UiSrgbComposite.cginc 참조.
Shader "Espoir/UI/UiSrgbBegin1"
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

        GrabPass { "_UiSrgbScene1" }

        Pass
        {
            Blend Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UiSrgbComposite.cginc"

            sampler2D _UiSrgbScene1;

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
                float3 scene = tex2D(_UiSrgbScene1, i.grab.xy / i.grab.w).rgb;
                return float4(UiLinToSrgb(scene), 0.0);
            }
            ENDCG
        }
    }
}
