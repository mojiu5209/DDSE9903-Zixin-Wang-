Shader "Skybox/DayNightPanoramicBlend"
{
    Properties
    {
        _DayTex ("Day HDR", 2D) = "gray" {}
        _NightTex ("Night HDR", 2D) = "gray" {}
        _Blend ("Day Blend", Range(0, 1)) = 1
        _Exposure ("Exposure", Range(0, 3)) = 1
        _Rotation ("Rotation", Range(0, 360)) = 0
        _FlipY ("Flip Vertical", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_DayTex);
            SAMPLER(sampler_DayTex);

            TEXTURE2D(_NightTex);
            SAMPLER(sampler_NightTex);

            float _Blend;
            float _Exposure;
            float _Rotation;
            float _FlipY;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.direction = input.positionOS.xyz;

                return output;
            }

            float2 DirectionToPanoramaUV(float3 direction)
            {
                direction = normalize(direction);

                float2 uv;

                uv.x =
                    atan2(direction.z, direction.x) *
                    0.159154943 +
                    0.5;

                uv.x = frac(uv.x + _Rotation / 360.0);

                uv.y =
                    asin(clamp(direction.y, -1.0, 1.0)) *
                    0.318309886 +
                    0.5;

                if (_FlipY > 0.5)
                {
                    uv.y = 1.0 - uv.y;
                }

                return uv;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv =
                    DirectionToPanoramaUV(input.direction);

                half4 nightColor =
                    SAMPLE_TEXTURE2D(
                        _NightTex,
                        sampler_NightTex,
                        uv
                    );

                half4 dayColor =
                    SAMPLE_TEXTURE2D(
                        _DayTex,
                        sampler_DayTex,
                        uv
                    );

                half4 finalColor =
                    lerp(
                        nightColor,
                        dayColor,
                        saturate(_Blend)
                    );

                finalColor.rgb *= _Exposure;
                finalColor.a = 1.0;

                return finalColor;
            }

            ENDHLSL
        }
    }

    FallBack Off
}