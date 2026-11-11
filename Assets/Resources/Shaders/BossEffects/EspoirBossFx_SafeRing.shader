
Shader "EspoirBossFx/SafeRing" {
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
      float uR;
      float uLeft;
      float uAlpha;
      float uFlash;
      float uT;
      float3 uC2;
      float3 uC3;
      struct appdata {
        float4 vertex : POSITION;
      };
      struct v2f {
        float4 pos : SV_POSITION;
        float2 vP : TEXCOORD0;
      };
      static float3 position; static float3 normal; static float2 uv;
      static float2 vP;
      static float4 _fc;

      v2f vert(appdata IN) {
        v2f o = (v2f)0;
        position = IN.vertex.xyz;

  vP = position.xy;
  o.pos = UnityObjectToClipPos(float4(position, 1.0));

        o.vP = vP;
        return o;
      }
      float4 frag(v2f i) : SV_Target {
#ifdef ESPOIR_GLOW
        if (EspoirGlowHidden(i.pos)) discard;
#endif
        vP = i.vP;
        _fc = 0;

  float2 q = float2(vP.x, -vP.y);
  float r = length(q);
  if (r > uR) discard;
  float a = atan2(q.x, -q.y);                         
  float f = glsl_mod(a + 6.2831853, 6.2831853) / 6.2831853;
  float band = 1.0 - smoothstep(0.1, 0.2, abs(r - (uR - 0.55)));
  float on = step(f, uLeft);
  float blink = uLeft < 0.18 ? 0.55 + 0.45 * step(0.0, sin(uT * 28.0)) : 1.0;
  float fill = (1.0 - smoothstep(uR - 1.2, uR, r)) * 0.0 + 0.07 * smoothstep(uR - 1.5, uR, r);
  float3 col = lerp(uC2, uC3, on * 0.85 + uFlash * 0.5);
  float alpha = (band * lerp(0.22, 0.95 * blink, on) + fill) * uAlpha;
  if (alpha < 0.01) discard;
  _fc = float4(col * (1.0 + 0.6 * uFlash), alpha);

        return EspoirOut(_fc);
      }
      ENDCG
    }
  }
}