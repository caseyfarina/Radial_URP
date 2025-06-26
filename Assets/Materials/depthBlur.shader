Shader "Custom/DepthBlur"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _BlurAmount("Blur Amount", Range(0, 20)) = 5
        _BlurDistance("Blur Distance", Range(0, 50)) = 10
        _FocusDistance("Focus Distance", Range(0, 100)) = 10
        _Transparency("Transparency", Range(0, 1)) = 0.5
        [KeywordEnum(Low, Medium, High)] _Quality("Blur Quality", Float) = 1
    }

        SubShader
        {
            Tags
            {
                "RenderType" = "Transparent"
                "Queue" = "Transparent"
                "RenderPipeline" = "UniversalPipeline"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            Pass
            {
                Name "DepthBlurPass"

                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma shader_feature_local _QUALITY_LOW _QUALITY_MEDIUM _QUALITY_HIGH

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct Varyings
                {
                    float4 positionHCS : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    float4 screenPos : TEXCOORD1;
                };

                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                TEXTURE2D(_CameraOpaqueTexture);
                SAMPLER(sampler_CameraOpaqueTexture);

                CBUFFER_START(UnityPerMaterial)
                    float4 _MainTex_ST;
                    float _BlurAmount;
                    float _BlurDistance;
                    float _FocusDistance;
                    float _Transparency;
                CBUFFER_END

                Varyings vert(Attributes input)
                {
                    Varyings output;
                    output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                    output.screenPos = ComputeScreenPos(output.positionHCS);
                    return output;
                }

                // Low quality blur with 8 samples
                half4 BlurLow(float2 uv, float radius)
                {
                    static const int SAMPLE_COUNT = 8;
                    static const float2 offsets[SAMPLE_COUNT] = {
                        float2(1, 0), float2(0.7071, 0.7071),
                        float2(0, 1), float2(-0.7071, 0.7071),
                        float2(-1, 0), float2(-0.7071, -0.7071),
                        float2(0, -1), float2(0.7071, -0.7071)
                    };

                    half4 blurredColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);

                    for (int i = 0; i < SAMPLE_COUNT; i++)
                    {
                        float2 sampleUV = uv + offsets[i] * radius;
                        blurredColor += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, sampleUV);
                    }

                    return blurredColor / (SAMPLE_COUNT + 1);
                }

                // Medium quality blur with 12 samples
                half4 BlurMedium(float2 uv, float radius)
                {
                    static const int SAMPLE_COUNT = 12;
                    static const float2 offsets[SAMPLE_COUNT] = {
                        float2(1, 0), float2(0.866, 0.5), float2(0.5, 0.866),
                        float2(0, 1), float2(-0.5, 0.866), float2(-0.866, 0.5),
                        float2(-1, 0), float2(-0.866, -0.5), float2(-0.5, -0.866),
                        float2(0, -1), float2(0.5, -0.866), float2(0.866, -0.5)
                    };

                    half4 blurredColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);

                    for (int i = 0; i < SAMPLE_COUNT; i++)
                    {
                        float2 sampleUV = uv + offsets[i] * radius;
                        blurredColor += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, sampleUV);
                    }

                    return blurredColor / (SAMPLE_COUNT + 1);
                }

                // High quality blur with 16 samples
                half4 BlurHigh(float2 uv, float radius)
                {
                    static const int SAMPLE_COUNT = 16;
                    static const float2 offsets[SAMPLE_COUNT] = {
                        float2(1, 0), float2(0.9239, 0.3827), float2(0.7071, 0.7071), float2(0.3827, 0.9239),
                        float2(0, 1), float2(-0.3827, 0.9239), float2(-0.7071, 0.7071), float2(-0.9239, 0.3827),
                        float2(-1, 0), float2(-0.9239, -0.3827), float2(-0.7071, -0.7071), float2(-0.3827, -0.9239),
                        float2(0, -1), float2(0.3827, -0.9239), float2(0.7071, -0.7071), float2(0.9239, -0.3827)
                    };

                    half4 blurredColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);

                    for (int i = 0; i < SAMPLE_COUNT; i++)
                    {
                        float2 sampleUV = uv + offsets[i] * radius;
                        blurredColor += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, sampleUV);
                    }

                    return blurredColor / (SAMPLE_COUNT + 1);
                }

                half4 frag(Varyings input) : SV_Target
                {
                    // Calculate screen UV coordinates
                    float2 screenUV = input.screenPos.xy / input.screenPos.w;

                    // Sample the base texture
                    half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                    // Sample the opaque scene texture (what's behind our object)
                    half4 sceneColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV);

                    // Sample the depth texture
                    float rawDepth = SampleSceneDepth(screenUV);
                    float sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                    // Calculate depth difference for blur effect
                    float depthDifference = sceneEyeDepth - _FocusDistance;

                    // Only blur objects beyond the focus distance
                    if (depthDifference > 0)
                    {
                        // Calculate blur amount based on depth
                        float blurFactor = saturate(depthDifference / _BlurDistance);
                        float blurRadius = blurFactor * _BlurAmount * 0.01;

                        // Don't blur if radius is too small
                        if (blurRadius > 0.001)
                        {
                            half4 blurredColor;

                            // Apply blur based on quality setting
                            #if defined(_QUALITY_LOW)
                                blurredColor = BlurLow(screenUV, blurRadius);
                            #elif defined(_QUALITY_HIGH)
                                blurredColor = BlurHigh(screenUV, blurRadius);
                            #else
                                blurredColor = BlurMedium(screenUV, blurRadius);
                            #endif

                                // Combine blurred and original scene colors based on blur factor
                                sceneColor = lerp(sceneColor, blurredColor, blurFactor);
                            }
                        }

                    // Blend base texture with scene
                    half4 finalColor = lerp(sceneColor, baseColor, baseColor.a * _Transparency);
                    finalColor.a = _Transparency;

                    return finalColor;
                }
                ENDHLSL
            }
        }

            // Fallback for older versions
                    FallBack "Universal Render Pipeline/Unlit"
}