// 궁극기와 전투의 흐름을 표시하는 원형 고리를 그립니다.
// radial-gradient(closest-side) 마스크로 만든 고리 트랙 + conic-gradient(from 회전각) 흐름 호(conic 마스크로 0 → flow·360deg) +
// drop-shadow.
Shader "Espoir/UI/HudRing"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Ring ("Radius (from, to, outer), union shadow", Vector) = (46.8, 47.84, 52, 0)
        _Track ("Track", Vector) = (1, 1, 1, 0.16)
        _TrackShadow ("Track shadow", Vector) = (1, 0.7529, 0.4392, 0.7)
        _Arc ("Flow, start deg, -, -", Vector) = (0, 0, 0, 0)
        _ArcShadow ("Arc shadow", Vector) = (1, 0.6667, 0.3529, 0.75)
        _Blur ("Track blur, Arc blur", Vector) = (5, 6, 0, 0)
        _Stop0 ("Stop 0", Vector) = (1, 1, 1, 1)
        _Stop1 ("Stop 1", Vector) = (1, 1, 1, 1)
        _Stop2 ("Stop 2", Vector) = (1, 1, 1, 1)
        _Stop3 ("Stop 3", Vector) = (1, 1, 1, 1)
        _Stop4 ("Stop 4", Vector) = (1, 1, 1, 1)
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

            float4 _Ring, _Track, _TrackShadow, _Arc, _ArcShadow, _Blur, _Stop0, _Stop1, _Stop2, _Stop3, _Stop4;
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

            float Band(float x, float a, float b, float sigma)
            {
                float k = 1.0 / (max(sigma, 1e-3) * 1.41421356);
                return saturate(0.5 * (HudErf((x - a) * k) - HudErf((x - b) * k)));
            }

            float3 Stops(float u)
            {
                float s = frac(u) * 4.0;
                float3 c = lerp(_Stop0.rgb, _Stop1.rgb, saturate(s));
                c = lerp(c, _Stop2.rgb, saturate(s - 1.0));
                c = lerp(c, _Stop3.rgb, saturate(s - 2.0));
                c = lerp(c, _Stop4.rgb, saturate(s - 3.0));
                return c;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 p = i.local;
                float r = length(p);
                float fw = max(fwidth(r), 1e-4);
                float r0 = _Ring.x;
                float r1 = _Ring.y;
                float ro = _Ring.z;

                // radial-gradient(closest-side, transparent r0, #000 r1) × 원(border-radius 50%)
                float ring = saturate((r - r0) / max(r1 - r0, fw)) * saturate(0.5 - (r - ro) / fw);

                // 각도: 12시에서 시계 방향 0~360
                float ang = degrees(atan2(p.x, p.y));
                ang = ang < 0.0 ? ang + 360.0 : ang;
                float span = saturate(_Arc.x) * 360.0;
                float arcLen = r * radians(1.0);
                float arcIn = span >= 359.99 ? 1.0 : saturate(0.5 + min(ang, span - ang) * arcLen / fw) *
                    step(0.0001, span);
                float arcA = ring * arcIn;

                float3 arcCol = Stops((ang - _Arc.y) / 360.0);

                // 그림자: 고리 단면은 반지름 방향 1차원 흐림, 호 끝은 호 길이 방향으로 흐림
                float rm = (r0 + r1) * 0.5;
                float ts = _Blur.x * 0.5;
                float arcSig = _Blur.y * 0.5;
                float trackBlur = Band(r, rm, ro, ts);
                float outD = min(max(-ang, ang - span), max(-(ang - 360.0), (ang - 360.0) - span));
                float inD = min(ang, span - ang);
                float dArc = (ang <= span ? -inD : max(outD, 0.0)) * arcLen;
                float arcBlur = span <= 0.0 ? 0.0 : (span >= 359.99 ? 1.0 : HudShadowEdge(dArc, max(arcSig, 1e-3)));
                arcBlur *= Band(r, rm, ro, arcSig);

                float4 col = 0;
                bool unionShadow = _Ring.w > 0.5;
                if (unionShadow)
                {
                    // .ps .fx.ring: 트랙 + 호를 한 요소로 보고 drop-shadow 하나
                    float sa = _TrackShadow.a * saturate(_Track.a * trackBlur + (1.0 - _Track.a) * arcBlur);
                    col = float4(_TrackShadow.rgb * sa, sa);
                    col = HudOver(float4(_Track.rgb * _Track.a * ring, _Track.a * ring), col);
                    col = HudOver(float4(arcCol * arcA, arcA), col);
                }
                else
                {
                    // R: 트랙(::before) 그림자 → 트랙 → 호(.ucr-ring) 그림자 → 호
                    float sa = _TrackShadow.a * _Track.a * trackBlur;
                    col = float4(_TrackShadow.rgb * sa, sa);
                    col = HudOver(float4(_Track.rgb * _Track.a * ring, _Track.a * ring), col);
                    float aa = _ArcShadow.a * arcBlur * (1.0 - arcA);
                    col = HudOver(float4(_ArcShadow.rgb * aa, aa), col);
                    col = HudOver(float4(arcCol * arcA, arcA), col);
                }

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
