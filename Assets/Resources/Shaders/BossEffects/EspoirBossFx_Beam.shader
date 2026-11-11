
Shader "EspoirBossFx/Beam" {
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
      float uLen;
      float uSeed;
      float uOuter;
      float3 uCore;
      float3 uGlow;
      float3 uAxis;
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

  
  

      v2f vert(appdata IN) {
        v2f o = (v2f)0;
        position = IN.vertex.xyz;
        normal = IN.normal;
        uv = IN.uv;
 vUv = uv; vN = normalize(mul((float3x3)unity_ObjectToWorld, normal)); float4 w = mul(unity_ObjectToWorld, float4(position, 1.0)); vW = w.xyz; o.pos = mul(UNITY_MATRIX_VP, w); 
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
  float ndv = abs(dot(normalize(vN), V));
  float d = vUv.y * uLen;
  float n1 = tex2D(uNoise, float2(vUv.x * 2.0 + uSeed, d * 0.02 - uT * 6.0)).r;
  float n2 = tex2D(uNoise, float2(vUv.x * 5.0 - uSeed, d * 0.05 - uT * 11.0)).g;
  float streak = smoothstep(0.45, 0.85, n1 * 0.6 + n2 * 0.6);
  float far = exp(-d / 300.0), start = smoothstep(0.0, 1.2, d);
  far *= smoothstep(6.0, 18.0, length(_WorldSpaceCameraPos - vW)); 
  far *= lerp(0.22, 1.0, smoothstep(0.985, 0.75, abs(dot(V, normalize(EspoirStageDir(uAxis)))))); 
  float3 col;
  if (uOuter < 0.5) {
    float core = pow(ndv, 1.5);
    col = lerp(uGlow * (0.5 + 0.9 * streak), uCore * 0.75, smoothstep(0.45, 0.9, core)) * (0.35 + 0.65 * ndv);
  } else {
    col = uGlow * (0.1 + 0.7 * streak) * pow(ndv, 2.5) * 0.6;
  }
  _fc = float4(col * uAlpha * far * start, 1.0);

        return EspoirOut(_fc);
      }
      ENDCG
    }
  }
}