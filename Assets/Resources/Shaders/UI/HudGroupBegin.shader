// HUD 합성 전 화면을 _HudSceneTex에 저장하고, 합성 영역의 알파 채널을 비웁니다.
// 이후 그려지는 HUD 요소가 알파 채널에 자기 덮임을 쌓아, 끝(HudGroupEnd)에서 묶음 한 장을 다시 뽑아낼 수 있게 한다.
Shader "Espoir/UI/HudGroupBegin"
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
        ZTest [unity_GUIZTestMode]

        GrabPass { "_HudSceneTex" }

        Pass
        {
            Blend Off
            ColorMask A

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 vert (float4 vertex : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(vertex);
            }

            float4 frag () : SV_Target
            {
                return float4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}
