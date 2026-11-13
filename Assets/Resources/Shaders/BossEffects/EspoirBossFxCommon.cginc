
#ifndef ESPOIR_BOSS_FX_COMMON
#define ESPOIR_BOSS_FX_COMMON

float _EspoirFxTone;      
float _EspoirFxExposure;  
float _EspoirFxBloomFeed; 
float4x4 _StageToWorld;   

float  glsl_mod(float x, float y)   { return x - y * floor(x / y); }
float2 glsl_mod(float2 x, float y)  { return x - y * floor(x / y); }
float3 glsl_mod(float3 x, float y)  { return x - y * floor(x / y); }
float2 glsl_mod(float2 x, float2 y) { return x - y * floor(x / y); }
float3 glsl_mod(float3 x, float3 y) { return x - y * floor(x / y); }

float3 invACES(float3 y) {
  const float3x3 invOut = float3x3(0.643038, 0.311187, 0.045775, 0.059269, 0.931436, 0.009295, 0.005962, 0.063929, 0.930118);
  const float3x3 invIn  = float3x3(1.764741, -0.675778, -0.088963, -0.147028, 1.160252, -0.013224, -0.036337, -0.162436, 1.198773);
  y = clamp(y, 0.0, 0.97);
  float3 w = clamp(mul(invOut, y), 0.0, 0.99);
  float3 A = 1.0 - 0.983729 * w, B = 0.0245786 - 0.4329510 * w, C = -(0.000090537 + 0.238081 * w);
  float3 v = (-B + sqrt(max(B * B - 4.0 * A * C, 0.0))) / (2.0 * A);
  return max(mul(invIn, v), 0.0) * 0.6;
}

float3 EspoirAces(float3 c) {
  const float3x3 inM  = float3x3(0.59719, 0.35458, 0.04823, 0.07600, 0.90834, 0.01566, 0.02840, 0.13383, 0.83777);
  const float3x3 outM = float3x3(1.60475, -0.53108, -0.07367, -0.10208, 1.10813, -0.00605, -0.00327, -0.07276, 1.07602);
  float e = _EspoirFxExposure > 0.0 ? _EspoirFxExposure : 1.0;
  c *= e / 0.6;
  c = mul(inM, c);
  float3 a = c * (c + 0.0245786) - 0.000090537;
  float3 b = c * (0.983729 * c + 0.4329510) + 0.238081;
  c = mul(outM, a / b);
  return saturate(c);
}

float _EspoirFxPostShow;  
float _ZTest;             
float4 _EspoirGlowSize;   
sampler2D_float _CameraDepthTexture;
bool EspoirGlowHidden(float4 svpos) {
  if (_ZTest > 7.5) return false;
  float2 uv = svpos.xy * _EspoirGlowSize.xy;
  float scene = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
  return LinearEyeDepth(svpos.z) > scene + 0.05;
}

float4 EspoirOut(float4 c) {
  if (!(c.r == c.r) || !(c.g == c.g) || !(c.b == c.b)) c.rgb = 0; 
  c.rgb = max(c.rgb, 0.0);
#ifdef ESPOIR_GLOW

  if (_EspoirFxPostShow > 0.5) return float4(_EspoirFxTone > 0.5 ? EspoirAces(c.rgb) : c.rgb, c.a);
  {
    float ge = _EspoirFxExposure > 0.0 ? _EspoirFxExposure : 1.0;

    return float4(c.rgb * ge, c.a);
  }
#endif
  if (_EspoirFxTone > 0.5) {
    float e = _EspoirFxExposure > 0.0 ? _EspoirFxExposure : 1.0;
    float3 over = max(c.rgb * e - 1.0, 0.0);
    c.rgb = EspoirAces(c.rgb) + over * _EspoirFxBloomFeed;
  }
  return c;
}

sampler2D _EspoirBossGrab;
float _EspoirGrabOn;
float4 EspoirDistortOutF(float4 off, float4 grabPos, float flipX);
float4 EspoirDistortOut(float4 off, float4 grabPos) { return EspoirDistortOutF(off, grabPos, 1.0); }

float4 EspoirDistortOutF(float4 off, float4 grabPos, float flipX) {
  float2 o = off.xy;
  if (!(o.x == o.x) || !(o.y == o.y)) o = 0;
  if (flipX > 0.5) o.x = -o.x; 
#if UNITY_UV_STARTS_AT_TOP
  o.y = -o.y;
#endif
  float2 uv = grabPos.xy / grabPos.w + o;

  return float4(tex2D(_EspoirBossGrab, uv).rgb, saturate(length(o) * 600.0) * _EspoirGrabOn);
}

float3 EspoirStagePoint(float3 p) { return mul(_StageToWorld, float4(p, 1.0)).xyz; }
float3 EspoirStageDir(float3 d)   { return mul((float3x3)_StageToWorld, d); }

#endif
