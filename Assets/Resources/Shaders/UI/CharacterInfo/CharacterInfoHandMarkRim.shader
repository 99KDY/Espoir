Shader "Espoir/UI/CharacterInfo/HandMarkRim"
{
    SubShader
    {
        Tags { "Queue" = "Transparent+5" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "RIM"
            Tags { "LightMode" = "Always" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back
            Offset -1, -2
            ColorMask RGB

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _RimColorA;
            float4 _RimColorB;
            float _RimPower;

            struct SVaryings
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 view : TEXCOORD1;
            };

            SVaryings Vert(float4 vertex : POSITION, float3 normal : NORMAL)
            {
                SVaryings o;
                o.pos = UnityObjectToClipPos(vertex);
                o.normal = UnityObjectToWorldNormal(normal);
                o.view = _WorldSpaceCameraPos - mul(unity_ObjectToWorld, vertex).xyz;
                return o;
            }

            float4 Frag(SVaryings i) : SV_Target
            {
                float facing = 1.0 - abs(dot(normalize(i.normal), normalize(i.view)));
                float edge = smoothstep(0.52, 0.60, facing);
                float hot = smoothstep(0.80, 0.86, facing);
                float3 color = _RimColorA.rgb * edge * 0.75
                    + lerp(_RimColorB.rgb, float3(1.0, 1.0, 1.0), 0.5) * hot * 2.4;
                return float4(color * _RimPower, 1.0);
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
