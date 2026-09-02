Shader "CrystalSprint/TerrainBlend"
{
    Properties
    {
        _GrassTex ("Grass", 2D) = "white" {}
        _RockTex ("Rock", 2D) = "white" {}
        _GrassColor ("Grass Tint", Color) = (0.22, 0.5, 0.12, 1)
        _RockColor ("Rock Tint", Color) = (0.5, 0.5, 0.46, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_GrassTex); SAMPLER(sampler_GrassTex);
        TEXTURE2D(_RockTex); SAMPLER(sampler_RockTex);
        CBUFFER_START(UnityPerMaterial)
        half4 _GrassColor;
        half4 _RockColor;
        CBUFFER_END
        struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; };
        struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; half blend : TEXCOORD3; half fog : TEXCOORD4; };
        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.uv = input.uv;
            output.blend = input.color.r;
            output.fog = ComputeFogFactor(output.positionCS.z);
            return output;
        }
        half4 Frag(Varyings input) : SV_Target
        {
            half3 grass = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, input.uv * 1.5).rgb * _GrassColor.rgb * 1.55;
            half3 rock = SAMPLE_TEXTURE2D(_RockTex, sampler_RockTex, input.uv * 0.8).rgb * _RockColor.rgb * 1.35;
            half3 albedo = lerp(grass, rock, smoothstep(0.08, 0.92, input.blend));
            half3 normal = normalize(input.normalWS);
            Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
            half3 lighting = SampleSH(normal) + sun.color * saturate(dot(normal, sun.direction)) * sun.shadowAttenuation;
            return half4(MixFog(albedo * lighting, input.fog), 1);
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
