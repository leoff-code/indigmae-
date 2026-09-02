Shader "CrystalSprint/Kit Interactive Grass"
{
    Properties
    {
        _BaseMap("Kit grass atlas", 2D) = "white" {}
        _BaseColor("Root tint", Color) = (.24,.37,.12,1)
        _TipColor("Tip tint", Color) = (.65,.75,.36,1)
        _Cutoff("Alpha cutoff", Range(0,1)) = .4
        _MeshHeight("Source mesh height", Float) = .8
        _WindStrength("Wind", Range(0,1)) = .065
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST, _BaseColor, _TipColor;
        float _Cutoff, _MeshHeight, _WindStrength;
        CBUFFER_END
        float4 _GrassInteractor;
        struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; float3 normalWS : TEXCOORD2; float height : TEXCOORD3; float fog : TEXCOORD4; };
        Varyings Vert(Attributes input)
        {
            UNITY_SETUP_INSTANCE_ID(input);
            Varyings o;
            float3 root = TransformObjectToWorld(float3(0,0,0));
            float3 world = TransformObjectToWorld(input.positionOS.xyz);
            float height = saturate(input.positionOS.y / max(.01, _MeshHeight));
            float tip = height * height;
            float breeze = sin(_Time.y * 1.65 + root.x * .7 + root.z * .41) + .35 * sin(_Time.y * 2.7 + root.z * 1.3);
            world.xz += float2(.8,.45) * breeze * _WindStrength * tip;
            float2 away = world.xz - _GrassInteractor.xz;
            float reach = saturate(1 - length(away) / max(.001, _GrassInteractor.w));
            reach *= step(.01, _GrassInteractor.w) * saturate(1 - abs(root.y - _GrassInteractor.y) * 3);
            world.xz += normalize(away + .0001) * reach * tip * .75;
            world.y -= reach * tip * .32;
            float distanceFade = 1 - smoothstep(56, 65, distance(root.xz, _WorldSpaceCameraPos.xz));
            world.y = lerp(root.y - .06, world.y, distanceFade);
            o.positionWS = world;
            o.positionCS = TransformWorldToHClip(world);
            o.normalWS = normalize(lerp(TransformObjectToWorldNormal(input.normalOS), float3(0,1,0), .65));
            o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
            o.height = height;
            o.fog = ComputeFogFactor(o.positionCS.z);
            return o;
        }
        half4 Frag(Varyings input) : SV_Target
        {
            half4 atlas = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
            clip(atlas.a - _Cutoff);
            Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
            half diffuse = saturate(dot(normalize(input.normalWS), sun.direction)) * .7 + .3;
            half3 illumination = SampleSH(half3(0,1,0)) + sun.color * diffuse * lerp(.2,1,sun.shadowAttenuation);
            half3 color = atlas.rgb * lerp(_BaseColor.rgb, _TipColor.rgb, input.height);
            return half4(MixFog(color * illumination, input.fog), 1);
        }
        half4 DepthFrag(Varyings input) : SV_Target { clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).a - _Cutoff); return 0; }
        half4 NormalFrag(Varyings input) : SV_Target { clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).a - _Cutoff); return half4(normalize(input.normalWS),0); }
        ENDHLSL
        Pass
        {
            Name "ForwardLit" Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags { "LightMode"="DepthOnly" } ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals" Tags { "LightMode"="DepthNormals" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment NormalFrag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
}
