// 지정된 UI 구간을 sRGB 공간에서 합성하기 위한 공용 함수입니다.
// Linear 공간에서의 알파 합성과 sRGB 공간에서의 알파 합성은 색과 그림자 결과가 다릅니다.
// 구간 앞뒤의 화면 사각형에서 색 공간을 변환해 UI 내부 합성 공간을 일정하게 유지합니다.
//   구간 시작(UiSrgbBegin): 장면 색 S 를 sRGB 숫자 그대로(선형 값인 척) 다시 써 둔다 → 화면 버퍼가 'sRGB 숫자'를 담는 상태.
//   구간 안 모든 UI 셰이더: _UiGammaOut = 1 이면 색을 선형으로 바꾸지 않고 sRGB 숫자 그대로 내보낸다 → GPU 의 섞기가 곧 sRGB 섞기(CSS 와 같은 식).
//   구간 끝(UiSrgbEnd): 버퍼의 sRGB 숫자를 진짜 선형 값으로 되돌려 쓴다.
// 구간 안에서 화면을 잡는(GrabPass) 셰이더는 잡은 값을 이미 sRGB 숫자로 읽는다.
#ifndef ESPOIR_UI_SRGB_COMPOSITE_INCLUDED
#define ESPOIR_UI_SRGB_COMPOSITE_INCLUDED

float _UiGammaOut;

float3 UiLinToSrgb(float3 c)
{
    c = max(c, 0.0);
    return c <= 0.0031308 ? c * 12.92 : 1.055 * pow(c, 1.0 / 2.4) - 0.055;
}

float3 UiSrgbToLin(float3 c)
{
    c = max(c, 0.0);
    return c <= 0.04045 ? c / 12.92 : pow((c + 0.055) / 1.055, 2.4);
}

// 미리곱 선형 출력 → 구간 안이면 미리곱 sRGB 숫자로 (TMP · 기본 UI 처럼 선형으로 계산하는 셰이더용)
float4 UiPremulOut(float4 c)
{
    if (_UiGammaOut < 0.5 || c.a <= 1e-5) return c;
    return float4(UiLinToSrgb(c.rgb / c.a) * c.a, c.a);
}

// 곧은(straight) 선형 색 출력 → 구간 안이면 sRGB 숫자로
float3 UiStraightOut(float3 c)
{
    return _UiGammaOut > 0.5 ? UiLinToSrgb(c) : c;
}

#endif
