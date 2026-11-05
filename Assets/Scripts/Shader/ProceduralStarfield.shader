Shader "Custom/ProceduralStarfield_URP"
{
    Properties
    {
        _CamPos ("Camera Position", Vector) = (0,0,0,0)
        _CamVelocity ("Camera Velocity", Vector) = (0,0,0,0)
        _Density ("Densidade das Estrelas", Range(10, 100)) = 50.0
        _ParallaxStrength ("Força do Movimento (Paralaxe)", Range(0.0, 5.0)) = 0.5
        _MinSize ("Tamanho Mínimo", Range(0.001, 0.1)) = 0.01
        _MaxSize ("Tamanho Máximo", Range(0.01, 0.5)) = 0.05
        _MaxBrightness ("Brilho Máximo", Range(0.1, 5.0)) = 1.0
        _TrailLength ("Tamanho do Rastro (Blur)", Range(0.0, 5.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Background" }
        LOD 100

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Biblioteca obrigatória do URP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // No URP, as variáveis precisam estar dentro de um CBUFFER para otimização de CPU (SRP Batcher)
            CBUFFER_START(UnityPerMaterial)
                float4 _CamPos;
                float4 _CamVelocity;
                float _Density;
                float _ParallaxStrength;
                float _MinSize;
                float _MaxSize;
                float _MaxBrightness;
                float _TrailLength;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                // Função matemática de conversão atualizada para URP
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 finalColor = float3(0.0, 0.0, 0.0);
                
                for(float j = 1.0; j <= 3.0; j++)
                {
                    float2 parallaxOffset = _CamPos.xy * (_ParallaxStrength / j);
                    float2 uv = (i.uv * _Density * j) + parallaxOffset;
                    
                    float2 id = floor(uv);
                    float2 gv = frac(uv) - 0.5;
                    
                    float rand = Hash21(id);
                    
                    if(rand > 0.98)
                    {
                        // rand so chega aqui acima de 0.98: reescala para 0..1, senao todas pulsam juntas e tem o mesmo tamanho.
                        float variacao = (rand - 0.98) / 0.02;
                        float brightness = (sin(_Time.y * 2.0 + variacao * 6.2832) * 0.5 + 0.5) * _MaxBrightness;
                        float baseSize = lerp(_MinSize, _MaxSize, variacao);
                        float starSize = baseSize / j;
                        
                        float2 trailVector = _CamVelocity.xy * _TrailLength * 0.01 / j;
                        float dotTrail = dot(trailVector, trailVector);
                        
                        float dist = 0.0;
                        
                        if(dotTrail < 0.0001)
                        {
                            dist = length(gv);
                        }
                        else
                        {
                            float h = clamp(dot(gv, trailVector) / dotTrail, 0.0, 1.0);
                            dist = length(gv - trailVector * h);
                            brightness *= lerp(1.0, 0.3, h); 
                        }
                        
                        float star = smoothstep(starSize, starSize * 0.1, dist);
                        finalColor += star * brightness * (1.0 / j);
                    }
                }
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}