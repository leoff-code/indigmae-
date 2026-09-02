Shader "CrystalSprint/Island Shore URP"
{
    Properties
    {
        _BaseMap("Existing meadow",2D)="white"{}
        _BaseColor("Meadow tint",Color)=(.72,.85,.58,1)
        _SandMap("Yughues sand albedo",2D)="white"{}
        _SandNormal("Yughues sand normal",2D)="bump"{}
        _SandSpec("Yughues sand specular",2D)="gray"{}
        _SandColor("Sand tint",Color)=(.85,.79,.65,1)
        _SandScale("Sand repeats per metre",Float)=.32
        _BumpScale("Sand relief",Float)=.38
    }
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_SandMap); SAMPLER(sampler_SandMap);
        TEXTURE2D(_SandNormal); SAMPLER(sampler_SandNormal);
        TEXTURE2D(_SandSpec); SAMPLER(sampler_SandSpec);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST; half4 _BaseColor; half4 _SandColor; float _SandScale; float _BumpScale;
        CBUFFER_END
        struct A { float4 p:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; };
        struct V { float4 p:SV_POSITION; float3 world:TEXCOORD0; float3 n:TEXCOORD1; float2 uv:TEXCOORD2; half fog:TEXCOORD3; };
        V vert(A a) { V o; o.world=TransformObjectToWorld(a.p.xyz); o.p=TransformWorldToHClip(o.world); o.n=TransformObjectToWorldNormal(a.n); o.uv=TRANSFORM_TEX(a.uv,_BaseMap); o.fog=ComputeFogFactor(o.p.z); return o; }
        half4 frag(V i):SV_Target
        {
            float angle=atan2(i.world.z,i.world.x);
            float shore=73+4*sin(angle*3+.7)+2.6*sin(angle*5-1.1)+1.3*sin(angle*9);
            float progress=saturate((length(i.world.xz)-55)/(shore-55));
            half3 grass=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
            float2 uv=i.world.xz*_SandScale;
            half3 sand=SAMPLE_TEXTURE2D(_SandMap,sampler_SandMap,uv).rgb*_SandColor.rgb;
            half variation=SAMPLE_TEXTURE2D(_SandMap,sampler_SandMap,uv*.13).r;
            half blend=smoothstep(.04,.44,progress+(variation-.5)*.10);
            half wet=1-smoothstep(-2.4,-1.75,i.world.y);
            sand*=lerp(1,.63,wet);
            half3 sn=UnpackNormalScale(SAMPLE_TEXTURE2D(_SandNormal,sampler_SandNormal,uv),_BumpScale);
            half3 n=normalize(i.n+half3(sn.x,0,sn.y)*blend);
            InputData data=(InputData)0;
            data.positionWS=i.world; data.normalWS=n; data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);
            data.shadowCoord=TransformWorldToShadowCoord(i.world); data.bakedGI=SampleSH(n);
            data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p); data.shadowMask=half4(1,1,1,1);
            SurfaceData s=(SurfaceData)0;
            s.albedo=lerp(grass,sand,blend); s.alpha=1; s.occlusion=1; s.normalTS=half3(0,0,1);
            half sandSpec=SAMPLE_TEXTURE2D(_SandSpec,sampler_SandSpec,uv).r;
            s.smoothness=lerp(.09,lerp(.12,.42+ sandSpec*.15,wet),blend); s.specular=.04;
            half4 col=UniversalFragmentPBR(data,s); col.rgb=MixFog(col.rgb,i.fog); return col;
        }
        half4 depth(V i):SV_Target{return 0;}
        half4 normals(V i):SV_Target{return half4(normalize(i.n),0);}
        ENDHLSL
        Pass
        {
            Name "ForwardLit" Tags{"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags{"LightMode"="DepthOnly"} ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment depth
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals" Tags{"LightMode"="DepthNormals"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment normals
            ENDHLSL
        }
    }
}
