// CSS text-shadow 여러 겹(가우스 흐림)을 글자 그림(CHudTextGlow 가 구운 렌더 텍스처)으로 그린다. 면은 그리지 않음(본 TMP 글자가 위에 그림).
// _MainTex: r = 글자 덮임, g = 그림자 1 흐림(σ1), b = 그림자 2 흐림(σ2) / _ShadowTex: g = 그림자 3 흐림(σ3)  (Hidden/Espoir/UI/DamageGlyph
// 로 구움)
// CSS 는 첫 그림자가 맨 위 → 아래부터 3 · 2 · 1 순서로 덮는다. 색은 CSS sRGB rgba 그대로, 전역 sRGB 합성 구간 안이면 sRGB 숫자로 출력.
// R 중첩 수의 그림자를 합성하며 최대 중첩에서는 별도 강조 설정을 사용한다.
Shader "Espoir/UI/TextShadowCss"
{
    Properties
    {
        [PerRendererData] _MainTex ("Glyph (r mask, g blur 1, b blur 2)", 2D) = "black" {}
        _ShadowTex ("Glyph 2 (g blur 3)", 2D) = "black" {}
        _S1 ("Shadow 1 (sRGB rgba)", Vector) = (1, 0.2392, 0.3216, 1)
        _S2 ("Shadow 2 (sRGB rgba)", Vector) = (1, 0.2392, 0.3216, 1)
        _S3 ("Shadow 3 (sRGB rgba)", Vector) = (0, 0, 0, 1)
        _Off12 ("Shadow 1 · 2 offset (uv to subtract)", Vector) = (0, 0, 0, 0)
        _Off3 ("Shadow 3 offset (uv to subtract)", Vector) = (0, 0, 0, 0)

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
            #include "UiSrgbComposite.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _ShadowTex;
            float4 _S1, _S2, _S3, _Off12, _Off3;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            // 미리곱 sRGB 위에 덮기
            float4 Over(float4 dst, float3 c, float a)
            {
                return float4(c * a + dst.rgb * (1.0 - a), a + dst.a * (1.0 - a));
            }

            float4 frag (v2f i) : SV_Target
            {
                float a1 = saturate(tex2D(_MainTex, i.uv - _Off12.xy).g) * _S1.a;
                float a2 = saturate(tex2D(_MainTex, i.uv - _Off12.zw).b) * _S2.a;
                float a3 = saturate(tex2D(_ShadowTex, i.uv - _Off3.xy).g) * _S3.a;

                float4 p = 0;
                p = Over(p, _S3.rgb, a3);
                p = Over(p, _S2.rgb, a2);
                p = Over(p, _S1.rgb, a1);
                p *= i.color.a;

                if (_UiGammaOut < 0.5 && p.a > 1e-5) p.rgb = UiSrgbToLin(p.rgb / p.a) * p.a;
                return p;
            }
            ENDCG
        }
    }
}
