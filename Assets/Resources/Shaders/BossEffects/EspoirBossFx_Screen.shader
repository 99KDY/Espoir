

Shader "Hidden/EspoirBossFx/Screen" {
  Properties { _MainTex ("", 2D) = "black" {} }
  CGINCLUDE
  #include "UnityCG.cginc"
  sampler2D _MainTex;
  sampler2D _EspoirInkMask;
  float4 _MainTex_TexelSize;
  float _CaAmt, _Aspect, _Expo, _InkMode, _InkSeed;
  float2 _CaCenter, _InkCenter;
  float _BloomFeed, _BloomStrength, _BlurRadius;
  float4 _BlurStep;
  sampler2D _B0, _B1, _B2, _B3, _B4;
  #include "EspoirBossFxCommon.cginc"
  ENDCG
  SubShader {
    ZTest Always Cull Off ZWrite Off
    Pass {
      CGPROGRAM
      #pragma vertex vert_img
      #pragma fragment frag
      float4 frag(v2f_img i) : SV_Target {
        float4 c = tex2D(_MainTex, i.uv);
        if (_CaAmt >= 0.0002) {
          float2 d = (i.uv - _CaCenter) * float2(_Aspect, 1.0);
          float r = length(d);
          float2 dir = r > 0.0001 ? d / r : 0;
          float2 off = dir / float2(_Aspect, 1.0) * _CaAmt * smoothstep(0.0, 0.3, r);
          float rr = 0, bb = 0;
          for (int k = 1; k <= 6; k++) {
            float f = k / 6.0;
            rr += tex2D(_MainTex, i.uv + off * f).r;
            bb += tex2D(_MainTex, i.uv - off * f).b;
          }
          c.r = rr / 6.0; c.b = bb / 6.0;
        }

        if (abs(_Expo - 1.0) > 0.002) c.rgb = EspoirAces(invACES(min(saturate(c.rgb), 0.97)) * _Expo);
        return c;
      }
      ENDCG
    }
    Pass {
      CGPROGRAM
      #pragma vertex vert_img
      #pragma fragment frag
      float h1(float n) { return frac(sin(n * 127.1 + _InkSeed * 31.7) * 43758.5453); }
      float4 frag(v2f_img i) : SV_Target {
        float sil = smoothstep(0.3, 0.6, tex2D(_EspoirInkMask, i.uv).r);
        float2 d = (i.uv - _InkCenter) * float2(_Aspect, 1.0);
        float r = length(d), a = atan2(d.y, d.x);
        float lane = floor(a * 70.0);
        float on = step(0.72, h1(lane)) * smoothstep(0.12 + 0.25 * h1(lane + 3.0), 0.75, r);
        float w = abs(frac(a * 70.0) - 0.5);
        float lines = on * (1.0 - smoothstep(0.12, 0.3, w));
        float ink = saturate(max(sil, lines * (1.0 - sil)));
        float v = _InkMode < 1.5 ? ink : 1.0 - ink;
        return float4(v, v, v, 1.0);
      }
      ENDCG
    }

    Pass {
      CGPROGRAM
      #pragma vertex vert_img
      #pragma fragment frag
      float4 frag(v2f_img i) : SV_Target {
        float3 c = tex2D(_MainTex, i.uv).rgb;
        if (!(c.r == c.r) || !(c.g == c.g) || !(c.b == c.b)) c = 0;
        float v = dot(c, float3(0.299, 0.587, 0.114));
        return float4(max(c, 0.0) * smoothstep(1.0, 1.01, v), 1);
      }
      ENDCG
    }

    Pass {
      CGPROGRAM
      #pragma vertex vert_img
      #pragma fragment frag
      float4 frag(v2f_img i) : SV_Target {
        float R = _BlurRadius;
        float w0 = 0.39894 / R;
        float ws = w0;
        float3 sum = tex2D(_MainTex, i.uv).rgb * w0;
        for (int k = 1; k < 11; k++) {
          if (k >= R) break;
          float w = 0.39894 * exp(-0.5 * k * k / (R * R)) / R;
          float2 o = _BlurStep.xy * k;
          sum += (tex2D(_MainTex, i.uv + o).rgb + tex2D(_MainTex, i.uv - o).rgb) * w;
          ws += 2.0 * w;
        }
        return float4(sum / ws, 1);
      }
      ENDCG
    }

    Pass {
      CGPROGRAM
      #pragma vertex vert_img
      #pragma fragment frag
      float4 frag(v2f_img i) : SV_Target {
        float4 c = tex2D(_MainTex, i.uv);
        float3 b = tex2D(_B0, i.uv).rgb * 0.72 + tex2D(_B1, i.uv).rgb * 0.66 + tex2D(_B2, i.uv).rgb * 0.60 + tex2D(_B3, i.uv).rgb * 0.54 + tex2D(_B4, i.uv).rgb * 0.48;
        b *= _BloomStrength;
        if (max(b.r, max(b.g, b.b)) < 1e-4) return float4(saturate(c.rgb), c.a);
        float3 y = saturate(c.rgb); 
        float3 lifted = EspoirAces(invACES(min(y, 0.97)) + b);
        c.rgb = y + max(lifted - min(y, 0.97), 0.0);
        return c;
      }
      ENDCG
    }

    Pass {
      CGPROGRAM
      #pragma vertex vert_img
      #pragma fragment frag
      float _WebLinear;
      float4 frag(v2f_img i) : SV_Target {
        float3 y = saturate(tex2D(_MainTex, i.uv).rgb);
        return float4(_WebLinear > 0.5 ? invACES(y) : y, 1);
      }
      ENDCG
    }

    Pass {
      CGPROGRAM
      #pragma vertex vert_img
      #pragma fragment frag
      sampler2D _WebBuf;
      float _WebLinear;
      float4 frag(v2f_img i) : SV_Target {
        float4 c = tex2D(_MainTex, i.uv);
        float3 y = saturate(c.rgb);
        float3 w = tex2D(_WebBuf, i.uv).rgb;
        if (!(w.r == w.r) || !(w.g == w.g) || !(w.b == w.b)) return float4(y, c.a);
        c.rgb = _WebLinear > 0.5 ? saturate(y + EspoirAces(max(w, 0.0)) - EspoirAces(invACES(y))) : saturate(w);
        return c;
      }
      ENDCG
    }
}
}
