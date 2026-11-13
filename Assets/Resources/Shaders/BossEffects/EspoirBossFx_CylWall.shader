
Shader "EspoirBossFx/CylWall" {
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
      float uSpin;
      float uAlpha;
      float uDis;
      float uTwist;
      float uGrow;
      float3 uCenter;
      float3 uL;
      float3 uM;
      float3 uS;
      float3 uRim;
      struct appdata {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
      };
      struct v2f {
        float4 pos : SV_POSITION;
        float2 vUv : TEXCOORD0;
        float3 vW : TEXCOORD1;
      };
      static float3 position; static float3 normal; static float2 uv;
      static float2 vUv;
      static float3 vW;
      static float4 _fc;

 

  

float nearSide(float3 w, float3 c) { float2 d = w.xz - c.xz, e = _WorldSpaceCameraPos.xz - c.xz; return dot(d, e) / max(length(d) * length(e), 1e-4); }

      v2f vert(appdata IN) {
        v2f o = (v2f)0;
        position = IN.vertex.xyz;
        uv = IN.uv;
 vUv = uv; float4 w = mul(unity_ObjectToWorld, float4(position, 1.0)); vW = w.xyz; o.pos = mul(UNITY_MATRIX_VP, w); 
        o.vUv = vUv;
        o.vW = vW;
        return o;
      }
      float4 frag(v2f i) : SV_Target {
#ifdef ESPOIR_GLOW
        if (EspoirGlowHidden(i.pos)) discard;
#endif
        vUv = i.vUv;
        vW = i.vW;
        _fc = 0;

  float y = vUv.y;
  if (y > uGrow) discard;
  float x = vUv.x - uSpin / 6.2831853;
  float2 q = float2(x * 3.0 + y * uTwist, y * 1.2 - uT * 0.12);
  float warp = tex2D(uNoise, q * 0.7).g;
  float n = tex2D(uNoise, float2(q.x * 2.0 + warp * 0.6, q.y * 0.45)).r * 0.65 + tex2D(uNoise, float2(q.x * 5.0 + warp, q.y * 1.1)).b * 0.35;
  float top = 0.78 + 0.18 * warp, bot = 0.05 + 0.12 * warp;
  float edgeY = smoothstep(bot - 0.04, bot + 0.04, y) * (1.0 - smoothstep(top - 0.06, top, y));
  float th = 0.36;
  float a = smoothstep(th, th + 0.03, n) * edgeY;
  float rim = 1.0 - smoothstep(th + 0.03, th + 0.1, n);
  a *= uAlpha * lerp(1.0, 0.32, smoothstep(-0.1, 0.6, nearSide(vW, EspoirStagePoint(uCenter))));
  float dz = tex2D(uNoise, float2(x * 4.0, y * 1.5)).a;
  a *= 1.0 - smoothstep(dz - 0.06, dz + 0.06, uDis * 1.1);
  if (a < 0.01) discard;
  float tone = (n - th) * 2.6 + y * 0.25;
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