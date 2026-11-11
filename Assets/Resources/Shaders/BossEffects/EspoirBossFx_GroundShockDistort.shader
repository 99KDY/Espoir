
Shader "EspoirBossFx/GroundShockDistort" {
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
      float uT;
      float uStrength;
      struct appdata {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
      };
      struct v2f {
        float4 pos : SV_POSITION;
        float2 vUv : TEXCOORD0;
        float4 grabPos : TEXCOORD1;
      };
      static float3 position; static float3 normal; static float2 uv;
      static float2 vUv;
      static float4 _fc;

      v2f vert(appdata IN) {
        v2f o = (v2f)0;
        position = IN.vertex.xyz;
        uv = IN.uv;

  vUv = uv * 2.0 - 1.0;
  o.pos = UnityObjectToClipPos(float4(position, 1.0));

        o.vUv = vUv;
        o.grabPos = ComputeGrabScreenPos(o.pos);
        return o;
      }
      float4 frag(v2f i) : SV_Target {
        vUv = i.vUv;
        _fc = 0;

  float r = length(vUv);
  float R = lerp(0.08, 0.95, 1.0 - pow(1.0 - uT, 2.5));
  float w = lerp(0.22, 0.06, uT);
  float q = (r - R) / w;
  float prof = exp(-q * q);
  float2 dir = r > 0.0001 ? vUv / r : ((float2)(0.0));
  _fc = float4(dir * prof * uStrength * (1.0 - uT), 0.0, 1.0);

        return EspoirDistortOutF(_fc, i.grabPos, 0.0); 
      }
      ENDCG
    }
  }
}