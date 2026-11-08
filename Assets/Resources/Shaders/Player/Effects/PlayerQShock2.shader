Shader "Espoir/Player/Effects/Q/Shock2"
{
    Properties
    {
        _Lifetime ("수명 (초)", Float) = 0.3
        _Strength ("세기 (화면 UV)", Float) = 0.035
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+50"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "EspoirDistort" = "On"
        }
        Pass
        {
            ColorMask 0 ZWrite Off ZTest Never Cull Off
        }
    }
    Fallback Off
}
