Shader "Custom/VertexColorReveal"
{
    Properties
    {
        _RevealCenter ("Reveal Center", Vector) = (0, 0, 0, 0)
        _RevealRadius ("Reveal Radius", Float) = -1
        _RevealWidth  ("Reveal Width (transition zone)", Float) = 4
        _DropHeight   ("Drop Height", Float) = -8
        _BounceStrength ("Bounce Strength", Float) = 0.3
        _ColorDarkness ("Color Darkness", Float) = 0.15
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _RevealCenter;
                float  _RevealRadius;
                float  _RevealWidth;
                float  _DropHeight;
                float  _BounceStrength;
                float  _ColorDarkness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float4 color       : COLOR;
                float3 normalWS    : TEXCOORD0;
                float  revealT     : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                float dist = distance(worldPos.xz, _RevealCenter.xz);

                float t = saturate((_RevealRadius - dist) / max(_RevealWidth, 0.001));

                t = t * t * (3.0 - 2.0 * t);

                float bouncePhase = saturate(1.0 - abs(t - 0.75) * 4.0);
                float bounce = _BounceStrength * sin(bouncePhase * 3.14159) * (1.0 - t * 0.5);

                float yOffset = lerp(_DropHeight, 0.0, t) + bounce;

                worldPos.y += yOffset;

                output.positionCS = TransformWorldToHClip(worldPos);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.color      = input.color;
                output.revealT    = t;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                clip(input.revealT - 0.01);

                float3 normal = normalize(input.normalWS);

                Light mainLight = GetMainLight();
                float NdotL     = saturate(dot(normal, mainLight.direction));
                float3 diffuse  = mainLight.color * NdotL;

                float3 ambient = SampleSH(normal);

                float3 lighting = ambient + diffuse;

                float colorMul = lerp(_ColorDarkness, 1.0, input.revealT);

                return half4(input.color.rgb * lighting * colorMul, 1.0);
            }
            ENDHLSL
        }
    }
}

