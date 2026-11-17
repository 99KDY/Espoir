// 배경, 그라데이션, 테두리, 그림자를 재질 설정에 따라 그립니다.
// 색 값은 sRGB 0~1 그대로(Vector) 받고, 요소 안의 겹침 · 그라데이션은 sRGB 에서 계산한 뒤 마지막에 선형으로 바꾼다(CSS 와 같은 공간).
// 정점 자료 (CCssBoxGraphic): uv0.xy = 상자 안 위치 0~1 (y 는 위에서 아래, 바깥 여백은 범위 밖), uv1.xy = 상자 크기(px)
//   uv2 · uv3 · normal · tangent = 요소마다 바뀌는 값 (모드별 의미는 아래 주석)
// 모드: 0 상자(배경 · 테두리 · 바깥 그림자 · 그림 + 그림 모양 빛) / 1 HP 막대 / 2 가로 가림 + 세로 그라데이션(베일 · 선) / 3 빛 알(더하기)
Shader "Espoir/UI/CssBox"
{
    Properties
    {
        [PerRendererData] _MainTex ("Image", 2D) = "white" {}
        _Mode ("Mode", Float) = 0

        // 공통: 요소 바깥 drop-shadow 두 겹 (rgba sRGB, σ px)
        _Glow1 ("Glow 1 rgba", Vector) = (0,0,0,0)
        _Glow2 ("Glow 2 rgba", Vector) = (0,0,0,0)
        _GlowLow1 ("Glow 1 low rgba", Vector) = (0,0,0,0)
        _GlowLow2 ("Glow 2 low rgba", Vector) = (0,0,0,0)
        _GlowSigma ("Glow σ (1, 2, low1, low2)", Vector) = (1,1,1,1)

        // 모드 0 상자
        _Background ("Background rgba", Vector) = (0,0,0,0)
        _BorderColor ("Border rgba", Vector) = (0,0,0,0)
        _BorderWidth ("Border px", Float) = 0
        _Radius ("Radius px", Float) = 0
        _BoxShadow ("Box shadow rgba", Vector) = (0,0,0,0)
        _BoxShadowGeom ("Box shadow (offX, offY, σ, spread)", Vector) = (0,0,0,0)
        _InsetShadow ("Inset shadow rgba", Vector) = (0,0,0,0)
        _InsetShadowGeom ("Inset shadow (offX, offY, σ, spread)", Vector) = (0,0,0,0)
        _ImageRect ("Image rect in box px (x, y, w, h)", Vector) = (0,0,0,0)
        _ImageGlow ("Image drop-shadow rgba", Vector) = (0,0,0,0)
        _ImageGlowSigma ("Image drop-shadow σ px", Float) = 0

        // 모드 1 막대
        _Track ("Track rgba", Vector) = (0,0,0,0)
        _Trail ("Trail rgba", Vector) = (0,0,0,0)
        _Stop0 ("Fill stop 0", Vector) = (1,1,1,1)
        _Stop1 ("Fill stop 1", Vector) = (1,1,1,1)
        _Stop2 ("Fill stop 2", Vector) = (1,1,1,1)
        _Stop3 ("Fill stop 3", Vector) = (1,1,1,1)
        _Stop4 ("Fill stop 4", Vector) = (1,1,1,1)
        _Gloss ("Gloss rgba (top) / end v", Vector) = (1,1,1,0.22)
        _GlossEnd ("Gloss end (0~1 of height)", Float) = 0.55
        _GradSize ("Fill gradient size (1 = bar width fixed, 3 = 300% of fill)", Float) = 1
        _GradAnim ("Gradient anim (period s, mode 0 none 1 ease-in-out alternate)", Vector) = (12,0,0,0)
        _Sheen ("Sheen rgba", Vector) = (1,1,1,0.3)
        _SheenGeom ("Sheen (angle deg, stop a, stop mid, stop b)", Vector) = (100,0.4,0.5,0.6)
        _SheenAnim ("Sheen anim (period, mode 0 boss 1 mob)", Vector) = (7,0,0,0)
        _Flash ("Hit strip rgba", Vector) = (1,0.4157,0.3137,0.7)
        _FlashCrit ("Hit strip crit rgba", Vector) = (1,0.7451,0.4314,1)
        _FlashWhite ("Fill white flash rgba", Vector) = (1,1,1,0.55)

        // 모드 2 가림 · 그라데이션
        _VeilColor ("Veil rgb", Vector) = (0,0,0,1)
        _VeilAlpha ("Veil alpha (top, mid, bottom, mid pos)", Vector) = (1,1,1,0.5)
        _MaskStops ("Horizontal mask (in end, out start)", Vector) = (0,1,0,0)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
        Blend [_SrcBlend] [_DstBlend]
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "UiSrgbComposite.cginc"
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
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 dynA : TEXCOORD2;
                float4 dynB : TEXCOORD3;
                float4 dynC : TEXCOORD4;
                float4 dynD : TEXCOORD5;
                float4 world : TEXCOORD6;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _ClipRect;
            float _Mode;
            float4 _Glow1, _Glow2, _GlowLow1, _GlowLow2, _GlowSigma;
            float4 _Background, _BorderColor, _BoxShadow, _BoxShadowGeom, _InsetShadow, _InsetShadowGeom,
                _ImageRect, _ImageGlow;
            float _BorderWidth, _Radius, _ImageGlowSigma;
            float4 _Track, _Trail, _Stop0, _Stop1, _Stop2, _Stop3, _Stop4, _Gloss, _GradAnim, _Sheen, _SheenGeom,
                _SheenAnim, _Flash, _FlashCrit, _FlashWhite;
            float _GlossEnd, _GradSize;
            float4 _VeilColor, _VeilAlpha, _MaskStops;

            v2f vert(appdata v)
            {
                v2f o;
                o.world = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv0 = v.uv0;
                o.uv1 = v.uv1;
                o.dynA = v.uv2;
                o.dynB = v.uv3;
                o.dynC = float4(v.normal, 0);
                o.dynD = v.tangent;
                return o;
            }

            // ── 도우미 ──
            float3 ToOutput(float3 srgb)
            {
            #ifdef UNITY_COLORSPACE_GAMMA
                return srgb;
            #else
                // 전역 sRGB 합성 구간 안(_UiGammaOut 1)이면 sRGB 숫자 그대로
                return _UiGammaOut > 0.5 ? saturate(srgb) : GammaToLinearSpace(saturate(srgb));
            #endif
            }

            // 미리곱 sRGB 겹치기: top 을 bottom 위에
            float4 Over(float4 top, float4 bottom)
            {
                return top + bottom * (1 - top.a);
            }

            float4 Premul(float4 c)
            {
                return float4(c.rgb * c.a, c.a);
            }

            // 화면 1px 크기를 반영한 구간 덮임 (가장자리 계단 없이)
            float Cover(float x, float a, float b, float px)
            {
                float h = 0.5 * px;
                return saturate((min(x + h, b) - max(x - h, a)) / px);
            }

            // erf 근사 (오차 1e-4 수준)
            float Erf(float x)
            {
                float x2 = x * x;
                float ax2 = 0.147 * x2;
                return sign(x) * sqrt(1 - exp(-x2 * (1.27323954 + ax2) / (1 + ax2)));
            }

            // [a, b] 구간 사각 알파를 표준편차 s 로 흐린 값
            float Blur1(float x, float a, float b, float s)
            {
                s = max(s, 0.0001) * 1.41421356;
                return 0.5 * (Erf((x - a) / s) - Erf((x - b) / s));
            }

            // cubic-bezier(x1, y1, x2, y2) 이징 (Newton 몇 번)
            float Bezier(float t, float4 c)
            {
                float u = t;
                for (int i = 0; i < 6; i++)
                {
                    float iu = 1 - u;
                    float x = 3 * iu * iu * u * c.x + 3 * iu * u * u * c.z + u * u * u - t;
                    float dx = 3 * iu * iu * c.x + 6 * iu * u * (c.z - c.x) + 3 * u * u * (1 - c.z);
                    u = saturate(u - x / max(dx, 1e-4));
                }
                float ju = 1 - u;
                return 3 * ju * ju * u * c.y + 3 * ju * u * u * c.w + u * u * u;
            }

            float4 Stops5(float g)
            {
                g = saturate(g) * 4;
                float4 c = lerp(_Stop0, _Stop1, saturate(g));
                c = lerp(c, _Stop2, saturate(g - 1));
                c = lerp(c, _Stop3, saturate(g - 2));
                c = lerp(c, _Stop4, saturate(g - 3));
                return c;
            }

            // 두 겹 drop-shadow: 두 번째 그림자는 (내용 + 첫 그림자) 의 흐림 — 첫 그림자 부분은 σ 합성으로 근사
            float4 DropShadows(float b1, float b2, float b12, float4 c1, float4 c2)
            {
                float4 s1 = Premul(float4(c1.rgb, c1.a * saturate(b1)));
                float a2 = saturate(b2 + c1.a * b12 * (1 - b2));
                float4 s2 = Premul(float4(c2.rgb, c2.a * a2));
                return Over(s1, s2);
            }

            // ── 모드 1: HP 막대 ──
            // dynA = (t, td, 번쩍 왼쪽, 번쩍 오른쪽) 막대 폭 비율 / dynB = (번쩍 투명도, 번쩍 밝기, 흰 번쩍, 치명 색 섞기)
            // dynC = (애니 위상 s, 빛 세기(0 이면 꺼짐), 낮음 섞기) / dynD = (자국 투명도 배수, 0, 0, 0)
            float4 BarColor(float2 p, float2 size, float px, float4 dA, float4 dB, float4 dC, float4 dD)
            {
                float W = size.x;
                float H = size.y;
                float fillW = dA.x * W;
                float trailW = dA.y * W;
                float time = _Time.y + dC.x;

                float inX = Cover(p.x, 0, W, px);
                float inY = Cover(p.y, 0, H, px);
                float inside = inX * inY;

                float4 col = Premul(float4(_Track.rgb, _Track.a * inside));

                float trailA = _Trail.a * dD.x;
                col = Over(Premul(float4(_Trail.rgb, trailA * Cover(p.x, 0, trailW, px) * inY)), col);

                float fillCover = Cover(p.x, 0, fillW, px) * inY;
                if (fillCover > 0)
                {
                    float g;
                    if (_GradSize > 1.5)
                    {
                        // 배경 크기 = 채움 폭의 300%, background-position p (0~1) → 보이는 좌표 (x/채움폭 + 2p) / 3
                        float k = frac(time / _GradAnim.x * 0.5) * 2;
                        float tri = k < 1 ? k : 2 - k;
                        float pos = _GradAnim.y > 0.5 ? Bezier(tri, float4(0.42, 0, 0.58, 1)) : 0;
                        g = (p.x / max(fillW, 0.001) + (_GradSize - 1) * pos) / _GradSize;
                    }
                    else
                    {
                        g = p.x / W;
                    }

                    float3 fill = Stops5(g).rgb;
                    float gA = _Gloss.a * saturate(1 - (p.y / H) / _GlossEnd);
                    fill = lerp(fill, _Gloss.rgb, gA);

                    // 지나가는 빛: 채움 폭의 300% 상자 안 100deg 그라데이션
                    float sp;
                    float sk = frac(time / _SheenAnim.x);
                    if (_SheenAnim.y < 0.5)
                    {
                        // 0%·55% = 130%, 92%·100% = -30%, 구간 ease-in-out
                        sp = sk < 0.55 ? 1.3 : sk < 0.92 ? lerp(1.3, -0.3,
                            Bezier((sk - 0.55) / 0.37, float4(0.42, 0, 0.58, 1))) : -0.3;
                    }
                    else
                    {
                        // 0% = 100%, 55%·100% = 0%, linear
                        sp = sk < 0.55 ? lerp(1, 0, sk / 0.55) : 0;
                    }

                    float iw = fillW * 3;
                    float left = (fillW - iw) * sp;
                    float ang = radians(_SheenGeom.x);
                    float2 dir = float2(sin(ang), -cos(ang));
                    float len = abs(iw * dir.x) + abs(H * dir.y);
                    float2 c = float2(left + iw * 0.5, H * 0.5);
                    float s = dot(p - c, dir) / max(len, 0.001) + 0.5;
                    float band = saturate((s - _SheenGeom.y) / (_SheenGeom.z - _SheenGeom.y)) * step(s, _SheenGeom.z)
                        + saturate((_SheenGeom.w - s) / (_SheenGeom.w - _SheenGeom.z)) * step(_SheenGeom.z, s);
                    fill = lerp(fill, _Sheen.rgb, _Sheen.a * band);
                    fill = lerp(fill, _FlashWhite.rgb, _FlashWhite.a * dB.z);

                    col = Over(float4(fill * fillCover, fillCover), col);
                }

                // 맞은 구간 번쩍
                float flashCover = Cover(p.x, dA.z * W, dA.w * W, px) * inY * dB.x;
                if (flashCover > 0)
                {
                    float4 fc = lerp(_Flash, _FlashCrit, dB.w);
                    fc.rgb = saturate(fc.rgb * dB.y);
                    col = Over(Premul(float4(fc.rgb, fc.a * flashCover)), col);
                }

                // 요소 전체 빛 번짐 (내용 알파의 가우스 흐림 — 사각 구간 합)
                float glowK = dC.y;
                if (glowK > 0)
                {
                    float low = saturate(dC.z);
                    float s1 = lerp(_GlowSigma.x, _GlowSigma.z, low);
                    float s2 = lerp(_GlowSigma.y, _GlowSigma.w, low);
                    float s12 = sqrt(s1 * s1 + s2 * s2);
                    float aT = _Track.a;
                    float aU = trailA * (1 - aT);
                    float aF = 1 - aT - aU;
                    float trailEnd = max(trailW, fillW);
                    float b1 = Blur1(p.y, 0, H, s1) *
                        (aT * Blur1(p.x, 0, W, s1) + aU * Blur1(p.x, 0, trailEnd, s1) + aF * Blur1(p.x, 0, fillW, s1));
                    float b2 = Blur1(p.y, 0, H, s2) *
                        (aT * Blur1(p.x, 0, W, s2) + aU * Blur1(p.x, 0, trailEnd, s2) + aF * Blur1(p.x, 0, fillW, s2));
                    float b12 = Blur1(p.y, 0, H, s12) *
                        (aT * Blur1(p.x, 0, W, s12) + aU * Blur1(p.x, 0, trailEnd, s12) + aF *
                        Blur1(p.x, 0, fillW, s12));
                    float4 g1 = lerp(_Glow1, _GlowLow1, low);
                    float4 g2 = lerp(_Glow2, _GlowLow2, low);
                    g1.a *= glowK;
                    g2.a *= glowK;
                    float4 under = DropShadows(b1, b2, b12, g1, g2);

                    // 막대 상자 그림자(box-shadow): 상자 밖에만
                    if (_BoxShadow.a > 0)
                    {
                        float bs = _BoxShadowGeom.z;
                        float sh = Blur1(p.x - _BoxShadowGeom.x, 0, W, bs) *
                            Blur1(p.y - _BoxShadowGeom.y, 0, H, bs) * (1 - inside);
                        under = Over(Premul(float4(_BoxShadow.rgb, _BoxShadow.a * sh)), under);
                    }

                    col = Over(col, under);
                }
                else if (_BoxShadow.a > 0)
                {
                    float bs = _BoxShadowGeom.z;
                    float sh = Blur1(p.x - _BoxShadowGeom.x, 0, W, bs) * Blur1(p.y - _BoxShadowGeom.y, 0, H, bs) *
                        (1 - inside);
                    col = Over(col, Premul(float4(_BoxShadow.rgb, _BoxShadow.a * sh)));
                }

                return col;
            }

            // ── 모드 0: 상자 ──
            // dynA = (그림 빛 세기, grayscale, brightness - 1, 바깥 그림자 세기) — 모두 0 이면 기본(필터 없음)
            float4 BoxColor(float2 p, float2 size, float px, float4 dA)
            {
                float W = size.x;
                float H = size.y;
                float inside = Cover(p.x, 0, W, px) * Cover(p.y, 0, H, px);

                float4 col = Premul(float4(_Background.rgb, _Background.a * inside));

                float bw = _BorderWidth;
                if (bw > 0)
                {
                    float inner = Cover(p.x, bw, W - bw, px) * Cover(p.y, bw, H - bw, px);
                    float ring = saturate(inside - inner);
                    col = Over(Premul(float4(_BorderColor.rgb, _BorderColor.a * ring)), col);
                }

                if (_InsetShadow.a > 0)
                {
                    float insetSigma = _InsetShadowGeom.z;
                    float spread = _InsetShadowGeom.w;
                    float hole = Blur1(p.x - _InsetShadowGeom.x, spread, W - spread, insetSigma)
                        * Blur1(p.y - _InsetShadowGeom.y, spread, H - spread, insetSigma);
                    col = Over(Premul(float4(_InsetShadow.rgb, _InsetShadow.a * (1 - hole) * inside)), col);
                }

                if (_ImageRect.z > 0)
                {
                    float2 iuv = (p - _ImageRect.xy) / _ImageRect.zw;
                    float2 tuv = float2(iuv.x, 1 - iuv.y);
                    float4 img = tex2D(_MainTex, tuv);
                    img.a *= Cover(p.x, _ImageRect.x, _ImageRect.x + _ImageRect.z, px) *
                        Cover(p.y, _ImageRect.y, _ImageRect.y + _ImageRect.w, px);

                    if (_ImageGlow.a > 0 && dA.x > 0)
                    {
                        // 그림 알파의 가우스 흐림 (9×9 표본, 간격 0.75σ)
                        float s = _ImageGlowSigma;
                        float2 texPerPx = float2(1.0 / _ImageRect.z, 1.0 / _ImageRect.w);
                        float lod = log2(max(1.0, s * 0.75 * _MainTex_TexelSize.z / _ImageRect.z));
                        float sum = 0;
                        float wsum = 0;
                        for (int j = -4; j <= 4; j++)
                        {
                            for (int i = -4; i <= 4; i++)
                            {
                                float2 o = float2(i, j) * (0.75 * s);
                                float w = exp(-dot(o, o) / (2 * s * s));
                                float2 q = iuv + o * texPerPx;
                                float inQ = step(0, q.x) * step(q.x, 1) * step(0, q.y) * step(q.y, 1);
                                sum += tex2Dlod(_MainTex, float4(q.x, 1 - q.y, 0, lod)).a * inQ * w;
                                wsum += w;
                            }
                        }

                        float ga = sum / wsum;
                        col = Over(Premul(float4(_ImageGlow.rgb, _ImageGlow.a * ga * dA.x)), col);
                    }

                    col = Over(Premul(img), col);
                }

                if (_BoxShadow.a > 0 && dA.w > 0)
                {
                    float bs = _BoxShadowGeom.z;
                    float sh = Blur1(p.x - _BoxShadowGeom.x, 0, W, bs) * Blur1(p.y - _BoxShadowGeom.y, 0, H, bs) *
                        (1 - inside);
                    col = Over(col, Premul(float4(_BoxShadow.rgb, _BoxShadow.a * dA.w * sh)));
                }

                // filter: grayscale · brightness
                if (col.a > 0)
                {
                    float3 rgb = col.rgb / col.a;
                    float luma = dot(rgb, float3(0.2126, 0.7152, 0.0722));
                    rgb = lerp(rgb, luma.xxx, dA.y);
                    rgb = saturate(rgb * (1 + dA.z));
                    col.rgb = rgb * col.a;
                }

                return col;
            }

            // ── 모드 2: 세로 그라데이션 × 가로 가림 ──
            float4 VeilColor(float2 uv)
            {
                float a = uv.y < _VeilAlpha.w
                    ? lerp(_VeilAlpha.x, _VeilAlpha.y, uv.y / _VeilAlpha.w)
                    : lerp(_VeilAlpha.y, _VeilAlpha.z, (uv.y - _VeilAlpha.w) / (1 - _VeilAlpha.w));
                float m = uv.x < _MaskStops.x ? uv.x / _MaskStops.x : uv.x > _MaskStops.y ? (1 - uv.x) / (1 -
                    _MaskStops.y) : 1;
                float inside = step(0, uv.x) * step(uv.x, 1) * step(0, uv.y) * step(uv.y, 1);
                return Premul(float4(_VeilColor.rgb, saturate(a) * saturate(m) * inside));
            }

            // ── 모드 3: 빛 알 (더하기) — dynA = (r, g, b HDR, 알파), dynB.x = 0 원광 / 1 불티 ──
            float4 GlowDot(float2 uv, float4 dA, float4 dB)
            {
                float2 q = uv * 2 - 1;
                float d = length(q);
                float3 c;
                float a;
                if (dB.x < 0.5)
                {
                    a = exp(-d * d * 3.2) * dA.w;
                    c = dA.rgb * a;
                }
                else
                {
                    float core = exp(-d * d * 9);
                    float glow = exp(-d * 3);
                    a = (core + glow * 0.35) * dA.w;
                    c = dA.rgb * (core * 1.6 + glow * 0.5) * a;
                }

                return float4(c, a * 0.6);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 size = i.uv1.xy;
                float2 p = i.uv0.xy * size;
                float px = max(length(float2(ddx(p.x), ddy(p.x))), 0.0001);
                float4 col;

                if (_Mode < 0.5)
                {
                    col = BoxColor(p, size, px, i.dynA);
                }
                else if (_Mode < 1.5)
                {
                    col = BarColor(p, size, px, i.dynA, i.dynB, i.dynC, i.dynD);
                }
                else if (_Mode < 2.5)
                {
                    col = VeilColor(i.uv0.xy);
                }
                else
                {
                    col = GlowDot(i.uv0.xy, i.dynA, i.dynB);
                    col.rgb = ToOutput(saturate(col.rgb)) * i.color.a;
                    col.a *= i.color.a;
                    return col;
                }

                // 미리곱 sRGB → 선형 미리곱 출력, 요소 투명도(vertex alpha)
                float a = col.a * i.color.a;
                float3 rgb = col.a > 0 ? col.rgb / col.a : 0;
                col = float4(ToOutput(rgb) * a, a);

            #ifdef UNITY_UI_CLIP_RECT
                col *= UnityGet2DClipping(i.world.xy, _ClipRect);
            #endif

            #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
            #endif

                return col;
            }
            ENDCG
        }
    }
}
