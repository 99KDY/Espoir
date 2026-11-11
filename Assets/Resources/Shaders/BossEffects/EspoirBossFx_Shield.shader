
Shader "EspoirBossFx/Shield" {
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
      float uFlash;
      float3 uC0;
      float3 uC1;
      float3 uC2;
      float3 uC3;
      struct appdata {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        float2 uv : TEXCOORD0;
      };
      struct v2f {
        float4 pos : SV_POSITION;
        float3 vN : TEXCOORD0;
        float3 vW : TEXCOORD1;
        float2 vUv : TEXCOORD2;
      };
      static float3 position; static float3 normal; static float2 uv;
      static float3 vN;
      static float3 vW;
      static float2 vUv;
      static float4 _fc;

float3 ramp(float i) {
  float3 c = uC0;
  c = lerp(c, uC1, smoothstep(0.2, 0.26, i));
  c = lerp(c, uC2, smoothstep(0.45, 0.51, i));
  c = lerp(c, uC3, smoothstep(0.72, 0.78, i));
  return c;
}
float hexD(float2 p) { p = abs(p); return max(dot(p, float2(0.5, 0.8660254)), p.x); }

      v2f vert(appdata IN) {
        v2f o = (v2f)0;
        position = IN.vertex.xyz;
        normal = IN.normal;
        uv = IN.uv;

  vUv = uv;
  float4 w = mul(unity_ObjectToWorld, float4(position, 1.0));
  vW = w.xyz;
  vN = normalize(mul((float3x3)unity_ObjectToWorld, normal));
  o.pos = mul(UNITY_MATRIX_VP, w);

        o.vN = vN;
        o.vW = vW;
        o.vUv = vUv;
        return o;
      }
      float4 frag(v2f i) : SV_Target {
#ifdef ESPOIR_GLOW
        if (EspoirGlowHidden(i.pos)) discard;
#endif
        vN = i.vN;
        vW = i.vW;
        vUv = i.vUv;
        _fc = 0;

  float3 V = normalize(_WorldSpaceCameraPos - vW);
  float fres = pow(max(1.0 - abs(dot(normalize(vN), V)), 0.0), 2.2);

  float2 uv = float2(vUv.x * 30.0, vUv.y * 8.0 * 1.7320508);
  float2 rr = float2(1.0, 1.7320508), h = rr * 0.5;
  float2 ga = glsl_mod(uv, rr) - h, gb = glsl_mod(uv - h, rr) - h;
  float2 gv = dot(ga, ga) < dot(gb, gb) ? ga : gb;
  float hexLine = 1.0 - smoothstep(0.03, 0.08, 0.5 - hexD(gv));
  float2 cell = uv - gv;
  float cellN = tex2D(uNoise, cell * 0.031 + float2(uT * 0.05, 0.0)).r;

  float flow = tex2D(uNoise, float2(vUv.x * 3.0, vUv.y * 1.5 - uT * 0.35)).g;
  float base = smoothstep(0.12, 0.0, vUv.y);
  float I = 0.3 + 0.55 * fres + 0.35 * hexLine * (0.35 + 0.65 * cellN) + 0.2 * (flow - 0.5) + 0.4 * base + 0.4 * uFlash;
  float3 col = ramp(clamp(I, 0.0, 1.0));
  float alpha = (0.06 + 0.55 * fres + 0.32 * hexLine * (0.3 + 0.7 * cellN) + 0.35 * base + 0.3 * uFlash) * uAlpha;
  if (alpha < 0.01) discard;
  _fc = float4(col, alpha);

        return EspoirOut(_fc);
      }
      ENDCG
    }
  }
}