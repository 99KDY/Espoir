Shader "Espoir/HerbalistToon" {
Properties {
 map("Albedo",2D)="white"{} uRampSkin("Skin Ramp",2D)="white"{} uRampCloth("Cloth Ramp",2D)="white"{} uRampHair("Hair Ramp",2D)="white"{}
 [Enum(Skin,0,Face,1,Hair,2,Cloth,3)] uPart("Part",Float)=3
 _OutlineWidth("Outline pixels",Range(0,3))=1
}
CGINCLUDE
#include "UnityCG.cginc"
#include "Lighting.cginc"
float _OutlineWidth;
float2 V2(float x){return x.xx;} float2 V2(float x,float y){return float2(x,y);}
float3 V3(float x){return x.xxx;} float3 V3(float x,float y,float z){return float3(x,y,z);} float3 V3(float3 x){return x;}
float4 V4(float3 x,float y){return float4(x,y);} float4 V4(float x,float y,float z,float w){return float4(x,y,z,w);}
struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float4 tangent:TANGENT;float4 color:COLOR;float2 uv:TEXCOORD0;};
struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 worldPos:TEXCOORD1;float3 normal:TEXCOORD2;float3 softNormal:TEXCOORD3;float ao:TEXCOORD4;};
v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.worldPos=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.softNormal=UnityObjectToWorldNormal(v.tangent.xyz);o.ao=v.color.r;return o;}
static const float uSoftNormal = 0.55;
static const float uAO = 0.2;
static const float uClothHueKeep = 0.2;
sampler2D map;
static const float3 uBaseColor = float3(1.000000000,1.000000000,1.000000000);
float uPart;
sampler2D uRampSkin;
sampler2D uRampCloth;
sampler2D uRampHair;
static const int uMode = 2;

static const float3 uLightColor = float3(1.000000000,1.000000000,1.000000000);
static const float3 uAmbient = float3(0.254152094,0.278894263,0.479320183);
static const float uAmbientStrength = 0.06;
static const float uCastShadow = 0;
static const float uAlbedoGamma = 1;
static const float uAlbedoSat = 1;
float3 uHeadCenter;
float3 uHeadRight;
float3 uHeadUp;
float3 uHeadForward;
float uHeadRadius;
static const float uHasFace = 1;
static const float uFaceSDF = 0;
static const float uFaceSoft = 0.06;
static const float uFaceCurve = 0.5;
static const float uFaceNormalSmooth = 1;
static const float uFaceCastShadow = 0.15;
static const float uFaceBias = 0.1;
static const float uNeckShadow = 1;
static const float uMetal = 0;
static const float uMetalBand = 0.55;
static const float uMetalSpec = 1;
static const float3 uMetalTint = float3(1.000000000,1.000000000,1.000000000);
static const float uSteel = 0;
static const float uNoGold = 0;
static const float uGoldAny = 0;

static const float3 uMetalSteelTint = float3(1.000000000,1.000000000,1.000000000);
static const float uHairSpec = 0.6;
static const float uHairSpecPow = 110;
static const float uHairSpecThresh = 0.74;
static const float3 uHairSpecColor = float3(0.930110858,0.679542470,0.693871761);
static const float uHairLuma = 0.55;
static const float uRim = 0.3;
static const float uRimWidth = 0.74;
static const float uRimSoft = 0.01;
static const float3 uRimColor = float3(1.000000000,0.896269353,0.791297940);
static const float uRimFace = 0.15;

static const float uSkinPick = 1;
static const float3 uSkinRef = float3(0.988235294,0.815686275,0.772549020);
static const float uNeckTint = 1;
static const float uFaceWrap = 0;
static const float uFaceEdge = 0.015;
static const float uFaceCenterLit = 0;
static const float uFaceBend = 1.3;
static const float uFaceBow = 0.35;
static const float uFaceShade = 1;
static const float uNeckShadeAmt = 0.7;

float luma(float3 c) { return dot(c, V3(0.2126, 0.7152, 0.0722)); }

