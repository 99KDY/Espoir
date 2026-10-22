Shader "Espoir/Environment/Window"
{
    Properties
    {
        _MapOrigin ("Map Origin", Vector) = (0, 0, 0, 0)
        _MapAxisX ("Map X Axis", Vector) = (1, 0, 0, 0)
        _MapAxisZ ("Map Z Axis", Vector) = (0, 0, 1, 0)

        _SkyZenith ("Sky Zenith", Color) = (0.478, 0.651, 0.863, 1)
        _SkyHorizon ("Sky Horizon", Color) = (0.984, 0.878, 0.8, 1)
        _Room ("Room", Color) = (0.369, 0.298, 0.282, 1)
        _RoomLit ("Room Lit", Color) = (1, 0.769, 0.416, 1)
        _Lit ("Room Light On", Float) = 0

        [Header(Lighting)]
        _LightTint ("Light Color", Color) = (1, 0.902, 0.776, 1)
        _FogColor ("Fog Color", Color) = (0.918, 0.855, 0.8, 1)
        _FogNear ("Fog Near", Float) = 100
        _FogFar ("Fog Far", Float) = 360
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "EnvironmentCommon.cginc"

            float3 _SkyZenith;
            float3 _SkyHorizon;
            float3 _Room;
            float3 _RoomLit;
            float _Lit;

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
                float3 normalDirection = normalize(MapDirection(i.worldNormal));
                if (facing < 0.0) normalDirection = -normalDirection;
                float3 viewDirection = normalize(MapDirection(_WorldSpaceCameraPos - i.worldPos));
                float ndv = max(dot(normalDirection, viewDirection), 0.0);
                float3 reflectionDirection = reflect(-viewDirection, normalDirection);
                float3 sky = lerp(_SkyHorizon, _SkyZenith, smoothstep(-0.15, 0.6, reflectionDirection.y));
                float fres = 0.1 + 0.9 * pow(1.0 - ndv, 4.0);

                // 창 안쪽 방: uv(0~1) 뒤로 깊이 0.8 상자를 시선으로 따라가 벽·바닥·천장을 칠함
                float3 T = normalize(cross(float3(0.0, 1.0, 0.0), normalDirection + float3(0.0001, 0.0, 0.0)));
                float3 B = float3(0.0, 1.0, 0.0);
                float3 rd = -viewDirection;
                float3 r = float3(dot(rd, T), dot(rd, B), min(dot(rd, normalDirection), -1e-3));
                float3 ro = float3(clamp(i.uv, 0.001, 0.999), 0.0);
                float dep = 0.8;
                float tx = (r.x > 0.0 ? 1.0 - ro.x : -ro.x) / (abs(r.x) < 1e-4 ? 1e-4 : r.x);
                float ty = (r.y > 0.0 ? 1.0 - ro.y : -ro.y) / (abs(r.y) < 1e-4 ? 1e-4 : r.y);
                float tz = -dep / r.z;
                tx = tx <= 0.0 ? 1e5 : tx;
                ty = ty <= 0.0 ? 1e5 : ty;
                float t = min(min(tx, ty), tz);
                float3 hp = ro + r * t;
                float dz = saturate(-hp.z / dep);
                float3 room;
                if (t == tz) room = _Room * (0.62 + 0.38 * hp.y);
                else if (t == ty) room = r.y > 0.0 ? _Room * 0.42 : _Room * 0.78;
                else room = _Room * (0.55 + 0.3 * hp.y);
                room *= lerp(1.0, 0.62, dz);
                float2 eg = min(i.uv, 1.0 - i.uv);
                room *= lerp(0.55, 1.0, smoothstep(0.0, 0.12, min(eg.x, eg.y)));
                float3 lit = _RoomLit * (1.15 - 0.45 * hp.y) * lerp(1.0, 0.75,
                    dz) * (0.85 + 0.15 * smoothstep(0.0, 0.1, min(eg.x, eg.y)));
                room = lerp(room, lit, _Lit);
                float cloud = smoothstep(0.55, 0.7,
                    sin(reflectionDirection.x * 9.0 + reflectionDirection.z * 4.0) * 0.5 + 0.5 +
                        sin(reflectionDirection.y * 13.0 - reflectionDirection.x * 5.0) * 0.25)
                    * smoothstep(0.0, 0.3, reflectionDirection.y);
                float3 reflectedSky = lerp(sky, float3(1.0, 0.98, 0.95), cloud * 0.6);
                float3 baseColor = lerp(room * float3(0.95, 1.05, 1.0) * 1.15, reflectedSky, fres * 0.6);

                // 창 크기에 관계없이 빛면과 그늘면의 대각선 각도를 유지한다.
                float width = length(float2(ddx(dot(MapPosition(i.worldPos), T)), ddy(dot(MapPosition(i.worldPos), T))))
                    / max(length(float2(ddx(i.uv.x), ddy(i.uv.x))), 0.00001);
                float height = length(float2(ddx(i.worldPos.y), ddy(i.worldPos.y)))
                    / max(length(float2(ddx(i.uv.y), ddy(i.uv.y))), 0.00001);
                float diagonal = (i.uv.x * width + (1.0 - i.uv.y) * height) / max(width + height, 0.00001);
                float edge = max(fwidth(diagonal), 0.00001);
                float lightMask = 1.0 - smoothstep(0.4 - edge, 0.4 + edge, diagonal);
                float darkMask = smoothstep(0.74 - edge, 0.74 + edge, diagonal);
                float3 col = lerp(baseColor, reflectedSky * 0.9 + 0.08, lightMask * 0.07)
                    * lerp(1.0, 0.94, darkMask);
                col = lerp(col, _FogColor, smoothstep(_FogNear, _FogFar, length(_WorldSpaceCameraPos - i.worldPos)));
                return float4(col, 1.0);
            }
            ENDCG
        }
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
