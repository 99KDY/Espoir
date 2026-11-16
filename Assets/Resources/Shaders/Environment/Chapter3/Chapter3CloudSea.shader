Shader "Hidden/Espoir/Chapter3CloudSea"
{
    // 시선을 따라 20회 표본을 읽고 구름층의 밀도와 빛을 누적합니다.
    // 노이즈 기준 좌표에 맞춰 Unity 월드 X를 반전합니다.
    HLSLINCLUDE
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
    float4x4 _CloudCameraToWorld;
    float4 _CloudProjection;   // x P00, y |P11|
    float4 _CloudLayer;        // x top, y bottom, z density, w scale
    float4 _CloudFog;          // x fogNear, y fogFar, z extinction, w time
    float4 _CloudShape;
    float4 _CloudLit;
    float4 _CloudShade;
    float4 _CloudFar;
    float4 _CloudSun;          // 노이즈 기준 좌표
    float4 _CloudWind;         // 노이즈 기준 좌표 x, z

    float H31(float3 p)
    {
        p = frac(p * 0.3183099 + 0.1);
        p *= 17.0;
        return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
    }

    float ValueNoise(float3 x)
    {
        float3 i = floor(x);
        float3 f = frac(x);
        f = f * f * (3.0 - 2.0 * f);
        return lerp(lerp(lerp(H31(i), H31(i + float3(1, 0, 0)), f.x),
            lerp(H31(i + float3(0, 1, 0)), H31(i + float3(1, 1, 0)), f.x), f.y),
                    lerp(lerp(H31(i + float3(0, 0, 1)), H31(i + float3(1, 0, 1)), f.x),
                        lerp(H31(i + float3(0, 1, 1)), H31(i + float3(1, 1, 1)), f.x), f.y), f.z);
    }

    // 아래는 꽉 차고 위로 갈수록 뭉게뭉게한 결만 남음 (큰 덩이 + 중간 결 + 잔 결)
    float Density(float3 p, int octaves)
    {
        float top = _CloudLayer.x;
        float bottom = _CloudLayer.y;
        float scale = _CloudLayer.w;
        float hgt = saturate((p.y - bottom) / (top - bottom));
        float3 w = float3(_CloudWind.x, 0.0, _CloudWind.y) * _CloudFog.w;
        float big = ValueNoise(float3(p.x, p.y * 0.4, p.z) * 0.014 * scale + w * 0.005 + 13.0) * 0.65
            + ValueNoise(float3(p.x, p.y * 0.6, p.z) * 0.034 * scale + w * 0.009 + 4.0) * 0.35;
        float3 q = float3(p.x, p.y * 1.7, p.z) * 0.052 * scale + w * 0.016;
        float n = ValueNoise(q) * 0.55 + ValueNoise(q * 2.07 + 5.3) * 0.28;
        if (octaves > 2) n += ValueNoise(q * 4.3 + 9.1) * 0.13 + ValueNoise(q * 8.9 + 2.7) * 0.06;
        else n += 0.09;
        float d = (1.0 - hgt) * _CloudShape.x + (big - 0.5) * _CloudShape.y + (n - 0.5) * _CloudShape.z - _CloudShape.w;
        return saturate(d) * _CloudLayer.z;
    }

    float4 Frag(VaryingsDefault i) : SV_Target
    {
        float3 sc = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord).rgb;
        float raw = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoord);
        float z = LinearEyeDepth(raw);
        float2 ndc = i.texcoord * 2.0 - 1.0;
        float3 viewPos = float3(ndc.x * z / _CloudProjection.x, ndc.y * z / _CloudProjection.y, -z);
        float3 worldPos = mul(_CloudCameraToWorld, float4(viewPos, 1.0)).xyz;
        float3 camPos = mul(_CloudCameraToWorld, float4(0.0, 0.0, 0.0, 1.0)).xyz;
        // 노이즈 기준 좌표로 변환
        worldPos.x = -worldPos.x;
        camPos.x = -camPos.x;
        float3 rd = worldPos - camPos;
        float tS = length(rd);
        rd /= tS;
        if (z > _ProjectionParams.z * 0.98) tS = 1e5;

        float top = _CloudLayer.x;
        float bottom = _CloudLayer.y;
        float ry = abs(rd.y) < 1e-4 ? 1e-4 : rd.y;
        float ta = (top - camPos.y) / ry;
        float tb = (bottom - camPos.y) / ry;
        float tn = max(min(ta, tb), 0.0);
        float tf = min(max(ta, tb), tS);
        if (tf <= tn) return float4(sc, 1.0);
        float span = min(tf - tn, 150.0);
        const int N = 20;
        float dt = span / N;
        float jit = frac(52.9829189 * frac(dot(i.vertex.xy, float2(0.06711056, 0.00583715))));
        float transmit = 1.0;
        float3 acc = 0.0;
        float3 sunDir = _CloudSun.xyz;
        [loop]
        for (int k = 0; k < N; k++)
        {
            float t = tn + (k + jit) * dt;
            float3 p = camPos + rd * t;
            float d = Density(p, 4);
            if (d > 0.002)
            {
                float dl = Density(p + sunDir * 4.5, 2);   // 해 쪽으로 구름이 얼마나 더 있나 → 그늘
                float hgt = saturate((p.y - bottom) / (top - bottom));
                float li = saturate(exp(-dl * 3.0) * 0.85 + hgt * 0.2);
                float3 c = lerp(_CloudShade.rgb, _CloudLit.rgb, smoothstep(0.12, 0.85, li));
                c = lerp(c, _CloudFar.rgb, smoothstep(_CloudFog.x, _CloudFog.y, t));
                float a = 1.0 - exp(-d * dt * _CloudFog.z);
                acc += transmit * a * c;
                transmit *= 1.0 - a;
                if (transmit < 0.015)
                {
                    transmit = 0.0;
                    break;
                }
            }
        }
        // 아주 비스듬한 먼 시선: 남은 구간은 지평선 안개
        if (tf - tn > 150.0 && transmit > 0.0)
        {
            acc += transmit * _CloudFar.rgb;
            transmit = 0.0;
        }
        return float4(sc * transmit + acc, 1.0);
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
            #pragma target 3.5
            ENDHLSL
        }
    }
}
