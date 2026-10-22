Shader "Espoir/Environment/Glass"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.875, 0.949, 0.933, 1)
        _SkyZenith ("Sky Zenith", Color) = (0.478, 0.651, 0.863, 1)
        _SkyHorizon ("Sky Horizon", Color) = (0.984, 0.878, 0.8, 1)

        [Header(Lighting)]
        _LightTint ("Light Color", Color) = (1, 0.902, 0.776, 1)
        _FogColor ("Fog Color", Color) = (0.918, 0.855, 0.8, 1)
        _FogNear ("Fog Near", Float) = 100
        _FogFar ("Fog Far", Float) = 360
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "EnvironmentCommon.cginc"

            float3 _Tint;
            float3 _SkyZenith;
            float3 _SkyHorizon;

            struct SVertexInput
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct SVertexOutput
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            SVertexOutput Vert(SVertexInput v)
            {
                SVertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            float4 Frag(SVertexOutput i, float facing : VFACE) : SV_Target
            {
                float3 normalDirection = normalize(i.worldNormal);
                if (facing < 0.0) normalDirection = -normalDirection;
                float3 viewDirection = normalize(_WorldSpaceCameraPos - i.worldPos);
                float ndv = max(dot(normalDirection, viewDirection), 0.0);
                float3 reflectionDirection = reflect(-viewDirection, normalDirection);
                float3 sky = lerp(_SkyHorizon, _SkyZenith, smoothstep(-0.15, 0.6, reflectionDirection.y));
                float sp = dot(reflectionDirection, normalize(_WorldSpaceLightPos0.xyz));
                float spec = smoothstep(0.985, 0.992, sp) * 1.6 + smoothstep(0.93, 0.97, sp) * 0.18;
                float rim = pow(1.0 - ndv, 1.6);
                float3 col = lerp(_Tint, sky, 0.55) * (0.85 + 0.3 * rim) + _LightTint * spec;
                float a = clamp(0.12 + rim * 0.75 + spec, 0.0, 0.92);
                col = lerp(col, _FogColor, smoothstep(_FogNear, _FogFar, length(_WorldSpaceCameraPos - i.worldPos)));
                return float4(col, a);
            }
            ENDCG
        }
    }
}
