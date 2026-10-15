Shader "NeonShooter/Voxel"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float3 normal:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; };
            v2f vert(appdata v) { UNITY_SETUP_INSTANCE_ID(v); v2f o; o.vertex=TransformObjectToHClip(v.vertex.xyz); o.color=v.color; return o; }
            float4 frag(v2f i):SV_Target { return i.color; }
            ENDHLSL
        }
    }
}
