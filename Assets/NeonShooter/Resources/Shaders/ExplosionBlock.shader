Shader "NeonShooter/ExplosionBlock"
{
    Properties { _Color("Color",Color)=(1,0.184,0.647,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END
            struct Attributes { float4 vertex:POSITION; float2 texcoord:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            v2f vert(Attributes v) { UNITY_SETUP_INSTANCE_ID(v); v2f o; o.pos=TransformObjectToHClip(v.vertex.xyz); o.uv=v.texcoord; return o; }
            float4 frag(v2f i):SV_Target { return _Color; }
            ENDHLSL
        }
    }
}
