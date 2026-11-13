
Shader "EspoirBossFx/Hook" {
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
      float3 uCenter;
      float uExpand;
      float uRigid;
      float uGrow;
      float uFloor;
      float uHead;
      float uTail;
      float uFade;
      float uSeed;
      float uAlpha;
      float uHot;
      float uLanes;
      float uGrain;
      float uDark;
      float uCap;
      float uCoreAmt;
      float uFlash;
      float3 uC0;
      float3 uC1;
      float3 uC2;
      float3 uC3;
      float3 uCore;
      struct appdata {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
      };
      struct v2f {
        float4 pos : SV_POSITION;
        float2 vUv : TEXCOORD0;
        float3 vWorld : TEXCOORD1;
      };
      static float3 position; static float3 normal; static float2 uv;
      static float2 vUv;
      static float3 vWorld;
      static float4 _fc;

float h1(float x) { return frac(sin(x * 127.1 + uSeed * 31.7) * 43758.5453); }
float h2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7)) + uSeed * 17.3) * 43758.5453); }
float3 ramp(float i) {
  float3 c = uC0;
  c = lerp(c, uC1, smoothstep(0.13, 0.23, i));
  c = lerp(c, uC2, smoothstep(0.35, 0.45, i));
  c = lerp(c, uC3, smoothstep(0.75, 0.85, i));
  return c;
}

      v2f vert(appdata IN) {
        v2f o = (v2f)0;
        position = IN.vertex.xyz;
        uv = IN.uv;

  vUv = uv;
  float3 p = uCenter + (position - uCenter) * (1.0 - uGrow);
  float3 d = p - uCenter;
  p += normalize(d) * uExpand * lerp(0.35 + uv.y, 1.0 + 0.25 * uv.y, uRigid);
  vWorld = mul(unity_ObjectToWorld, float4(p, 1.0)).xyz;
  vWorld.y = max(vWorld.y, EspoirStagePoint(float3(0.0, uFloor, 0.0)).y); 
  o.pos = mul(UNITY_MATRIX_VP, float4(vWorld, 1.0));

        o.vUv = vUv;
        o.vWorld = vWorld;
        return o;
      }
      float4 frag(v2f i) : SV_Target {
#ifdef ESPOIR_GLOW
        if (EspoirGlowHidden(i.pos)) discard;
#endif
        vUv = i.vUv;
        vWorld = i.vWorld;
        _fc = 0;

  float s = vUv.x, y = vUv.y;
  float lag = uHead - s;
  if (lag < 0.0) discard;
  float span = max(uHead - uTail, 0.001);
  float age = clamp(lag / span, 0.0, 1.0);            
  float c = abs(y - 0.5) * 2.0;                        

  float env = lerp(1.0, 0.16, pow(age, 1.15));
  if (lag < uCap) { float dx = 1.0 - lag / uCap; env = min(env, sqrt(max(0.0, 1.0 - dx * dx))); }

  env *= 1.0 - 0.75 * smoothstep(0.0, 1.0, uFade);
  env = min(1.0, env * (1.0 + 0.35 * uFlash));

  float L = y * uLanes, lane = floor(L), ly = frac(L);
  float r = h1(lane), r2 = h1(lane + 7.3);
  float len = lerp(1.0, 0.5, c) * lerp(0.72, 1.0, r);
  len *= 1.0 - smoothstep(r2 * 0.4, r2 * 0.4 + 0.5, uFade);
  float aw = fwidth(age) * 1.5 + 0.002;
  float laneCut = 1.0 - smoothstep(len - aw, len + aw, age + (1.0 - ly) * 0.03);
  float gap = lerp(1.0, smoothstep(0.0, 0.14, ly) * smoothstep(1.0, 0.86, ly), step(0.5, r2) * 0.9);

  float solid = 1.0 - smoothstep(uCap * 0.8, uCap * 2.6, lag);
  laneCut = max(laneCut, solid);
  gap = lerp(gap, 1.0, solid);

  float cw = fwidth(c) * 1.5 + 0.003;
  float body = 1.0 - smoothstep(env - cw, env + cw, c);
  float rim = smoothstep(env - 0.22, env - 0.05, c);    

  float headGlow = exp(-lag / max(uCap * 0.75, 0.001));
  float I = 0.26 + 0.24 * (1.0 - c);
  I *= lerp(1.0, 0.55, age);
  I += headGlow * (0.55 + 0.35 * (1.0 - c)) * (0.6 + 0.4 * uHot);
  I += rim * 0.32 * (1.0 - age) * (1.0 + uDark);
  I *= 1.0 - 0.5 * uFade;
  I += uFlash * (0.35 + 0.4 * (1.0 - c)) * (1.0 - 0.5 * age);

  I = lerp(I, I * lerp(0.25, 1.0, max(rim, headGlow)), uDark);

  float3 col = ramp(clamp(I, 0.0, 1.0));

  float core = (1.0 - smoothstep(0.06, 0.12, c / max(env, 0.05))) * (1.0 - uDark) * (1.0 - headGlow * 0.7);
  col = lerp(col, uCore, core * uCoreAmt);

  if (uGrain > 0.0) {
    float2 g = float2(s * 300.0 - uHead * 40.0, y * 40.0);
    float gr = step(0.96, h2(floor(g))) * (0.5 + 0.5 * (1.0 - age));
    col += gr * uC3 * 0.3 * uGrain;
  }
  float a = body * laneCut * gap * uAlpha;
  a *= lerp(smoothstep(0.1, 0.3, I + 0.1), 0.95, uDark);
  if (a < 0.01) discard;
  _fc = float4(col, a);

        return EspoirOut(_fc);
      }
      ENDCG
    }
  }
}