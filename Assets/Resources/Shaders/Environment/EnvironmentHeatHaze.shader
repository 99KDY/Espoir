Shader "Hidden/Espoir/EnvironmentHeatHaze"
{
    HLSLINCLUDE
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
    float4 _HeatHaze; // x 강도, yz 거리 범위, w 화면 비율
    float _HeatHazeTime;

    float Hash(float2 p)
    {
        return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
    }

    float Noise(float2 p)
    {
        float2 cell = floor(p);
        float2 blend = frac(p);
        blend = blend * blend * (3.0 - 2.0 * blend);
        return lerp(lerp(Hash(cell), Hash(cell + float2(1.0, 0.0)), blend.x),
            lerp(Hash(cell + float2(0.0, 1.0)), Hash(cell + 1.0), blend.x), blend.y) * 2.0 - 1.0;
    }

    float4 Frag(VaryingsDefault i) : SV_Target
    {
        float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoord);
        float sky = step(0.99999, Linear01Depth(depth));
        float distance = lerp(LinearEyeDepth(depth), 400.0, sky);
        float fade = smoothstep(_HeatHaze.y, _HeatHaze.z, distance) * lerp(1.0, 0.5, sky);
        [branch]
        if (fade == 0.0 || _HeatHaze.x == 0.0)
        {
            return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(i.texcoord));
        }

        float2 p = i.texcoord * float2(_HeatHaze.w, 1.0);
        float time = _HeatHazeTime;
        float waveX = Noise(float2(p.x * 70.0, p.y * 26.0 - time * 2.6)) * 0.65
            + Noise(float2(p.x * 150.0, p.y * 55.0 - time * 4.2)) * 0.35;
        float waveY = Noise(float2(p.x * 55.0 + 9.0, p.y * 22.0 - time * 2.1));
        float gust = 0.55 + 0.45 * Noise(float2(p.x * 4.0 - time * 0.15, p.y * 3.0 - time * 0.25));
        float strength = fade * gust * _HeatHaze.x;
        float2 offset = float2(waveX * 0.0018, waveY * 0.0026) * strength;
        float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(i.texcoord + offset));
        color.rgb *= 1.0 + waveX * 0.02 * strength;
        return color;
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
