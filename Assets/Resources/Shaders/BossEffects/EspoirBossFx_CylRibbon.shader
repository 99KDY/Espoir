
Shader "EspoirBossFx/CylRibbon" {
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
      float uSpin;
      float uT;
      float uTilt;
      float uOpen;
      sampler2D uNoise;
      float uAlpha;
      float uDis;
      float uGrow;
      float uStyle;
      float uFlow;
      float uLen;
      float3 uCenter;
      float3 uL;
      float3 uM;
      float3 uS;
      float3 uRim;
      struct appdata {
        float4 vertex : POSITION;
        float4 c0 : TEXCOORD1;
        float4 c1 : TEXCOORD2;
      };
      struct v2f {
        float4 pos : SV_POSITION;
        float vU : TEXCOORD0;
        float vV : TEXCOORD1;
        float vK : TEXCOORD2;
        float3 vW : TEXCOORD3;
      };
      static float3 position; static float3 normal; static float2 uv;
      static float aU;
      static float aV;
      static float aA;
      static float aY;
      static float aR;
      static float aW;
      static float aK;
      static float aSpd;
      static float vU;
      static float vV;
      static float vK;
      static float3 vW;
      static float4 _fc;

   

  

float nearSide(float3 w, float3 c) { float2 d = w.xz - c.xz, e = _WorldSpaceCameraPos.xz - c.xz; return dot(d, e) / max(length(d) * length(e), 1e-4); }
float n2(float2 p) { return tex2D(uNoise, p).r; }

      v2f vert(appdata IN) {
        v2f o = (v2f)0;
        position = IN.vertex.xyz;
        aU = IN.c0.x;
        aV = IN.c0.y;
        aA = IN.c0.z;
        aY = IN.c0.w;
        aR = IN.c1.x;
        aW = IN.c1.y;
        aK = IN.c1.z;
        aSpd = IN.c1.w;

  vU = aU; vV = aV; vK = aK;
  float ang = aA + uSpin * aSpd;
  float r = aR * (1.0 + uOpen) + 0.35 * sin(uT * 2.3 + aK * 17.0 + aU * 7.0);
  float3 rad = float3(sin(ang), 0.0, cos(ang));
  float3 up = normalize(float3(0.0, 1.0, 0.0) + rad * uTilt);
  float3 p = rad * r + float3(0.0, aY + 0.25 * sin(uT * 1.7 + aK * 9.0 + aU * 4.0), 0.0) + up * aV * aW;
  float4 w = mul(unity_ObjectToWorld, float4(p, 1.0));
  vW = w.xyz;
  o.pos = mul(UNITY_MATRIX_VP, w);

        o.vU = vU;
        o.vV = vV;
        o.vK = vK;
        o.vW = vW;
        return o;
      }
      float4 frag(v2f i) : SV_Target {
#ifdef ESPOIR_GLOW
        if (EspoirGlowHidden(i.pos)) discard;
#endif
        vU = i.vU;
        vV = i.vV;
        vK = i.vK;
        vW = i.vW;
        _fc = 0;

  if (vU > uGrow) discard;
  float av = abs(vV);
  float s = vU * uLen - uT * uFlow * (0.8 + 0.4 * vK);
  float a = 1.0, tone = 1.0 - av, rim = 0.0;
  float ends = smoothstep(0.0, 0.12, vU) * (1.0 - smoothstep(0.75, 1.0, vU));
  int st = int(uStyle + 0.5);
  if (st == 0) {
    a = 1.0 - smoothstep(0.6, 1.0, av);
  } else if (st == 1) {            
    float wv = 0.55 + 0.45 * n2(float2(s * 0.15 + vK * 5.0, 0.3));
    float rag = n2(float2(s * 0.9 + vK * 3.0, vV * 1.3));
    a = 1.0 - smoothstep(wv - 0.06, wv, av + rag * 0.5);
    float streak = n2(float2(s * 0.06 + vK, vV * 3.0 + vK * 2.0));
    tone = (1.0 - av / wv) * 0.45 + streak * 0.55 - 0.1;
    a *= 0.7 + 0.3 * step(0.32, streak);
  } else if (st == 2) {            
    float b = n2(float2(s * 0.35 + vK * 4.0, vV * 0.6 + vK)) * 0.7 + n2(float2(s * 0.9, vV * 1.4 + vK * 3.0)) * 0.3;
    float th = 0.32 + 0.45 * av;
    a = smoothstep(th, th + 0.03, b);
    rim = 1.0 - smoothstep(th + 0.03, th + 0.16, b);
    tone = b * 1.3 - 0.3;
  } else if (st == 3) {            
    float prof = sin(3.14159 * clamp(vU, 0.0, 1.0));
    float e = av / max(prof, 0.04);
    a = 1.0 - smoothstep(0.8, 1.0, e);
    tone = 1.05 - e * 0.85;
    ends = 1.0;
  } else if (st == 4) {            
    a = 1.0 - smoothstep(0.3, 1.0, av);
    float d = frac(s * 0.5 + vK * 7.0);
    a *= smoothstep(0.0, 0.12, d) * (1.0 - smoothstep(0.45, 0.85, d));
    tone = 0.55 + 0.45 * (1.0 - av);
  } else {                         
    a = 1.0 - smoothstep(0.93, 1.0, av);
    rim = smoothstep(0.74, 0.8, av);
    float fl = n2(float2(s * 0.12 + vK * 2.0, vV * 0.5));
    tone = (1.0 - av) * 0.7 + fl * 0.45;
  }
  a *= ends * uAlpha * lerp(1.0, 0.38, smoothstep(-0.1, 0.6, nearSide(vW, EspoirStagePoint(uCenter))));
  float dz = n2(float2(s * 0.3 + vK * 9.0, vV * 0.5 + 0.5));
  a *= 1.0 - smoothstep(dz - 0.06, dz + 0.06, uDis * 1.1);
  if (a < 0.01) discard;
  float w = fwidth(tone) * 1.5 + 0.01;
  float3 c = lerp(uS, uM, smoothstep(0.35 - w, 0.35 + w, tone));
  c = lerp(c, uL, smoothstep(0.68 - w, 0.68 + w, tone));
  c = lerp(c, uRim, clamp(rim, 0.0, 1.0));
  _fc = float4(invACES(c), clamp(a, 0.0, 0.95));

        return EspoirOut(_fc);
      }
      ENDCG
    }
  }
}