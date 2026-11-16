// 데미지 글자 마스크에 색상, 그림자, 등장 섬광을 합성합니다.
// 텍스처(DamageGlyph 로 구움): r = 글자 m, g = 조금 흐린 글자 sw, b = 많이 흐린 글자 sb.
//   c = mix(c0, c1, v·0.6)·m ;  a = m + (1 − m)·sat(sw·0.12 + sb·0.7)·shade.w ;  c = mix(shade.rgb·(1 − m), c, m)
//   c += c2 · 훑는 빛(uIn) · m · (0.45 + 0.75·crit) ;  a *= uAlpha ;  미리곱 (c·a, a)
//   블룸(밝은 곳만 흐려 더함) 근사 + COMP_FS 톤(최대값 1 넘으면 c/m·(1 + log m·0.35)) → min(c, a)
// 입력 색상은 화면 기준 sRGB 값입니다.
// _UiGammaOut이 켜진 합성 구간에서는 sRGB 값을 그대로 출력합니다.
Shader "Espoir/UI/DamageFlat"
{
    Properties
    {
        [PerRendererData] _MainTex ("Glyph", 2D) = "black" {}
        _C0 ("c0 face top (web raw)", Vector) = (1, 1, 1, 0)
        _C1 ("c1 face bottom (web raw)", Vector) = (1, 1, 1, 0)
        _Shade ("shade rgb, strength", Vector) = (0.96, 0.66, 0.32, 0.72)
        _C2 ("c2 sweep light (HDR)", Vector) = (1.3, 1.16, 0.95, 0)
        _Fx ("uIn, uAlpha, crit, bloom", Vector) = (1, 1, 0, 1)

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
            float4 _C0, _C1, _Shade, _C2, _Fx;

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
                float4 s = tex2D(_MainTex, i.uv);
                float m = s.r, sw = s.g, sb = s.b;
                float vy = 1.0 - i.uv.y;                      // 웹 uv.y (위 = 0)
                float uIn = _Fx.x, crit = _Fx.z;

                float3 face = lerp(_C0.rgb, _C1.rgb, vy * 0.6);
                float3 c = face * m;
                float a = m + (1.0 - m) * saturate(sw * 0.12 + sb * 0.7) * _Shade.w;
                c = lerp(_Shade.rgb * (1.0 - m), c, m);

                float d = (i.uv.x - uIn * 1.4 + 0.2) * 5.0;
                float sweep = exp(-d * d) * (1.0 - uIn);
                c += _C2.rgb * sweep * m * (0.45 + 0.75 * crit);
                a *= _Fx.y * i.color.a;
                float3 p = c * a;

                // 여기서는 그 넘친 밝기를 많이 흐린 글자(sb)로 퍼뜨려 근사(면이 1 을 거의 넘지 않아 평소엔 아주 옅고, 훑는 빛 순간만 보임)
                float3 peak = face + _C2.rgb * (1.0 - uIn) * (0.45 + 0.75 * crit);
                float over = smoothstep(0.95, 1.45, max(peak.r, max(peak.g, peak.b)));
                float3 bloom = peak * over * sb * 1.3 * _Fx.w * _Fx.y * i.color.a;
                p += bloom;

                // COMP_FS 톤
                float mx = max(p.r, max(p.g, p.b));
                if (mx > 1.0) p = p / mx * (1.0 + log(mx) * 0.35);
                p = min(p, 1.0);
                a = saturate(max(a, max(bloom.r, max(bloom.g, bloom.b)) * 1.2));
                p = min(p, a);

                // 화면: 구간 안이면 sRGB 숫자 그대로, 아니면 선형으로
                if (_UiGammaOut < 0.5 && a > 1e-5) p = UiSrgbToLin(p / a) * a;
                return float4(p, a);
            }
            ENDCG
        }
    }
}
