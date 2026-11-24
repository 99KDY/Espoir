Shader "Hidden/Espoir/NpcPreAORestore"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            Name "PRE_AO_RESTORE"
            Cull Off
            ZWrite Off
            ZTest LEqual
            Offset -1, -1

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ _CREATURE_FUR
            #include "UnityCG.cginc"
            #include "NpcCreatureFur.cginc"

            sampler2D _PreAOColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 restPos : TEXCOORD2;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
            #if defined(_CREATURE_FUR)
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz + _Jig.xyz * JigMask(v.restPos);
                o.pos = UnityWorldToClipPos(worldPos);
            #endif
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return tex2D(_PreAOColor, i.screenPos.xy / i.screenPos.w);
            }
            ENDCG
        }
    }
    Fallback Off
}
