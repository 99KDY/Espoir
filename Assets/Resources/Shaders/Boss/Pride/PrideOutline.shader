Shader "Espoir/PrideOutline"
{
    Properties
    {
        _MainTex ("Albedo (map)", 2D) = "white" {}
        _BaseColor ("Base Color (uBaseColor)", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Width px (uOutlineWidth)", Float) = 1
        _OutlineZ ("Depth Push (uOutlineZ)", Float) = 0.0004
        _OutlineColor ("Color (uOutlineColor)", Color) = (0.078, 0.047, 0.094, 1)
        _OutlineTex ("Use Texture (uOutlineTex)", Float) = 1
        _OutlineDark ("Texture Darkness (uOutlineDark)", Float) = 0.35
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex; float4 _MainTex_ST;
    float4 _BaseColor;
    float _OutlineWidth;
    float _OutlineZ;
    float4 _OutlineColor;
    float _OutlineTex;
    float _OutlineDark;

    struct appdata
    {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        float4 screenPos : TEXCOORD1;
    };

    v2f vert(appdata v)
    {
        v2f o;
        float4 clip = UnityObjectToClipPos(v.vertex);
        float3 vn = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
        float2 dir = mul((float2x2)UNITY_MATRIX_P, vn.xy);
        float dl = length(dir);
        dir = dl > 1e-5 ? dir / dl : float2(0.0, 0.0);
        float distScale = clamp(2.6 / clip.w, 0.75, 1.0);
        clip.xy += dir * _OutlineWidth * distScale * 2.0 / _ScreenParams.xy * clip.w;
        #if defined(UNITY_REVERSED_Z)
            clip.z -= _OutlineZ * 0.5 * clip.w;
        #else
            clip.z += _OutlineZ * clip.w;
        #endif
        o.pos = clip;
        o.screenPos = ComputeScreenPos(clip);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        return o;
    }

    float4 frag(v2f i) : SV_Target
    {
        float3 a = tex2D(_MainTex, i.uv).rgb * _BaseColor.rgb;
        float3 texCol = pow(max(a, 0.0), 1.4) * _OutlineDark;
        return float4(lerp(_OutlineColor.rgb, texCol, _OutlineTex), 1.0);
    }
    UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

    float EyeDepth(float depth)
    {
        if (unity_OrthoParams.w > 0.5)
        {
            #if defined(UNITY_REVERSED_Z)
                depth = 1.0 - depth;
            #endif
            return lerp(_ProjectionParams.y, _ProjectionParams.z, depth);
        }

        return LinearEyeDepth(depth);
    }

    float4 FragMask(v2f i) : SV_Target
    {
        float sceneDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos));
        float eyeDepth = EyeDepth(i.pos.z);

        const float DEPTH_EPSILON = 0.0001;
        clip(EyeDepth(sceneDepth) - eyeDepth + max(1.0, eyeDepth) * DEPTH_EPSILON);
        return float4(1, 1, 1, 1);
    }
    ENDCG

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+1" }

        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode" = "Always" }
            Cull Front

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }

        Pass
        {
            Name "ENVIRONMENT_MASK"
            Tags { "LightMode" = "EnvironmentMask" }
            Cull Front
            ZWrite Off
            ZTest Always
            ColorMask R

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment FragMask
            ENDCG
        }
    }
    Fallback Off
}
