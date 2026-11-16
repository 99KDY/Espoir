// CSS 상자 하나를 그린다: 둥근 모서리 · 배경색/선형 그라데이션 · 테두리(실선/점선/위쪽만) · 바깥 box-shadow 1개 · inset box-shadow 1개 ·
// backdrop-filter blur · 훑는 빛(linear-gradient 115deg 띠) 모드. 값은 CHudBox 가 정점 데이터로 넘긴다(같은 재질로 묶어 그림).
// 색 합성은 CSS 처럼 sRGB 공간에서 하고 마지막에 선형으로 바꿔 내보낸다(프리멀티플라이드).
Shader "Espoir/UI/HudBox"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" =
            "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "HudCommon.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
                float4 uv3 : TEXCOORD3;
                float4 tangent : TANGENT;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float4 local : TEXCOORD0;
                nointerpolation float4 shape : TEXCOORD1;
                nointerpolation float4 colA : TEXCOORD2;
                nointerpolation float4 colB : TEXCOORD3;
                nointerpolation float4 extra : TEXCOORD4;
                nointerpolation float3 flags : TEXCOORD5;
                float4 grab : TEXCOORD6;
                float4 world : TEXCOORD7;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _HudSceneTex;
            float4 _HudSceneTex_TexelSize;
            float4 _ClipRect;
            float _HudBackdropOn;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.local = v.uv0;
                o.shape = v.uv1;
                o.colA = v.uv2;
                o.colB = v.uv3;
                o.extra = v.tangent;
                o.flags = v.normal;
                o.grab = ComputeGrabScreenPos(o.pos);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 p = i.local.xy;
                float2 hs = i.local.zw;
                float radius = min(i.shape.x, min(hs.x, hs.y));
                float bw = i.shape.y;
                float shadowBlur = i.shape.z;
                float insetBlur = i.shape.w;
                float4 fillA = HudUnpack(i.colA.x, i.colA.y);
                float4 fillB = HudUnpack(i.colA.z, i.colA.w);
                float4 border = HudUnpack(i.colB.x, i.colB.y);
                float4 shadow = HudUnpack(i.colB.z, i.colB.w);
                float4 inset = HudUnpack(i.extra.x, i.extra.y);
                float shadowOffsetY = i.extra.z;
                float backdrop = i.extra.w;
                float gradAngle = i.flags.x;
                int flags = (int)round(i.flags.y);
                float param = i.flags.z;
                bool dashed = (flags & 1) != 0;
                bool topOnly = (flags & 2) != 0;
                bool sheen = (flags & 4) != 0;

                float d = HudRoundRect(p, hs, radius);
                float fw = max(fwidth(d), 1e-4);
                float cov = saturate(0.5 - d / fw);

                // 안쪽(패딩 박스) — 테두리 두께만큼 줄인 상자
                float2 innerHs = hs - (topOnly ? float2(0.0, bw * 0.5) : bw.xx);
                float2 innerP = topOnly ? p + float2(0.0, bw * 0.5) : p;
                float dIn = HudRoundRect(innerP, innerHs, max(radius - bw, 0.0));
                float covIn = bw > 0.0 ? saturate(0.5 - dIn / fw) : cov;

                float4 res = 0;
                if (sheen)
                {
                    // linear-gradient(115deg, transparent 38%, rgba(255,255,255,.6) 50%, transparent 62%) 를 폭의 param
                    // 배만큼 옮김
                    float2 q = p - float2(param * hs.x * 2.0, 0.0);
                    float t = HudGradientT(q, hs, 115.0);
                    float a = 0.6 * saturate(1.0 - abs(t - 0.5) / 0.12);
                    res = float4(1, 1, 1, 1) * a * covIn;
                }
                else
                {
                    float4 inside = 0;
                    if (backdrop > 0.0 && _HudBackdropOn > 0.5)
                    {
                        float2 uv = i.grab.xy / i.grab.w;
                        float pxScale = 1.0 / max(fwidth(p.x), 1e-4);     // 캔버스 1px 가 화면 몇 px 인지
                        // 전역 sRGB 합성 구간 안이면 잡은 화면이 sRGB 숫자 → sRGB 에서 흐림(크롬 backdrop-filter 와 같은 공간)
                        float3 bg = HudBlurScene(_HudSceneTex, _HudSceneTex_TexelSize, uv, backdrop * pxScale);
                        inside = float4(HudGrabToSrgb(bg), 1.0);
                    }

                    float t = gradAngle >= 0.0 ? HudGradientT(p, hs, gradAngle) : 0.0;
                    float4 fill = lerp(fillA, fillB, saturate(t));
                    inside = HudOver(float4(fill.rgb * fill.a, fill.a), inside);

                    if (inset.a > 0.0)
                    {
                        float sigma = max(insetBlur * 0.5, 1e-3);
                        float ia = inset.a * HudShadowEdge(-dIn, sigma) * covIn;
                        inside = HudOver(float4(inset.rgb * ia, ia), inside);
                    }

                    if (bw > 0.0)
                    {
                        float bm = saturate(cov - covIn);
                        if (topOnly) bm *= step(0.0, p.y);
                        if (dashed)
                        {
                            // 1px 점선: 3px 칠하고 3px 비움 (변을 따라)
                            float2 ap = abs(p);
                            float along = (hs.x - ap.x < hs.y - ap.y) ? p.y : p.x;
                            bm *= step(fmod(along + 1000.5, 6.0), 3.0);
                        }
                        float ba = border.a * bm;
                        inside = HudOver(float4(border.rgb * ba, ba), inside);
                    }

                    res = inside * cov;
                    if (shadow.a > 0.0)
                    {
                        float sigma = max(shadowBlur * 0.5, 1e-3);
                        float2 sq = p - float2(0.0, shadowOffsetY);
                        float sc;
                        if (radius < 0.5)
                        {
                            // 각진 상자: CSS 가우스 그림자를 정확히(가로 · 세로 erf 곱). 1px 선 같은 얇은 상자에서 반평면 근사는 빛이 너무 셈
                            float k = 0.70710678 / sigma;
                            sc = 0.25 * (HudErf((sq.x + hs.x) * k) - HudErf((sq.x - hs.x) * k)) *
                                (HudErf((sq.y + hs.y) * k) - HudErf((sq.y - hs.y) * k));
                        }
                        else
                        {
                            sc = HudShadowEdge(HudRoundRect(sq, hs, radius), sigma);
                        }
                        float sa = shadow.a * sc * (1.0 - cov);
                        res += float4(shadow.rgb * sa, sa);
                    }
                }

                res *= i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                res *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(res.a - 0.001);
                #endif
                return HudOut(res);
            }
            ENDCG
        }
    }
}
