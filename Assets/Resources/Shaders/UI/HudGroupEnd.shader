// 합성 전후의 화면과 알파 채널로 HUD 레이어를 분리합니다.
// 분리한 HUD에 아래 순서로 효과를 적용합니다:
//   복제 잔상(ghost) · 글리치 복제(screen) → url(#ux-f) 색수차 → brightness → drop-shadow(진홍, 오른쪽) → translate(흔들림) · scale(punch)
//   → 장면 위에 sRGB 로 합성(CSS 와 같은 합성 공간) → 'bl' 빛 번짐(screen).
Shader "Espoir/UI/HudGroupEnd"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Fx ("brightness, ca, red, punch", Vector) = (1, 0, 0, 0)
        _Shake ("shake x, y", Vector) = (0, 0, 0, 0)
        _Cb ("cb rect (x0, y0, x1, y1) local", Vector) = (0, 0, 1, 1)
        _RCenter ("R center local, R radius", Vector) = (0, 0, 42, 0)
        _GhostS ("ghost scale 1-3, count", Vector) = (1, 1, 1, 0)
        _GhostA ("ghost alpha 1-3", Vector) = (0, 0, 0, 0)
        _Gl0 ("glitch0 y0, y1, dx, alpha", Vector) = (0, 0, 0, 0)
        _Gl1 ("glitch1 y0, y1, dx, alpha", Vector) = (0, 0, 0, 0)
        _GlHue ("glitch hue-rotate deg 0, 1", Vector) = (-50, 150, 0, 0)
        _Bloom ("bloom amp, local time", Vector) = (0, 0, 0, 0)
        _C1 ("c1", Vector) = (1, 0.2392, 0.3216, 1)
        _C2 ("c2", Vector) = (1, 0.8235, 0.4784, 1)
        _Clip ("group content rect (x0, y0, x1, y1) local", Vector) = (-100000, -100000, 100000, 100000)
        _Bloom2 ("bloom 2 amp, local time", Vector) = (0, 0, 0, 0)
        _Bloom2C ("bloom 2 center local, radius", Vector) = (0, 0, 34, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        GrabPass { "_HudGroupTex" }

        Pass
        {
            Blend Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"
            #include "HudCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv0 : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 local : TEXCOORD0;
                float4 grab : TEXCOORD1;
            };

            sampler2D _HudSceneTex;
            sampler2D _HudGroupTex;
            float4 _Fx, _Shake, _Cb, _RCenter, _GhostS, _GhostA, _Gl0, _Gl1;
            float4 _GlHue, _Bloom, _C1, _C2;
            float4 _Clip, _Bloom2, _Bloom2C;

            static float2 s_uv;
            static float2 s_lp;
            static float2 s_j;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.local = v.uv0;
                o.grab = ComputeGrabScreenPos(o.pos);
                return o;
            }

            float2 UvAt(float2 q)
            {
                return s_uv + (q - s_lp) * s_j;
            }

            // 묶음 한 장 H (프리멀티플라이드 sRGB). 알파 = HUD 덮임, 색 = (그린 뒤 − (1 − a)·그리기 전)
            float4 H(float2 q)
            {
                if (q.x < _Clip.x || q.y < _Clip.y || q.x > _Clip.z || q.y > _Clip.w) return float4(0, 0, 0, 0);
                float2 uv = UvAt(q);
                float4 after = tex2Dlod(_HudGroupTex, float4(uv, 0, 0));
                float3 before = tex2Dlod(_HudSceneTex, float4(uv, 0, 0)).rgb;
                float a = saturate(after.a);
                float3 prem = max(after.rgb - (1.0 - a) * before, 0.0);
                float4 h = float4(min(prem, a), a);
                // 전역 sRGB 합성 구간 안이면 잡은 값이 이미 sRGB 숫자 → 묶음도 sRGB 에서 정확히 되살아남
                return _UiGammaOut > 0.5 ? h : HudPremulLinearToSrgb(h);
            }

            float4 Screen(float4 dst, float4 src, float o)
            {
                src *= o;
                return dst + src - dst * src;
            }

            // CSS filter: sepia(1) saturate(9) hue-rotate(deg) — 단계마다 0~1 로 자름 (sRGB 값에 그대로)
            float3 GlitchTint(float3 c, float deg)
            {
                c = saturate(float3(dot(float3(0.393, 0.769, 0.189), c), dot(float3(0.349, 0.686, 0.168), c),
                    dot(float3(0.272, 0.534, 0.131), c)));
                float s = 9.0;
                c = saturate(float3(dot(float3(0.213 + 0.787 * s, 0.715 - 0.715 * s, 0.072 - 0.072 * s), c),
                    dot(float3(0.213 - 0.213 * s, 0.715 + 0.285 * s, 0.072 - 0.072 * s), c),
                    dot(float3(0.213 - 0.213 * s, 0.715 - 0.715 * s, 0.072 + 0.928 * s), c)));
                float a = radians(deg);
                float co = cos(a);
                float si = sin(a);
                return saturate(float3(
                    dot(float3(0.213 + co * 0.787 - si * 0.213, 0.715 - co * 0.715 - si * 0.715, 0.072 - co *
                        0.072 + si * 0.928), c),
                    dot(float3(0.213 - co * 0.213 + si * 0.143, 0.715 + co * 0.285 + si * 0.140, 0.072 - co *
                        0.072 - si * 0.283), c),
                    dot(float3(0.213 - co * 0.213 - si * 0.787, 0.715 - co * 0.715 + si * 0.715, 0.072 + co *
                        0.928 + si * 0.072), c)));
            }

            float4 Glitch(float2 q, float4 g, float4 band, float deg)
            {
                if (band.w <= 0.0) return g;
                float cbTop = _Cb.w;
                float cbH = _Cb.w - _Cb.y;
                float yTop = cbTop - band.x * cbH;
                float yBot = cbTop - band.y * cbH;
                if (q.y > yTop || q.y < yBot) return g;
                float4 l = H(q - float2(band.z, 0.0));
                if (l.a <= 0.0) return g;
                float3 c = l.rgb / l.a;
                float3 t = GlitchTint(c, deg);
                return Screen(g, float4(t * l.a, l.a), band.w);
            }

            // .cb 안 내용(원본 + 복제 잔상 + 글리치 복제)
            float4 G(float2 q)
            {
                float4 g = H(q);
                if (_GhostS.w > 0.5)
                {
                    float2 rc = _RCenter.xy;
                    g = HudOver(H(rc + (q - rc) / _GhostS.x) * _GhostA.x, g);
                    g = HudOver(H(rc + (q - rc) / _GhostS.y) * _GhostA.y, g);
                    g = HudOver(H(rc + (q - rc) / _GhostS.z) * _GhostA.z, g);
                }
                g = Glitch(q, g, _Gl0, _GlHue.x);
                g = Glitch(q, g, _Gl1, _GlHue.y);
                return g;
            }

            // url(#ux-f): R · G · B 채널을 따로 옮겨 screen 으로 합침 (SVG y 아래 + → Unity y 위 +)
            float4 Filtered(float2 q)
            {
                float ca = _Fx.y;
                if (ca <= 0.01) return G(q);
                bool red = _Fx.z > 0.5;
                float2 dR = float2(ca, -0.15 * ca);
                float2 dB = float2(red ? -0.2 * ca : -ca, 0.1 * ca);
                float4 r = G(q - dR);
                float4 g = G(q);
                float4 b = G(q - dB);
                float a = 1.0 - (1.0 - r.a) * (1.0 - g.a) * (1.0 - b.a);
                return float4(r.r, g.g, b.b, a);
            }

            float4 Bright(float4 c, float k)
            {
                if (c.a <= 1e-5 || abs(k - 1.0) < 1e-3) return c;
                float3 u = saturate(c.rgb / c.a * k);
                return float4(u * c.a, c.a);
            }

            float3 BloomAt(float2 c, float radius, float4 b)
            {
                float r = length(s_lp - c);
                float rr = radius + 10.0;
                float w = 26.0 + 70.0 * saturate(b.y);
                return (lerp(_C1.rgb, _C2.rgb, 0.45) * exp(-max(r - rr * 0.25, 0.0) / w) * 0.9 + exp(-r / 22.0) *
                    0.35) * b.x;
            }

            float4 frag (v2f i) : SV_Target
            {
                s_lp = i.local;
                s_uv = i.grab.xy / i.grab.w;
                float2 dl = float2(ddx(i.local.x), ddy(i.local.y));
                s_j = float2(ddx(s_uv.x) / (abs(dl.x) > 1e-6 ? dl.x : 1e-6),
                    ddy(s_uv.y) / (abs(dl.y) > 1e-6 ? dl.y : 1e-6));

                float4 before = tex2D(_HudSceneTex, s_uv);
                float3 scene = HudGrabToSrgb(before.rgb);

                // translate(흔들림) · scale(1 + punch) 를 거꾸로 (transform-origin = .cb 가운데)
                float2 cbC = (_Cb.xy + _Cb.zw) * 0.5;
                float2 q = cbC + (s_lp - _Shake.xy - cbC) / (1.0 + _Fx.w);

                float4 f = Bright(Filtered(q), _Fx.x);
                float ca = _Fx.y;
                if (_Fx.z > 0.5 && ca > 0.5)
                {
                    float2 so = float2(ca * 0.6, 0.0);
                    float sa;
                    {
                        float4 fs = Filtered(q - so);
                        sa = 0.55 * fs.a;
                    }
                    f = HudOver(f, float4(_C1.rgb * sa, sa));
                }

                // sRGB 합성 구간에서는 R과 W의 빛 번짐을 포함해 색과 덮임 알파를 합성한다.
                if (_UiGammaOut > 0.5 && _Bloom.x <= 0.0 && _Bloom2.x <= 0.0)
                {
                    return float4(f.rgb + (1.0 - f.a) * before.rgb, f.a + (1.0 - f.a) * before.a);
                }

                float3 outc = f.rgb + (1.0 - f.a) * scene;

                // R과 W의 빛을 더한 뒤 한 번에 스크린 합성한다.
                float3 add = 0.0;
                if (_Bloom.x > 0.0) add += BloomAt(_RCenter.xy, _RCenter.z, _Bloom);
                if (_Bloom2.x > 0.0) add += BloomAt(_Bloom2C.xy, _Bloom2C.z, _Bloom2);
                if (_Bloom.x > 0.0 || _Bloom2.x > 0.0)
                {
                    outc = 1.0 - (1.0 - saturate(outc)) * (1.0 - saturate(add));
                }

                return float4(_UiGammaOut > 0.5 ? saturate(outc) : HudSrgbToLinear(saturate(outc)), 1.0);
            }
            ENDCG
        }
    }
}
