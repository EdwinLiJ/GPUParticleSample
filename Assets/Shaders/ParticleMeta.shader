Shader "Hidden/GPUParticleSystem/ParticleMeta"
{
    Properties
    {
        _MainTex ("_", 2D) = ""{}
        _EmitterPos ("_", Vector) = (0, 0, 20, 0)
        _EmitterSize ("_", Vector) = (40, 40, 40, 0)
        _Direction ("_", Vector) = (0, 0, -1, 0.2)
        _NoiseParams ("_", Vector) = (0.2, 0.1, 1)
        _TargetPosition("_", Vector) = (0, 0, 5, 0)
    }

    HLSLINCLUDE
    // 固定移动方向
    #pragma multi_compile_local _ _FIXED_MOVE_DIRECTION
    // 移动到固定位置
    #pragma multi_compile_local _ _MOVE_TO_TARGET_POSITION
    // 从中心点开始爆炸
    #pragma multi_compile_local _ _EXPLOSION_FROM_CENTER
    // 围绕固定位置旋转
    #pragma multi_compile_local _ _MOVE_AROUND_TARGET_POSITION
    // 根据特定噪声图生成粒子初始位置
    #pragma multi_compile_local _ _START_POS_MAP

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    CBUFFER_START(UnityPerMaterial)
    float4 _NoiseParams;
    float4 _Direction;
    float4 _TargetPosition;

    float3 _EmitterPos;
    float3 _EmitterSize;

    float2 _MinMaxSpeed;
    half _RandomDirectionScale;
    half _RandomSeed;
    half _Throttle;
    half _LifeTime;
    half _CustomDeltaTime;
    half _RotateAngleRange;

    // default: 0.01
    half _StartPosMapYScale;
    // default: 0.1
    half _StartPosMapYThreshold;
    // defualt: 4
    half _StartPosMapBlockCount;
    
    CBUFFER_END

    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);

    // R通道做deltaTime的offset
    TEXTURE2D(_NoiseTex);
    SAMPLER(sampler_NoiseTex);

    TEXTURE2D(_StartPosMap);
    SAMPLER(sampler_StartPosMap);


    struct Attributes
    {
        float4 vertex : POSITION;
        half2 uv : TEXCOORD0;
    };

    struct Varyings
    {
        float4 pos : SV_POSITION;
        half2 uv : TEXCOORD0;
    };

    float2 Rotate(half2 position, half2 pivot, half angleRange)
    {
        // 用于计算轨迹的固定参数，越低旋转越顺畅
        // Use to calculate a rotation track, a lower angle leads to a smoother moving path
        float angle = angleRange;
        float cosAngle = cos(angle);
        float sinAngle = sin(angle);
        float2x2 rot = half2x2(cosAngle, -sinAngle, sinAngle, cosAngle);
        position -= pivot;
        float2 output = mul(rot, position);
        output += pivot;
        return output;
    }


    float GetRandom(float2 uv, float salt)
    {
        uv += float2(salt, _RandomSeed);
        return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
    }

    float4 new_particle(float2 uv)
    {
        float t = _Time.x;

        #ifdef _START_POS_MAP
            half dividedStartPosMapBlockCount = 1.0 / _StartPosMapBlockCount;
            float2 newUV = float2(uv.x % (dividedStartPosMapBlockCount) * _StartPosMapBlockCount, uv.y % dividedStartPosMapBlockCount * _StartPosMapBlockCount);
            // float2 newUV = uv;
            half4 mapColor = SAMPLE_TEXTURE2D(_StartPosMap, sampler_StartPosMap, newUV);
            float index = floor(uv.y * _StartPosMapBlockCount) * _StartPosMapBlockCount + floor(uv.x * _StartPosMapBlockCount);
            float y = index * _StartPosMapYScale;
            // Original Code 原始代码
            // To avoid shader conditional, use 【step】 instead of 【if】
            // 为了避免可能产生的分支（实际上因为设备的差异可能并不会），使用step替代if
            // float x = 1e8;
            // float z = 1e8;
            // if (mapColor.r > y + _StartPosMapYThreshold)
            // {
            //     x = newUV.x;
            //     z = newUV.y;
            // }
            float x = newUV.x + step(mapColor.r, y + _StartPosMapYThreshold) * 1e8;
            float z = newUV.y + step(mapColor.r, y + _StartPosMapYThreshold) * 1e8;
        
        #else
            // 计算出生位置
            // TODO：
            // 实际上我们并不是真的需要随机值，考虑到随机值计算其实并不划算，采样一个低分辨率的噪声图相比三次随机值计算可能会更好
            // Actually we dont really need random numbers always
            // On mobile devices it's better to sample a low resolution noise texture rather than calculate random numbers
            float x = GetRandom(uv, t + 1);
            float y = GetRandom(uv, t + 2);
            float z = GetRandom(uv, t + 3);
        #endif
        
        
        float3 p = float3(x, y, z);
        p -= 0.5;
        p = p * _EmitterSize + _EmitterPos;
        
        // 生命周期
        float l = _LifeTime;
        l = l * 0.5 + GetRandom(uv, t + 4) * _LifeTime * 0.5;

        // 裁剪不需要看到的粒子
        // Clip particles by moving to a faraway place
        half throttleFactor = step(_Throttle, uv.x);
        float4 positionOffset = float4(1e8, 1e8, 1e8, -1e8) * throttleFactor;

        return float4(p, l) + positionOffset;
    }

    float3 GetFixedDirectionVelocity(float2 uv)
    {
        float3 velocity = float3(GetRandom(uv, 4), GetRandom(uv, 5), GetRandom(uv, 6));
        velocity = saturate(velocity - (float3)0.5 * 2);

        velocity = lerp(_Direction.xyz, velocity, _RandomDirectionScale);
        velocity = normalize(velocity) * lerp(_MinMaxSpeed.x, _MinMaxSpeed.y, GetRandom(uv, 7));

        return velocity;
    }

    float3 GetTargetPositionVelocity(float3 position, float3 targetPosition, float2 uv)
    {
        float3 velocity = float3(GetRandom(uv, 4), GetRandom(uv, 5), GetRandom(uv, 6));
        velocity = (velocity - (float3)0.5) * 2;

        float3 direction = normalize(targetPosition - position);
        velocity = lerp(direction, velocity, _RandomDirectionScale);
        velocity = normalize(velocity) * lerp(_MinMaxSpeed.x, _MinMaxSpeed.y, GetRandom(uv, 7));
        return velocity;
    }

    float3 GetCenterExplosionVelocity(float3 position, float3 centerPosition, float2 uv)
    {
        float3 velocity = float3(GetRandom(uv, 4), GetRandom(uv, 5), GetRandom(uv, 6));
        velocity = (velocity - (float3)0.5) * 2;

        float3 direction = normalize(position - centerPosition);
        velocity = lerp(direction, velocity, _RandomDirectionScale);
        velocity = normalize(velocity) * lerp(_MinMaxSpeed.x, _MinMaxSpeed.y, GetRandom(uv, 7));
        return velocity;
    }

    float3 GetTargetAroundVelocity(float3 position, float3 centerAxis, float2 uv)
    {
        float3 velocity = float3(GetRandom(uv, 4), GetRandom(uv, 5), GetRandom(uv, 6));
        velocity = (velocity - (float3)0.5) * 2;

        float3 targetPos = position;
        targetPos.xz = Rotate(targetPos.xz, centerAxis.xz, _RotateAngleRange);
        float3 direction = normalize(position - targetPos);
        velocity = lerp(direction, velocity, _RandomDirectionScale);
        velocity = normalize(velocity) * lerp(_MinMaxSpeed.x, _MinMaxSpeed.y, GetRandom(uv, 7));
        return velocity;
    }

    // 推力计算
    float3 GetParticleVelocity(float3 position, float2 uv)
    {
        float3 velocity = 0;
        #ifdef _FIXED_MOVE_DIRECTION
            velocity = GetFixedDirectionVelocity(uv);
        #elif defined(_MOVE_TO_TARGET_POSITION)
            velocity = GetTargetPositionVelocity(position, _TargetPosition, uv);
        #elif defined(_EXPLOSION_FROM_CENTER)
            velocity = GetCenterExplosionVelocity(position, _EmitterPos, uv);
        #elif defined(_MOVE_AROUND_TARGET_POSITION)
            velocity = GetTargetAroundVelocity(position, _EmitterPos, uv);
        #endif

        return velocity;
    }


    Varyings vert(Attributes v)
    {
        Varyings o;
        o.pos = TransformObjectToHClip(v.vertex);
        o.uv = v.uv;
        return o;
    }

    // 初始化的时候创建新的粒子
    float4 frag_init(Varyings i) : SV_Target
    {
        return new_particle(i.uv);
    }

    // 每帧tick计算粒子移动轨迹
    float4 frag_update(Varyings i) : SV_Target
    {
        float4 p = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
        if (p.w > 0)
        {
            // half dtScale = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.uv);
            // float dt = _CustomDeltaTime * dtScale;
            float dt = _CustomDeltaTime;
            p.xyz += GetParticleVelocity(p.xyz, i.uv) * dt;
            // p.xyz += 0.1 * dt;
            p.w -= dt; // life
            return p;
        }
        return new_particle(i.uv);
    }
    ENDHLSL

    SubShader
    {
        // Pass 0: Initialization
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_init
            ENDHLSL
        }
        // Pass 1: Update
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_update
            ENDHLSL
        }
    }
}