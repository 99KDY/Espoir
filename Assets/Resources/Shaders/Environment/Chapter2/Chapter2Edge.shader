Shader "Hidden/Espoir/Chapter2Edge"
{
    HLSLINCLUDE
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
    TEXTURE2D_SAMPLER2D(_Chapter2EdgeMask, sampler_Chapter2EdgeMask);
    float4 _MainTex_TexelSize;
    float4 _Edge;            // x 두께, y 깊이 선 세기, z 노멀 선 세기, w 깊이 기준
    float4 _Edge2;           // x 노멀 기준, y 어둡게, z 장면 색 섞기, w 사라지는 거리
    float4 _EdgeColor;
    float4 _ViewProjection;  // x P00, y |P11|

    float3 ViewPos(float2 uv)
    {
        float raw = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
        float z = LinearEyeDepth(raw);
        float2 ndc = uv * 2.0 - 1.0;
        return float3(ndc.x * z / _ViewProjection.x, ndc.y * z / _ViewProjection.y, -z);
    }

    float3 ViewNormal(float2 uv)
    {
        float2 t = _MainTex_TexelSize.xy;
        float3 p = ViewPos(uv);
        float3 px = ViewPos(uv + float2(t.x, 0.0)) - p;
        float3 py = ViewPos(uv + float2(0.0, t.y)) - p;
        float3 qx = p - ViewPos(uv - float2(t.x, 0.0));
        float3 qy = p - ViewPos(uv - float2(0.0, t.y));
        float3 dx = abs(px.z) < abs(qx.z) ? px : qx;
        float3 dy = abs(py.z) < abs(qy.z) ? py : qy;
        return normalize(cross(dx, dy));
    }

    float4 Frag(VaryingsDefault i) : SV_Target
    {
        float3 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord).rgb;
        float d0 = -ViewPos(i.texcoord).z;
        [branch]
        if (d0 > _ProjectionParams.z * 0.98) return float4(col, 1.0);

        // 플레이어 위에는 노멀 선을 그리지 않는다 (캐릭터 셰이더 외곽선과 겹치지 않게)
        float mask = SAMPLE_TEXTURE2D(_Chapter2EdgeMask, sampler_Chapter2EdgeMask, i.texcoord).r;
        float normalStrength = _Edge.z * (1.0 - step(0.5, mask));

        float3 n0 = ViewNormal(i.texcoord);
        float best = d0;
        float2 bestUv = i.texcoord;
        float nEdge = 0.0;
        [unroll]
        for (int k = 0; k < 8; k++)
        {
            float a = k * 0.7853982;
            float2 uv2 = i.texcoord + float2(cos(a), sin(a)) * _MainTex_TexelSize.xy * _Edge.x;
            float d = -ViewPos(uv2).z;
            if (d < best) { best = d; bestUv = uv2; }
            if (k < 4 && normalStrength > 0.0) nEdge = max(nEdge, 1.0 - dot(n0, ViewNormal(uv2)));
        }
        float thr = _Edge.w * d0 + 0.02;
        float e = smoothstep(thr, thr * 1.8, d0 - best) * _Edge.y;
        float en = smoothstep(_Edge2.x, _Edge2.x + 0.25, nEdge) * normalStrength;
        float fade = 1.0 - smoothstep(_Edge2.w * 0.6, _Edge2.w, d0);
        float edge = saturate(max(e, en) * fade);
        float3 nearCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, bestUv).rgb;
        float3 lineCol = lerp(_EdgeColor.rgb, min(col, nearCol) * _Edge2.y, _Edge2.z);
        return float4(lerp(col, lineCol, edge), 1.0);
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
            #pragma target 3.0
            ENDHLSL
        }
    }
}
