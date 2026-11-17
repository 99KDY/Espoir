// 흐린 배경 위에 반투명 세로 그라데이션을 합성합니다.
// 뒤 화면을 GrabPass 로 한 번 잡아(이름 붙인 잡기 = 프레임당 한 번) 가우스 흐림(표준편차 _BlurRadius px)을 하고,
// 그 위에 배경색을 브라우저처럼 감마(sRGB) 공간에서 섞는다. 흐림 표본은 요소 사각형 안으로 붙잡아 가장자리 밖이 번져 들지 않게 한다.
Shader "Espoir/UI/BackdropBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _BlurRadius ("Blur (CSS px, 1080 기준)", Float) = 10
        _TintTop ("Background Top", Color) = (0.031, 0.039, 0.071, 0.46)
        _TintBottom ("Background Bottom", Color) = (0.031, 0.039, 0.071, 0.72)

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
            "PreviewType" = "Plane"
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
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        GrabPass { "_TalkBackdropTex" }

        Pass
        {
            Name "BACKDROP_BLUR"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "../UiSrgbComposite.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 grabPos : TEXCOORD1;
                float4 worldPos : TEXCOORD2;
            };

            sampler2D _TalkBackdropTex;
            float4 _TalkBackdropTex_TexelSize;
            fixed4 _Color;
            float _BlurRadius;
            float4 _TintTop;
            float4 _TintBottom;
            float4 _ClipRect;

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPos = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float3 ToGamma(float3 c)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                    return c;
                #else
                    return LinearToGammaSpace(c);
                #endif
            }

            float3 ToLinear(float3 c)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                    return c;
                #else
                    // 전역 sRGB 합성 구간 안(_UiGammaOut 1)이면 sRGB 숫자 그대로 내보냄
                    return _UiGammaOut > 0.5 ? c : GammaToLinearSpace(c);
                #endif
            }

            // 잡은 화면 색 → 감마. 구간 안이면 이미 sRGB 숫자
            float3 GrabGamma(float3 c)
            {
                return _UiGammaOut > 0.5 ? c : ToGamma(c);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 grabUv = i.grabPos.xy / i.grabPos.w;
                float2 texel = _TalkBackdropTex_TexelSize.xy;
                // CSS px(1080 기준)를 화면 픽셀로: 화면 높이 / 1080
                float sigma = max(_BlurRadius * (_ScreenParams.y / 1080.0), 0.001);

                // 요소 사각형(uv 0~1)이 화면에서 차지하는 범위 → 표본을 그 안으로 붙잡음
                //  (UI 는 축에 맞춰 놓이므로 grab.x 는 uv.x 에만, grab.y 는 uv.y 에만 따름. 부호까지 써서 뒤집힘도 맞춤)
                float dux = ddx(i.uv.x);
                float duy = ddy(i.uv.y);
                float2 scale = float2(ddx(grabUv.x) / (abs(dux) > 1e-7 ? dux : 1e-7),
                    ddy(grabUv.y) / (abs(duy) > 1e-7 ? duy : 1e-7));
                float2 lowGrab = grabUv - i.uv * scale;
                float2 highGrab = grabUv + (1.0 - i.uv) * scale;
                float2 lo = min(lowGrab, highGrab) + texel * 0.5;
                float2 hi = max(lowGrab, highGrab) - texel * 0.5;

                // 가우스 흐림: 반지름 3σ 안의 64점 나선 표본 + 가우스 가중치
                const int COUNT = 64;
                const float GOLDEN = 2.39996323;
                // 흐림 0 = 브라우저처럼 감마 공간에서 반투명 배경만 섞음 (회전한 요소도 됨)
                if (_BlurRadius < 0.01)
                {
                    float4 flat = lerp(_TintTop, _TintBottom, saturate(1.0 - i.uv.y));
                    float3 under = tex2D(_TalkBackdropTex, grabUv).rgb;
                    float4 plain = float4(ToLinear(lerp(GrabGamma(under), ToGamma(flat.rgb), flat.a)), i.color.a);
                    #ifdef UNITY_UI_CLIP_RECT
                        plain.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                    #endif
                    return plain;
                }

                float3 sum = tex2D(_TalkBackdropTex, clamp(grabUv, lo, hi)).rgb;
                float weightSum = 1.0;

                [unroll]
                for (int k = 0; k < COUNT; k++)
                {
                    float r = sqrt((k + 0.5) / COUNT) * 3.0 * sigma;
                    float a = k * GOLDEN;
                    float2 offset = float2(cos(a), sin(a)) * r * texel;
                    float w = exp(-(r * r) / (2.0 * sigma * sigma));
                    sum += tex2D(_TalkBackdropTex, clamp(grabUv + offset, lo, hi)).rgb * w;
                    weightSum += w;
                }

                float3 blurred = sum / weightSum;
                float4 tint = lerp(_TintTop, _TintBottom, saturate(1.0 - i.uv.y));
                float3 color = ToLinear(lerp(GrabGamma(blurred), ToGamma(tint.rgb), tint.a));
                float4 result = float4(color, i.color.a);

                #ifdef UNITY_UI_CLIP_RECT
                    result.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}
