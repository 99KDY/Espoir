Shader "Espoir/Player/Effects/Q/DragonHead"
{
    Properties
    {
        _EspoirHeadGlow ("용머리 발광 배율", Float) = 2
        _EspoirHeadForm ("HeadForm", Float) = 0.55
        _EspoirHeadBrush ("HeadBrush", Float) = 1

        [NoScaleOffset] _Noise ("Noise (RGBA 데이터)", 2D) = "gray" {}
        _C0 ("Ramp 0 (Linear HDR)", Vector) = (0.03, 0, 0.01, 0)
        _C1 ("Ramp 1", Vector) = (0.3, 0, 0.03, 0)
        _C2 ("Ramp 2", Vector) = (2, 0.04, 0.06, 0)
        _C3 ("Ramp 3", Vector) = (4.5, 1.6, 1.2, 0)
        _Lifetime ("파티클 수명 (초)", Float) = 0.72
        [Header(Ghost)]
        _Lag ("잔상 지연 (본체 0)", Float) = 0
        _GhostAlpha ("불투명도 배율", Float) = 1
        _HotOn ("달아오름 사용 (잔상 0)", Float) = 1
        _NoDepth ("깊이 안 씀 (검기 획 모드 1)", Float) = 0
        _Flutter ("갈기 출렁임", Float) = 1
        _Thick ("굵기 배율 (위아래·좌우)", Float) = 1
        [Header(Head)]
        _HScale ("크기", Float) = 4.6
        _HLen ("모델 길이", Float) = 0.9995
        _HLengthen ("용머리 길이 배율", Float) = 1.4
        _HCharge ("차징 시간", Float) = 0.09
        _HReveal ("차징 때 드러나는 비율", Float) = 0.45
        _HBack ("차징 때 뒤로 물러남", Float) = 0.12
        _HLunge ("돌진 시간", Float) = 0.05
        _HHold ("버티는 시간", Float) = 0.12
        _HExit ("퇴장 시간", Float) = 0.36
        _HOvershoot ("돌진 넘침", Float) = 0.04
        _HPop ("크기 튐", Float) = 0.03
        _HDash ("퇴장 때 밀려나는 거리", Float) = 1
        _HHotHold ("버티는 동안 밝기 하한", Float) = 0.7
        [Header(Capture)]
        _SeedFixed ("고정 시드 (음수 = 랜덤)", Float) = -1
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+12"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        CGINCLUDE
        #include "UnityCG.cginc"
        #include "PlayerQHeadState.cginc"
        #include "PlayerQColorReference.cginc"
        #include "PlayerQLookRamp.cginc"

        sampler2D _Noise;
        float4 _C0, _C1, _C2, _C3;
        float _Lifetime, _Lag, _GhostAlpha, _HotOn, _Flutter, _SeedFixed, _NoDepth;
        float _Thick;
        float _EspoirHeadGlow;
        float _EspoirHeadForm;
        float _EspoirHeadBrush;

        struct Appdata
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float4 tangent : TANGENT;
            float4 uvAge : TEXCOORD0;
            float3 center : TEXCOORD1;
        };

        struct V2F
        {
            float4 pos : SV_POSITION;
            float3 n : TEXCOORD0;
            float3 v : TEXCOORD1;
            float3 l : TEXCOORD2;
            float4 state : TEXCOORD3;
            float2 time : TEXCOORD4;
        };

        V2F vert(Appdata v)
        {
            V2F o;
            float age = v.uvAge.z * _Lifetime;
            float t = age - _Lag;
            HeadState s = HeadAt(t);
            float gate = 1.0;
            if (_Lag > 0.0)
            {
                HeadState lead = HeadAt(age);
                gate = saturate(abs(lead.off - s.off) / 0.08);
                if (gate <= 0.01) s.alpha = 0.0;
            }
            FxBasis b = FxBasisOf(v.tangent.xyz);
            float3 position = FxToModel(b, v.vertex.xyz - v.center) / FX_MESH_SCALE;
            float3 normal = FxToModel(b, v.normal);
            float seed = _SeedFixed >= 0.0 ? _SeedFixed : v.uvAge.w * 7.0;
            float3 lp = position;
            float z = saturate(position.z / _HLen);
            lp.xy *= _Thick;
            normal.xy /= _Thick;

            float k = pow(1.0 - z, 1.4) * _Flutter * _HLen;
            float ph = z * 9.0 + t * 20.0 + seed;
            lp.x += (sin(ph) * 0.09 + sin(ph * 1.7 + 1.3) * 0.04) * k;
            lp.y += (cos(ph * 0.85 + 0.7) * 0.07 + sin(ph * 2.3) * 0.02) * k;
            lp.z += sin(ph * 0.6 + 2.0) * 0.03 * k;

            float3 hp = HeadPlace(lp, s, t);
            float3 hn = mul(HeadWobble(t, s.wob), normal * HeadScaleOf(s));
            float3 wp = v.center + FxFromModel(b, hp);
            float3 wn = FxFromModel(b, hn);
            o.pos = s.alpha > 0.0 ? UnityWorldToClipPos(wp) : float4(0.0, 0.0, -2.0, 1.0);
            o.n = normalize(wn);
            o.v = normalize(_WorldSpaceCameraPos - wp);
            o.l = position;
            o.state = float4(s.reveal * _HLen, s.dis, s.hot * _HotOn, s.alpha * gate * _GhostAlpha);
            o.time = float2(t, seed);
            return o;
        }

        float3 Ramp(float i)
        {
            return LookRamp(i, _C0.rgb, _C1.rgb, _C2.rgb, _C3.rgb);
        }

        float4 Shade(V2F i)
        {
            float3 vL = i.l;
            float uReveal = i.state.x, uT = i.state.y, uHot = i.state.z, uAlpha = i.state.w, uTime = i.time.x, uSeed
                = i.time.y;
            float fr = saturate(1.0 - abs(dot(normalize(i.n), normalize(i.v))));
            float n = tex2D(_Noise, vL.xz * 0.9 + float2(uSeed, vL.y * 0.6)).a * 0.65 + tex2D(_Noise, vL.xy * 1.7 +
                uSeed).r * 0.35;

            float grow = uReveal - vL.z + (n - 0.5) * 0.12;
            if (grow < 0.0) discard;
            float growHot = 1.0 - smoothstep(0.0, 0.12, grow);
            float th = uT * 1.2 - 0.1;

            float dk = n;
            float vis = smoothstep(th, th + 0.05, dk);
            float edge = vis * (1.0 - smoothstep(th + 0.05, th + 0.12, dk)) * step(0.02, uT);

            float zf = saturate(vL.z / max(_HLen, 0.001));
            float life = 1.0 - zf;
            float y = fr;
            float sn = zf + vL.x * 0.7 + vL.y * 0.4;
            float n1 = tex2D(_Noise, float2(sn * 1.6 + uSeed, y * 0.35 - uTime * 0.12)).r;
            float n2 = tex2D(_Noise, float2(sn * 4.0 - uTime * 0.35 + uSeed, y * 1.2)).g;
            float streak = tex2D(_Noise, float2(sn * 0.8 + uSeed * 0.3, y * 1.0)).b;
            float width = lerp(0.62, 0.28, pow(life, 0.7));
            float inner = 1.0 - width + (n1 - 0.5) * 0.7 * (0.45 + life) - streak * 0.22 * (0.4 + life);
            float body = smoothstep(inner, inner + 0.035, y);
            float yy = saturate((y - inner) / max(1.0 - inner, 0.001));
            float Is = 0.22 + 0.78 * pow(yy, 1.35);
            Is *= lerp(1.08, 0.5, life);
            Is += (n2 - 0.5) * 0.3 + (streak - 0.5) * 0.35 * yy * LookStreak();
            Is += smoothstep(0.9, 0.99, y) * 0.35 * (1.0 - life);
            Is += smoothstep(0.55, 0.95, yy) * uHot * 0.5 + growHot * 0.5 + edge * 0.3;
            Is += smoothstep(0.72, 1.0, zf) * smoothstep(0.35, 0.9, yy) * 0.35;
            float3 col = Ramp(saturate(Is));
            float Ifin = Is;

            float rag = tex2D(_Noise, float2(sn * 3.0 + uSeed, y * 0.5 + uTime * 0.05)).r * 0.6 + tex2D(_Noise,
                float2(sn * 7.0 - uSeed, 0.37)).g * 0.4;
            float outerCut = 0.98 - rag * rag * 0.55;
            float rip = tex2D(_Noise, float2(vL.z * 0.6 - uTime * 0.4 + uSeed, (vL.x * 1.3 + vL.y) * 9.0)).b;
            float ripKeep = smoothstep(0.18, 0.3, rip + zf * 0.45 - 0.12);
            float a = body * vis * uAlpha * smoothstep(0.02, 0.2, Is + 0.15) * (1.0 - smoothstep(outerCut - 0.04,
                outerCut, y)) * ripKeep;
            if (_EspoirHeadBrush > 0.5)
            {

                float thB = atan2(vL.y, vL.x) / 6.2832 + 0.5;
                float cc = thB * 8.0 + zf * 0.4 + sin(zf * 4.0 + uSeed) * 0.08;
                float fc = floor(cc);
                float sid2 = fc - 8.0 * floor(fc / 8.0);
                float v = cc - fc;
                float r1 = frac(sin(sid2 * 12.9898 + uSeed) * 43758.5453), r2 = frac(sin(sid2 * 78.233 + uSeed *
                    1.3) * 24634.6345);
                float z0 = r1 * 0.3;
                float bRag = tex2D(_Noise, float2(zf * 2.0 + sid2 * 0.37, 0.21 + uSeed * 0.1)).r;
                float zr = saturate((zf - z0) / max(1.0 - z0, 0.01));
                float w = lerp(0.55, 1.15, pow(zr, 0.5)) * (0.94 + 0.12 * r2);
                float hw = 0.5 * w;
                float snout = smoothstep(0.7, 0.9, zf);
                float strandM = smoothstep(hw + 0.03, hw - 0.07, abs(v - 0.5)) * smoothstep(z0 - 0.03, z0 + 0.12, zf
                    + (bRag - 0.5) * 0.08);
                strandM = max(strandM, snout);
                float hc = min(hw, 0.5);
                float yb = saturate((v - (0.5 - hc)) / max(2.0 * hc, 1e-3));
                float stB = tex2D(_Noise, float2(zf * 0.8 + sid2 * 0.31 + uSeed * 0.2, yb)).b;
                float nB = tex2D(_Noise, float2(zf * 4.0 - uTime * 0.35 + sid2, yb * 1.2)).g;
                float lifeB = 1.0 - zr;
                float Ib = 0.22 + 0.78 * pow(yb, 1.35);
                Ib *= lerp(1.08, 0.5, lifeB);
                Ib += (nB - 0.5) * 0.3 + (stB - 0.5) * 0.35 * yb;
                Ib += smoothstep(0.55, 0.95, yb) * uHot * 0.6 * (1.0 - lifeB) + growHot * 0.5 + edge * 0.3;
                float bEdge = smoothstep(0.0, 0.14, min(yb, 1.0 - yb));
                Ib = lerp(0.5 * lerp(1.08, 0.5, lifeB) + 0.1, Ib, bEdge);
                Ib += fr * fr * 0.2;

                if (_EspoirHeadForm > 0.0)
                {
                    float lam = saturate(dot(normalize(i.n), normalize(float3(0.25, 0.85, 0.45))));
                    float IbF = Is * 0.85 + lam * 0.3 * (1.0 - lifeB * 0.5) + (Ib - (0.5 * lerp(1.08, 0.5, lifeB) +
                        0.1)) * 0.4;
                    Ib = lerp(Ib, IbF, _EspoirHeadForm);
                }
                Ib = lerp(Ib, Is, snout);
                col = Ramp(saturate(Ib));
                Ifin = Ib;
                a = strandM * vis * uAlpha * smoothstep(0.02, 0.2, Ib + 0.15);
            }

            float glow = max(_EspoirHeadGlow, 0.0);
            col *= glow < 1.0 ? glow : 1.0 + (glow - 1.0) * smoothstep(0.3, 0.85, saturate(Ifin));
            return float4(col, a);
        }

        ENDCG

        Pass
        {
            ColorMask 0 ZWrite On Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            float4 frag(V2F i) : SV_Target
            {
                if (_EspoirHeadGlow <= 0.0) discard;

                float4 c = Shade(i);
                if (c.a < 0.3 || _NoDepth > 0.5) discard;
                return 0;
            }
            ENDCG
        }
        Pass
        {
            ZWrite Off Cull Back Blend One OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            float4 frag(V2F i) : SV_Target
            {
                if (_EspoirHeadGlow <= 0.0) discard;

                float4 c = Shade(i);
                if (c.a < 0.01) discard;
                if (_EspoirLook.x > 0.5)
                {

                    float level = max(c.r, max(c.g, c.b)) / max(max(_EspoirRamp2.r, max(_EspoirRamp2.g,
                        _EspoirRamp2.b)), 0.001);
                    float solid = saturate(c.a * 1.15);
                    float3 col = c.rgb + _EspoirRamp2.rgb * 0.3 * (1.0 - saturate(level));
                    return float4(col * solid, solid);
                }

                float3 composited = c.rgb * c.a + REFERENCE_GROUND * (1.0 - c.a);
                float3 luma = float3(0.2126, 0.7152, 0.0722);
                float floorLuma = dot(REFERENCE_GROUND, luma);
                float cover = saturate((floorLuma - dot(composited, luma)) / floorLuma);
                float3 light = max(composited - REFERENCE_GROUND * (1.0 - cover), 0.0);
                if (cover < 0.01 && max(light.r, max(light.g, light.b)) < 0.001) discard;
                return float4(light, cover);
            }
            ENDCG
        }
    }
    Fallback Off
}
