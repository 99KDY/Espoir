// uv: 빌보드 모서리(-1~1), uv2.x: 검광 꼬리의 순서(0~1).
Shader "Espoir/UI/CharacterInfo/Glint"
{
    SubShader
    {
        Tags { "Queue" = "Transparent+8" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "GLINT"
            Tags { "LightMode" = "Always" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off
            ColorMask RGB

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            // 기준 검 길이 1.54에 맞춘 좌표계. 가드는 0.55에 위치한다.
            float4 _GlintStart;     // xyz 자루 끝 (검 좌표), w 기준 길이 1 이 검 좌표로 얼마인지
            float4 _GlintAxis;      // xyz 칼끝 쪽 방향 (검 좌표), w 기준 길이 1 이 월드로 몇 m 인지
            float4 _GlintValues;    // x 머리 자리, y 세기, z 밝기
            float4 _GlintColorA;
            float4 _GlintColorB;

            struct SVaryings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 fade : TEXCOORD1;
            };

            SVaryings Vert(float4 vertex : POSITION, float2 uv : TEXCOORD0, float2 uv2 : TEXCOORD1)
            {
                SVaryings o;
                float order = uv2.x;
                float along = _GlintValues.x - order * 0.3;
                float alpha = (1.0 - order * 0.85) * smoothstep(0.52, 0.68, along)
                    * (1.0 - smoothstep(1.48, 1.55, along));
                float3 local = _GlintStart.xyz + _GlintAxis.xyz * (clamp(along, 0.5, 1.54) * _GlintStart.w);
                float3 view = UnityObjectToViewPos(local);
                // 칼날에 가려지지 않도록 검광을 카메라 쪽으로 이동한다.
                view -= normalize(view) * (0.07 * _GlintAxis.w);
                view.xy += uv * (lerp(0.3, 0.08, order) * _GlintAxis.w * 0.5);
                o.pos = mul(UNITY_MATRIX_P, float4(view, 1.0));
                o.uv = uv;
                o.fade = float2(alpha * _GlintValues.y, order);
                return o;
            }

            float4 Frag(SVaryings i) : SV_Target
            {
                float d = length(i.uv);
                float glow = exp(-d * d * 5.0) * (1.0 - smoothstep(0.75, 1.0, d));
                float3 color = lerp(_GlintColorA.rgb, _GlintColorB.rgb, frac(i.fade.y * 7.31));
                return float4(color * (glow * i.fade.x * _GlintValues.z), 1.0);
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
