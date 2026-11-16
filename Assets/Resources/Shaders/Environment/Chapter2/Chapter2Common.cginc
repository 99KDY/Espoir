#ifndef ESPOIR_CHAPTER2_COMMON_INCLUDED
#define ESPOIR_CHAPTER2_COMMON_INCLUDED

// 사막 맵 공용 함수(노이즈·판석·램프·맵 좌표)를 그대로 쓰고, 2챕터에만 필요한 값을 더한다.
#include "../EnvironmentCommon.cginc"

#define CHAPTER2_LAMP_SLOTS 40
float4 _Chapter2LampPos[CHAPTER2_LAMP_SLOTS];     // xyz = 월드 위치, w = 반경
float4 _Chapter2LampColor[CHAPTER2_LAMP_SLOTS];   // rgb = 선형 색 × 세기 × 깜빡임
float _Chapter2LampCount;
// 1 = 가운데가 환하고 멀리 길게 풀리는 빛, 0 = 사막 맵과 같은 2단 원
float _Chapter2LampSoft;

// 풀 덮개: CChapter2Grass 가 플레이어 둘레 반경을 알려 준다 (z = 반경, 0 이면 꺼짐)
float4 _Chapter2GrassCenter;   // x, y = 월드 x, z  /  z = 반경
float4 _Chapter2GrassLod;      // x = 시작 거리, y = 끝 거리, z = 먼 곳 대비
float _Chapter2GrassUnder;
float _Chapter2GrassWave;
float _Chapter2GrassRim;

float Chapter2Luma(float3 c)
{
    return dot(c, float3(0.299, 0.587, 0.114));
}

float3 Chapter2LampLight(float3 p, float3 normalDirection)
{
    float3 sum = 0.0;
    int count = (int)_Chapter2LampCount;
    for (int i = 0; i < CHAPTER2_LAMP_SLOTS; i++)
    {
        if (i >= count) break;
        float3 d = _Chapter2LampPos[i].xyz - p;
        float r = _Chapter2LampPos[i].w;
        float limit = r * 1.35;
        if (dot(d, d) >= limit * limit) continue;
        float dist = length(d);
        float att = 1.0 - smoothstep(r * 0.15, r, dist);
        att *= att;
        float ndl = max(dot(normalDirection, d / max(dist, 1e-4)), 0.0) * 0.7 + 0.3;
        float stepAtt = smoothstep(0.08, 0.12, att) * 0.55 + smoothstep(0.45, 0.5, att) * 0.45;
        // 등 바로 옆 벽이 하얗게 타지 않게 등 높이만큼은 떨어진 것으로 치고, 세로 벽은 절반만 받는다
        float soft = 1.0 - smoothstep(0.0, r * 1.35, max(dist, 3.0));
        soft = soft * soft * soft * 1.9 * lerp(0.5, 1.0, saturate(normalDirection.y));
        sum += _Chapter2LampColor[i].rgb * lerp(lerp(att, stepAtt, 0.6), soft, _Chapter2LampSoft) * ndl;
    }
    return sum;
}

// 가장 강하게 비추는 등불의 방향과 세기 (밤 반짝임용)
float Chapter2LampDominant(float3 p, out float3 direction)
{
    float best = 0.0;
    direction = float3(0.0, 1.0, 0.0);
    int count = (int)_Chapter2LampCount;
    for (int i = 0; i < CHAPTER2_LAMP_SLOTS; i++)
    {
        if (i >= count) break;
        float3 d = _Chapter2LampPos[i].xyz - p;
        float limit = _Chapter2LampPos[i].w * 1.1;
        if (dot(d, d) >= limit * limit) continue;
        float dist = length(d);
        float att = 1.0 - smoothstep(_Chapter2LampPos[i].w * 0.1, _Chapter2LampPos[i].w * 1.1, dist);
        float lum = att * att * Chapter2Luma(_Chapter2LampColor[i].rgb);
        if (lum > best)
        {
            best = lum;
            direction = d / max(dist, 1e-4);
        }
    }
    return best;
}

float Chapter2GrassWave(float2 p, float t)
{
    float a = sin(dot(p, float2(0.6, 0.8)) * 0.21 - t * 1.1 + sin(dot(p, float2(-0.8, 0.6)) * 0.13 + t * 0.3) * 1.4);
    float b = sin(dot(p, float2(0.9, 0.44)) * 0.11 - t * 0.6 + 1.7);
    return smoothstep(0.3, 1.0, a * 0.65 + b * 0.35 + 0.15);
}

// 풀 반경 안쪽 정도 (반경 가장자리 28% 에서 0 으로)
float Chapter2GrassNear(float3 worldPos)
{
    return 1.0 - smoothstep(_Chapter2GrassCenter.z * 0.72, _Chapter2GrassCenter.z,
        length(worldPos.xz - _Chapter2GrassCenter.xy));
}

#endif
