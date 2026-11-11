
Shader "EspoirBossFx/PunchFlash" {
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
      float uT;
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

  vUv = uv * 2.0 - 1.0;
  o.pos = UnityObjectToClipPos(float4(position, 1.0));

        o.vUv = vUv;
        return o;
      }
      float4 frag(v2f i) : SV_Target {
#ifdef ESPOIR_GLOW
        if (EspoirGlowHidden(i.pos)) discard;
#endif
        vUv = i.vUv;
        _fc = 0;

  float r = length(vUv);
  float k = 1.0 - uT;
  float core = exp(-r * r * 14.0) * k * k;
  float halo = exp(-r * r * 3.5) * k * 0.45;
  float a = clamp(core * 1.6 + halo, 0.0, 1.0);
  if (a < 0.01) discard;
  _fc = float4(lerp(uC2, uC3, clamp(core * 1.5, 0.0, 1.0)), a);

        return EspoirOut(_fc);
      }
      ENDCG
    }
  }
}