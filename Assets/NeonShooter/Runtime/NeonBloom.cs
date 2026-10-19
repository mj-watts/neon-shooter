using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace NeonShooter.Unity
{
    /// <summary>URP's native volume effects, executed by its Render Graph.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class NeonBloom : MonoBehaviour
    {
        public const float StandardIntensity=1.5f, Threshold=.9f, Scatter=.45f;
        public float MusicPulse;
        public float BlastPulse;
        Volume volume;
        VolumeProfile profile;
        Bloom bloom;
        void OnEnable()
        {
            var cameraData=GetComponent<Camera>().GetUniversalAdditionalCameraData();
            GetComponent<Camera>().allowHDR=true;
            cameraData.renderPostProcessing=true;
            GetComponent<Camera>().SetVolumeFrameworkUpdateMode(VolumeFrameworkUpdateMode.EveryFrame);
            cameraData.volumeLayerMask= cameraData.volumeLayerMask.value | 1;
            var effects=new GameObject("Flight Post Processing"); effects.transform.SetParent(transform);
            volume=effects.AddComponent<Volume>(); volume.isGlobal=true; volume.priority=10;
            volume.sharedProfile=Resources.Load<VolumeProfile>("FlightPostProcessing");
            profile=volume.profile;
            if(!profile.TryGet(out bloom)) bloom=profile.Add<Bloom>(true);
            bloom.threshold.Override(Threshold); bloom.scatter.Override(Scatter);
            // Keep the bloom pyramid shallow so bright pixels illuminate their neighbours,
            // rather than bleeding into a screen-wide veil during chain explosions.
            bloom.highQualityFiltering.Override(true); bloom.maxIterations.Override(4);
            bloom.clamp.Override(4f);
            if(!profile.TryGet<Tonemapping>(out var tone)) tone=profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            if(!profile.TryGet<ColorAdjustments>(out var colour)) colour=profile.Add<ColorAdjustments>(true);
            colour.postExposure.Override(Mathf.Log(.95f,2));
            Update();
        }
        void Update()
        {
            if(bloom!=null) bloom.intensity.Override(StandardIntensity*(1+Mathf.Clamp01(MusicPulse)*.08f+Mathf.Clamp01(BlastPulse)*.12f));
            BlastPulse*=Mathf.Exp(-Time.unscaledDeltaTime*7);
        }
        void OnDisable()
        {
            if(volume!=null) Destroy(volume.gameObject);
            if(profile!=null)
            {
                foreach(var component in profile.components) if(component!=null) Destroy(component);
                Destroy(profile);
            }
        }
    }
}
