// 선형·타원형 그라데이션과 선형 알파 마스크를 합성합니다.
// 색 멈춤점은 4개까지, 브라우저처럼 감마(sRGB) 공간에서 미리 곱한 알파(premultiplied)로 보간한다.
//  _Mode 0 = linear (_Angle: CSS 각도, 90 = 왼→오, 180 = 위→아래), 1 = radial (_Center · _Radius: uv 단위 타원 중심 · 반지름)
//  마스크(_MaskCount > 0): 같은 방식의 알파 멈춤점 (_MaskAngle), 결과 알파에 곱함
Shader "Espoir/UI/CssGradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Mode ("Mode (0 linear, 1 radial)", Float) = 0
        _Angle ("CSS Angle (deg)", Float) = 90
        _Center ("Radial Center (uv)", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radial Radius (uv)", Vector) = (0.5, 0.5, 0, 0)
        _StopCount ("Stop Count", Float) = 2
        _C0 ("Stop 0", Color) = (1, 1, 1, 0)
        _C1 ("Stop 1", Color) = (1, 1, 1, 1)
        _C2 ("Stop 2", Color) = (1, 1, 1, 0)
        _C3 ("Stop 3", Color) = (1, 1, 1, 0)
        _Pos ("Stop Positions", Vector) = (0, 1, 1, 1)
        _MaskCount ("Mask Stop Count (0 = off)", Float) = 0
        _MaskAngle ("Mask CSS Angle (deg)", Float) = 180
        _MaskAlpha ("Mask Alphas", Vector) = (0, 1, 1, 0)
        _MaskPos ("Mask Positions", Vector) = (0, 0.25, 0.75, 1)

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

        Pass
        {
            Name "CSS_GRADIENT"

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
                float4 worldPos : TEXCOORD1;
            };

            fixed4 _Color;
            float _Mode;
            float _Angle;
            float4 _Center;
            float4 _Radius;
            float _StopCount;
            float4 _C0;
            float4 _C1;
            float4 _C2;
            float4 _C3;
            float4 _Pos;
            float _MaskCount;
            float _MaskAngle;
            float4 _MaskAlpha;
            float4 _MaskPos;
            float4 _ClipRect;

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPos = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
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

            // CSS 각도 → uv 위에서의 진행도 (uv.y 는 아래가 0, CSS 는 위가 0)
            float LinearT(float2 uv, float angle)
            {
                float a = radians(angle);
                float2 dir = float2(sin(a), -cos(a));
                float2 p = float2(uv.x, 1.0 - uv.y) - 0.5;
                float2 css = float2(p.x, -p.y);
                float len = abs(dir.x) + abs(dir.y);
                return dot(float2(css.x, -css.y), dir) / len + 0.5;
            }

            float4 Premul(float4 c)
            {
                float3 g = ToGamma(c.rgb);
                return float4(g * c.a, c.a);
            }

            float4 Stops(float t)
            {
                int count = (int)_StopCount;
                float4 c0 = Premul(_C0);
                float4 c1 = Premul(_C1);
                float4 c2 = Premul(_C2);
                float4 c3 = Premul(_C3);

                if (t <= _Pos.x) return c0;
                if (count <= 1) return c0;
                if (t <= _Pos.y) return lerp(c0, c1, saturate((t - _Pos.x) / max(_Pos.y - _Pos.x, 1e-5)));
                if (count <= 2) return c1;
                if (t <= _Pos.z) return lerp(c1, c2, saturate((t - _Pos.y) / max(_Pos.z - _Pos.y, 1e-5)));
                if (count <= 3) return c2;
                if (t <= _Pos.w) return lerp(c2, c3, saturate((t - _Pos.z) / max(_Pos.w - _Pos.z, 1e-5)));
                return c3;
            }

            float MaskStops(float t)
            {
                int count = (int)_MaskCount;

                if (t <= _MaskPos.x) return _MaskAlpha.x;
                if (count <= 1) return _MaskAlpha.x;
                if (t <= _MaskPos.y) return lerp(_MaskAlpha.x, _MaskAlpha.y,
                    saturate((t - _MaskPos.x) / max(_MaskPos.y - _MaskPos.x, 1e-5)));
                if (count <= 2) return _MaskAlpha.y;
                if (t <= _MaskPos.z) return lerp(_MaskAlpha.y, _MaskAlpha.z,
                    saturate((t - _MaskPos.y) / max(_MaskPos.z - _MaskPos.y, 1e-5)));
                if (count <= 3) return _MaskAlpha.z;
                if (t <= _MaskPos.w) return lerp(_MaskAlpha.z, _MaskAlpha.w,
                    saturate((t - _MaskPos.z) / max(_MaskPos.w - _MaskPos.z, 1e-5)));
                return _MaskAlpha.w;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t;

                if (_Mode > 0.5)
                {
                    // CSS 는 위가 0 이므로 y 를 뒤집어 잼
                    float2 p = (float2(i.uv.x, 1.0 - i.uv.y) - _Center.xy) / max(_Radius.xy, 1e-5);
                    t = length(p);
                }

                else
                {
                    t = LinearT(i.uv, _Angle);
                }

                float4 pm = Stops(t);
                float alpha = pm.a;
                float3 color = alpha > 1e-5 ? pm.rgb / alpha : 0;

                if (_MaskCount > 0.5) alpha *= MaskStops(LinearT(i.uv, _MaskAngle));

                float4 result = float4(ToLinear(color), alpha) * i.color;

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
