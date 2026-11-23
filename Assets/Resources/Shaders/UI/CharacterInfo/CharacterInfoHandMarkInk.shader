// 피부색에 곱해 기존 조명을 유지하고 발광 시에도 문양 색이 남도록 한다.
Shader "Espoir/UI/CharacterInfo/HandMarkInk"
{
    Properties
    {
        [NoScaleOffset] _MarkTex ("Mark (R line, G narrow, B wide)", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+3" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "INK"
            Tags { "LightMode" = "Always" }
            Blend DstColor Zero
            ZWrite Off
            ZTest LEqual
            Cull Back
            Offset -2, -4
            ColorMask RGB

            CGPROGRAM
            #pragma vertex MarkVert
            #pragma fragment Frag
            #pragma target 3.0
            #include "CharacterInfoHandMarkCommon.cginc"

            float4 _MarkInk;
            float _MarkInkAmount;
            float4 _MarkGlowTint;

            float4 Frag(SMarkVaryings i) : SV_Target
            {
                float3 mark = MarkSample(i.uv, i.face);
                float3 spread = MarkSpread(i.uv);
                float3 color = lerp(float3(1.0, 1.0, 1.0), _MarkInk.rgb, mark.r * _MarkInkAmount);
                float tint = saturate(_MarkGlow * spread.x
                    * (mark.g * _MarkSpread.x + mark.b * _MarkSpread.y * (1.0 + 0.5 * spread.z)));
                color *= lerp(float3(1.0, 1.0, 1.0), _MarkGlowTint.rgb, tint);
                return float4(color, 1.0);
            }
            ENDCG
        }

        // 손 피부가 이미 기록한 환경 색 보정 마스크를 유지한다.
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
