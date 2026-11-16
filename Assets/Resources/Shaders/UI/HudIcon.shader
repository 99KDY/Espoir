// 아이콘 이미지 + CSS filter: drop-shadow(color 0 0 blur) 한 겹. 그림자 색 · 흐림 · 아이콘 글자색(uv2.z, 흰 그림에 곱함)은 CHudIconShadow 가 정점
// 데이터로 넘긴다.
Shader "Espoir/UI/HudIcon"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" =
            "Plane"
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

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
                float4 uv3 : TEXCOORD3;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                nointerpolation float4 shadow : TEXCOORD1;
                nointerpolation float4 size : TEXCOORD2;
                float4 world : TEXCOORD3;
                nointerpolation float4 uvRect : TEXCOORD4;
            };

            sampler2D _MainTex;
            float4 _ClipRect;

            v2f vert (appdata v)
            {
                v2f o;
                o.world = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv0;
                o.shadow = v.uv1;
                o.size = v.uv2;
                o.uvRect = v.uv3;
                return o;
            }

            float Inside(float2 uv, float4 r)
            {
                return step(r.x, uv.x) * step(uv.x, r.z) * step(r.y, uv.y) * step(uv.y, r.w);
            }

            float SampleA(float2 uv, float4 r)
            {
                return tex2Dlod(_MainTex, float4(clamp(uv, r.xy, r.zw), 0, 0)).a * Inside(uv, r);
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 rect = i.uvRect;
                float4 tex = tex2D(_MainTex, clamp(i.uv, rect.xy, rect.zw)) * Inside(i.uv, rect);
                float4 content = float4(HudLinearToSrgb(tex.rgb) * tex.a, tex.a);
                content.rgb *= HudUnpack(i.size.z, 1.0).rgb;

                float4 sc = HudUnpack(i.shadow.x, i.shadow.y);
                float sigma = i.shadow.z * 0.5;
                if (sc.a > 0.0 && sigma > 0.0)
                {
                    float2 px = (rect.zw - rect.xy) / max(i.size.xy, 1.0);
                    float stepPx = sigma * 0.6;
                    float sum = 0;
                    float wsum = 0;
                    [unroll]
                    for (int y = -4; y <= 4; y++)
                    {
                        [unroll]
                        for (int x = -4; x <= 4; x++)
                        {
                            float2 o = float2(x, y) * stepPx;
                            float w = exp(-dot(o, o) / (2.0 * sigma * sigma));
                            sum += SampleA(i.uv + o * px, rect) * w;
                            wsum += w;
                        }
                    }
                    float sa = sc.a * sum / wsum;
                    content = HudOver(content, float4(sc.rgb * sa, sa));
                }

                content *= i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                content *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                return HudOut(content);
            }
            ENDCG
        }
    }
}
