
Shader "EspoirBossFx/Rock" {
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
      float uPush;
      float uGlow;
      float3 uGold;
      float3 uLightDir;
      float3 uLight;
      float3 uMid;
      float3 uShade;
      sampler2D uNoise;
      float uDis;
      float uSeed;
      float uH;
      struct appdata {
        float4 vertex : POSITION;
      };
      struct v2f {
        float4 pos : SV_POSITION;
        float3 vW : TEXCOORD0;
        float3 vL : TEXCOORD1;
        float vY : TEXCOORD2;
      };
      static float3 position; static float3 normal; static float2 uv;
      static float3 vW;
      static float3 vL;
      static float vY;
      static float4 _fc;

float rockDis() {
  float h = clamp(vL.y / uH, 0.0, 1.0);
  return tex2D(uNoise, vL.xz * 0.45 + float2(vL.y * 0.3, uSeed)).a * 0.6 + (1.0 - h) * 0.4;
}

  

      v2f vert(appdata IN) {
        v2f o = (v2f)0;
        position = IN.vertex.xyz;

  float3 p = position;
  vY = position.y;
  vL = position;

  p += normalize(position - float3(0.0, 0.4, 0.0) + 1e-4) * uPush;
  float4 w = mul(unity_ObjectToWorld, float4(p, 1.0));
  vW = w.xyz;
  o.pos = mul(UNITY_MATRIX_VP, w);

        o.vW = vW;
        o.vL = vL;
        o.vY = vY;
        return o;
      }
      float4 frag(v2f i) : SV_Target {
#ifdef ESPOIR_GLOW
        if (EspoirGlowHidden(i.pos)) discard;
#endif
        vW = i.vW;
        vL = i.vL;
        vY = i.vY;
        _fc = 0;

  float dz = rockDis();
  if (dz < uDis) discard;
  float burn = uDis > 0.0 ? 1.0 - smoothstep(0.0, 0.07, dz - uDis) : 0.0;
  float3 n = normalize(cross(ddx(vW), ddy(vW)));
  float3 V = normalize(_WorldSpaceCameraPos - vW);
  if (dot(n, V) < 0.0) n = -n;
  float l = dot(n, normalize(EspoirStageDir(uLightDir)));
  float3 c = lerp(uShade, uMid, smoothstep(-0.12, -0.02, l));
  c = lerp(c, uLight, smoothstep(0.42, 0.52, l));
  c = invACES(c);
  float fr = pow(1.0 - max(dot(n, V), 0.0), 2.5);
  float bottom = 1.0 - smoothstep(0.0, 0.6, vY / uH);

  c += uGold * (uGlow * (0.9 * bottom * bottom + 0.6 * fr) + 0.12 * bottom * bottom);
  c = lerp(c, uGold * 1.4, burn);
  _fc = float4(c, 1.0);

        return EspoirOut(_fc);
      }
      ENDCG
    }
  }
}