// 금색 영역은 sRGB 색상으로 판별한다.
float goldMask(float3 lin) {
  float3 c = pow(max(lin, 0.0), V3(1.0 / 2.2));
  float mx = max(max(c.r, c.g), c.b);
  float mn = min(min(c.r, c.g), c.b);
  float sat = (mx - mn) / max(mx, 1e-4);
  float hue = (c.g - c.b) / max(mx - mn, 1e-4);
  float redMax = step(c.g, c.r + 0.04);
  return smoothstep(0.35, 0.55, sat) * smoothstep(0.30, 0.50, mx) * smoothstep(0.18, 0.38, hue) * redMax;
}

float steelMask(float3 lin) {
  float3 c = pow(max(lin, 0.0), V3(1.0 / 2.2));
  float mx = max(max(c.r, c.g), c.b);
  float mn = min(min(c.r, c.g), c.b);
  float sat = (mx - mn) / max(mx, 1e-4);
  return (1.0 - smoothstep(0.12, 0.22, sat)) * smoothstep(0.38, 0.55, mx);
}

// 옷 텍스처에 포함된 피부 영역을 구분한다.
float skinMask(float3 lin) {
  float3 c = pow(max(lin, 0.0), V3(1.0 / 2.2));
  float mx = max(max(c.r, c.g), c.b);
  float mn = min(min(c.r, c.g), c.b);
  float sat = (mx - mn) / max(mx, 1e-4);
  float redMax = step(c.g, c.r) * step(c.b, c.r);
  float near = 1.0 - smoothstep(0.1, 0.18, length(c - uSkinRef));
  return smoothstep(0.15, 0.19, sat) * (1.0 - smoothstep(0.42, 0.5, sat)) * smoothstep(0.6, 0.7, mx) * redMax * near;
}

