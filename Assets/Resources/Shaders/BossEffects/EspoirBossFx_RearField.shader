
Shader "EspoirBossFx/RearField" {
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
      float uR;
      float uR0;
      float uA0;
      float uSpan;
      float uRev;
      float uT;
      float uFade;
      float uSeed;
      float uFlash;
      float uAlpha;
      float uStyle;
      float3 uC0;
      float3 uC1;
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
  if (r > uR || r < uR0) discard;
  float a = atan2(q.x, q.y);
  float d = glsl_mod(a - uA0 + 12.5663706, 6.2831853);
  float shown = uSpan * uRev;
  if (d > shown) discard;
  float rn = (r - uR0) / (uR - uR0);

  float lead = (1.0 - smoothstep(0.0, 0.22, shown - d)) * (1.0 - smoothstep(0.97, 1.0, uRev));

  float side = min(d, uSpan - d) * max(r, 0.5);
  float sideK = smoothstep(0.0, 0.9, side);

  float rim = smoothstep(uR - 2.2, uR - 0.15, r); 
  float cut = 1.0 - smoothstep(uR - 0.1, uR, r);

  float n = tex2D(uNoise, q * 0.07 + uSeed).r;
  float2 sc = float2(d * 0.9 + uSeed - uT * 0.05, rn * 2.2);
  if (uStyle > 1.5 && uStyle < 2.5) sc = float2(rn * 0.6 - uT * 0.35 + uSeed, d * 1.3);
  if (uStyle > 2.5 && uStyle < 3.5) sc = float2(rn * 0.5 + uSeed, (d + rn * 1.8 - uT * 0.6) * 0.7);
  float streak = tex2D(uNoise, sc).b;
  float swirl = tex2D(uNoise, float2(d * 0.45 + rn * 0.35 + uSeed * 0.3, rn * 0.9 - uT * 0.08)).r;
  float dark = step(0.5, uStyle) * step(uStyle, 1.5);
  float faint = step(3.5, uStyle);
  float sw = (uStyle > 1.5 && uStyle < 3.5) ? 0.3 : 0.16;

  float2 gq = floor(q * 9.0);
  float g = step(0.985 - 0.01 * dark, frac(sin(dot(gq, float2(127.1, 311.7)) + uSeed) * 43758.5453));

  float flowS = 0.0, sheen = 0.0, fg = 0.0;
  if (dark > 0.5) {
    float lanes = 30.0; 
    float v = rn * lanes + 0.45 * sin(d * 1.7 + rn * 2.3 + uSeed);
    float li = floor(v), lv = frac(v);
    float h1 = frac(sin(li * 91.7 + uSeed * 13.1) * 43758.5453);
    float sp = (0.6 + 0.9 * rn) * (0.75 + 0.5 * h1);           
    float arc = d * max(r, 0.5);                                  
    float L = 2.2 + 2.6 * h1;                                     
    float uu = (arc - uT * sp * 3.2) / L + h1 * 7.0;
    float ci = floor(uu), cu = frac(uu);
    float h2 = frac(sin(ci * 17.3 + li * 57.1 + uSeed) * 43758.5453);
    float h3 = frac(sin(ci * 41.9 + li * 11.3 + uSeed * 2.1) * 43758.5453);
    float on = step(0.08, h2);                                    
    float len = 0.55 + 0.37 * h3;                                  
    float cs = cu / len;
    float shape = pow(smoothstep(0.0, 0.9, cs), 1.6) * (1.0 - smoothstep(0.93, 1.0, cs)) * step(cs, 1.0);
    float wdt = 0.05 + 0.17 * h2 * sqrt(shape);                   
    float band = 1.0 - smoothstep(wdt, wdt + 0.22, abs(lv - 0.5)); 
    flowS = on * shape * band * (0.55 + 0.45 * h3);

    sheen = tex2D(uNoise, float2(d * 0.3 - uT * 0.1 + uSeed, rn * 0.45)).r - 0.5;
  }
  float base = lerp(lerp(0.36, 0.1, dark), 0.27, faint);
  float I = base + sw * (streak - 0.5) * (1.0 - dark) + 0.12 * (swirl - 0.5) * (1.0 - dark) + 0.1 * rn * (1.0 - dark)
          + (0.4 + 0.4 * dark) * rim * rim + 0.5 * lead + 0.35 * uFlash
          + 0.5 * flowS * (0.5 + 0.5 * rn) + 0.1 * sheen * dark;

  float3 col = ramp(clamp(I, 0.0, 1.0)) + g * uC3 * 0.35 * (0.4 + 0.6 * rn) * (1.0 - dark) + fg * uC3 * 0.0;
  float abase = lerp(lerp(0.62, 0.74, dark), 0.3, faint);
  float alpha = (abase + 0.12 * (streak - 0.5) * (1.0 - dark) + 0.25 * rim + 0.2 * lead) * cut * sideK * uAlpha;

  float dz = tex2D(uNoise, q * 0.11 + float2(uSeed * 1.7, 0.3)).a * 0.8 + (1.0 - rn) * 0.2;
  if (dz < uFade * 1.1) discard;
  alpha *= 1.0 - smoothstep(0.0, 0.08, uFade * 1.1 - dz + 0.08) * step(0.001, uFade);
  if (alpha < 0.01) discard;
  _fc = float4(col, alpha);

        return EspoirOut(_fc);
      }
      ENDCG
    }
  }
}