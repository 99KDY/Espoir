Shader "Espoir/UI/HudComboRail"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
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
            #include "HudCommon.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
                float4 uv3 : TEXCOORD3;
                float4 tangent : TANGENT;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float4 local : TEXCOORD0;
                nointerpolation float4 c01 : TEXCOORD1;
                nointerpolation float4 c23 : TEXCOORD2;
                nointerpolation float4 bs : TEXCOORD3;
                nointerpolation float4 s2 : TEXCOORD4;
                nointerpolation float3 prm : TEXCOORD5;
                float4 world : TEXCOORD6;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _ClipRect;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.local = v.uv0;
                o.c01 = v.uv1;
                o.c23 = v.uv2;
                o.bs = v.uv3;
                o.s2 = v.tangent;
                o.prm = v.normal;
                return o;
            }

            float4 Premul(float4 c)
            {
                return float4(c.rgb * c.a, c.a);
            }
            float RectShadow(float2 p, float2 hs, float sigma)
            {
                float k = 0.70710678 / max(sigma, 1e-3);
                return 0.25 * (HudErf((p.x + hs.x) * k) - HudErf((p.x - hs.x) * k)) *
                    (HudErf((p.y + hs.y) * k) - HudErf((p.y - hs.y) * k));
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 p = i.local.xy;
                float2 hs = max(i.local.zw, 0.0);
                float bw = i.prm.x;
                float brightness = i.prm.y;
                int packed = (int)round(i.prm.z);
                int kind = packed % 4;
                int stops = (packed / 4) % 8;
                int seg = (packed / 32) % 8;
                int segCount = max(packed / 256, 1);

                float4 s0 = Premul(HudUnpack(i.c01.x, i.c01.y));
                float4 s1 = Premul(HudUnpack(i.c01.z, i.c01.w));
                float4 s2c = Premul(HudUnpack(i.c23.x, i.c23.y));
                float4 s3 = Premul(HudUnpack(i.c23.z, i.c23.w));
                float4 border = HudUnpack(i.bs.x, i.bs.y);
                float4 sh1 = HudUnpack(i.bs.z, i.bs.w);
                float4 sh2 = HudUnpack(i.s2.x, i.s2.y);
                float sigma1 = i.s2.z;
                float sigma2 = i.s2.w;
                float2 q = abs(p) - hs;
                float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
                float fw = max(fwidth(d), 1e-4);
                float cov = (hs.x > 0.0 && hs.y > 0.0) ? saturate(0.5 - d / fw) : 0.0;
                float2 qi = abs(p) - (hs - bw);
                float dIn = length(max(qi, 0.0)) + min(max(qi.x, qi.y), 0.0);
                float covIn = bw > 0.0 ? saturate(0.5 - dIn / fw) : cov;
                float4 fill = s0;
                if (kind > 0 && stops >= 2)
                {
                    float t;
                    if (kind == 1)
                    {
                        float u = hs.x > 0.0 ? (p.x + hs.x) / (2.0 * hs.x) : 0.0;
                        t = (u + seg) / segCount;
                    }
                    else
                    {
                        t = hs.y > 0.0 ? (p.y + hs.y) / (2.0 * hs.y) : 0.0;
                    }

                    float x = saturate(t) * (stops - 1);
                    int i0 = min((int)floor(x), stops - 2);
                    float4 a = i0 == 0 ? s0 : (i0 == 1 ? s1 : s2c);
                    float4 b = i0 == 0 ? s1 : (i0 == 1 ? s2c : s3);
                    fill = lerp(a, b, saturate(x - i0));
                }

                float ring = saturate(cov - covIn);
                float4 bp = float4(border.rgb, 1.0) * border.a * ring;
                float4 box = bp + (1.0 - bp.a) * fill * cov;
                float4 shadow = 0;
                if (sh2.a > 0.0 && sigma2 > 0.0)
                {
                    float a2 = sh2.a * RectShadow(p, hs, sigma2);
                    shadow = float4(sh2.rgb * a2, a2);
                }

                if (sh1.a > 0.0 && sigma1 > 0.0)
                {
                    float a1 = sh1.a * RectShadow(p, hs, sigma1);
                    shadow = float4(sh1.rgb * a1, a1) + (1.0 - a1) * shadow;
                }

                shadow *= 1.0 - cov;
                float4 res = box + (1.0 - box.a) * shadow;
                res.rgb = min(res.rgb * brightness, res.a);
                res *= i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                res *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(res.a - 0.001);
                #endif
                return HudOut(res);
            }
            ENDCG
        }
    }
}