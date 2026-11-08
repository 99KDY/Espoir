#ifndef ESPOIR_FX_COMMON
#define ESPOIR_FX_COMMON
#include "UnityCG.cginc"
#ifdef _FOLLOW
#include "PlayerQHeadState.cginc"
float _FollowDelay;
#endif

sampler2D _Noise;

sampler2D _Times;
float _HasTimes;
float _Lifetime, _Duration, _Linger, _HoldTail, _TailStart, _Expand, _FloorOffset, _Alpha;

float _SeedFixed, _TimeBase;

struct FxAppdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
#if defined(_FOLLOW) || defined(_FACE)
    float4 tangent : TANGENT;
#endif
    float4 uvAge : TEXCOORD0;
    float3 center : TEXCOORD1;
};

float FxSaturate(float v)
{
    return saturate(v);
}
float FxEaseOut(float t)
{
    float k = 1.0 - t;
    return 1.0 - k * k * k;
}
float FxAge(FxAppdata v)
{
    return v.uvAge.z * _Lifetime;
}
float FxSeed(FxAppdata v)
{
    return _SeedFixed >= 0.0 ? _SeedFixed : v.uvAge.w * 10.0;
}
float FxTime(FxAppdata v, float age)
{
    return (_TimeBase >= 0.0 ? _TimeBase : v.uvAge.w * 37.0) + age;
}

float FxTail(float age)
{
    float D = _Duration;
    return pow(FxSaturate((age - _HoldTail - D * _TailStart) / (D * (1.0 - _TailStart) + _Linger * 0.6)), 1.3);
}

float FxHeadAt(float u)
{
    if (_HasTimes < 0.5) return u;
    return tex2Dlod(_Times, float4(u * 255.0 / 256.0 + 0.5 / 256.0, 0.5, 0.0, 0.0)).r;
}

float3 FxWorldPos(FxAppdata v, float expand, float age)
{
#ifdef _FACE

    float3 axis = normalize(v.tangent.xyz), baked = normalize(v.normal);
    float3 q = v.vertex.xyz - v.center;
    float along = dot(q, axis), w = dot(q, baked);
    float3 side = normalize(cross(_WorldSpaceCameraPos - v.center, axis));
    if (dot(side, baked) < 0.0) side = -side;
    return v.center + axis * along + side * (w + (w < 0.0 ? -expand : expand) * (0.35 + v.uvAge.y));
#elif defined(_FOLLOW)
    FxBasis b = FxBasisOf(v.tangent.xyz);
    float3 q = FxToModel(b, v.vertex.xyz - v.center) / FX_MESH_SCALE;
    q += normalize(FxToModel(b, v.normal)) * expand * (0.35 + v.uvAge.y);
    float t = age + _FollowDelay;
    return v.center + FxFromModel(b, HeadPlace(q, HeadAt(t), t));
#else
    float3 p = v.vertex.xyz + normalize(v.normal) * expand * (0.35 + v.uvAge.y);
    p.y = max(p.y, v.center.y + _FloorOffset);
    return p;
#endif
}
#endif
