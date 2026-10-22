Shader "Espoir/Environment/Water"
{
    // 호수: uv1.x = 수심. 우물(Pool Depth From UV 켬): 원판 uv 로 수심 계산
    Properties
    {
        _MapOrigin ("Map Origin", Vector) = (0, 0, 0, 0)
        _MapAxisX ("Map X Axis", Vector) = (1, 0, 0, 0)
        _MapAxisZ ("Map Z Axis", Vector) = (0, 0, 1, 0)

        _Tint ("Shallow Tint", Color) = (0.557, 0.886, 0.839, 1)
        _DeepTint ("Deep Tint", Color) = (0.165, 0.565, 0.706, 1)
        _Foam ("Foam", Color) = (1, 0.973, 0.902, 1)
        _SkyZenith ("Sky Zenith", Color) = (0.478, 0.651, 0.863, 1)
        _SkyHorizon ("Sky Horizon", Color) = (0.984, 0.878, 0.8, 1)
        _MaxDepth ("Max Depth", Float) = 0.7
        _SurfaceOcclusion ("Surface Occlusion", Range(0, 1)) = 0
        _Flow ("Flow", Vector) = (0.7, -0.7, 0, 0)
        [Toggle] _PoolDepth ("Pool Depth From UV", Float) = 0

        [Header(Lighting)]
        _LightTint ("Light Color", Color) = (1, 0.902, 0.776, 1)
        _FogColor ("Fog Color", Color) = (0.918, 0.855, 0.8, 1)
        _FogNear ("Fog Near", Float) = 100
        _FogFar ("Fog Far", Float) = 360
        _LampPos ("Lamp Position (w = radius)", Vector) = (0, -100, 0, 0)
        _LampColor ("Lamp Color", Color) = (0, 0, 0, 1)
        _LampFlicker ("Lamp Flicker", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "EnvironmentCommon.cginc"

            float3 _Tint;
            float3 _DeepTint;
            float3 _Foam;
            float3 _SkyZenith;
            float3 _SkyHorizon;
            float _MaxDepth;
            float _PoolDepth;
            float2 _Flow;

            struct SVertexInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct SVertexOutput
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float depth : TEXCOORD1;
            };

            SVertexOutput Vert(SVertexInput v)
            {
                SVertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float r = length(v.uv - 0.5) * 2.0;
                o.depth = _PoolDepth > 0.5 ? _MaxDepth * (1.0 - smoothstep(0.55, 1.02, r)) + 0.004 : v.uv1.x;
                return o;
            }

            float WaveHeight(float2 p)
            {
                float t = _Time.y;
                float2 f = _Flow * t;
                return SimplexNoise(p * 0.8 + f * 0.35) * 0.5 + SimplexNoise(p * 1.9 - f * 0.2 + t * 0.1) * 0.2;
            }

            float4 Frag(SVertexOutput i) : SV_Target
            {
                float depth = i.depth;
                if (depth < -0.01) discard;
                float t = _Time.y;
                float3 viewDirection = normalize(MapDirection(_WorldSpaceCameraPos - i.worldPos));
                float2 p = MapPosition(i.worldPos).xz;
                float e = 0.04;
                float h0 = WaveHeight(p);
                float3 normalDirection = normalize(float3(-(WaveHeight(p + float2(e, 0.0)) - h0) / e * 0.06, 1.0,
                    -(WaveHeight(p + float2(0.0, e)) - h0) / e * 0.06));
                float ndv = max(dot(normalDirection, viewDirection), 0.0);
                float fres = 0.03 + 0.97 * pow(1.0 - ndv, 5.0);
                float3 reflectionDirection = reflect(-viewDirection, normalDirection);
                float3 sky = lerp(_SkyHorizon, _SkyZenith, smoothstep(-0.05, 0.6, reflectionDirection.y));
                float d01 = smoothstep(0.03, _MaxDepth, depth);
                float3 body = lerp(_Tint, _DeepTint, d01);
                float alpha = lerp(0.05, 0.62, d01);
                float sp = dot(reflectionDirection, normalize(MapDirection(_WorldSpaceLightPos0.xyz)));
                float spec = smoothstep(0.988, 0.992, sp) * 1.4 + smoothstep(0.94, 0.97, sp) * 0.22;
                float tw = smoothstep(0.55, 0.6, h0 + SimplexNoise(p * 5.0 - t * 0.6) * 0.2) * smoothstep(0.88,
                    0.96, sp * 0.5 + 0.5);
                float gl = smoothstep(0.66, 0.72,
                    SimplexNoise(p * 0.22 + _Flow * t * 0.08) * 0.5 + 0.5 + (normalDirection.x +
                        normalDirection.z) * 0.6);
                float3 lampRef = 0.0;
                if (_LampPos.w > 0.0)
                {
                    float3 ld = MapDirection(_LampPos.xyz - i.worldPos);
                    float dl = length(ld);
                    lampRef = LampColor() * smoothstep(0.975, 0.993, dot(reflectionDirection,
                        ld / dl)) * (1.0 - smoothstep(0.0, _LampPos.w * 2.0, dl)) * 1.4;
                }
                float3 col = body * (1.0 - fres * 0.5) + sky * fres;
                col = lerp(col, lerp(sky, _LightTint, 0.4), gl * 0.45);
                col += _LightTint * (spec + tw * 0.6) + lampRef;
                float foam = 1.0 - smoothstep(0.0, 0.035, depth + SimplexNoise(p * 3.0 + t * 0.4) * 0.012);
                col = lerp(col, _Foam, foam * 0.85);
                alpha = saturate(alpha + fres * 0.5 + gl * 0.3 + spec + tw * 0.5 + foam * 0.85 + dot(lampRef,
                    0.33) * 0.6);
                col = lerp(col, _FogColor, smoothstep(_FogNear, _FogFar, length(_WorldSpaceCameraPos - i.worldPos)));
                return float4(col, alpha);
            }
            ENDCG
        }

        // 수면과 물밑이 합성된 색을 함께 감쇠한다.
        Pass
        {
            Name "SurfaceOcclusion"
            Blend DstColor Zero, Zero One
            ZWrite Off

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            float _SurfaceOcclusion;
            float _PoolDepth;
            float _MaxDepth;

            struct SVertexInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct SVertexOutput
            {
                float4 position : SV_POSITION;
                float depth : TEXCOORD0;
            };

            SVertexOutput Vert(SVertexInput v)
            {
                SVertexOutput o;
                o.position = UnityObjectToClipPos(v.vertex);
                float radius = length(v.uv - 0.5) * 2.0;
                o.depth = _PoolDepth > 0.5
                    ? _MaxDepth * (1.0 - smoothstep(0.55, 1.02, radius)) + 0.004
                    : v.uv1.x;
                return o;
            }

            float4 Frag(SVertexOutput i) : SV_Target
            {
                clip(i.depth + 0.01);
                float shade = 1.0 - _SurfaceOcclusion;
                return float4(shade, shade, shade, 1.0);
            }
            ENDCG
        }
    }
}
