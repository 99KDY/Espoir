Shader "Espoir/Environment/Chapter3/GlassLight"
{
    // 색유리를 통과해 바닥에 맺힌 빛을 그립니다.
    // 창 그림을 밉을 크게 올려 읽어 색 덩어리만 남김. 곱하기 판(물들임) + 더하기 판(옅은 번짐) 두 겹.
    // uv1.x = 가장자리 풀림(aEdge), uv1.y = 창 위아래 거리(aDist)
    Properties
    {
        _MainTex ("Light Art", 2D) = "white" {}
        _Amount ("Amount", Float) = 0.7
        [Toggle] _Multiply ("Multiply (off = Additive)", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 3
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+5" "IgnoreProjector"="True" }
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Amount;
            float _Multiply;

            struct SVertexInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct SVertexOutput
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 edgeDist : TEXCOORD1;
            };

            SVertexOutput Vert(SVertexInput v)
            {
                SVertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.edgeDist = v.uv1;
                return o;
            }

            float4 Frag(SVertexOutput i) : SV_Target
            {
                float vE = i.edgeDist.x;
                float vD = i.edgeDist.y;
                float3 c = tex2Dbias(_MainTex, float4(i.uv, 0.0, 4.2 + vD * 0.8)).rgb * 0.6
                    + tex2Dbias(_MainTex, float4(i.uv, 0.0, 5.6)).rgb * 0.4;
                float k = smoothstep(0.0, 1.0, vE) * lerp(1.0, 0.72, vD);
                if (_Multiply > 0.5) return float4(lerp(1.0, c * 0.9 + 0.2, k * _Amount), 1.0);
                return float4(c * k * _Amount, 1.0);
            }
            ENDCG
        }
    }
}
