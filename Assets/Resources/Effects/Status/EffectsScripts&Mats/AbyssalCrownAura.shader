Shader "Espoir/VFX/Abyssal Crown Aura"
{
    Properties
    {
        [Toggle] _Ember ("Ember", Float) = 0
        [HDR] _Accent ("Accent", Color) = (0.89, 0.035, 0.15, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.84
        _SpinSpeed ("Rotation Speed", Float) = 0.30
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float _Ember;
            float4 _Accent;
            float _Opacity;
            float _SpinSpeed;

            struct VertexInput
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Interpolators
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Interpolators Vert(VertexInput input)
            {
                Interpolators output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float DistanceToLine(float2 positionOnQuad, float2 start, float2 end)
            {
                float2 delta = end - start;
                float along = saturate(dot(positionOnQuad - start, delta) / dot(delta, delta));
                return length(positionOnQuad - start - along * delta);
            }

            float4 DrawSeal(float2 positionOnQuad)
            {
                float sine, cosine;
                sincos(_Time.y * _SpinSpeed, sine, cosine);
                float2 p = float2(cosine * positionOnQuad.x - sine * positionOnQuad.y,
                                  sine * positionOnQuad.x + cosine * positionOnQuad.y);
                float radius = length(p);
                float angle = atan2(p.y, p.x + 0.00001);

                float2 halfCrown = float2(abs(p.x), p.y);
                float crown = DistanceToLine(halfCrown, float2(0, 0.49), float2(0.16, 0.10));
                crown = min(crown, DistanceToLine(halfCrown, float2(0.16, 0.10), float2(0.45, 0.35)));
                crown = min(crown, DistanceToLine(halfCrown, float2(0.45, 0.35), float2(0.33, -0.21)));
                crown = min(crown, DistanceToLine(halfCrown, float2(0.33, -0.21), float2(0, -0.21)));
                crown = min(crown, DistanceToLine(halfCrown, float2(0.29, -0.31), float2(0, -0.31)));
                float centerGem = abs(abs(p.x) + abs(p.y + 0.045) - 0.075) * 0.7071;

                float outerRing = max(abs(radius - 0.88), (0.03 - cos(angle * 4.0)) * 0.13);
                float innerTicks = max(abs(radius - 0.74), (0.88 - cos(angle * 16.0)) * 0.17);
                float2 gem = abs(p) - float2(0.62, 0.62);
                float outerGems = abs(abs(gem.x) + abs(gem.y) - 0.067) * 0.7071;

                float distanceToMark = min(min(crown, centerGem), min(min(outerRing, innerTicks), outerGems));
                float antialias = clamp(fwidth(distanceToMark), 0.003, 0.035);
                float outline = 1.0 - smoothstep(0.032, 0.043 + antialias, distanceToMark);
                float enamel = 1.0 - smoothstep(0.014, 0.022 + antialias, distanceToMark);
                float highlight = 1.0 - smoothstep(0.003, 0.010 + antialias, distanceToMark);
                float glow = 1.0 - smoothstep(0.025, 0.085, distanceToMark);
                float pulse = 0.90 + 0.10 * sin(_Time.y * 2.3 + 0.5);
                float warmEdge = 0.43 + 0.14 * sin(angle * 3.0 - _Time.y * 1.5);

                float3 color = lerp(float3(0.105, 0.007, 0.026), _Accent.rgb, enamel);
                color = lerp(color, float3(1.0, 0.42, 0.32), highlight * warmEdge);
                float opacity = max(outline, glow * 0.32) * _Opacity * pulse;
                return float4(color, opacity);
            }

            float4 DrawEmber(float2 positionOnQuad, float4 particleColor)
            {
                float radius = length(positionOnQuad * float2(1.2, 0.85));
                float core = 1.0 - smoothstep(0.07, 0.52, radius);
                float glow = 1.0 - smoothstep(0.17, 0.86, radius);
                float3 color = lerp(particleColor.rgb * 0.62, particleColor.rgb * 1.55, core);
                return float4(color, max(core, glow * 0.43) * particleColor.a * _Opacity);
            }

            float4 Frag(Interpolators input) : SV_Target
            {
                float2 positionOnQuad = input.uv * 2.0 - 1.0;
                if (_Ember > 0.5)
                {
                    return DrawEmber(positionOnQuad, input.color);
                }

                return DrawSeal(positionOnQuad);
            }
            ENDCG
        }
    }
    Fallback Off
}
