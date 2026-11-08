#ifndef ESPOIR_LOOK_RAMP
#define ESPOIR_LOOK_RAMP

float4 _EspoirLook;
float4 _EspoirRamp0, _EspoirRamp1, _EspoirRamp2, _EspoirRamp3;
float4 _EspoirWisp0, _EspoirWisp1;

float3 LookRamp(float i, float3 c0, float3 c1, float3 c2, float3 c3)
{
    const float soft = 0.05;
    float hotAt = 0.80, bodyAt = 0.40, hotSoft = soft;
    if (_EspoirLook.x > 0.5)
    {
        c0 = _EspoirRamp0.rgb;
        c1 = _EspoirRamp1.rgb;
        c2 = _EspoirRamp2.rgb;
        c3 = _EspoirRamp3.rgb;
        hotAt = _EspoirLook.y;
        bodyAt = _EspoirLook.z;
        hotSoft = soft * 0.6;
    }
    float3 c = c0;
    c = lerp(c, c1, smoothstep(0.18 - soft, 0.18 + soft, i));
    c = lerp(c, c2, smoothstep(bodyAt - soft, bodyAt + soft, i));
    c = lerp(c, c3, smoothstep(hotAt - hotSoft, hotAt + hotSoft, i));
    return c;
}

float LookStreak()
{
    return _EspoirLook.x > 0.5 ? 1.0 + _EspoirLook.w : 1.0;
}

#endif
