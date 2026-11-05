Shader "Espoir/Player/Effects/GroundShock"
{
    Properties
    {
        _Lifetime ("수명 (초)", Float) = 0.42
        _R0 ("시작 반지름 (m)", Float) = 0.5
        _Expand ("퍼지는 거리 (m)", Float) = 3
        _Width ("고리 폭 (m)", Float) = 0.45
        _Half ("판 반지름 (m) = 시작 + 퍼짐 + 폭 x3", Float) = 4.85
        _Strength ("세기 (화면 UV)", Float) = 0.05
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+50" "RenderType" = "Transparent"
            "IgnoreProjector" = "True" "EspoirDistort" = "Ground"
        }
        Pass
        {
            ColorMask 0 ZWrite Off ZTest Never Cull Off
        }
    }
    Fallback Off
}
