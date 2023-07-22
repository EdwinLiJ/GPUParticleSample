Shader "Hidden/GPUParticleSystem/ParticleDisplay"
{
    Properties
    {
        _MainTex("-", 2D) = ""{}
        [HDR] _Color ("-", Color) = (1, 1, 1, 1)
        _Tail ("-", Float) = 1
    }

    HLSLINCLUDE
    #pragma multi_compile_local _ _USE_MESH_LINE
    #pragma multi_compile_local _ _USE_MESH_LINE_STRIP

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    #define BEGIN_TIME_NODE 0.3
    #define END_TIME_NODE 0.6

    struct appdata
    {
        float4 positionOS : POSITION;
        float2 uv : TEXCOORD0;
        float2 uv2 : TEXCOORD01;
    };

    struct v2f
    {
        float4 positionCS : SV_POSITION;
        half4 color : COLOR;
        float2 uv2 : TEXCOORD01;
    };

    // float4 GetBillboardPositionCS(float3 positionOS, float3 center)
    // {
    //     float3 cameraPosWS = GetCameraPositionWS();
    //     float3 cameraPosOS = TransformWorldToObject(cameraPosWS);
    //     // float3 center = float3(0, 0, 0);
    //
    //     float3 normalDir = cameraPosOS - center;
    //     normalDir = normalize(normalDir);
    //     float3 upDir = abs(normalDir.y) > 0.999 ? float3(0, 0, 1) : float3(0, 1, 0);
    //     float3 rightDir = normalize(cross(upDir, normalDir));
    //     upDir = normalize(cross(normalDir, rightDir));
    //     float3 centerOffs = positionOS - center;
    //     float3 localPos = center + rightDir * centerOffs.x + upDir * centerOffs.y + normalDir * centerOffs.z;
    //     float4 positionCS = TransformObjectToHClip(localPos);
    //     return positionCS;
    // }
    //
    // float2 Rotate(half2 position, half2 pivot, half angleRange)
    // {
    //     // 用于计算轨迹的固定参数，越低旋转越顺畅
    //     float angle = angleRange;
    //     float cosAngle = cos(angle);
    //     float sinAngle = sin(angle);
    //     float2x2 rot = float2x2(cosAngle, -sinAngle, sinAngle, cosAngle);
    //     position -= pivot;
    //     float2 output = mul(rot, position);
    //     output += pivot;
    //     return output;
    // }

    CBUFFER_START(UnityPerMaterial)
    float4 _ParticleTex_TexelSize;
    float4 _TargetPosition;
    half4 _Color;
    //自定义线段中，头部亮点的颜色
    half4 _LineStripHeadColor;
    //自定义线段中，尾部的颜色
    half4 _LineStripEndColor;
    half _LineStripHeadColorInt;
    half _Tail;
    half _ParticleDuration;
    //自定义线段中，线段的头部占比
    half _LineStripHeadPosition;
    CBUFFER_END

    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);

    TEXTURE2D(_ParticleTex);
    SAMPLER(sampler_ParticleTex);

    v2f vert(appdata IN)
    {
        v2f OUT;

        float2 uv = IN.uv.xy + _ParticleTex_TexelSize.xy * 0.5;

        float4 p = SAMPLE_TEXTURE2D_LOD(_ParticleTex, sampler_ParticleTex, uv, 0);
        OUT.positionCS = TransformObjectToHClip(p.xyz);
        
        OUT.uv2 = IN.uv2;
        OUT.color = _Color;

        return OUT;
    }

    half4 frag(v2f IN) : SV_Target
    {
        half4 Final = IN.color;
        return Final;
    }
    ENDHLSL

    SubShader
    {

        Pass
        {
            Tags
            {
                "RenderType"="Transparent"
                "Queue"="Transparent"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}