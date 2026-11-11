
Shader "EspoirBossFx/OrbCore" {
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
      float uCharge;
      float uFlash;
      float uAlpha;
      float uSeed;
      float uStyle;
      float3 uC1;
      float3 uC2;
      float3 uC3;
      float3 uS0;
      float3 uS1;
      float3 uS2;
      float3 uS3;
      struct appdata {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
      };
      struct v2f {
        float4 pos : SV_POSITION;
        float3 vN : TEXCOORD0;
        float3 vW : TEXCOORD1;
        float3 vL : TEXCOORD2;
      };
      static float3 position; static float3 normal; static float2 uv;
      static float3 vN;
      static float3 vW;
      static float3 vL;
      static float4 _fc;

float3 sandRamp(float i) { float3 c = uS0; c = lerp(c, uS1, smoothstep(0.13, 0.23, i)); c = lerp(c, uS2, smoothstep(0.35, 0.45, i)); return lerp(c, uS3, smoothstep(0.75, 0.85, i)); }
float nz(float2 q) { return tex2D(uNoise, q).r; }
float3 goldRamp(float i) { float3 c = uC1; c = lerp(c, uC2, smoothstep(0.42, 0.48, i)); return lerp(c, uC3, smoothstep(0.7, 0.76, i)); }

      v2f vert(appdata IN) {
        v2f o = (v2f)0;
        position = IN.vertex.xyz;
        normal = IN.normal;

  vL = position;
  float4 w = mul(unity_ObjectToWorld, float4(position, 1.0));
  vW = w.xyz;
  vN = normalize(mul((float3x3)unity_ObjectToWorld, normal));
  o.pos = mul(UNITY_MATRIX_VP, w);

        o.vN = vN;
        o.vW = vW;
        o.vL = vL;
        return o;
      }
      float4 frag(v2f i) : SV_Target {
#ifdef ESPOIR_GLOW
        if (EspoirGlowHidden(i.pos)) discard;
#endif
        vN = i.vN;
        vW = i.vW;
        vL = i.vL;
        _fc = 0;

  float3 V = normalize(_WorldSpaceCameraPos - vW), N = normalize(vN), p = normalize(vL);
  float ndv = clamp(dot(N, V), 0.0, 1.0), rim = pow(max(1.0 - ndv, 0.0), 2.0);
  float3 col; float a = uAlpha;
  if (uStyle < 0.5) {

    float lat = asin(clamp(p.y, -1.0, 1.0)), lon = atan2(p.z, p.x);
    float lanes = 14.0, v = (lat / 3.1415927 + 0.5) * lanes + 0.35 * sin(lon * 2.0 + uSeed);
    float li = floor(v), lv = frac(v);
    float h1 = frac(sin(li * 91.7 + uSeed * 13.1) * 43758.5453);
    float uu = (lon * 1.6 - uT * (1.2 + 2.5 * uCharge) * (0.7 + 0.6 * h1)) / (0.9 + 0.8 * h1) + h1 * 7.0;
    float cu = frac(uu), h2 = frac(sin(floor(uu) * 17.3 + li * 57.1 + uSeed) * 43758.5453);
    float shape = pow(smoothstep(0.0, 0.85, cu), 1.5) * (1.0 - smoothstep(0.9, 1.0, cu)) * step(0.15, h2);
    float band = 1.0 - smoothstep(0.08 + 0.12 * h2, 0.3 + 0.12 * h2, abs(lv - 0.5));
    float heat = pow(ndv, 8.0) * (0.15 + 0.6 * uCharge);
    float I = 0.08 + 0.75 * shape * band + 0.85 * rim * rim + 0.6 * heat + uFlash;
    col = sandRamp(clamp(I, 0.0, 1.0)) + float3(1.0, 0.85, 0.6) * heat * 0.8;
    col *= 0.85 + 0.4 * uCharge;
  } else if (uStyle < 1.5) {

    float stripe = smoothstep(0.35, 0.5, abs(frac(vW.y * 1.6 - uT * (1.0 + 2.0 * uCharge)) - 0.5) * 2.0);
    float core = pow(ndv, 5.0);
    float I = 0.3 + 0.25 * stripe + 0.6 * rim + 0.5 * core * (0.4 + 0.6 * uCharge) + uFlash;
    col = goldRamp(clamp(I, 0.0, 1.0)) * (0.7 + 0.4 * uCharge);
    a *= clamp(0.35 + 0.25 * stripe + 0.6 * rim + 0.5 * core, 0.0, 1.0);
  } else if (uStyle > 2.5 && uStyle < 3.5) {

    float edge = pow(max(1.0 - ndv, 0.0), 3.0);
    float flick = tex2D(uNoise, float2(atan2(p.z, p.x) * 0.5 + uT * 0.8, p.y * 0.5 + uSeed)).r;
    float I = edge * (1.4 + 0.6 * flick) * (0.7 + 0.6 * uCharge) + 0.6 * uFlash;
    col = lerp(uS0, uC2, smoothstep(0.15, 0.5, I));
    col = lerp(col, uC3, smoothstep(0.6, 1.1, I));
  } else if (uStyle > 3.5 && uStyle < 4.5) {

    float3 Nf = normalize(cross(ddx(vW), ddy(vW)));
    float lit = dot(Nf, normalize(float3(0.4, 0.8, 0.45)));
    float3 rock = lerp(float3(0.29, 0.13, 0.07), float3(0.58, 0.33, 0.17), smoothstep(-0.2, 0.0, lit));
    rock = lerp(rock, float3(0.83, 0.6, 0.37), smoothstep(0.45, 0.6, lit));
    float cn = tex2D(uNoise, p.xy * 0.9 + p.z * 0.6 + uSeed).r * 0.6 + tex2D(uNoise, p.yz * 1.7 - p.x * 0.4 + 0.3).g * 0.4;
    float crack = 1.0 - smoothstep(0.0, 0.008 + 0.022 * uCharge, abs(cn - 0.5)); 
    float pulse = 0.75 + 0.25 * sin(uT * (6.0 + 10.0 * uCharge));
    rock = pow(rock, ((float3)(2.2))) * 1.6; 
    col = lerp(rock, float3(1.0, 0.75, 0.35) * (1.2 + 1.2 * uCharge) * pulse, crack) + float3(1.0, 0.55, 0.15) * pow(max(1.0 - ndv, 0.0), 3.0) * 0.35 * uCharge + uFlash;
  } else if (uStyle > 5.5 && uStyle < 6.5) {

    float cn = nz(p.xy * 0.8 + p.z * 0.5 + uSeed) * 0.6 + nz(p.yz * 1.6 - p.x * 0.4 + 0.3) * 0.4;
    float crack = 1.0 - smoothstep(0.0, 0.01 + 0.03 * uCharge, abs(cn - 0.5));
    float spec = step(0.93, dot(N, normalize(V + float3(0.35, 0.65, 0.2))));
    col = float3(0.012, 0.008, 0.006) + float3(0.32, 0.22, 0.14) * spec + uC3 * crack * (0.8 + 1.2 * uCharge) + uC2 * pow(max(1.0 - ndv, 0.0), 3.0) * 0.7 + uFlash;
  } else if (uStyle > 6.5 && uStyle < 7.5) {

    float3 Nv = normalize(mul((float3x3)UNITY_MATRIX_V, N));
    float r = length(Nv.xy), an = atan2(Nv.y, Nv.x);
    float sp = sin(an * 3.0 + log(max(r, 0.02)) * 7.0 + uT * (4.0 + 6.0 * uCharge));
    float band = smoothstep(0.1, 0.7, sp);
    float hot = smoothstep(0.34, 0.24, r) * smoothstep(0.12, 0.24, r);
    float I = 0.06 + 0.55 * band * (1.0 - r) + 1.1 * hot + 0.5 * uFlash;
    col = sandRamp(clamp(I, 0.0, 1.0));
    col = lerp(col, ((float3)(0.0)), step(r, 0.18));
  } else if (uStyle > 7.5 && uStyle < 8.5) {

    float n = nz(p.xy * 0.7 + p.z * 0.45 + float2(uT * 0.06, -uT * 0.04) + uSeed) * 0.6 + nz(p.zx * 1.3 + uT * 0.05) * 0.4;
    float crust = step(0.56 - 0.08 * uCharge, n);
    float edge = smoothstep(0.03, 0.0, abs(n - (0.56 - 0.08 * uCharge)));
    float3 gold = lerp(float3(0.95, 0.5, 0.1), float3(1.0, 0.85, 0.45), smoothstep(0.2, 0.9, ndv));
    col = pow(lerp(gold, float3(0.35, 0.12, 0.04), crust * 0.85), ((float3)(2.2))) * 1.7 + float3(1.0, 0.8, 0.4) * edge * 1.5 + uFlash;
  } else if (uStyle > 8.5 && uStyle < 9.5) {

    float3 Nv = normalize(mul((float3x3)UNITY_MATRIX_V, N));
    float r = length(Nv.xy), an = atan2(Nv.y, Nv.x);
    float ring = smoothstep(0.5, 0.58, r) * (1.0 - smoothstep(0.93, 1.0, r));
    float fl = nz(float2(an * 0.8 - uT * (0.8 + 1.5 * uCharge), r * 2.0 + uSeed));
    float edgeL = exp(-abs(r - 0.58) * 40.0) + exp(-abs(r - 0.93) * 40.0);
    col = goldRamp(clamp(0.35 + 0.4 * fl + 0.4 * edgeL, 0.0, 1.0)) * (0.8 + 0.5 * uCharge) + uFlash;
    a *= ring;
  } else if (uStyle > 9.5 && uStyle < 10.5) {

    float3 Nv = normalize(mul((float3x3)UNITY_MATRIX_V, N));
    float r = length(Nv.xy);
    float inner = step(r, 0.5);
    float shellI = 0.3 + 0.8 * rim + 0.2 * nz(p.xy * 1.5 + uT * 0.2);
    col = lerp(goldRamp(clamp(shellI, 0.0, 1.0)) * (0.7 + 0.4 * uCharge), float3(0.01, 0.006, 0.004) + uC2 * exp(-abs(r - 0.5) * 30.0), inner) + uFlash;
    a *= lerp(clamp(0.25 + 0.75 * rim, 0.0, 1.0), 1.0, inner);
  } else if (uStyle > 10.5 && uStyle < 11.5) {

    float g = pow(ndv, 3.0);
    col = lerp(uC2 * 0.6, float3(1.0, 0.97, 0.9) * 1.1, smoothstep(0.5, 0.95, g)) * (0.6 + 0.4 * uCharge + uFlash); 
    a *= smoothstep(0.1, 0.7, g);
  } else if (uStyle > 11.5 && uStyle < 12.5) {

    float3 Nv = normalize(mul((float3x3)UNITY_MATRIX_V, N));
    float r = length(Nv.xy);
    col = lerp(float3(0.82, 0.74, 0.6) * (0.8 + 0.25 * uCharge), float3(0.015, 0.008, 0.005), step(0.7 - 0.05 * sin(uT * 14.0) * uCharge, r)) + uFlash; 
  } else if (uStyle > 12.5 && uStyle < 13.5) {

    float3 Nf = normalize(cross(ddx(vW), ddy(vW)));
    float lit = dot(Nf, normalize(float3(0.4, 0.8, 0.45))), fv = dot(Nf, V);
    float3 gem = lerp(float3(0.55, 0.22, 0.04), float3(0.95, 0.6, 0.15), step(0.0, lit));
    gem = lerp(gem, float3(1.0, 0.92, 0.6), step(0.55, lit));
    col = pow(gem, ((float3)(2.2))) * (1.4 + 0.8 * uCharge) + uC3 * pow(max(fv, 0.0), 6.0) * (0.3 + 0.9 * uCharge) + uFlash;
  } else if (uStyle > 13.5 && uStyle < 14.5) {

    float lat = p.y, lon = atan2(p.z, p.x);
    float bandId = floor((lat + 1.0) * 5.0);
    float h = frac(sin(bandId * 37.1 + uSeed) * 917.3);
    float flow = nz(float2(lon * 0.5 + uT * (0.15 + 0.35 * h) * (1.0 + uCharge), lat * 2.0 + h));
    float I = 0.04 + 0.55 * step(0.62, h) * (0.4 + 0.6 * flow) + 0.12 * flow + 0.3 * rim + 0.2 * pow(ndv, 6.0) * uCharge + uFlash; 
    col = sandRamp(clamp(I, 0.0, 1.0));
  } else if (uStyle > 14.5 && uStyle < 15.5) {

    float3 Nv = normalize(mul((float3x3)UNITY_MATRIX_V, N));
    float r = length(Nv.xy), an = atan2(Nv.y, Nv.x);
    float spokes = nz(float2(an * 1.6 + uSeed, r * 0.3)) ;
    float vein = smoothstep(0.5 - (0.02 + 0.03 * uCharge), 0.5, abs(frac(an * 2.2 + spokes * 1.5) - 0.5)); 
    vein *= smoothstep(0.15, 0.4, r) * (0.6 + 0.4 * nz(float2(an * 3.0, r * 3.0 - uT)));
    col = float3(0.012, 0.008, 0.005) + uC3 * vein * (0.35 + 0.5 * uCharge) + uC2 * pow(max(1.0 - ndv, 0.0), 3.0) * 0.6 + uFlash;
  } else if (uStyle > 15.5 && uStyle < 16.5) {

    float2 uv = float2(atan2(p.z, p.x) / 6.2831853 * 18.0, p.y * 5.0 * 1.7320508);
    float2 rr = float2(1.0, 1.7320508), hh = rr * 0.5;
    float2 ga = glsl_mod(uv, rr) - hh, gb = glsl_mod(uv - hh, rr) - hh;
    float2 gv = dot(ga, ga) < dot(gb, gb) ? ga : gb;
    float2 aq = abs(gv);
    float hexD = max(dot(aq, float2(0.5, 0.8660254)), aq.x);
    float hexLine = 1.0 - smoothstep(0.015, 0.045, 0.5 - hexD);
    float cell = nz((uv - gv) * 0.05 + uT * 0.05);
    col = lerp(float3(0.02, 0.012, 0.008) + uC2 * 0.12 * cell * uCharge, uC3 * (0.22 + 0.35 * uCharge), hexLine) + uC2 * rim * 0.4 + uFlash; 
  } else if (uStyle > 16.5 && uStyle < 17.5) {

    float3 Nv = normalize(mul((float3x3)UNITY_MATRIX_V, N));
    float r = length(Nv.xy), an = atan2(Nv.y, Nv.x);
    float wob = nz(float2(an * 0.6 + uSeed, -uT * 1.4)) * 0.25 + nz(float2(an * 1.7, -uT * 2.3)) * 0.12;
    float inner = step(r, 0.45 + wob + 0.1 * uCharge);
    col = lerp(float3(0.95, 0.42, 0.08), float3(1.0, 0.86, 0.4), inner);
    col = lerp(col, float3(1.0, 0.97, 0.85), step(r, 0.2 + wob * 0.5) * uCharge);
    col = pow(col, ((float3)(2.2))) * (1.4 + 0.6 * uCharge) + uFlash;
  } else if (uStyle > 17.5 && uStyle < 18.5) {

    float3 Rf = reflect(-V, N);
    float y = Rf.y + 0.15 * sin(uT * 2.0 + Rf.x * 3.0);
    float3 m1 = lerp(float3(0.35, 0.14, 0.03), float3(0.95, 0.62, 0.2), step(-0.05, y));
    m1 = lerp(m1, float3(1.0, 0.93, 0.7), step(0.35, y));
    col = pow(m1, ((float3)(2.2))) * (1.3 + 0.5 * uCharge) + uC3 * pow(max(1.0 - ndv, 0.0), 4.0) * 0.4 + uFlash;
  } else if (uStyle > 18.5 && uStyle < 19.5) {

    float n = nz(p.xy * 0.9 + p.z * 0.55 + uSeed) * 0.6 + nz(p.yz * 1.8 + 0.4) * 0.4;
    float hole = smoothstep(0.66 - 0.12 * uCharge, 0.62 - 0.12 * uCharge, 1.0 - n); 
    float lit = dot(N, normalize(float3(0.4, 0.8, 0.45)));
    float3 shell = lerp(float3(0.29, 0.13, 0.07), float3(0.58, 0.33, 0.17), smoothstep(-0.2, 0.0, lit));
    shell = pow(lerp(shell, float3(0.83, 0.6, 0.37), smoothstep(0.45, 0.6, lit)), ((float3)(2.2))) * 1.6;
    float rimH = 1.0 - smoothstep(0.0, 0.04, abs(n - (0.34 + 0.12 * uCharge)));
    col = lerp(shell, uC3 * (0.45 + 0.35 * uCharge), 1.0 - hole) + float3(1.0, 0.7, 0.3) * rimH * 1.2 + uFlash;
  } else if (uStyle > 19.5 && uStyle < 20.5) {

    float3 Nv = normalize(mul((float3x3)UNITY_MATRIX_V, N));
    float r = length(Nv.xy);
    col = pow(lerp(float3(1.0, 0.7, 0.2), float3(1.0, 0.97, 0.85), step(r, 0.45)), ((float3)(2.2))) * (1.5 + 0.8 * uCharge) + uFlash;
  } else if (uStyle > 20.5 && uStyle < 21.5) {

    float lat = asin(clamp(p.y, -1.0, 1.0)), lon = atan2(p.z, p.x);
    float la = smoothstep(0.46, 0.5, abs(frac(lat / 3.1415927 * 6.0) - 0.5));
    float lo = smoothstep(0.46, 0.5, abs(frac(lon / 6.2831853 * 8.0 + uT * 0.05) - 0.5));
    float glow = 0.6 + 0.4 * sin(uT * 4.0 - lat * 6.0);
    col = sandRamp(0.05 + 0.25 * rim) + uC3 * max(la, lo) * glow * (0.3 + 0.5 * uCharge) + uFlash;
  } else if (uStyle > 21.5 && uStyle < 22.5) {

    float3 Nv = normalize(mul((float3x3)UNITY_MATRIX_V, N));
    float r = length(Nv.xy);
    float3 c3 = lerp(float3(1.0, 0.9, 0.55), float3(1.0, 0.6, 0.15), step(0.5, r));
    c3 = lerp(c3, float3(0.85, 0.3, 0.06), step(0.82, r));
    col = pow(c3, ((float3)(2.2))) * (1.3 + 0.6 * uCharge) + uFlash;
  } else if (uStyle > 4.5 && uStyle < 5.5) {

    float3 Nv = normalize(mul((float3x3)UNITY_MATRIX_V, N));
    float r = length(Nv.xy), d = dot(Nv, normalize(float3(-0.5, 0.6, 0.6)));
    col = lerp(float3(0.85, 0.42, 0.1), float3(1.0, 0.74, 0.26), step(0.05, d));           
    col = lerp(col, float3(1.0, 0.95, 0.78), step(r, 0.28 + 0.04 * sin(uT * 12.0)) * (0.5 + 0.5 * uCharge));
    col = lerp(col, float3(1.0, 0.98, 0.92), step(0.9, d) * step(0.55, r));
    col = lerp(col, float3(0.7, 0.3, 0.06), step(0.88, r));
    col = pow(col, ((float3)(2.2))) * 1.5 + uFlash; 
  } else {

    float g = pow(ndv, 1.6);
    col = lerp(uC2, float3(1.0, 0.95, 0.85) * 1.8, smoothstep(0.55, 0.95, g)) * (0.8 + 0.6 * uCharge + uFlash);
    a *= smoothstep(0.0, 0.6, g);
  }
  _fc = float4(col, a);

        return EspoirOut(_fc);
      }
      ENDCG
    }
  }
}