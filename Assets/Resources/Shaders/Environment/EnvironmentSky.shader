Shader "Espoir/Environment/Sky"
{
    Properties
    {
        _Zenith ("Zenith", Color) = (0.478, 0.651, 0.863, 1)
        _Horizon ("Horizon", Color) = (0.984, 0.878, 0.8, 1)
        _SunGlow ("Sun Glow", Color) = (1, 0.949, 0.847, 1)
        _Cloud ("Cloud", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "PreviewType"="Skybox" }
        Cull Front
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "EnvironmentCommon.cginc"

            float3 _Zenith;
            float3 _Horizon;
            float3 _SunGlow;
            float3 _Cloud;

            struct SVertexOutput
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            SVertexOutput Vert(float4 vertex : POSITION)
            {
                SVertexOutput o;
                o.pos = UnityObjectToClipPos(vertex);
                // 먼 평면 바로 앞에 그려 모든 지형 뒤에 놓고, 카메라 기본 Skybox 가 덮어 그리지 않게 한다
                #if defined(UNITY_REVERSED_Z)
                o.pos.z = o.pos.w * 1e-6;
                #else
                o.pos.z = o.pos.w * (1.0 - 1e-6);
                #endif
                o.dir = mul((float3x3)unity_ObjectToWorld, vertex.xyz);
                return o;
            }

            float4 Frag(SVertexOutput i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = clamp(d.y, -0.2, 1.0);
                float3 c = lerp(_Horizon, _Zenith, smoothstep(0.0, 0.55, h));
                float sd = max(dot(d, normalize(_WorldSpaceLightPos0.xyz)), 0.0);
                c += _SunGlow * (pow(sd, 8.0) * 0.45 + smoothstep(0.9975, 0.9985, sd) * 1.5);
                float2 cp = d.xz / max(d.y + 0.25, 0.05) * 1.5 + float2(_Time.y * 0.01, 0.0);
                float cl = smoothstep(0.35, 0.6, FractalNoise(cp * 0.6) * 0.5 + 0.5) * smoothstep(0.02, 0.25, d.y);
                c = lerp(c, _Cloud, cl * 0.55);
                return float4(c, 1.0);
            }
            ENDCG
        }
    }
}
