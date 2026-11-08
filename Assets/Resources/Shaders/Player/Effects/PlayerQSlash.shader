Shader "Espoir/Player/Effects/Q/Slash"
{
    Properties
    {
        [NoScaleOffset] _Noise ("Noise (RGBA 데이터)", 2D) = "gray" {}
        [NoScaleOffset] _Times ("칼끝 시간표", 2D) = "black" {}
        _HasTimes ("칼끝 시간표 사용", Float) = 0
        _C0 ("Ramp 0 (Linear HDR)", Vector) = (0.03, 0, 0.01, 0)
        _C1 ("Ramp 1", Vector) = (0.3, 0, 0.03, 0)
        _C2 ("Ramp 2", Vector) = (2, 0.04, 0.06, 0)
        _C3 ("Ramp 3", Vector) = (4.5, 1.6, 1.2, 0)
        [Header(Timing)]
        _Lifetime ("파티클 수명 (초)", Float) = 0.59
        _Duration ("그려지는 시간", Float) = 0.1
        _Linger ("남는 시간", Float) = 0.34
        _HoldTail ("꼬리 유지", Float) = 0
        _TailStart ("꼬리 시작 비율", Float) = 0.3
        [Header(Shape)]
        _Expand ("퍼짐", Float) = 0.18
        _Thick ("두께", Float) = 0.85
        _Rough ("안쪽 찢김", Float) = 1
        _Freq ("호 방향 노이즈 밀도", Float) = 1
        _Hot ("바깥 날 달아오름", Float) = 0
        _SoftEnds ("붓끝처럼 끝 풀기", Float) = 0
        _SoftHead ("앞 끝 풀기", Float) = 0
        _EdgeFade ("모서리로 볼 때 옅어짐", Float) = 0
        _Alpha ("불투명도", Float) = 1
        [Header(Burst Ring)]
        _Burst ("터지는 고리 (2 = 한 바퀴)", Float) = 0
        _BurstHold ("유지", Float) = 0
        _BurstGrow ("시작 크기 부족분", Float) = 0
        _BurstGrowTime ("자라는 시간", Float) = 0.08
        _BurstFadeFrom ("옅어지기 시작", Float) = 0.3
        _BurstR0 ("안쪽 반지름", Float) = 0
        _BurstR1 ("바깥 반지름", Float) = 0

        _BurstCentered ("버스트: 파티클 중심에서 자라남 (1 = 웹처럼 중심 기준 축소)", Float) = 0
        _Rigid ("버스트: 띠 전체가 같이 밀림 (1 = 얇은 막 유지)", Float) = 0
        _BurstEnds ("버스트: 부분 호 양 끝을 찢기며 흐리게", Float) = 0
        _OuterRough ("바깥 날 불꽃 끝처럼 들쭉날쭉", Float) = 0
        _DissolveMax ("버스트: 디졸브 끝값 (낮으면 덜 부서지고 옅어짐으로 사라짐)", Float) = 1.05
        _FloorOffset ("바닥 높이 (파티클 중심 기준)", Float) = -1.03
        [Header(Follow Head)]
        _FollowDelay ("머리 나이 - 가닥 나이", Float) = 0
        _HScale ("크기", Float) = 4.6
        _HLen ("모델 길이", Float) = 0.9995
        _HLengthen ("앞뒤 늘림", Float) = 1.4
        _HCharge ("차징 시간", Float) = 0.09
        _HReveal ("차징 때 드러나는 비율", Float) = 0.45
        _HBack ("차징 때 뒤로 물러남", Float) = 0.12
        _HLunge ("돌진 시간", Float) = 0.05
        _HHold ("버티는 시간", Float) = 0.12
        _HExit ("퇴장 시간", Float) = 0.36
        _HOvershoot ("돌진 넘침", Float) = 0.04
        _HPop ("크기 튐", Float) = 0.03
        _HDash ("퇴장 때 밀려나는 거리", Float) = 1
        _HHotHold ("버티는 동안 밝기 하한", Float) = 0.7
        [Header(Capture)]
        _SeedFixed ("고정 시드 (음수 = 랜덤)", Float) = -1
        _TimeBase ("고정 시간 기준 (음수 = 랜덤)", Float) = -1
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+1"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        Pass
        {
            Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma shader_feature_local _FOLLOW
            #pragma shader_feature_local _FACE
            #include "PlayerQFxCommon.cginc"
            #include "PlayerQLookRamp.cginc"

            float4 _C0, _C1, _C2, _C3;
            float _Thick, _Rough, _Freq, _Hot, _SoftEnds, _SoftHead, _EdgeFade;
            float _Burst, _BurstHold, _BurstGrow, _BurstGrowTime, _BurstFadeFrom, _BurstR0, _BurstR1;
            float _BurstCentered, _Rigid, _BurstEnds, _OuterRough, _DissolveMax;

            struct V2F
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 state : TEXCOORD1;
                float seed : TEXCOORD2;
                float3 world : TEXCOORD3;
                float2 burst : TEXCOORD4;
            };

            V2F vert(FxAppdata v)
            {
                V2F o;
                float age = FxAge(v), D = _Duration;
                float diss = FxSaturate((age - _HoldTail - D - 0.03) / _Linger);
                o.state.x = FxHeadAt(FxSaturate(age / D));
                o.state.y = FxTail(age) * 0.92;
                o.state.z = diss <= 0.0 ? -0.2 : lerp(0.05, 1.05, diss);
                o.state.w = FxTime(v, age);
                o.seed = FxSeed(v);
                o.uv = v.uvAge.xy;
                o.world = FxWorldPos(v, _Expand * FxEaseOut(FxSaturate(age / (D + _Linger))), age);
                o.burst = float2(0.0, 1.0);
                if (_Burst > 0.5)
                {

                    float k = FxSaturate(age / (_Linger + 0.12));
                    float q = 1.0 - k, burst = FxSaturate((age - _BurstHold) / _Linger);
                    float grow = _BurstGrow * (1.0 - FxEaseOut(FxSaturate(age / _BurstGrowTime)));
                    o.state.x = 1.1;
                    o.state.y = 0.0;
                    o.state.z = burst <= 0.0 ? -0.2 : lerp(0.05, _DissolveMax, burst);
                    float3 dir = normalize(v.normal);
                    float3 p = v.vertex.xyz - dir * lerp(_BurstR0, _BurstR1, v.uvAge.y) * grow;

                    if (_BurstCentered > 0.5) p = v.center + (v.vertex.xyz - v.center) * (1.0 - grow);
                    p += dir * _Expand * (1.0 - q * q * q * q) * lerp(0.35 + v.uvAge.y, 1.0 + 0.25 * v.uvAge.y, _Rigid);
                    p.y = max(p.y, v.center.y + _FloorOffset);
                    o.world = p;
                    o.burst = float2(0.08 + 0.82 * k, 1.0 - smoothstep(_BurstFadeFrom, 1.0, k));
                }
                o.pos = UnityWorldToClipPos(o.world);
                return o;
            }

            float3 Ramp(float i)
            {
                return LookRamp(i, _C0.rgb, _C1.rgb, _C2.rgb, _C3.rgb);
            }

            float4 frag(V2F i) : SV_Target
            {
                float s = i.uv.x, y = i.uv.y;
                float uHead = i.state.x, uTail = i.state.y, uDissolve = i.state.z, uTime = i.state.w, uSeed = i.seed;
                float lag = uHead - s;
                if (lag < 0.0) discard;
                float span = max(uHead - uTail, 0.001);
                float life = _Burst > 0.5 ? i.burst.x : saturate(lag / span);
                life = max(life, 0.3 * _SoftHead);
                float sn = s * _Freq;

                float n1 = tex2D(_Noise, float2(sn * 1.6 + uSeed, y * 0.35 - uTime * 0.12)).r;
                float n2 = tex2D(_Noise, float2(sn * 4.0 - uTime * 0.35 + uSeed, y * 1.2)).g;
                float streak = tex2D(_Noise, float2(sn * 0.8 + uSeed * 0.3, y * 1.0)).b;

                float width = lerp(_Thick, _Thick * 0.22, pow(life, 0.7));
                float inner = 1.0 - width + ((n1 - 0.5) * 0.55 * (0.25 + life) - streak * 0.18 * life) * _Rough;
                float outerY = 1.0;
                float tip = tex2D(_Noise, float2(sn * 3.0 + uSeed, 0.31 + uTime * 0.05)).r * 0.6 + tex2D(_Noise,
                    float2(sn * 8.0 + uSeed, 0.73)).g * 0.4;
                outerY -= tip * tip * _OuterRough;
                outerY -= tip * tip * (0.2 + 0.4 * (1.0 - smoothstep(0.1, 0.45, s))) * _SoftEnds;
                float body = smoothstep(inner, inner + 0.035, y);
                body *= 1.0 - smoothstep(outerY - 0.035, outerY, y);
                float tailCut = smoothstep(uTail, uTail + 0.06, s + (n2 - 0.5) * 0.12);
                if (_SoftEnds > 0.0)
                {
                    float tear = ((n1 - 0.5) * 0.5 + (streak - 0.5) * 0.3) * _SoftEnds;
                    float sc = s + (1.0 - y) * 0.18 * _SoftEnds + tear;
                    tailCut = smoothstep(uTail, uTail + 0.22, sc) * smoothstep(0.0, 0.35, s + tear * 0.5);
                    tailCut *= 1.0 - smoothstep(0.8, 1.0, s + tear * 0.6 - (1.0 - y) * 0.12);
                }
                if (_BurstEnds > 0.5 && _Burst > 0.5 && _Burst < 1.5) tailCut *= smoothstep(0.0, 0.24, s + (n1 -
                    0.5) * 0.25) * (1.0 - smoothstep(0.74, 0.98, s + (n1 - 0.5) * 0.25));
                float headCut = smoothstep(0.0, 0.012 + 0.15 * _SoftHead, lag + (n1 - 0.5) * 0.08 * _SoftHead);

                float yy = saturate((y - inner) / max(1.0 - inner, 0.001));
                float I = 0.22 + 0.78 * pow(yy, 1.35);
                I *= lerp(1.08, 0.5, life);
                I += (n2 - 0.5) * 0.3 + (streak - 0.5) * 0.35 * yy * LookStreak();
                I += smoothstep(0.9, 0.99, y) * 0.35 * (1.0 - life);
                I += smoothstep(0.55, 0.95, yy) * _Hot * (1.0 - life);
                I += (1.0 - smoothstep(0.0, 0.05, lag)) * 0.5 * (1.0 - _SoftHead);

                float d = tex2D(_Noise, float2(sn * 2.4 + uSeed, y * 1.4)).a * 0.6 + n2 * 0.4;
                float vis = smoothstep(uDissolve, uDissolve + 0.03, d);
                float dEdge = vis * (1.0 - smoothstep(uDissolve + 0.03, uDissolve + 0.12, d)) * step(0.0, uDissolve);

                float3 col = Ramp(saturate(I));
                col += dEdge * _C2.rgb * 1.2;
                float a = body * tailCut * headCut * vis * _Alpha * i.burst.y;
                a *= smoothstep(0.02, 0.2, I + 0.15);
                if (_EdgeFade > 0.0)
                {
                    float3 nrm = normalize(cross(ddx(i.world), ddy(i.world)));
                    float facing = abs(dot(nrm, normalize(_WorldSpaceCameraPos - i.world)));
                    a *= lerp(1.0, smoothstep(0.06, 0.5, facing), _EdgeFade);
                }
                if (a < 0.01) discard;

                return float4(col, a);
            }
            ENDCG
        }
    }
    Fallback Off
}
