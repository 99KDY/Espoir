Shader "Espoir/Environment/Chapter2/Firefly"
{
    // 반딧불: 사각형 하나가 반딧불 하나. uv = 모서리(-1~1), uv1.x = 반딧불마다 다른 0~1 값
    Properties
    {
        _Color ("Color", Color) = (1, 0.886, 0.627, 1)
        _Size ("Size", Float) = 0.26
        _Amount ("Amount", Float) = 1.6
        _FogNear ("Fog Near", Float) = 130
        _FogFar ("Fog Far", Float) = 520
        _MapAxisX ("Map X Axis", Vector) = (-1, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float3 _Color;
            float _Size;
            float _Amount;
            float _FogNear;
            float _FogFar;
            float4 _MapAxisX;

            struct SVertexInput
            {
                float4 vertex : POSITION;
                float2 corner : TEXCOORD0;
                float2 seed : TEXCOORD1;
            };

            struct SVertexOutput
            {
                float4 pos : SV_POSITION;
                float2 corner : TEXCOORD0;
                float alpha : TEXCOORD1;
            };

            SVertexOutput Vert(SVertexInput v)
            {
                SVertexOutput o;
                float t = _Time.y;
                float seed = v.seed.x;
                float s = seed * 6.2832;
                float3 drift;
                drift.x = sin(t * 0.23 + s * 3.0) * 0.9 + sin(t * 0.71 + s * 7.0) * 0.25;
                drift.z = cos(t * 0.19 + s * 5.0) * 0.9 + cos(t * 0.63 + s * 2.0) * 0.25;
                drift.y = sin(t * 0.31 + s * 4.0) * 0.35;
                drift.x *= _MapAxisX.x;
                float3 center = mul(unity_ObjectToWorld, v.vertex).xyz + drift;
                float3 view = mul(UNITY_MATRIX_V, float4(center, 1.0)).xyz;
                float twinkle = 0.5 + 0.5 * sin(t * (0.9 + seed * 1.3) + s * 9.0);
                o.alpha = smoothstep(0.15, 0.75, twinkle) * (1.0 - smoothstep(_FogNear, _FogFar, -view.z));
                // 렌더 텍스처에 그릴 때(D3D) 투영 행렬 [1][1] 이 음수가 되므로 abs
                float pixels = max(_Size * abs(UNITY_MATRIX_P[1][1]) * 400.0 / max(-view.z, 0.01) *
                    (0.6 + 0.4 * o.alpha), 1.5);
                float halfSize = pixels / max(_ScreenParams.y, 1.0) * -view.z / abs(UNITY_MATRIX_P[1][1]);
                view.xy += v.corner * halfSize;
                o.pos = mul(UNITY_MATRIX_P, float4(view, 1.0));
                o.corner = v.corner;
                return o;
            }

            float4 Frag(SVertexOutput i) : SV_Target
            {
                float d = length(i.corner);
                float a = pow(max(1.0 - d, 0.0), 2.2);
                return float4(_Color * a * i.alpha * _Amount, 1.0);
            }
            ENDCG
        }
    }
}
