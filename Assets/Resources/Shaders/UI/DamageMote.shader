// 치명타 숫자 주변의 섬광과 빛 번짐을 그립니다.
// 사각형 q(−1~1)·크기: d = |q|, core = exp(−9d²), glow = exp(−3d), a = (core + 0.35·glow)·vA, c = 색·(1.6·core + 0.5·glow).
// HDR 색상은 톤 매핑으로 압축하고, 주변 빛 번짐은 사각형 3배 범위로 근사합니다.
// 정점 알파 = vA(나타남 · 사라짐). uv 0~1 이 사각형 3배(블룸 포함) 범위.
Shader "Espoir/UI/DamageMote"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("HDR color", Vector) = (1.95, 1.85, 1.65, 0)
        _Bloom ("Bloom amount", Float) = 0.35

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

            float4 _Color;
            float _Bloom;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 q = (i.uv * 2.0 - 1.0) * 3.0;
                float d = length(q);
                float inside = step(d, 1.0);
                float core = exp(-d * d * 9.0);
                float glow = exp(-d * 3.0);
                float va = i.color.a;
                float a = (core + glow * 0.35) * va * inside;
                float3 p = _Color.rgb * (core * 1.6 + glow * 0.5) * a;
                // 블룸 근사: 넘친 밝기가 사각형 밖으로 옅게 번짐
                float halo = exp(-d * d / 2.0) * _Bloom * va;
                p += _Color.rgb * halo * 0.5;
                a = saturate(max(a, halo * 0.6));
                float mx = max(p.r, max(p.g, p.b));
                if (mx > 1.0) p = p / mx * (1.0 + log(mx) * 0.35);
                p = min(min(p, 1.0), a);
                if (_UiGammaOut < 0.5 && a > 1e-5) p = UiSrgbToLin(p / a) * a;
                return float4(p, a);
            }
            ENDCG
        }
    }
}
