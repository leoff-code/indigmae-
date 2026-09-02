Shader "CrystalSprint/InteractiveFoliage"
{
    Properties
    {
        _MainTex ("Foliage Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.08, 0.3, 0.045, 1)
        _TipColor ("Tip Color", Color) = (0.25, 0.55, 0.1, 1)
        _BendStrength ("Bend Strength", Range(0, 1)) = 0.42
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _BaseColor;
        fixed4 _TipColor;
        float _BendStrength;
        float4 _FoliageInteractor;

        struct Input
        {
            float2 uv_MainTex;
            float layerHeight;
        };

        void vert(inout appdata_full vertex, out Input output)
        {
            UNITY_INITIALIZE_OUTPUT(Input, output);
            float influence = saturate(vertex.vertex.y * 0.85 + 0.15);
            float3 world = mul(unity_ObjectToWorld, vertex.vertex).xyz;
            float3 difference = world - _FoliageInteractor.xyz;
            float distanceToPlayer = length(difference);
            float interaction = saturate(1.0 - distanceToPlayer / max(_FoliageInteractor.w, 0.01)) * influence;
            if (distanceToPlayer > 0.001)
            {
                world.xz += normalize(difference.xz) * interaction * _BendStrength;
                world.y -= interaction * 0.12;
            }
            vertex.vertex = mul(unity_WorldToObject, float4(world, 1.0));
            output.layerHeight = influence;
        }

        void surf(Input input, inout SurfaceOutput output)
        {
            fixed3 textureColor = tex2D(_MainTex, input.uv_MainTex).rgb;
            fixed3 tint = lerp(_BaseColor.rgb, _TipColor.rgb, input.layerHeight);
            output.Albedo = textureColor * tint * 1.7;
            output.Emission = output.Albedo * 0.05;
            output.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
