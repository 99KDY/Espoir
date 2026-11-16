// 미니맵의 가장자리 투명도, 채도와 톤 곡선을 적용합니다.
// 입력은 미니맵 카메라의 HDR 렌더 텍스처이며 _Tone 설정에 따라 밝기를 압축합니다.
// 예비(구운 이미지): 칸 안 좌표 p(−1~1)를 _UvCenter · _UvAxisX · _UvAxisY 로 지도 UV 로 바꾸고, 톤은 구울 때 적용됐으므로 _Tone 0.
Shader "Espoir/UI/MinimapMap"
{
    Properties
    {
        [PerRendererData] _MainTex ("Map", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Fade ("Edge Fade Width", Range(0, 1)) = 0.55
        _Floor ("Edge Alpha", Range(0, 1)) = 0.28
        _Saturation ("Saturation", Range(0, 2)) = 1.12
        _Gain ("Gain", Range(0, 2)) = 1
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Tone ("HDR Tone Curve", Range(0, 1)) = 1
        _UvCenter ("UV Center", Vector) = (0.5, 0.5, 0, 0)
        _UvAxisX ("UV Axis X", Vector) = (0.5, 0, 0, 0)
        _UvAxisY ("UV Axis Y", Vector) = (0, 0.5, 0, 0)

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
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "False"
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
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Fade;
            float _Floor;
            float _Saturation;
            float _Gain;
            float _Alpha;
            float _Tone;
            float4 _UvCenter;
            float4 _UvAxisX;
            float4 _UvAxisY;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;

                // 각진 사각(shape 3): d = max(|x|, |y|), 테두리는 fwidth 로 부드럽게, 가장자리로 갈수록 floor 까지 비침
                float d = max(abs(p.x), abs(p.y));
                float aa = fwidth(d) * 1.5;
                float mask = 1.0 - smoothstep(1.0 - aa, 1.0, d);
                float fd = 1.0 - smoothstep(1.0 - _Fade, 1.0, d);
                mask *= lerp(_Floor, 1.0, fd);

                float2 uv = _UvCenter.xy + p.x * _UvAxisX.xy + p.y * _UvAxisY.xy;
                float3 c = tex2D(_MainTex, uv).rgb;

                // 0.5를 넘는 밝기만 x / (1 + 2.2x)로 압축합니다.
                c = min(c, 4.0);
                float3 over = max(c - 0.5, 0.0);
                c = lerp(c, min(c, 0.5) + over / (1.0 + over * 2.2), _Tone);

                float l = dot(min(c, 2.0), float3(0.3, 0.59, 0.11));
                float3 col = lerp(l.xxx, min(c, 1.6), _Saturation) * i.color.rgb * _Gain;
                // 선형 지도 색을 sRGB로 변환하며, sRGB 합성 구간에서는 변환한 값을 그대로 출력합니다.
                return fixed4(UiStraightOut(col), _Alpha * mask * i.color.a);
            }
            ENDCG
        }
    }
}
