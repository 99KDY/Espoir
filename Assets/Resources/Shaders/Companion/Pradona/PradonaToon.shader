Shader "Espoir/PradonaToon"
{
    Properties
    {
        [Header(Base)]
        [NoScaleOffset] _MainTex ("Albedo", 2D) = "white" {}
        [Enum(Face,1,Hair,2,Cloth,3)] _Part ("Part", Float) = 3

        [Header(Ramp)]
        [NoScaleOffset] _RampSkin ("Skin Ramp", 2D) = "white" {}
        [NoScaleOffset] _RampCloth ("Cloth Ramp", 2D) = "white" {}
        [NoScaleOffset] _RampHair ("Hair Ramp", 2D) = "white" {}

        [Header(Shade Map)]
        [ToggleUI] _UseShadeMap ("Use Shade Map", Float) = 0
        [NoScaleOffset] _ShadeMap ("Shade Map", 2D) = "white" {}

        [Header(Outline)]
        _OutlineWidth ("Width (px)", Range(0, 5)) = 1
        _OutlineZ ("Depth Offset", Float) = 0.0004
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    sampler2D _RampSkin, _RampCloth, _RampHair;
    sampler2D _ShadeMap;
    float _Part, _UseShadeMap;
    float _OutlineWidth, _OutlineZ;

    // CToonHeadTracker에서 설정하는 머리 좌표.
    float4 _ToonHeadCenter, _ToonHeadUp;
    float _ToonHeadRadius;

    // xyz: 광원 방향, w: 방향 재정의 여부. 0이면 씬의 주광원을 사용한다.
    float4 _ToonLightDir;

    static const float _SOFT_NORMAL = 0.55;
    static const float _AO = 0.3;
    static const float _CLOTH_HUE_KEEP = 0.45;
    static const float _HAIR_HUE_KEEP = 0.85;
    static const float3 _AMBIENT = float3(0.254152, 0.278894, 0.479320);
    static const float _AMBIENT_STRENGTH = 0.06;

    static const float _FACE_NORMAL_SMOOTH = 0.82;
    static const float _FACE_WRAP = 0.1;
    static const float _FACE_EDGE = 0.05;
    static const float3 _SKIN_REF = float3(0.968627, 0.776471, 0.788235);    // sRGB 값
    static const float _NECK_SHADE = 0.5;

    static const float _SHADE_HUE = 0.5;

    static const float _HAIR_SPEC_POW = 110;
    static const float _HAIR_SPEC_CUT = 0.72;
    static const float2 _HAIR_STRAND = float2(0.85, 1.35);
    static const float3 _HAIR_SPEC_COLOR = float3(1.0, 0.854993, 0.896269);
    static const float _HAIR_LUMA = 0.480649;                                 // 머리 텍스처 평균 밝기
    static const float _HAIR_RING_POS = 0.54;
    static const float _HAIR_RING_WIDTH = 0.07;
    static const float _HAIR_RING_CURVE = 0.35;

    static const float _RIM = 0.04;
    static const float _RIM_WIDTH = 0.7;
    static const float _RIM_SOFT = 0.01;
    static const float3 _RIM_COLOR = float3(1.0, 0.879622, 0.887923);
    static const float _RIM_FACE = 0.15;

    static const float _OUTLINE_DARK = 0.32;

    // CEnvironmentGradeMask로 맵 색 보정을 제외하므로 캐릭터 색 보정은 여기서 적용한다.
    static const float _GRADE_SATURATION = 0.97;
    static const float _GRADE_CONTRAST = 1.02;
    static const float _GRADE_TONE = 0.12;
    static const float3 _GRADE_SHADOW = float3(0.1441, 0.1946, 0.4342);
    static const float3 _GRADE_HIGHLIGHT = float3(1.0, 0.807, 0.5776);

    float Luma(float3 c)
    {
        return dot(c, float3(0.2126, 0.7152, 0.0722));
    }

    float3 Grade(float3 c)
    {
        c = c / (1.0 + max(c - 0.85, 0.0) * 0.9);
        float l = dot(c, float3(0.299, 0.587, 0.114));
        c = lerp(l.xxx, c, _GRADE_SATURATION);
        c = (c - 0.5) * _GRADE_CONTRAST + 0.5;

        c += (_GRADE_SHADOW - 0.5) * (1.0 - smoothstep(0.0, 0.55, l)) * _GRADE_TONE;
        c += (_GRADE_HIGHLIGHT - 0.5) * smoothstep(0.45, 1.0, l) * _GRADE_TONE;
        return max(c, 0.0);
    }
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }
            Cull Off

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fwdbase
            #pragma target 3.0
            #include "Lighting.cginc"

            struct SAttributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;   // xyz = 다듬은 노멀
                float4 color : COLOR;       // r = AO
                float2 uv : TEXCOORD0;
            };
            struct SVaryings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 softNormal : TEXCOORD3;
                float ao : TEXCOORD4;
            };

            SVaryings Vert(SAttributes v)
            {
                SVaryings o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.softNormal = UnityObjectToWorldNormal(v.tangent.xyz);
                o.ao = v.color.r;
                return o;
            }

            // 옷 텍스처의 피부 영역을 기준 피부색과의 거리로 구분한다(sRGB).
            float SkinMask(float3 lin)
            {
                float3 c = pow(max(lin, 0), 1.0 / 2.2);
                float mx = max(max(c.r, c.g), c.b);
                float mn = min(min(c.r, c.g), c.b);
                float sat = (mx - mn) / max(mx, 1e-4);
                float redMax = step(c.g, c.r) * step(c.b, c.r);
                float near = 1.0 - smoothstep(0.2, 0.3, length(c - _SKIN_REF));
                // 청색 성분이 강한 프릴은 피부 영역에서 제외한다.
                float warm = smoothstep(-0.01, 0.01, c.g - c.b);
                return warm * smoothstep(0.07, 0.11, sat) * (1.0 - smoothstep(0.42, 0.5, sat)) *
                    smoothstep(0.6, 0.7, mx) * redMax * near;
            }

            // part: 0 피부, 1 얼굴, 2 머리카락, 3 옷. x = 빛 받는 정도
            float3 Ramp(int part, float x)
            {
                float4 uv = float4(x, 0.5, 0, 0);
                if (part <= 1) return tex2Dlod(_RampSkin, uv).rgb;
                if (part == 2) return tex2Dlod(_RampHair, uv).rgb;
                return tex2Dlod(_RampCloth, uv).rgb;
            }

            float3 KeepHue(float3 shaded, float3 albedo, float3 ramp, float satMax, float amount)
            {
                float rl = Luma(ramp);
                float satBoost = lerp(1.0, satMax, saturate((1.0 - rl) * 2.0));
                float3 keep = max(lerp(Luma(albedo).xxx, albedo, satBoost), 0) * rl;
                return lerp(shaded, keep, amount);
            }

            float4 Frag(SVaryings i, float facing : VFACE) : SV_Target
            {
                float3 albedo = tex2D(_MainTex, i.uv).rgb;
                float3 shadeAlbedo = tex2D(_ShadeMap, i.uv).rgb;
                float la = Luma(albedo);
                int part = (int)(_Part + 0.5);

                float3 up = _ToonHeadUp.xyz;
                float3 hp = i.worldPos - _ToonHeadCenter.xyz;
                float radius = max(_ToonHeadRadius, 1e-3);
                float headV = dot(hp, up) / radius;

                // 옷 재질에 포함된 목에는 피부용 음영을 적용한다.
                float neckW = 0;
                if (part != 2)
                {
                    float rq = length(hp - up * dot(hp, up)) / radius;
                    float mx = max(max(albedo.r, albedo.g), albedo.b);
                    neckW = (1.0 - smoothstep(0.5, 0.68, rq)) * smoothstep(-0.5, -0.65, headV) *
                        (1.0 - smoothstep(1.9, 2.2, -headV)) * smoothstep(0.25, 0.4, mx);
                }
                if (part == 3 && neckW < 0.05 && SkinMask(albedo) > 0.5) part = 0;

                float3 N = normalize(i.worldNormal);
                float3 SN = normalize(i.softNormal);
                if (facing < 0)
                {
                    N = -N;
                    SN = -SN;
                }
                // 얼굴 노멀을 구형으로 보간해 코와 입 주변의 음영 경계를 완화한다.
                if (part == 1) N = normalize(lerp(SN, normalize(hp + 1e-5), _FACE_NORMAL_SMOOTH));
                else N = normalize(lerp(N, SN, _SOFT_NORMAL));
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 L = normalize(lerp(_WorldSpaceLightPos0.xyz, _ToonLightDir.xyz, _ToonLightDir.w));

                float lf = dot(N, L) * 0.5 + 0.5;
                if (part == 1) lf = max(saturate(lf + _FACE_WRAP), 0.3);

                float3 ramp = Ramp(part, clamp(lf, 0.004, 0.996));
                // 얼굴의 램프 경계만 여러 번 샘플링해 부드럽게 만든다.
                if (part == 1)
                {
                    ramp = (Ramp(1, clamp(lf - _FACE_EDGE, 0.004, 0.996))
                        + Ramp(1, clamp(lf - _FACE_EDGE * 0.5, 0.004, 0.996))
                        + ramp
                        + Ramp(1, clamp(lf + _FACE_EDGE * 0.5, 0.004, 0.996))
                        + Ramp(1, clamp(lf + _FACE_EDGE, 0.004, 0.996))) / 5.0;
                }
                if (neckW > 0)
                {
                    float3 neckRamp = lerp(Ramp(0, 0.9), Ramp(0, 0.3), _NECK_SHADE);
                    ramp = lerp(ramp, neckRamp, neckW);
                    lf = lerp(lf, min(lf, 0.3), neckW);
                }

                // 버텍스 AO로 음영을 보정하되 얼굴과 목의 적용 강도는 낮춘다.
                float occl = (1.0 - smoothstep(0.45, 0.85, i.ao)) * _AO * (part == 1 ? 0.35 : 1.0);
                float3 rampShadow = lerp(Ramp(part, 0.3), Ramp(0, 0.3), neckW);
                occl *= 1.0 - 0.65 * neckW;
                ramp = lerp(ramp, min(ramp, rampShadow), occl);
                lf = lerp(lf, min(lf, 0.32), occl);
                float litMask = smoothstep(0.38, 0.6, lf);

                float3 shaded = albedo * ramp;
                if (part == 3) shaded = KeepHue(shaded, albedo, ramp, 1.25, _CLOTH_HUE_KEEP * (1.0 - neckW));
                if (part == 2) shaded = KeepHue(shaded, albedo, ramp, 1.12, _HAIR_HUE_KEEP);

                // 음영 텍스처가 있으면 램프 색조와 함께 적용한다.
                if (_UseShadeMap > 0.5)
                {
                    float3 rampLit = Ramp(part, 0.996);
                    float3 rampDark = Ramp(part, 0.3);
                    float litW = saturate((Luma(ramp) - Luma(rampDark)) /
                        max(Luma(rampLit) - Luma(rampDark), 1e-3));
                    float3 hue = rampDark / max(max(max(rampDark.r, rampDark.g), rampDark.b), 1e-3);
                    float3 shadeColor = shadeAlbedo * lerp(1.0.xxx, hue, _SHADE_HUE);
                    shaded = lerp(shadeColor, albedo * rampLit, litW);
                }
                float3 col = shaded + albedo * _AMBIENT * _AMBIENT_STRENGTH;

                if (part == 2)
                {
                    // 머리카락 접선으로 이방성 하이라이트를 계산한다.
                    float3 H = normalize(L + V);
                    float3 T = normalize(up - N * dot(N, up) + 1e-5);
                    float th = dot(T, H);
                    float kk = pow(sqrt(max(1.0 - th * th, 0.0)), _HAIR_SPEC_POW);
                    float strand = smoothstep(_HAIR_STRAND.x, _HAIR_STRAND.y, la / _HAIR_LUMA);
                    float spec = smoothstep(_HAIR_SPEC_CUT, _HAIR_SPEC_CUT + 0.04, kk) * strand * litMask;

                    // 머리 중심의 구형 노멀로 정수리 하이라이트 띠를 계산한다.
                    float3 Ns = normalize(hp + 1e-5);
                    float3 vs = normalize(mul((float3x3)UNITY_MATRIX_V, Ns));
                    float headZone = (1.0 - smoothstep(2.4, 3.0, length(hp) / radius)) *
                        smoothstep(-0.6, 0.0, headV);
                    float ax = abs(vs.x);
                    float w = _HAIR_RING_WIDTH * (1.0 - 0.65 * ax);
                    float c = _HAIR_RING_POS - _HAIR_RING_CURVE * ax * ax;
                    float e = 0.55 * w;
                    float band = smoothstep(c - w, c - w + e, vs.y) * (1.0 - smoothstep(c + w - e, c + w, vs.y)) *
                        (1.0 - smoothstep(0.6, 0.9, ax));
                    float gap = smoothstep(0.7, 1.0, la / _HAIR_LUMA) * smoothstep(0.45, 0.75, dot(N, Ns));
                    float ring = band * gap * headZone * smoothstep(0.05, 0.3, vs.z) * lerp(0.55, 1.0, litMask);

                    col = lerp(col, _HAIR_SPEC_COLOR, saturate(max(spec * (1.0 - headZone), ring)));
                }

                float fres = 1.0 - max(dot(N, V), 0);
                float rim = smoothstep(_RIM_WIDTH, _RIM_WIDTH + _RIM_SOFT, fres) * (0.3 + 0.7 * litMask);
                col += rim * _RIM_COLOR * _RIM * (part == 1 ? _RIM_FACE : 1.0) * lerp(1.0.xxx, albedo * 3.0, 0.4);

                return float4(Grade(col), 1);
            }
            ENDCG
        }

        // tangent에 저장된 보정 노멀로 픽셀 단위 외곽선을 확장한다.
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode"="Always" }
            Cull Front

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            struct SAttributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };
            struct SVaryings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            SVaryings Vert(SAttributes v)
            {
                SVaryings o;
                o.uv = v.uv;
                float4 clip = UnityObjectToClipPos(v.vertex);
                float3 n = length(v.tangent.xyz) > 0.01 ? v.tangent.xyz : v.normal;
                float3 vn = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, n));
                float2 dir = mul((float2x2)UNITY_MATRIX_P, vn.xy);
                float dl = length(dir);
                dir = dl > 1e-5 ? dir / dl : 0;
                float distScale = clamp(2.6 / clip.w, 0.75, 1.0);
                clip.xy += dir * _OutlineWidth * distScale * 2.0 / _ScreenParams.xy * clip.w;
                #if defined(UNITY_REVERSED_Z)
                    clip.z -= _OutlineZ * clip.w;
                #else
                    clip.z += _OutlineZ * clip.w;
                #endif
                o.pos = _OutlineWidth > 0.01 ? clip : float4(0, 0, -2, 1);
                return o;
            }

            float4 Frag(SVaryings i) : SV_Target
            {
                float3 a = tex2D(_MainTex, i.uv).rgb;
                return float4(Grade(pow(max(a, 0), 1.4) * _OUTLINE_DARK), 1);
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
    Fallback Off
}
