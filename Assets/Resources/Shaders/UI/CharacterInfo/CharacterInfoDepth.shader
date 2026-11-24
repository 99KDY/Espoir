// 양면 재질까지 깊이를 기록해 캐릭터가 배경 흐림에 포함되지 않도록 한다.
Shader "Hidden/Espoir/CharacterInfoDepth"
{
    SubShader
    {
        Pass
        {
            Cull Off
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            struct SVaryings
            {
                float4 position : SV_POSITION;
                float eyeDepth : TEXCOORD0;
            };

            SVaryings Vert(appdata_base v)
            {
                SVaryings o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.eyeDepth = -UnityObjectToViewPos(v.vertex).z;
                return o;
            }

            float4 Frag(SVaryings i) : SV_Target
            {
                return float4(i.eyeDepth, 0.0, 0.0, 1.0);
            }
            ENDCG
        }
    }
}
