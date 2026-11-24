// 밝은 피부 위에서도 문양 색을 유지하도록 발광량에 따라 배경 기여도를 낮춘다.

Shader "Espoir/UI/CharacterInfo/HandMarkGlow"
{
    Properties
    {
        [NoScaleOffset] _MarkTex ("Mark (R line, G narrow, B wide)", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+4" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "GLOW"
            Tags { "LightMode" = "Always" }
            Blend One OneMinusSrcAlpha
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

            float4 _MarkGlowColor;
            float4 _MarkLine;       // x 선 밝기, y 선을 흰색 쪽으로 섞는 정도, z 좁은 빛, w 넓은 빛
            float4 _MarkKeep;       // x 색 지키기 세기, y 문양 밑 피부의 가장 밝은 채널 값

            float Shoulder(float x)
            {
                return x > 0.7 ? 0.7 + 0.3 * (1.0 - exp(-(x - 0.7) / 0.3)) : x;
            }

            float4 Frag(SMarkVaryings i) : SV_Target
            {
                float3 mark = MarkSample(i.uv, i.face);
                float3 spread = MarkSpread(i.uv);

                float coreAmount = mark.r * _MarkLine.x * (1.0 + 1.6 * spread.z + 4.0 * spread.y);
                float haloAmount = (mark.g * _MarkLine.z + mark.b * _MarkLine.w)
                    * (1.0 + 1.5 * spread.z + 4.0 * spread.y);
                float3 color = lerp(_MarkGlowColor.rgb, float3(1.0, 1.0, 1.0), _MarkLine.y) * coreAmount
                    + _MarkGlowColor.rgb * haloAmount;
                float3 glow = color * max(spread.x, spread.y) * _MarkGlow
                    + _MarkGlowColor.rgb * mark.r * _MarkSpread.w;

                float skinPeak = max(_MarkKeep.y, 0.001);
                float peak = skinPeak + max(max(glow.r, glow.g), glow.b);
                float keep = lerp(1.0, (Shoulder(peak) / peak) / (Shoulder(skinPeak) / skinPeak), _MarkKeep.x);
                return float4(glow * keep, 1.0 - keep);
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
