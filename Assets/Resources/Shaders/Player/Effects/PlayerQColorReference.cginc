#ifndef ESPOIR_COLOR_REFERENCE
#define ESPOIR_COLOR_REFERENCE

static const float3 DEFAULT_GROUND = float3(0.217604, 0.051244, 0.026199);

float4 _EspoirGround;
#define REFERENCE_GROUND (_EspoirGround.w > 0.5 ? _EspoirGround.rgb : DEFAULT_GROUND)

#endif
