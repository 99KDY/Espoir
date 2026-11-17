Shader "Espoir/Environment/Chapter3/StainedGlass"
{
    // 스테인드글라스 그림에 투과광과 표면 반사를 더합니다.
    // MapDirection으로 무늬 기준 좌표에 맞춰 시선과 접선 방향을 변환합니다.
    Properties
    {
        _MapOrigin ("Map Origin", Vector) = (0, 0, 0, 0)
        _MapAxisX ("Map X Axis", Vector) = (-1, 0, 0, 0)
        _MapAxisZ ("Map Z Axis", Vector) = (0, 0, 1, 0)

        _Stain ("Stained Art", 2D) = "black" {}
        _StainGlow ("Stain Glow", Float) = 1.05
        _SkyZenith ("Sky Zenith", Color) = (0.42, 0.635, 0.902, 1)
        _SkyHorizon ("Sky Horizon", Color) = (0.902, 0.937, 0.98, 1)
        _Night ("Night", Float) = 0

        [Header(Lighting)]
        _LightTint ("Light Color", Color) = (1, 0.941, 0.863, 1)
        _FogColor ("Fog Color", Color) = (0.875, 0.91, 0.965, 1)
        _FogNear ("Fog Near", Float) = 110
        _FogFar ("Fog Far", Float) = 420
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "../Chapter2/Chapter2Common.cginc"

            sampler2D _Stain;
            float _StainGlow;
            float3 _SkyZenith;
            float3 _SkyHorizon;
            float _Night;

            struct SVertexInput
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct SVertexOutput
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            float StainHash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float StainNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(StainHash(i), StainHash(i + float2(1.0, 0.0)), f.x),
                    lerp(StainHash(i + float2(0.0, 1.0)), StainHash(i + float2(1.0, 1.0)), f.x), f.y);
            }

            // 0 = 납선 (빛을 막음)
            float StainMask(float3 c)
            {
                return smoothstep(0.03, 0.14, max(c.r, max(c.g, c.b)));
            }

            SVertexOutput Vert(SVertexInput v)
            {
                SVertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                return o;
            }

            float4 Frag(SVertexOutput i, float facing : VFACE) : SV_Target
            {
                float3 n = normalize(MapDirection(i.worldNormal));
                if (facing < 0.0) n = -n;
                float3 v = normalize(MapDirection(_WorldSpaceCameraPos - i.worldPos));
                float3 l = normalize(MapDirection(_WorldSpaceLightPos0.xyz));
                float3 wp = MapPosition(i.worldPos);
                float ndv = max(dot(n, v), 0.0);
                float3 r = reflect(-v, n);
                float3 sky = lerp(_SkyHorizon, _SkyZenith, smoothstep(-0.15, 0.6, r.y));
                sky = lerp(sky, _SkyHorizon * 0.55, _Night * 0.5);
                float fres = 0.1 + 0.9 * pow(1.0 - ndv, 4.0);
                float sp = dot(r, l);
                float spec = smoothstep(0.985, 0.992, sp) * 1.6 + smoothstep(0.93, 0.97, sp) * 0.18;
                float3 t = normalize(cross(float3(0.0, 1.0, 0.0), n + float3(0.0001, 0.0, 0.0)));
                float cloud = smoothstep(0.55, 0.7, sin(r.x * 9.0 + r.z * 4.0) * 0.5 + 0.5 +
                    sin(r.y * 13.0 - r.x * 5.0) * 0.25)
                    * smoothstep(0.0, 0.3, r.y);
                float3 skyR = lerp(sky, float3(1.0, 0.98, 0.95), cloud * 0.6);
                float time = _Time.y;

                float3 st = tex2D(_Stain, i.uv).rgb;
                float glassM = StainMask(st);
                // ① 유리 두께: 살짝 어긋난 안쪽 층을 한 번 더 읽어 섞음
                float2 par = float2(dot(v, t), v.y) * 0.011;
                float3 sc = lerp(st, tex2D(_Stain, i.uv - par).rgb, 0.3);
                // ② 납선 돋을새김
                float2 eo = float2(0.0042, 0.0017);
                float emb = StainMask(tex2D(_Stain, i.uv - eo).rgb) - StainMask(tex2D(_Stain, i.uv + eo).rgb);
                // ③ 유리 속을 천천히 흐르는 빛
                float flow = StainNoise(i.uv * float2(7.0, 17.0) + float2(dot(v, t), v.y) * 2.6 +
                    float2(time * 0.07, -time * 0.05));
                float shim = smoothstep(0.45, 0.95, flow);
                // ④ 비스듬한 햇살 띠
                float beam = pow(0.5 + 0.5 * sin((i.uv.x * 0.55 + i.uv.y * 0.8 - time * 0.035) * 6.2832), 8.0);
                float back = max(dot(-n, l), 0.0);
                // ⑤ 스스로 머금은 빛 (밝은 조각은 1 을 넘겨 화면 빛 번짐이 받음)
                float3 trans = sc * _StainGlow * (1.0 + 0.3 * back) * (1.0 + _Night * 0.12);
                trans += sc * sc * _StainGlow * (0.46 + 0.3 * back + _Night * 0.08);
                trans *= (1.0 + shim * 0.42 + beam * 0.3) * lerp(1.0, 0.6, _Night);
                float sw = (dot(wp, t) + wp.y * 0.55) / 2.4 + dot(v, t) * 0.9;
                float f = frac(sw);
                float sheen = smoothstep(0.08, 0.1, f) * (1.0 - smoothstep(0.22, 0.24, f))
                    + 0.6 * smoothstep(0.31, 0.325, f) * (1.0 - smoothstep(0.375, 0.39, f));
                float3 col = trans * (1.0 - max(-emb, 0.0) * 0.3) + glassM *
                    (skyR * fres * 0.5 + (skyR * 0.3 + 0.06) * sheen * 0.36);
                col += float3(1.0, 0.97, 0.9) * max(emb, 0.0) * (0.34 + 0.3 * back);
                col += _LightTint * spec;
                col = lerp(col, _FogColor, smoothstep(_FogNear, _FogFar, length(_WorldSpaceCameraPos - i.worldPos)));
                return float4(col, 1.0);
            }
            ENDCG
        }
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
