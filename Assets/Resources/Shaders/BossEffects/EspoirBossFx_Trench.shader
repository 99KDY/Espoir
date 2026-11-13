
Shader "EspoirBossFx/Trench" {
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
      float uAlpha;
      float uLen;
      float uRev;
      float uStyle;
      float uHW;
      float uSeed;
      float uCool;
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

  
float h21(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
float ln(float v, float w) { float f = max(fwidth(v), 1e-4); return 1.0 - smoothstep(w - f, w + f, abs(v)); } 
float2 vor(float2 p) {
  float2 i = floor(p), f = frac(p); float d1 = 8.0, d2 = 8.0;
  for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) {
    float2 g = float2(float(x), float(y)), o = float2(h21(i + g), h21(i + g + 13.7));
    float d = length(g + o - f);
    if (d < d1) { d2 = d1; d1 = d; } else if (d < d2) d2 = d;
  }
  return float2(d1, d2 - d1);
}
float hexD(float2 p) { p = abs(p); return max(dot(p, float2(0.5, 0.8660254)), p.x); }

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

  if (vUv.y > uRev) discard;
  float x = (vUv.x * 2.0 - 1.0) * uHW, d = vUv.y * uLen, ax = abs(x);
  float3 col = ((float3)(0.0)); float a = 1.0;
  int s = int(uStyle + 0.5);
  if (s == 1) { 
    col = uGlow * (ln(ax - 3.0, 0.07) + exp(-pow(ax - 3.0, 2.0) * 5.0) * 0.35 + exp(-ax * ax * 1.5) * 0.18) + uCore * ln(ax - 3.0, 0.025) * 0.8;
  } else if (s == 2) { 
    float w = lerp(2.6, 0.9, uCool), g = exp(-ax * ax / (w * w) * 2.2);
    col = lerp(uGlow * 0.9, uCore * 1.1, smoothstep(0.55, 1.0, g)) * g + uGlow * 0.22 * exp(-ax * ax / (w * w * 4.0));
  } else if (s == 3) { 
    float sc = 1.0 - smoothstep(2.2, 4.0, ax + (h21(float2(floor(d * 1.2), uSeed)) - 0.5) * 0.5);
    float ember = ln(x + 0.22 * sin(d * 0.6 + uSeed), 0.05), glow = exp(-x * x * 3.0) * (1.0 - 0.7 * uCool);
    col = lerp(float3(0.025, 0.014, 0.008), uGlow * 1.4, clamp(ember + glow * 0.6, 0.0, 1.0)) + uCore * ember * 0.6;
    a = max(sc * 0.85, clamp(ember + glow * 0.5, 0.0, 1.0));
  } else if (s == 4) { 
    float2 v = vor(float2(x * 0.55, d * 0.35) + uSeed);
    float crack = 1.0 - smoothstep(0.0, 0.05 + fwidth(v.y), v.y), band = 1.0 - smoothstep(1.2, 5.0, ax);
    col = (uGlow * 1.2 + uCore * 0.4) * crack * band + uGlow * 0.14 * exp(-ax * ax * 0.5);
  } else if (s == 5) { 
    float c = ln(frac(d * 0.3 - ax * 0.35 - uT * 4.0) - 0.5, 0.07) * (1.0 - smoothstep(2.4, 3.8, ax));
    col = uGlow * c * 0.9 + uCore * ln(x, 0.035) * 0.5 + uGlow * exp(-ax * ax * 1.5) * 0.2;
  } else if (s == 6) { 
    float u = d * 0.6 + ax * 1.2, r = h21(float2(floor(u), uSeed));
    float stroke = ln(frac(u) - 0.5, 0.06) * step(0.35, r) * smoothstep(1.2, 2.0, ax) * (1.0 - smoothstep(3.5 + 2.0 * r, 6.0, ax));
    col = uGlow * (stroke * 0.8 + exp(-pow(ax - 1.3, 2.0) * 8.0) * 0.6) + uCore * ln(ax - 1.3, 0.03) * 0.6;
  } else if (s == 7) { 
    float lane = floor((x + uHW) / 0.9), r = h21(float2(lane, uSeed)), lx = frac((x + uHW) / 0.9) - 0.5;
    float f = frac(d * 0.06 - uT * (2.0 + 3.0 * r) + r);
    float m = ln(lx, 0.07) * smoothstep(0.0, 0.1, f) * (1.0 - smoothstep(0.25, 0.45, f)) * step(0.45, r) * (1.0 - smoothstep(2.0, 4.0, ax));
    col = uGlow * m * 1.1 + uCore * m * 0.3 + uGlow * exp(-ax * ax) * 0.16;
  } else if (s == 8) { 
    float2 p = float2(x, d) * 0.5, R = float2(1.0, 1.7320508), H = R * 0.5;
    float2 ga = glsl_mod(p, R) - H, gb = glsl_mod(p - H, R) - H, g = dot(ga, ga) < dot(gb, gb) ? ga : gb;
    float e = 0.5 - hexD(g), l = 1.0 - smoothstep(0.0, 0.03 + fwidth(e), e);
    col = uGlow * l * (1.0 - smoothstep(1.0, 4.5, ax)) + uGlow * exp(-ax * ax * 0.8) * 0.2;
  } else if (s == 9) { 
    float rip = ln(frac((ax - uT * 9.0) * 0.35) - 0.5, 0.05) * smoothstep(0.6, 1.5, ax) * exp(-ax * 0.3) * step(ax, uT * 9.0);
    col = uGlow * rip * 1.2 + uGlow * exp(-ax * ax * 1.2) * 0.3 + uCore * ln(x, 0.03) * 0.4;
  } else { 
    col = uGlow * exp(-ax * ax * 0.35) * 0.4 + uCore * (ln(x, 0.035) * 0.9 + exp(-ax * ax * 6.0) * 0.3);
  }
  float fade = (1.0 - smoothstep(uHW * 0.85, uHW, ax)) * exp(-d / 140.0) * smoothstep(0.0, 2.5, d) * uAlpha;
  if (s == 3) _fc = float4(col, a * fade);
  else _fc = float4(col * fade, 1.0);

        return EspoirOut(_fc);
      }
      ENDCG
    }
  }
}