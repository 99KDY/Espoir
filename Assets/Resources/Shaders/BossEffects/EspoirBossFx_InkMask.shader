
Shader "Hidden/EspoirBossFx/InkMask" {
  SubShader {
    Pass {
      ZWrite On ZTest LEqual Cull Off
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"
      float4 vert(float4 v : POSITION) : SV_POSITION { return UnityObjectToClipPos(v); }
      float4 frag() : SV_Target { return 1; }
      ENDCG
    }
  }
}