float getCastShadow(){return 1.0;}
float4 frag(v2f i, float facing : VFACE) : SV_Target {
float2 vUv=i.uv; float3 vWorldPos=i.worldPos; float3 vWorldNormal=i.normal; float3 vSoftNormal=i.softNormal; float3 vViewNormal=mul((float3x3)UNITY_MATRIX_V,i.normal); float vAO=i.ao; float3 uLightDir=normalize(UnityWorldSpaceLightDir(i.worldPos));
  float4 tex = tex2D(map, vUv);
  float3 rawAlbedo = tex.rgb * uBaseColor;
  float3 albedo = pow(max(rawAlbedo, 0.0), V3(uAlbedoGamma));
  float la = luma(albedo);
  albedo = max(lerp(V3(la), albedo, uAlbedoSat), 0.0);

  int part = int(uPart + 0.5);

  float neckW = 0.0, neckShadeW = 0.0;
  if ((part == 3 || part == 1) && uNeckTint > 0.0) {
    float3 hq = vWorldPos - uHeadCenter;
    float vq = dot(hq, uHeadUp) / uHeadRadius;
    float rq = length(hq - uHeadUp * dot(hq, uHeadUp)) / uHeadRadius;
    float mxq = max(max(rawAlbedo.r, rawAlbedo.g), rawAlbedo.b);

    neckW = (1.0 - smoothstep(0.5, 0.68, rq)) * smoothstep(-0.5, -0.65, vq) * (1.0 - smoothstep(1.9, 2.2, -vq))
          * smoothstep(0.25, 0.4, mxq) * uNeckTint;

    neckShadeW = neckW;

  }
  if (part == 3 && uSkinPick > 0.0 && neckW < 0.05 && skinMask(rawAlbedo) * uSkinPick > 0.5) part = 0;
  float3 N = normalize(vWorldNormal);
  float3 VN = normalize(vViewNormal);
  float3 SN = normalize(vSoftNormal);
  if (facing < 0) { N = -N; VN = -VN; SN = -SN; }
  if (part != 1) N = normalize(lerp(N, SN, uSoftNormal));
  float3 V = normalize(_WorldSpaceCameraPos - vWorldPos);
  float3 L = normalize(uLightDir);
  float3 hp = vWorldPos - uHeadCenter;

  // 얼굴 노멀을 구형으로 보간해 작은 굴곡의 명암을 완화한다.
  if (part == 1) N = normalize(lerp(N, normalize(hp + 1e-5), uFaceNormalSmooth));

  float NdotL = dot(N, L);
  float sh = getCastShadow();
  float shadowMul = lerp(1.0, sh, uCastShadow);

  float lf = NdotL * 0.5 + 0.5;

  float faceLit = 1.0;
  float faceW = 0.0;
  if (part <= 1) {
    float3 hpH = hp - uHeadUp * dot(hp, uHeadUp);
    float rH = length(hpH);
    float3 dirH = rH > 1e-5 ? hpH / rH : uHeadForward;
    float v = dot(hp, uHeadUp) / uHeadRadius;

    float region = part == 1 ? 1.0
      : (1.0 - smoothstep(1.3, 1.9, rH / uHeadRadius)) * (1.0 - smoothstep(3.2, 4.2, -v)) * uHasFace;

    float faceShadowMul = lerp(1.0, sh, uCastShadow * uFaceCastShadow);

    if (part == 1) lf = clamp(NdotL * 0.5 + 0.5 + uFaceWrap, 0.0, 1.0) * faceShadowMul;

    // 머리 기준 수평 광원 각도로 얼굴의 명암 경계를 조절한다.
    if (part == 1 && uFaceBend > 1.0) {
      float3 Lh = L - uHeadUp * dot(L, uHeadUp);
      float lh = length(Lh);
      if (lh > 1e-4) {
        Lh /= lh;
        float th = acos(clamp(dot(Lh, uHeadForward), -1.0, 1.0));
        float sd = dot(Lh, uHeadRight) >= 0.0 ? 1.0 : -1.0;
        float th2 = UNITY_PI * pow(th / UNITY_PI, uFaceBend);

        float3 Lb = uHeadForward * cos(th2) + uHeadRight * (sd * sin(th2));

        float sideAmt = 1.0 - abs(dot(Lh, uHeadForward));
        float bow = uFaceBow * sideAmt * (exp(-pow((v + 0.35) / 0.45, 2.0)) - 0.35);
        lf = clamp(dot(N, normalize(Lb)) * 0.5 + 0.5 + uFaceWrap + bow, 0.0, 1.0) * faceShadowMul;
      }
    }

    if (part == 1 && uFaceCenterLit > 0.0) {
      float3 Lf = L - uHeadUp * dot(L, uHeadUp);
      Lf = length(Lf) > 1e-4 ? normalize(Lf) : uHeadForward;
      float lightFront = smoothstep(-0.25, 0.25, dot(Lf, uHeadForward));
      float keepLit = smoothstep(uFaceCenterLit - 0.05, uFaceCenterLit + 0.01, dot(dirH, uHeadForward)) * lightFront;
      lf = lerp(lf, max(lf, 0.8), keepLit);
    }

    if (uFaceSDF > 0.0) {
      float3 Lh = L - uHeadUp * dot(L, uHeadUp);
      float lhl = length(Lh);
      Lh = lhl > 1e-4 ? Lh / lhl : uHeadForward;
      float facing = dot(Lh, uHeadForward);
      float front = max(dot(dirH, uHeadForward), 0.0);
      float side = 1.0 - abs(facing);
      float cheek = exp(-pow((v + 0.45) / 0.32, 2.0));
      float shape = 0.15 * cheek * uFaceCurve * side * front;
      float bias = uFaceBias * side * front;
      float d = dot(dirH, Lh) + shape + bias;
      float aa = fwidth(d) * 1.2;
      faceLit = smoothstep(-uFaceSoft - aa, uFaceSoft + aa, d);
      faceW = region * uFaceSDF;
      lf = lerp(lf, lerp(0.32, 0.88, faceLit) * faceShadowMul, faceW);
    }

    float jawUnder = part == 1
      ? smoothstep(-0.4, -0.7, dot(normalize(vWorldNormal), uHeadUp)) * smoothstep(-0.85, -1.05, v)
      : smoothstep(-0.95, -1.25, v) * region;
    lf = lerp(lf, min(lf, 0.34), jawUnder * uNeckShadow);

    if (part == 1 || jawUnder * uNeckShadow > 0.01) lf = max(lf, 0.3 * faceShadowMul);
  }

  float castSh = part == 1 ? 1.0 : sh;
  float2 rampUv = V2(clamp(lf, 0.004, 0.996), 0.5);
  float2 rampUvCast = V2(clamp(lf * castSh, 0.004, 0.996), 0.5);
  float3 ramp, rampCast;
  if (part <= 1) { ramp = tex2D(uRampSkin, rampUv).rgb; rampCast = tex2D(uRampSkin, rampUvCast).rgb; }

  if (part == 1 && (uFaceEdge > 0.0 || uFaceShade < 1.0)) {
    float3 fr = ramp;
    if (uFaceEdge > 0.0) {
      fr = (tex2D(uRampSkin, V2(clamp(lf - uFaceEdge, 0.004, 0.996), 0.5)).rgb
          + tex2D(uRampSkin, V2(clamp(lf - uFaceEdge * 0.5, 0.004, 0.996), 0.5)).rgb
          + ramp
          + tex2D(uRampSkin, V2(clamp(lf + uFaceEdge * 0.5, 0.004, 0.996), 0.5)).rgb
          + tex2D(uRampSkin, V2(clamp(lf + uFaceEdge, 0.004, 0.996), 0.5)).rgb) / 5.0;
    }
    ramp = lerp(tex2D(uRampSkin, V2(0.996, 0.5)).rgb, fr, uFaceShade);
    rampCast = ramp;
  }
  else if (part == 2) { ramp = tex2D(uRampHair, rampUv).rgb; rampCast = tex2D(uRampHair, rampUvCast).rgb; }
  else { ramp = tex2D(uRampCloth, rampUv).rgb; rampCast = tex2D(uRampCloth, rampUvCast).rgb; }
  ramp = lerp(ramp, rampCast, uCastShadow);

  if (neckW > 0.0) {

    float lfN = min(lf, 0.3);
    float3 neckRamp = lerp(tex2D(uRampSkin, V2(clamp(lf, 0.004, 0.996), 0.5)).rgb, lerp(tex2D(uRampSkin, V2(0.9, 0.5)).rgb, tex2D(uRampSkin, V2(0.3, 0.5)).rgb, uNeckShadeAmt), neckShadeW / max(neckW, 1e-4));
    ramp = lerp(ramp, neckRamp, neckW);
    lf = lerp(lf, lfN, neckShadeW);
  }
  lf *= lerp(1.0, castSh, uCastShadow);

  float occl = (1.0 - smoothstep(0.45, 0.85, vAO)) * uAO * (part == 1 ? 0.35 : 1.0);
  float2 rampUvShadow = V2(0.3, 0.5);
  float3 rampShadow = part <= 1 ? tex2D(uRampSkin, rampUvShadow).rgb
    : part == 2 ? tex2D(uRampHair, rampUvShadow).rgb : tex2D(uRampCloth, rampUvShadow).rgb;

  rampShadow = lerp(rampShadow, tex2D(uRampSkin, rampUvShadow).rgb, neckW);
  occl *= 1.0 - 0.65 * neckW;
  ramp = lerp(ramp, min(ramp, rampShadow), occl);
  lf = lerp(lf, min(lf, 0.32), occl);

  float3 H = normalize(L + V);
  float NdotH = max(dot(N, H), 0.0);
  float litMask = smoothstep(0.38, 0.6, lf);
  float gGold = (part == 3 || uGoldAny > 0.5) && uNoGold < 0.5 ? goldMask(rawAlbedo) : 0.0;
  float gSteel = part == 3 && uSteel > 0.0 ? steelMask(rawAlbedo) * uSteel : 0.0;
  float gm = max(gGold, gSteel);
  float3 metalTint = lerp(uMetalTint, uMetalSteelTint, gSteel / max(gGold + gSteel, 1e-4));
  float ring = 0.0;

  float3 col;
  if (uMode == 0) {
    col = albedo;
  } else if (uMode == 1) {
    col = albedo * (uLightColor * max(NdotL, 0.0) * shadowMul + uAmbient * 0.35);
  } else {
    float3 shaded = albedo * ramp;

    if (part == 3 && uClothHueKeep > 0.0) {
      float rl = luma(ramp);
      float satBoost = lerp(1.0, 1.25, clamp((1.0 - rl) * 2.0, 0.0, 1.0));
      float3 keep = max(lerp(V3(luma(albedo)), albedo, satBoost), 0.0) * rl;
      shaded = lerp(shaded, keep, uClothHueKeep * (1.0 - neckW));
    }
    col = shaded * uLightColor + albedo * uAmbient * uAmbientStrength;

    if (uMetal > 0.0 && gm > 0.0) {
      float mc = VN.y * 0.5 + 0.5 + VN.x * 0.12;
      float band = smoothstep(uMetalBand - 0.035, uMetalBand + 0.035, mc);
      float spec = smoothstep(0.90, 0.93, NdotH);
      float3 metalCol = albedo * metalTint * lerp(0.5, 1.3, band) * lerp(0.55, 1.0, litMask)
                    + albedo * spec * uMetalSpec * 2.5;
      col = lerp(col, metalCol, gm * uMetal);
    }

    // 머리 방향을 기준으로 이방성 하이라이트를 계산한다.
    if (part == 2 && uHairSpec > 0.0) {
      float3 T = normalize(uHeadUp - N * dot(N, uHeadUp) + 1e-5);
      float th = dot(T, H);
      float kk = pow(sqrt(max(1.0 - th * th, 0.0)), uHairSpecPow);

      float strand = smoothstep(0.85, 1.35, la / max(uHairLuma, 1e-4));
      ring = smoothstep(uHairSpecThresh, uHairSpecThresh + 0.04, kk) * strand * litMask;

      float k = ring * uHairSpec;
      col = lerp(col, uHairSpecColor * uLightColor, clamp(k, 0.0, 1.0)) + uHairSpecColor * max(k - 1.0, 0.0) * 0.5;
    }

    float fres = 1.0 - max(dot(N, V), 0.0);
    float rim = smoothstep(uRimWidth, uRimWidth + uRimSoft, fres) * (0.3 + 0.7 * litMask);

    col += rim * uRimColor * uRim * (part == 1 ? uRimFace : 1.0) * lerp(V3(1.0), albedo * 3.0, 0.4);

  }

  return float4(col,1);
}

// 화면 해상도를 기준으로 외곽선 두께를 유지한다.
v2f outlineVert(appdata v){v2f o=vert(v);float3 n=mul((float3x3)UNITY_MATRIX_V,o.softNormal);float2 d=mul(UNITY_MATRIX_P,float4(n,0)).xy;d/=max(length(d),1e-5);float scale=clamp(2.6/max(o.pos.w,.001),.75,1);o.pos.xy+=d*_OutlineWidth*scale*2/_ScreenParams.xy*o.pos.w;
#if defined(UNITY_REVERSED_Z)
o.pos.z-=.0004*o.pos.w;
#else
o.pos.z+=.0004*o.pos.w;
#endif
return o;}
float4 outlineFrag(v2f i):SV_Target{return float4(tex2D(map,i.uv).rgb*.32,1);}
ENDCG
SubShader { Tags{"RenderType"="Opaque" "Queue"="Geometry"}
Pass {Name "FORWARD" Tags{"LightMode"="ForwardBase"} Cull Off
CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fwdbase
ENDCG
}
Pass {Name "OUTLINE" Cull Front
CGPROGRAM
#pragma target 3.0
#pragma vertex outlineVert
#pragma fragment outlineFrag
ENDCG
}
UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
}
Fallback Off
}
