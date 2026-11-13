
Shader "EspoirBossFx/Basic" {
  Properties {
    [HideInInspector] _SrcBlend ("", Float) = 5
    [HideInInspector] _DstBlend ("", Float) = 10
    [HideInInspector] _ZWrite ("", Float) = 0
    [HideInInspector] _ZTest ("", Float) = 4
    [HideInInspector] _Cull ("", Float) = 2
  }
  SubShader {
    Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
    Pass {
      Blend [_SrcBlend] [_DstBlend] ZWrite [_ZWrite] ZTest [_ZTest] Cull [_Cull]
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_local __ ESPOIR_GLOW
      #include "UnityCG.cginc"
      #include "EspoirBossFxCommon.cginc"
      float3 _Color;
      float _Opacity;
      float4 vert(float4 v : POSITION) : SV_POSITION { return UnityObjectToClipPos(v); }
      float4 frag(float4 pos : SV_POSITION) : SV_Target {
#ifdef ESPOIR_GLOW
        if (EspoirGlowHidden(pos)) discard;
#endif
        return EspoirOut(float4(_Color, _Opacity));
      }
      ENDCG
    }
  }
}
