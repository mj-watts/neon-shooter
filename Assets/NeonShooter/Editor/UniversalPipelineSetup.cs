using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using NeonShooter.Unity;

namespace NeonShooter.Editor
{
    public static class UniversalPipelineSetup
    {
        const string Settings="Assets/NeonShooter/Settings";
        const string PipelinePath=Settings+"/NeonUniversalPipeline.asset";
        const string RendererPath=Settings+"/NeonUniversalRenderer.asset";
        const string ProfilePath="Assets/NeonShooter/Resources/FlightPostProcessing.asset";
        [InitializeOnLoadMethod]
        static void Initialize() { EditorApplication.delayCall+=Ensure; }
        [MenuItem("Neon Shooter/Configure Universal Render Pipeline")]
        public static void Ensure()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(PlayerSettings.colorSpace!=ColorSpace.Linear) PlayerSettings.colorSpace=ColorSpace.Linear;
            Directory.CreateDirectory(Settings);
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if(renderer==null)
            {
                renderer=ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.renderingMode=RenderingMode.Forward;
                AssetDatabase.CreateAsset(renderer,RendererPath);
            }
            if(renderer.postProcessData==null)
            {
                renderer.postProcessData=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                EditorUtility.SetDirty(renderer);
            }
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if(pipeline==null)
            {
                pipeline=UniversalRenderPipelineAsset.Create(renderer); pipeline.name="Neon Universal Pipeline";
                pipeline.supportsHDR=true; pipeline.msaaSampleCount=1;
                pipeline.supportsCameraDepthTexture=false; pipeline.supportsCameraOpaqueTexture=false;
                pipeline.useSRPBatcher=true; AssetDatabase.CreateAsset(pipeline,PipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline=pipeline;
            int quality=QualitySettings.GetQualityLevel();
            for(int i=0;i<QualitySettings.names.Length;i++) { QualitySettings.SetQualityLevel(i,false); QualitySettings.renderPipeline=pipeline; }
            QualitySettings.SetQualityLevel(quality,false);
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if(profile==null)
            {
                profile=ScriptableObject.CreateInstance<VolumeProfile>(); profile.name="Flight Post Processing"; AssetDatabase.CreateAsset(profile,ProfilePath);
                var bloom=profile.Add<Bloom>(true); bloom.intensity.Override(NeonBloom.StandardIntensity); bloom.threshold.Override(NeonBloom.Threshold); bloom.scatter.Override(NeonBloom.Scatter); bloom.highQualityFiltering.Override(true); bloom.maxIterations.Override(8);
                var tone=profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
                foreach(var component in profile.components) AssetDatabase.AddObjectToAsset(component,profile);
                EditorUtility.SetDirty(profile);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("NEON_URP_CONFIGURED: Universal pipeline assigned to Graphics and all quality levels.");
        }
    }
}
