Shader "Custom/Point Surface GPU"
{
    Properties
    {
        _Smoothness("Smoothness", Range(0,1)) = 0.5
    }

    SubShader
    {
        //Tags { "RenderType"="Opaque" }
        //LOD 200

        CGPROGRAM
        #pragma surface ConfigureSurface Lambert noshadow//Standard fullforwardshadows addshadow
        #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
        #pragma target 4.5

        float _Smoothness;

        struct Input {
            float3 worldPos;
        };

        #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
        StructuredBuffer<float3> _Positions;
        StructuredBuffer<float4> _Colors;
        #endif

        void ConfigureProcedural() {
            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
            float3 position = _Positions[unity_InstanceID];

            unity_ObjectToWorld = 0.0;
            unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 1.0);
            unity_ObjectToWorld._m00_m11_m22 = 1.0;
            #endif
        }

        void ConfigureSurface(Input input, inout SurfaceOutput surface) {
            //clip(frac((input.worldPos.y + input.worldPos.z * 0.1) * 5) - 0.5);
            //float4 color = float4(0.79, 0.48, 0.43, 1.0);
            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
            
            //float4 color = float4(col.x,col.y,col.z, 1.0);
            float4 color = _Colors[unity_InstanceID];
            surface.Albedo =color;//saturate(input.worldPos * 0.5 + 0.5);
            #endif
            float h = input.worldPos.y;
            /*
            if (h < 0.1) color = float4(0.77, 0.90, 0.98, 1.0);
            else if (h < 0.2) color = float4(0.82, 0.92, 0.99, 1.0);
            else if (h < 0.3)color = float4(0.91, 0.97, 0.99, 1.0);
            else if (h < 0.45)color = float4(0.62, 0.75, 0.59, 1.0);
            else if (h < 0.55) color = float4(0.86, 0.90, 0.68, 1.0);
            else if (h < 0.65) color = float4(0.99, 0.99, 0.63, 1.0);
            else if (h < 0.75)color = float4(0.99, 0.83, 0.59, 1.0);
            else if (h < 0.90)color = float4(0.98, 0.71, 0.49, 1.0);
            else if (h < 0.95) color = float4(0.98, 0.57, 0.47, 1.0);
            */

            //surface.Smoothness = _Smoothness;
        }


        ENDCG
    }
    FallBack "Diffuse"
}
