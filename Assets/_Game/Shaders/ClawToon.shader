Shader "Claw3D/Toon"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color", Color) = (0.32,0.28,0.38,1)
        _RimColor ("Rim Color", Color) = (1,0.86,0.95,1)
        _RimPower ("Rim Power", Range(1,8)) = 3.5
        _Steps ("Light Steps", Range(2,5)) = 3
        _OutlineColor ("Outline Color", Color) = (0.055,0.045,0.075,1)
        _OutlineWidth ("Outline Width", Range(0,0.015)) = 0.0025
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadowColor;
                half4 _RimColor;
                half4 _OutlineColor;
                float _RimPower;
                float _Steps;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings OutlineVert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                positionWS += normalWS * _OutlineWidth;
                output.positionHCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 OutlineFrag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex ToonVert
            #pragma fragment ToonFrag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadowColor;
                half4 _RimColor;
                half4 _OutlineColor;
                float _RimPower;
                float _Steps;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings ToonVert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 ToonFrag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                float ndotl = saturate(dot(normalWS, mainLight.direction));
                float steps = max(2.0, _Steps);
                float band = saturate(round(ndotl * (steps - 1.0)) / (steps - 1.0));
                float attenuation = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                float litAmount = saturate(band * attenuation);

                half3 toon = lerp(_ShadowColor.rgb, _BaseColor.rgb, litAmount);
                toon *= (0.22h + mainLight.color * 0.88h);

                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                half3 rim = _RimColor.rgb * pow(fresnel, _RimPower) * 0.32h;

                return half4(toon + rim, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
