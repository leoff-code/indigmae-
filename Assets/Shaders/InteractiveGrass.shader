Shader "CrystalSprint/InteractiveGrass"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.08, 0.28, 0.035, 1)
        _TipColor ("Tip Color", Color) = (0.35, 0.7, 0.12, 1)
        _WindStrength ("Wind Strength", Range(0, 0.3)) = 0.08
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+5" }
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
        half4 _BaseColor;
        half4 _TipColor;
        float _WindStrength;
        CBUFFER_END
        float4 _GrassInteractor;
        float3 _LightDirection;
        float3 _LightPosition;
        struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; };
        struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half tip : TEXCOORD1; half fog : TEXCOORD2; half3 normalWS : TEXCOORD3; };
        Varyings Vert(Attributes input)
        {
            Varyings output;
            float tip = input.color.r;
            float3 world = TransformObjectToWorld(input.positionOS.xyz);
            world.x += sin(_Time.y * 1.8 + world.x * 0.32 + world.z * 0.27) * _WindStrength * tip;
            float2 away = world.xz - _GrassInteractor.xz;
            float distanceToPlayer = length(away);
            float interaction = saturate(1.0 - distanceToPlayer / max(_GrassInteractor.w, 0.01)) * tip;
            world.xz += away / max(distanceToPlayer, 0.001) * interaction * 0.58;
            world.y -= interaction * 0.14;
            output.positionWS = world;
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.positionCS = TransformWorldToHClip(world);
            output.tip = tip;
            output.fog = ComputeFogFactor(output.positionCS.z);
            return output;
        }
        Varyings ShadowVert(Attributes input)
        {
            Varyings output = Vert(input);
            float3 lightDirection = _LightDirection;
            #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                lightDirection = normalize(_LightPosition - output.positionWS);
            #endif
            output.positionCS = TransformWorldToHClip(ApplyShadowBias(output.positionWS, output.normalWS, lightDirection));
            #if UNITY_REVERSED_Z
                output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
            return output;
        }
        half4 Frag(Varyings input) : SV_Target
        {
            half3 albedo = lerp(_BaseColor.rgb, _TipColor.rgb, input.tip);
            Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
            half3 normal = normalize(input.normalWS);
            half3 lighting = SampleSH(normal) + sun.color * saturate(dot(normal, sun.direction)) * sun.shadowAttenuation;
            return half4(MixFog(albedo * (lighting + 0.14), input.fog), 1);
        }
        half4 Depth(Varyings input) : SV_Target { return 0; }
        half4 Normals(Varyings input) : SV_Target { return half4(normalize(input.normalWS), 0); }
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment Depth
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Depth
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Normals
            ENDHLSL
        }
    }
}
