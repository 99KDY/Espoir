
Shader "EspoirBossFx/Bolt" {
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
      float3 uCore;
      float3 uGlow;
      float uAlpha;
      struct appdata {
        float4 vertex : POSITION;
        float4 c0 : TEXCOORD1;
      };
      struct v2f {
        float4 pos : SV_POSITION;
        float vX : TEXCOORD0;
        float vS : TEXCOORD1;
      };
      static float3 position; static float3 normal; static float2 uv;
      static float aX;
      static float aS;
      static float vX;
      static float vS;
      static float4 _fc;

   

      v2f vert(appdata IN) {
        v2f o = (v2f)0;
        position = IN.vertex.xyz;
        aX = IN.c0.x;
        aS = IN.c0.y;
 vX = aX; vS = aS; o.pos = UnityObjectToClipPos(float4(position, 1.0)); 
        o.vX = vX;
        o.vS = vS;
        return o;
      }
      float4 frag(v2f i) : SV_Target {
#ifdef ESPOIR_GLOW
        if (EspoirGlowHidden(i.pos)) discard;
#endif
        vX = i.vX;
        vS = i.vS;
        _fc = 0;

  float x = abs(vX);
  float core = exp(-x * x * 40.0), glow = exp(-x * 4.0) * 0.4;
  float ends = smoothstep(0.0, 0.04, vS) * (1.0 - smoothstep(0.96, 1.0, vS));
  _fc = float4((uCore * core + uGlow * glow) * uAlpha * ends, 1.0);

        return EspoirOut(_fc);
      }
      ENDCG
    }
  }
}