Shader "CrystalSprint/PondWater"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.08, 0.55, 0.48, 0.72)
        _DeepColor ("Deep Color", Color) = (0.025, 0.19, 0.3, 0.86)
        _WaveStrength ("Wave Strength", Range(0, 0.12)) = 0.045
        _WaveSpeed ("Wave Speed", Range(0, 3)) = 1.15
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.72
        _RippleScale ("Ripple Scale", Range(0.2, 8)) = 2.8
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard alpha:fade vertex:vert
        #pragma target 3.0
        fixed4 _ShallowColor;
        fixed4 _DeepColor;
        float _WaveStrength;
        float _WaveSpeed;
        float _ReflectionStrength;
        float _RippleScale;

        struct Input
        {
            float3 worldPos;
            float3 viewDir;
        };

        void vert(inout appdata_full vertex)
        {
            float3 world = mul(unity_ObjectToWorld, vertex.vertex).xyz;
            float wave = sin(world.x * 1.7 + _Time.y * _WaveSpeed) + cos(world.z * 2.1 - _Time.y * _WaveSpeed * 0.8);
            vertex.vertex.y += wave * _WaveStrength * 0.5;
        }

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            float fresnel = pow(1.0 - saturate(dot(normalize(input.viewDir), float3(0, 1, 0))), 2.2);
            float phaseA = input.worldPos.x * _RippleScale + input.worldPos.z * 1.7 + _Time.y * _WaveSpeed;
            float phaseB = input.worldPos.z * (_RippleScale * 1.23) - input.worldPos.x * 1.15 - _Time.y * _WaveSpeed * 0.82;
            float ripple = (sin(phaseA) + cos(phaseB)) * 0.024;
            fixed4 color = lerp(_ShallowColor, _DeepColor, saturate(fresnel * 0.72 + ripple + 0.16));
            output.Albedo = color.rgb;
            output.Normal = normalize(float3(cos(phaseA) * 0.13, sin(phaseB) * 0.11, 1.0));
            output.Emission = lerp(color.rgb * 0.025, fixed3(0.18, 0.32, 0.38), fresnel * _ReflectionStrength * 0.34);
            output.Metallic = 0.14;
            output.Smoothness = 0.97;
            output.Alpha = color.a;
        }
        ENDCG
    }
    Fallback "Transparent/Diffuse"
}
