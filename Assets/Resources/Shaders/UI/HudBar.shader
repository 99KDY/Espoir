// HP·MP 막대의 트랙, 채움 그라데이션, 광택, 감소 잔상을 그립니다.
// 궁 상태 채움 filter(saturate · brightness · 진홍 drop-shadow) · 막대 묶음 drop-shadow 두 겹. 값은 CHudBar 가 재질 속성으로 넣는다.
Shader "Espoir/UI/HudBar"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _BarSize ("Size (w, h, slant)", Vector) = (532, 14, 11, 0)
        _Ratio ("Fill · Lag ratio", Vector) = (1, 1, 0, 0)
        _Track ("Track", Vector) = (0.3059, 0.1137, 0.1294, 0.59)
        _Stop0 ("Stop 0", Vector) = (1, 1, 1, 1)
        _Stop1 ("Stop 1", Vector) = (1, 1, 1, 1)
        _Stop2 ("Stop 2", Vector) = (1, 1, 1, 1)
        _Stop3 ("Stop 3", Vector) = (1, 1, 1, 1)
        _Stop4 ("Stop 4", Vector) = (1, 1, 1, 1)
        _Anim ("Aurora pos, Sheen pos, Gloss a, Sheen a", Vector) = (0, 1.3, 0.22, 0.3)
        _Lag ("Lag color", Vector) = (0.9137, 0.6392, 0.6784, 1)
        _LagFx ("Lag alpha, Lag brightness", Vector) = (0, 1, 0, 0)
        _Shadow1 ("Shadow 1", Vector) = (1, 0.4157, 0.3137, 0.5)
        _Shadow2 ("Shadow 2", Vector) = (1, 0.5412, 0.2902, 0.38)
        _ShadowBlur ("Shadow blur (1, 2)", Vector) = (3, 11, 0, 0)
        _Crimson ("Crimson color", Vector) = (1, 0.2392, 0.3216, 1)
        _CrimsonW ("Crimson weight", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
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
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "HudCommon.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv0 : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 local : TEXCOORD0;
                float4 world : TEXCOORD1;
            };

            float4 _BarSize, _Ratio, _Track, _Stop0, _Stop1, _Stop2, _Stop3, _Stop4, _Anim, _Lag, _LagFx;
            float4 _Shadow1, _Shadow2, _ShadowBlur, _Crimson;
            float _CrimsonW;
            float4 _ClipRect;

            v2f vert (appdata v)
            {
                v2f o;
                o.world = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.local = v.uv0;
                return o;
            }

            // 가우시안으로 흐린 구간 [a, b] 의 덮임 정도
            float Band(float x, float a, float b, float sigma)
            {
                float k = 1.0 / (max(sigma, 1e-3) * 1.41421356);
                return saturate(0.5 * (HudErf((x - a) * k) - HudErf((x - b) * k)));
            }

            float3 Stops(float u)
            {
                float s = saturate(u) * 4.0;
                float3 c = lerp(_Stop0.rgb, _Stop1.rgb, saturate(s));
                c = lerp(c, _Stop2.rgb, saturate(s - 1.0));
                c = lerp(c, _Stop3.rgb, saturate(s - 2.0));
                c = lerp(c, _Stop4.rgb, saturate(s - 3.0));
                return c;
            }

            // CSS saturate(s) 행렬 (sRGB 값에 그대로)
            float3 Saturate(float3 c, float s)
            {
                float3x3 m = float3x3(
                    0.213 + 0.787 * s, 0.715 - 0.715 * s, 0.072 - 0.072 * s,
                    0.213 - 0.213 * s, 0.715 + 0.285 * s, 0.072 - 0.072 * s,
                    0.213 - 0.213 * s, 0.715 - 0.715 * s, 0.072 + 0.928 * s);
                return mul(m, c);
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 p = i.local;
                float w = _BarSize.x;
                float h = _BarSize.y;
                float s = _BarSize.z;
                float fw = max(fwidth(p.x), 1e-4);

                // clip-path: polygon(s 0, 100% 0, calc(100% - s) 100%, 0 100%) — CSS 의 위(0)는 Unity 의 위(h)
                float len = sqrt(h * h + s * s);
                float dLeft = (s * p.y - h * p.x) / len;
                float dRight = (h * (p.x - (w - s)) - s * p.y) / len;
                float dPoly = max(max(p.y - h, -p.y), max(dLeft, dRight));
                float poly = saturate(0.5 - dPoly / fw);

                float fillW = _Ratio.x * w;
                float lagW = _Ratio.y * w;
                float inFill = saturate(0.5 - (p.x - fillW) / fw) * saturate(0.5 + p.x / fw);
                float inLag = saturate(0.5 - (p.x - lagW) / fw) * (1.0 - inFill) * saturate(0.5 + (p.x - fillW) / fw);

                float4 col = float4(_Track.rgb * _Track.a, _Track.a) * poly;

                // .dz HP 감소 잔상 (채움 끝 ~ lag)
                float la = _LagFx.x * inLag * poly;
                col = HudOver(float4(saturate(_Lag.rgb * _LagFx.y) * la, la), col);

                // 채움: 그라데이션이 채움 폭의 3배, background-position 으로 흐름
                float fW = max(fillW, 1.0);
                float u = (p.x + 2.0 * fW * _Anim.x) / (3.0 * fW);
                float3 c = Stops(u);
                float glossT = (h - p.y) / h;
                float ga = _Anim.z * (1.0 - saturate(glossT / 0.55));
                c = c * (1.0 - ga) + ga;
                // ::before 훑는 빛 linear-gradient(100deg, transparent 40%, rgba(255,255,255,a) 50%, transparent 60%) 300%
                // 크기
                float bx = p.x + 2.0 * fW * _Anim.y;
                float st = HudGradientT(float2(bx - 1.5 * fW, p.y - h * 0.5), float2(1.5 * fW, h * 0.5), 100.0);
                float sa = _Anim.w * saturate(1.0 - abs(st - 0.5) / 0.1);
                c = c * (1.0 - sa) + sa;
                // 궁 상태 filter: saturate(1.4) brightness(1.12) drop-shadow(0 0 5px c1) (전환 가중치 w)
                c = saturate(Saturate(c, 1.0 + 0.4 * _CrimsonW) * (1.0 + 0.12 * _CrimsonW));
                if (_CrimsonW > 0.0)
                {
                    float sig = 2.5 * _CrimsonW;
                    float glow = Band(p.x, 0.0, fillW, sig) * Band(p.y, 0.0, h, sig) * _CrimsonW * poly *
                        (1.0 - inFill);
                    col = HudOver(float4(_Crimson.rgb * glow, glow), col);
                }
                float fa = inFill * poly;
                col = HudOver(float4(c * fa, fa), col);

                // .gw drop-shadow 두 겹: 내용 알파 = 트랙 .59 + 채움 나머지 (평행사변형은 가운데 선 기준 사각형으로 근사)
                float x0 = s * 0.5;
                float x1 = w - s * 0.5;
                float fx1 = min(fillW, x1);
                float s1 = _ShadowBlur.x * 0.5;
                float s2 = _ShadowBlur.y * 0.5;
                float a1 = (_Track.a * Band(p.x, x0, x1, s1) + (1.0 - _Track.a) * Band(p.x, x0, fx1, s1)) *
                    Band(p.y, 0.0, h, s1);
                float a2 = (_Track.a * Band(p.x, x0, x1, s2) + (1.0 - _Track.a) * Band(p.x, x0, fx1, s2)) *
                    Band(p.y, 0.0, h, s2);
                float sh1 = _Shadow1.a * a1;
                float sh2 = _Shadow2.a * saturate(a2 + sh1 * 0.0);
                float4 under = HudOver(float4(_Shadow1.rgb * sh1, sh1), float4(_Shadow2.rgb * sh2, sh2));
                col = HudOver(col, under);

                col *= i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                col *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                return HudOut(col);
            }
            ENDCG
        }
    }
}
