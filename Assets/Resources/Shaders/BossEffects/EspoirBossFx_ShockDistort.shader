
Shader "EspoirBossFx/ShockDistort" {
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
      float uR;
      float uW;
      float uStrength;
      float uHalf;
      float uAspect;
      float2 uC;
      struct appdata {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
      };
      struct v2f {
        float4 pos : SV_POSITION;
        float2 vUv : TEXCOORD0;
        float4 vClip : TEXCOORD1;
        float4 grabPos : TEXCOORD2;
      };
      static float3 position; static float3 normal; static float2 uv;
      static float2 vUv;
      static float4 vClip;
      static float4 _fc;

      v2f vert(appdata IN) {
        v2f o = (v2f)0;
        position = IN.vertex.xyz;
        uv = IN.uv;

  vUv = uv;
  o.pos = UnityObjectToClipPos(float4(position, 1.0));
  vClip = o.pos;

        o.vUv = vUv;
        o.vClip = vClip;
        o.grabPos = ComputeGrabScreenPos(o.pos);
        return o;
      }
      float4 frag(v2f i) : SV_Target {
        vUv = i.vUv;
        vClip = i.vClip;
        _fc = 0;

  float r = length(vUv * 2.0 - 1.0) * uHalf;
  float q = (r - uR) / uW, q2 = (r - uR + uW * 1.6) / (uW * 1.4);

  float prof = exp(-q * q) - 0.45 * exp(-q2 * q2);
  float4 cc = UnityObjectToClipPos(float4(0.0, 0.0, 0.0, 1.0)); float asp = _ScreenParams.x / _ScreenParams.y; 
  float2 d = (vClip.xy / vClip.w - cc.xy / cc.w) * float2(asp, 1.0);
  float L = length(d);
  float2 dir = L > 0.0001 ? d / L : ((float2)(0.0));
  dir.x /= asp;
  _fc = float4(dir * prof * uStrength, 0.0, 1.0);

        return EspoirDistortOutF(_fc, i.grabPos, 0.0); 
      }
      ENDCG
    }
  }
}