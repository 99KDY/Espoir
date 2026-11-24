#ifndef CHARACTER_INFO_HAND_MARK_COMMON_INCLUDED
#define CHARACTER_INFO_HAND_MARK_COMMON_INCLUDED

// 문양 텍스처 채널: R 선, G 좁은 번짐, B 넓은 번짐. UV 위쪽이 손가락 방향이다.

#include "UnityCG.cginc"

sampler2D _MarkTex;
float _MarkGlow;
float _MarkReveal;
float _MarkTime;
float4 _MarkSpread;     // x 좁은 물들임, y 넓은 물들임, z 선을 타고 흐르는 빛, w 꺼져 있을 때 남는 빛

struct SMarkInput
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float2 face : TEXCOORD1;
};

struct SMarkVaryings
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float face : TEXCOORD1;
};

SMarkVaryings MarkVert(SMarkInput v)
{
    SMarkVaryings o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    o.face = v.face.x;
    return o;
}

float3 MarkSample(float2 uv, float face)
{
    float2 inside = step(0.0, uv) * step(uv, 1.0);
    return tex2D(_MarkTex, uv).rgb * inside.x * inside.y * smoothstep(0.1, 0.45, face);
}

// 반환값: x 발광 영역, y 확산 경계, z 선을 따라 흐르는 빛.
float3 MarkSpread(float2 uv)
{
    float d = distance(uv, float2(0.5, 0.5));
    float radius = _MarkReveal * 1.05;
    float lit = 1.0 - smoothstep(radius - 0.1, radius, d);
    float dr = (d - radius) / 0.05;
    float front = exp(-dr * dr) * (1.0 - smoothstep(0.8, 1.0, _MarkReveal));
    float wave = pow(0.5 + 0.5 * sin(d * 15.0 - _MarkTime * 3.6), 8.0) * _MarkSpread.z;
    return float3(lit, front, wave);
}

#endif
