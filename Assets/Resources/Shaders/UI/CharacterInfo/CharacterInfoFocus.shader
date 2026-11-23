Shader "Hidden/Espoir/CharacterInfoFocus"
{
    HLSLINCLUDE
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
    TEXTURE2D_SAMPLER2D(_CharacterInfoDepth, sampler_CharacterInfoDepth);
    float4 _MainTex_TexelSize;
    float4 _Focus;        // 초점 거리, 초점 범위, 흐림 반지름(px), 화면 가로세로 비
    float4 _Grade;        // 어둡게, 채도, 비네트
    float4 _Tint;
    float4 _Center;       // 캐릭터 화면 위치 xy, 배경 처리 강도, 초점 거리의 1m에 해당하는 화면 높이 비율
    float4 _Protect;      // 효과에서 빼는 원: xy, 반지름, 세기 (문양 손 둘레)
    float4 _Impact;       // 섬광, 줌 블러, 흑백, 흑백 반전
    float3 _ThemeLight;
    float4 _ThemeTint;
    float4 _ThemeAmount;  // 배경 색상 강도, 가장자리 색상 강도
    float4 _Panel;        // 정보 패널 흐림 반지름(px), 오른쪽 경계, 경계 폭

    // x: 씬과 캐릭터 중 가까운 깊이, y: 캐릭터 영역 여부.

    float2 SampleDepth(float2 uv)
    {
        float scene = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, uv, 0));
        float character = SAMPLE_TEXTURE2D_LOD(_CharacterInfoDepth, sampler_CharacterInfoDepth, uv, 0).r;
        return float2(min(scene, character), step(character, scene + 0.05));
    }

    float Back(float d)
    {
        return smoothstep(_Focus.x + _Focus.y, _Focus.x + _Focus.y * 4.0 + 3.0, d);
    }

    // 가까운 경계는 초점 거리에 비례시켜 근접 촬영에서도 캐릭터 초점을 유지한다.
    float Coc(float d)
    {
        float edge = _Focus.x - min(_Focus.y * 1.4, _Focus.x * 0.55);
        return max(Back(d), (1.0 - smoothstep(edge * 0.45, edge, d)) * 0.9);
    }

    // 패널 뒤 배경에는 깊이와 관계없이 흐림을 적용한다.
    float PanelMask(float2 uv, float isCharacter)
    {
        return step(0.5, _Panel.x) * saturate((_Panel.y - uv.x) / max(_Panel.z, 0.0001)) * (1.0 - isCharacter);
    }

    float Luma(float3 c)
    {
        return dot(c, float3(0.299, 0.587, 0.114));
    }

    float4 Frag(VaryingsDefault i) : SV_Target
    {
        float2 uv = i.texcoord;
        float2 aspect = float2(_Focus.w, 1.0);
        float2 depth0 = SampleDepth(uv);
        float d = depth0.x;
        float c0 = Coc(d);
        float3 source = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0).rgb;
        float3 col = source;
        float protectDistance = length((uv - _Protect.xy) * aspect);
        float protect = _Protect.w * (1.0 - smoothstep(_Protect.z * 0.55, _Protect.z, protectDistance));

        // 초점 흐림과 패널 흐림을 분산 합으로 결합한다.
        float panel0 = PanelMask(uv, depth0.y);
        float blurry0 = max(c0, panel0);
        float focusRadius = _Focus.z * c0 * (1.0 - 0.75 * protect);
        float panelRadius = _Panel.x * panel0 * (1.0 - protect);
        float radius = sqrt(focusRadius * focusRadius + panelRadius * panelRadius);

        if (radius > 0.5)
        {
            float3 sum = col;
            float weightSum = 1.0;

            for (int k = 0; k < 40; k++)
            {
                float fi = float(k) + 0.5;
                float r = sqrt(fi / 40.0);
                float a = fi * 2.39996323;
                float2 tap = uv + float2(cos(a), sin(a)) * r * radius * _MainTex_TexelSize.xy;
                // 초점 영역의 색이 배경으로 번지지 않도록 가중치를 낮춘다.
                float2 depthTap = SampleDepth(tap);
                float blurryTap = max(Coc(depthTap.x), PanelMask(tap, depthTap.y));
                float w = smoothstep(0.0, 0.4, blurryTap / max(blurry0, 0.001));
                sum += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, tap, 0).rgb * w;
                weightSum += w;
            }

            col = sum / weightSum;
        }

        if (_Impact.y > 0.001)
        {
            float3 zoom = 0.0;
            float2 dir = (_Center.xy - uv) * _Impact.y;
            for (int k = 0; k < 12; k++)
            {
                zoom += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv + dir * (float(k) / 11.0), 0).rgb;
            }

            col = lerp(col, zoom / 12.0, 0.8);
        }

        float away = length((uv - _Center.xy) * aspect);
        float m = max(Back(d), _Center.z * smoothstep(0.16, 0.6, away)) * (1.0 - protect);
        col = lerp(Luma(col).xxx, col, lerp(1.0, _Grade.y, m));
        col = lerp(col, col * _Tint.rgb * (1.0 - _Grade.x), m);
        float2 q = uv - 0.5;
        col *= 1.0 - _Grade.z * smoothstep(0.3, 0.95, length(q * float2(1.1, 1.0)));

        // 근접 촬영에서 얼굴색을 유지하도록 몸통 주변은 테마 색 보정에서 제외한다.
        float mTheme = max(Back(d), _Center.z * smoothstep(max(0.16, _Center.w * 0.8), max(0.6, _Center.w * 1.5), away))
            * (1.0 - protect);
        col *= lerp(1.0, _ThemeTint.rgb, mTheme * _ThemeAmount.x);
        float edge = smoothstep(0.38, 1.02, length(q * float2(1.15, 1.0)));
        col = lerp(col, col * _ThemeTint.rgb * _ThemeTint.rgb + _ThemeLight * 0.1, edge * _ThemeAmount.y);

        float ca = _Impact.x * 0.014;
        if (ca > 0.0001)
        {
            float keep = 1.0 - c0;
            col.r += (SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv + q * ca, 0).r - source.r) * keep;
            col.b += (SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv - q * ca, 0).b - source.b) * keep;
        }

        col = col * (1.0 + _Impact.x * 0.45) + _Impact.x * 0.09;

        if (_Impact.z > 0.001)
        {
            float ink = smoothstep(0.26, 0.4, Luma(col));
            col = lerp(col, (lerp(ink, 1.0 - ink, _Impact.w) * 1.05).xxx, _Impact.z);
        }

        return float4(max(col, 0.0), 1.0);
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
