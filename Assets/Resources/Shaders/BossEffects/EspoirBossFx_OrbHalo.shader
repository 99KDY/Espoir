
Shader "EspoirBossFx/OrbHalo" {
  Properties {
    [HideInInspector] _SrcBlend ("", Float) = 5
    [HideInInspector] _DstBlend ("", Float) = 1
    [HideInInspector] _ZWrite ("", Float) = 0
    [HideInInspector] _ZTest ("", Float) = 4
    [HideInInspector] _Cull ("", Float) = 0
  }
  SubShader {
    Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
    Pass {
      Blend [_SrcBlend] [_DstBlend] ZWrite [_ZWrite] ZTest [_ZTest] Cull [_Cull]
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_local __ ESPOIR_GLOW
      #pragma target 3.5
      #include "UnityCG.cginc"
      #include "EspoirBossFxCommon.cginc"
      float uCharge;
      float uAlpha;
      float3 uC2;
      float3 uC3;
      struct appdata {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
      };
      struct v2f {
        float4 pos : SV_POSITION;
        float2 vUv : TEXCOORD0;
      };
      static float3 position; static float3 normal; static float2 uv;
      static float2 vUv;
      static float4 _fc;

  

      v2f vert(appdata IN) {
        v2f o = (v2f)0;
        position = IN.vertex.xyz;
        uv = IN.uv;
 vUv = uv; o.pos = UnityObjectToClipPos(float4(position, 1.0)); 
        o.vUv = vUv;
        return o;
      }
      float4 frag(v2f i) : SV_Target {
#ifdef ESPOIR_GLOW
        if (EspoirGlowHidden(i.pos)) discard;
#endif
        vUv = i.vUv;
        _fc = 0;

  float2 p = vUv * 2.0 - 1.0; float r = length(p);
  float out1 = smoothstep(0.24, 0.27, r), g = exp(-max(r - 0.25, 0.0) * 6.5) * out1 * (1.0 - smoothstep(0.85, 1.0, r));
  float3 col = lerp(uC2, uC3, exp(-max(r - 0.25, 0.0) * 14.0)) * g * (0.3 + 0.45 * uCharge);
  _fc = float4(col * uAlpha, 1.0);

        return EspoirOut(_fc);
      }
      ENDCG
    }
  }
}