Shader "NeonShooter/Exhaust"
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
            float time,boost;
            CBUFFER_END
            struct Attributes { float4 vertex:POSITION; float2 texcoord:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            v2f vert(Attributes v) { UNITY_SETUP_INSTANCE_ID(v); v2f o; o.pos=TransformObjectToHClip(v.vertex.xyz); o.uv=v.texcoord; return o; }
            float4 frag(v2f i):SV_Target
            {
                float x=(i.uv.x-.5)*2,y=i.uv.y,tail=1-y;
                float core=exp(-x*x/(.003+y*.028))*pow(y,1.8);
                float sheath=exp(-x*x/(.012+.12*sin(tail*3.14159)))*pow(y,.8)*.45;
                float wisps=0;
                for(int laneIndex=0;laneIndex<5;laneIndex++)
                {
                    float lane=laneIndex-2;
                    float bend=lane*(.018+tail*.14)+sin(tail*(18+laneIndex*3)-time*(5+laneIndex))*tail*.065;
                    float width=.009+y*.008;
                    wisps+=exp(-pow((x-bend)/width,2))*pow(y,.75)*.55;
                }
                float diamonds=pow(max(0,sin(tail*42-time*12)),10)*core*.35;
                float3 cool=lerp(float3(.28,.05,1),float3(.12,.9,1),pow(y,2));
                float3 hot=lerp(float3(1,.25,.015),float3(1,.95,.2),y);
                float3 color=lerp(cool,hot,boost);
                float3 rgb=color*(sheath+wisps*1.4+diamonds)+lerp(float3(.7,.95,1),float3(1,1,.65),boost)*core*2;
                float fade=smoothstep(0,.12,y)*(1-smoothstep(.88,1,abs(x)));
                return float4(rgb*(1.3+boost*.5),saturate(core*.9+sheath*.8+wisps*.55)*fade);
            }
            ENDHLSL
        }
    }
}
