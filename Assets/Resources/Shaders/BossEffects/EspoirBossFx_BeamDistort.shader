
Shader "EspoirBossFx/BeamDistort" {
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
      Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest [_ZTest] Cull Off
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma target 3.5
      #include "UnityCG.cginc"
      #include "EspoirBossFxCommon.cginc"
      sampler2D uNoise;
      float uT;
      float uAmt;
      float uLen;
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
        float4 grabPos : TEXCOORD3;
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
        o.grabPos = ComputeGrabScreenPos(o.pos);
        return o;
      }
      float4 frag(v2f i) : SV_Target {
        vN = i.vN;
        vW = i.vW;
        vUv = i.vUv;
        _fc = 0;

  float3 V = normalize(_WorldSpaceCameraPos - vW);
  float ndv = abs(dot(normalize(vN), V)), d = vUv.y * uLen;
  float2 n = float2(tex2D(uNoise, float2(vUv.x * 3.0, d * 0.03 - uT * 4.0)).r, tex2D(uNoise, float2(vUv.x * 3.0 + 0.5, d * 0.03 - uT * 5.0)).g) - 0.5;
  _fc = float4(n * uAmt * smoothstep(0.0, 0.6, ndv) * exp(-d / 150.0), 0.0, 1.0);

        return EspoirDistortOut(_fc, i.grabPos);
      }
      ENDCG
    }
  }
}