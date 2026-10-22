Shader "Hidden/Espoir/PlayerToonPost"
{
    HLSLINCLUDE
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
    float4 _MainTex_TexelSize;
    float4 _Edge;       // x 세기, y 두께(px), z 판정 기준, w 1px 시야각
    float4 _EdgeColor;  // rgb 선 색, a 장면 색 사용 정도
    float4 _Grade;      // x 노출, y 채도, z 대비, w 비네팅
    float _EdgeDark;
    float _SeparateGrading;
    float _EdgeFadeDistance;

    float ViewDepth(float2 uv)
    {
        return LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv));
    }

    float3 ToSRGB(float3 c) { return lerp(c * 12.92, 1.055 * pow(max(c, 0), 1.0 / 2.4) - 0.055, step(0.0031308, c)); }
    float3 ToLinear(float3 c) { return lerp(c / 12.92, pow(max((c + 0.055) / 1.055, 0), 2.4), step(0.04045, c)); }

    float4 Frag(VaryingsDefault i) : SV_Target
    {
        float3 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord).rgb;

        // 주변 픽셀과의 깊이 차이로 외곽선을 구한다.
        if (_Edge.x > 0.001)
        {
            float d0 = ViewDepth(i.texcoord);
            float best = d0;
            float2 bestUv = i.texcoord;
            [unroll]
            for (int k = 0; k < 8; k++)
            {
                float a = k * 0.7853982;
                float2 uv2 = i.texcoord + float2(cos(a), sin(a)) * _MainTex_TexelSize.xy * _Edge.y;
                float d = ViewDepth(uv2);
                if (d < best) { best = d; bestUv = uv2; }
            }
            float thr = _Edge.z + d0 * _Edge.w * _Edge.y * 2.5;
            float e = smoothstep(thr, thr * 1.6, d0 - best) * _Edge.x;
            float3 nearCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, bestUv).rgb;
            float3 lineCol = lerp(_EdgeColor.rgb, nearCol * _EdgeDark, _EdgeColor.a);
            if (_SeparateGrading > 0.5)
            {
                thr = _Edge.z * d0 + 0.02;
                float fade = 1.0 - smoothstep(_EdgeFadeDistance * 0.6, _EdgeFadeDistance, d0);
                e = smoothstep(thr, thr * 1.8, d0 - best) * _Edge.x * fade;
                lineCol = lerp(_EdgeColor.rgb, min(col, nearCol) * _EdgeDark, _EdgeColor.a);
            }
            col = lerp(col, lineCol, e);
        }

        if (_SeparateGrading > 0.5) return float4(col, 1.0);

        // 선형 공간에서 노출·채도를 조정하고 sRGB 공간에서 대비·비네팅을 적용한다.
        col *= _Grade.x;
        float l = dot(col, float3(0.2126, 0.7152, 0.0722));
        col = max(lerp(l.xxx, col, _Grade.y), 0);
        col = col / (1.0 + max(col - 0.9, 0) * 0.6);
        float3 s = ToSRGB(col);
        s = (s - 0.5) * _Grade.z + 0.5;
        float v = smoothstep(0.95, 0.25, length((i.texcoord - 0.5) * float2(1.1, 1.0)));
        s *= lerp(1.0 - _Grade.w, 1.0, v);
        return float4(ToLinear(saturate(s)), 1);
    }
    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
