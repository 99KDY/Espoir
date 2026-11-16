// 숫자 한 글자를 작은 렌더 텍스처에 굽습니다.
//   r = 글자 덮임(캔버스 fillText), g = 조금 흐린 글자(σ = max(1.2, 0.06·크기) px), b = 많이 흐린 글자(σ = max(3, 0.22·크기) px)
// 로 굽는다. 패스 0: TMP 글자 메쉬(SDF 아틀라스)를 덮임으로 그림. 패스 1 · 2: 가로 · 세로 가우스 흐림(g, b 두 σ 를 한 번에).
Shader "Hidden/Espoir/UI/DamageGlyph"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Sigma ("Sigma texel (g, b)", Vector) = (2, 8, 0, 0)
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        // 0: 글자 덮임 (SDF → 화면 미분으로 안티에일리어스). 겹치는 글자 사각형은 큰 값으로 합침
        Pass
        {
            BlendOp Max
            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float sdf = tex2D(_MainTex, i.uv).a;
                float w = max(fwidth(sdf), 1e-5);
                float m = saturate((sdf - 0.5) / w + 0.5);
                return float4(m, 0, 0, m);
            }
            ENDCG
        }

        // 1: 가로 흐림 — r 그대로, g · b = r 를 두 σ 로 가로 흐림
        Pass
        {
            Blend Off

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Sigma;

            float Blur1(float2 uv, float2 dir, float sigma)
            {
                int r = (int)ceil(sigma * 3.0);
                float sum = 0;
                float wsum = 0;
                [loop]
                for (int k = -r; k <= r; k++)
                {
                    float w = exp(-(k * k) / (2.0 * sigma * sigma));
                    sum += tex2Dlod(_MainTex, float4(uv + dir * k, 0, 0)).r * w;
                    wsum += w;
                }
                return sum / wsum;
            }

            float4 frag (v2f_img i) : SV_Target
            {
                float2 dir = float2(_MainTex_TexelSize.x, 0);
                float m = tex2D(_MainTex, i.uv).r;
                return float4(m, Blur1(i.uv, dir, _Sigma.x), Blur1(i.uv, dir, _Sigma.y), 1);
            }
            ENDCG
        }

        // 2: 세로 흐림 — g · b 를 각자 σ 로 세로 흐림
        Pass
        {
            Blend Off

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Sigma;

            float4 frag (v2f_img i) : SV_Target
            {
                float2 dir = float2(0, _MainTex_TexelSize.y);
                float4 c = tex2D(_MainTex, i.uv);
                float g = 0, b = 0, wg = 0, wb = 0;
                int r = (int)ceil(_Sigma.y * 3.0);
                [loop]
                for (int k = -r; k <= r; k++)
                {
                    float4 s = tex2Dlod(_MainTex, float4(i.uv + dir * k, 0, 0));
                    float w1 = exp(-(k * k) / (2.0 * _Sigma.x * _Sigma.x));
                    float w2 = exp(-(k * k) / (2.0 * _Sigma.y * _Sigma.y));
                    g += s.g * w1;
                    wg += w1;
                    b += s.b * w2;
                    wb += w2;
                }
                return float4(c.r, g / wg, b / wb, 1);
            }
            ENDCG
        }
    }
}
