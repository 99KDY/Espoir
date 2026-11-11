
Shader "EspoirBossFx/LbPillar" {
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
      sampler2D uNoise;
      float uT;
      float uAlpha;
      float uSeed;
      float3 uCore;
      float3 uGlow;
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

  float x = vUv.x * 2.0 - 1.0, y = vUv.y;
  float wob = (tex2D(uNoise, float2(y * 1.5 - uT * 2.0, uSeed)).r - 0.5) * 0.25;
  float xx = abs(x - wob);
  float core = exp(-xx * xx * 260.0), glow = exp(-xx * 9.0) * 0.4;
  float flow = tex2D(uNoise, float2(x * 0.6 + uSeed, y * 2.0 - uT * 3.5)).g;
  float fade = (1.0 - smoothstep(0.55, 1.0, y)) * smoothstep(0.0, 0.03, y) * (1.0 - smoothstep(0.85, 1.0, abs(x)));
  _fc = float4((uCore * core * (0.7 + 0.6 * flow) + uGlow * glow * (0.5 + 0.8 * flow)) * uAlpha * fade, 1.0);

        return EspoirOut(_fc);
      }
      ENDCG
    }
  }
}