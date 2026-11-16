// CSS text-shadow 를 TMP 글자(SDF 아틀라스)로 그대로 그린다: 글자 모양을 가우스(σ = blur / 2)로 흐린 그림자, 면은 그리지 않음.
// 글자마다 정점 색으로 그림자 묶음을 고른다: 흰색(r > .5) = 값 글자(_ValGlow 위 + _ValShadow 아래), 검정 = 나머지(_BaseShadow).
// 정점 알파 = 그 글자 그림자 세기(CSS opacity). 색 값은 CSS sRGB rgba 그대로(Vector), 위치는 CSS px(아래 +).
// TMP 의 SDF 덮임 식(scale · bias)을 그대로 써서 각 표본점의 덮임을 구하고, 표본 간격만큼 덮임 경계를 넓혀(상자 필터) 얇은 획에서도 고르게 흐림.
// 체력과 MP 글자의 색 그림자와 검은 그림자를 분리해 흐린다.
Shader "Espoir/UI/TextShadowBlur"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _FaceColor ("Face Color", Color) = (1, 1, 1, 1)
        _GradientScale ("Gradient Scale", Float) = 5
        _TextureWidth ("Texture Width", Float) = 512
        _TextureHeight ("Texture Height", Float) = 512
        _ScaleX ("Scale X", Float) = 1
        _ScaleY ("Scale Y", Float) = 1
        _Sharpness ("Sharpness", Range(-1, 1)) = 0
        _WeightNormal ("Weight Normal", Float) = 0
        _WeightBold ("Weight Bold", Float) = 0.5
        _ScaleRatioA ("Scale Ratio A", Float) = 1
        // TMP 가 글자 사각형 여백(padding)을 정할 때 읽는 값 — 그림자가 잘리지 않게 Underlay 를 켜 둔 것처럼 넓힘(셰이더는 쓰지 않음)
        _UnderlayColor ("Underlay Color", Color) = (0, 0, 0, 0)
        _UnderlayOffsetX ("Underlay Offset X", Range(-1, 1)) = 0
        _UnderlayOffsetY ("Underlay Offset Y", Range(-1, 1)) = 0
        _UnderlayDilate ("Underlay Dilate", Range(-1, 1)) = 0
        _UnderlaySoftness ("Underlay Softness", Range(0, 1)) = 1
        _ScaleRatioB ("Scale Ratio B", Float) = 1
        _ScaleRatioC ("Scale Ratio C", Float) = 1
        _FaceDilate ("Face Dilate", Range(-1, 1)) = 0
        _OutlineWidth ("Outline Thickness", Range(0, 1)) = 0
        _OutlineSoftness ("Outline Softness", Range(0, 1)) = 0

        _ValGlow ("Value shadow 1 (sRGB rgba)", Vector) = (1, 0.5412, 0.2902, 0.45)
        _ValGlowGeom ("Value shadow 1 (offX, offY, blur px)", Vector) = (0, 0, 10, 0)
        _ValShadow ("Value shadow 2 (sRGB rgba)", Vector) = (0, 0, 0, 1)
        _ValShadowGeom ("Value shadow 2 (offX, offY, blur px)", Vector) = (0, 1, 3, 0)
        _BaseShadow ("Other shadow (sRGB rgba)", Vector) = (0, 0, 0, 0.75)
        _BaseShadowGeom ("Other shadow (offX, offY, blur px)", Vector) = (0, 1, 4, 0)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        ZWrite Off
        Lighting Off
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
            #include "UiSrgbComposite.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 local : TEXCOORD1;
                // TMP 덮임 식: 덮임 = saturate((sdf − 0.5 + weight) · scale + 0.5)  (x = scale, y = weight)
                float2 param : TEXCOORD2;
            };

            sampler2D _MainTex;
            float _GradientScale, _ScaleX, _ScaleY, _Sharpness, _WeightNormal, _WeightBold, _ScaleRatioA;
            float4 _ValGlow, _ValGlowGeom, _ValShadow, _ValShadowGeom, _BaseShadow, _BaseShadowGeom;

            v2f vert (appdata v)
            {
                v2f o;
                float4 vPosition = UnityObjectToClipPos(v.vertex);
                // TMP_SDF-Mobile 과 같은 화면 px 당 SDF 기울기
                float2 pixelSize = vPosition.w;
                pixelSize /= float2(_ScaleX, _ScaleY) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float scale = rsqrt(dot(pixelSize, pixelSize));
                scale *= abs(v.texcoord1.y) * _GradientScale * (_Sharpness + 1);
                float bold = step(v.texcoord1.y, 0);
                float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
                weight = weight * _ScaleRatioA * 0.5;
                o.param = float2(scale, weight);
                o.pos = vPosition;
                o.color = v.color;
                o.uv = v.texcoord0;
                o.local = v.vertex.xy;
                return o;
            }

            static float2 s_uv;
            static float2x2 s_m;      // 로컬(캔버스 px, 위 +) → uv
            static float2 s_param;

            // 로컬 오프셋 o 의 덮임, 표본 간격 w px 만큼 경계를 넓힘
            float Cov(float2 o, float w)
            {
                float2 uv = s_uv + mul(s_m, o);
                float sdf = tex2Dlod(_MainTex, float4(uv, 0, 0)).a;
                return saturate((sdf - 0.5 + s_param.y) * s_param.x / max(w, 1.0) + 0.5);
            }

            // 가우스 흐린 덮임: CSS (offX, offY(아래 +), blur) → σ = blur / 2, 반지름 2.5σ
            float Blur(float4 geom)
            {
                float sigma = max(geom.z * 0.5, 0.35);
                float2 off = float2(geom.x, -geom.y);
                float stepPx = max(sigma * 0.5, 0.5);
                float sum = 0;
                float wsum = 0;
                [loop]
                for (int y = -5; y <= 5; y++)
                {
                    [loop]
                    for (int x = -5; x <= 5; x++)
                    {
                        float2 d = float2(x, y) * stepPx;
                        float w = exp(-dot(d, d) / (2.0 * sigma * sigma));
                        sum += Cov(d - off, stepPx) * w;
                        wsum += w;
                    }
                }
                return sum / wsum;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                s_uv = i.uv;
                s_param = i.param;
                // uv 와 로컬 위치의 화면 미분으로 로컬 → uv 변환 (Jacobian)
                float2 du = ddx(i.uv), dv = ddy(i.uv);
                float2 dlx = ddx(i.local), dly = ddy(i.local);
                float det = dlx.x * dly.y - dlx.y * dly.x;
                float2x2 inv = float2x2(dly.y, -dly.x, -dlx.y, dlx.x) / (abs(det) > 1e-8 ? det : 1e-8);
                // uv = J_uv · J_local^-1 · local
                float2x2 juv = float2x2(du.x, dv.x, du.y, dv.y);
                s_m = mul(juv, inv);

                float4 res;
                if (i.color.r > 0.5)
                {
                    float a1 = _ValGlow.a * Blur(_ValGlowGeom);
                    float a2 = _ValShadow.a * Blur(_ValShadowGeom);
                    float4 top = float4(_ValGlow.rgb * a1, a1);
                    float4 bottom = float4(_ValShadow.rgb * a2, a2);
                    res = top + (1.0 - top.a) * bottom;
                }
                else
                {
                    float a = _BaseShadow.a * Blur(_BaseShadowGeom);
                    res = float4(_BaseShadow.rgb * a, a);
                }

                // CSS opacity 는 글자와 그림자를 한 덩어리로 흐리게 함(작은 라벨 .8): 본 글자(알파 o·m)가 위에 그려질 때 결과가
                // o·(글자 over 그림자) 가 되도록 그림자에 o·(1 − m) / (1 − o·m) 를 곱함 (m = 이 화소의 글자 덮임). o = 1 이면 그대로
                float o = i.color.a;
                float m = Cov(float2(0.0, 0.0), 1.0);
                res *= o >= 0.999 ? 1.0 : o * (1.0 - m) / max(1.0 - o * m, 1e-3);
                // sRGB 미리곱 → 화면 (전역 sRGB 합성 구간 안이면 그대로)
                if (_UiGammaOut < 0.5 && res.a > 1e-5) res.rgb = UiSrgbToLin(res.rgb / res.a) * res.a;
                return res;
            }
            ENDCG
        }
    }
}
