using System.Collections;
using System.IO;
using NUnit.Framework;
using NeonShooter.Unity;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
namespace NeonShooter.Tests
{
    public sealed class GlowRegressionTests
    {
        static Texture2D Capture(Camera camera,RenderTexture target,VoxelAssets assets,bool halo)
        {
            assets.Draw("blockcyan",Vector3.zero,new Vector3(.5f,.5f,.1f),0,camera);
            if(halo) assets.Glow("cyan",new Vector3(0,0,-1),new Vector3(4,4,1),camera,.6f);
            assets.Flush(camera);
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
            var previous=RenderTexture.active; RenderTexture.active=target;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGBAFloat,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply(); RenderTexture.active=previous;
            return image;
        }
        static float RingBrightness(Texture2D image)
        {
            float total=0; int count=0;
            for(int x=-28;x<=28;x++) for(int y=-28;y<=28;y++)
            {
                float r=Mathf.Sqrt(x*x+y*y); if(r<12 || r>28) continue;
                var p=image.GetPixel(image.width/2+x,image.height/2+y);
                // ARGBHalf readback contains linear light in a linear project.
                // Compare display-space brightness, as in the original gamma-space regression.
                if(QualitySettings.activeColorSpace==ColorSpace.Linear) p=p.gamma;
                total+=(p.r+p.g+p.b)/3; count++;
            }
            return total/count;
        }
        [UnityTest]
        public IEnumerator FireworksRemainLuminousThenExpireAndMaterialsStayBounded()
        {
            var root=Object.FindAnyObjectByType<NeonGame>();
            if(root==null) root=new GameObject("Visual parity regression").AddComponent<NeonGame>();
            yield return null;
            root.Launch(); root.enabled=false;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var type=typeof(NeonGame);
            type.GetField("reducedMotion",flags).SetValue(root,false);
            root.SendMessage("Handle",new NeonShooter.Core.GameEvent { Type="explosion",X=0,Y=0,Size=1.5,Color="cyan" });
            var rings=(System.Collections.IList)type.GetField("rings",flags).GetValue(root);
            Assert.That(rings.Count,Is.GreaterThanOrEqualTo(3),"Fireworks need staggered spark shells");
            type.GetMethod("UpdateEffects",flags).Invoke(root,new object[]{.4f});
            Assert.That(((System.Collections.IList)type.GetField("blasts",flags).GetValue(root)).Count,Is.GreaterThan(0),"Coloured blast light must outlive the initial flash");
            Assert.That(((System.Collections.IList)type.GetField("particles",flags).GetValue(root)).Count,Is.GreaterThan(0),"Comet sparks must still be burning");
            type.GetMethod("UpdateEffects",flags).Invoke(root,new object[]{3f});
            foreach(string field in new[]{"particles","rings","blasts"})
                Assert.That(((System.Collections.IList)type.GetField(field,flags).GetValue(root)).Count,Is.Zero,field+" must expire");
            var assets=new VoxelAssets(); var camera=root.GetComponentInChildren<Camera>();
            for(int i=0;i<1000;i++) assets.Glow("cyan",Vector3.zero,Vector3.one,camera,i/999f);
            var materials=(System.Collections.IDictionary)typeof(VoxelAssets).GetField("glows",flags).GetValue(assets);
            Assert.That(materials.Count,Is.LessThanOrEqualTo(32),"Animated alpha must reuse materials");
            assets.Dispose(); Object.Destroy(root.gameObject); yield return null;
        }
        [UnityTest]
        public IEnumerator BloomAndRadialGlowProduceVisibleLightOutsideTheEmitter()
        {
            var root=Object.FindAnyObjectByType<NeonGame>();
            if(root==null) root=new GameObject("Glow regression flight").AddComponent<NeonGame>();
            yield return null;
            root.enabled=false;
            var camera=root.GetComponentInChildren<Camera>();
            var controller=camera.GetComponent<NeonBloom>(); controller.ReducedMotion=false; controller.MusicPulse=controller.BlastPulse=0;
            camera.orthographicSize=16; camera.backgroundColor=Color.black;
            var target=new RenderTexture(1280,800,24,RenderTextureFormat.ARGBHalf); camera.targetTexture=target;
            var assets=new VoxelAssets();
            var volume=root.GetComponentInChildren<Volume>();
            Assert.That(volume,Is.Not.Null);
            Assert.That(volume.profile.TryGet<Bloom>(out var bloom),Is.True);
            yield return null;
            float configured=bloom.intensity.value;
            bloom.intensity.Override(0);
            VolumeManager.instance.Update(camera.transform,camera.GetUniversalAdditionalCameraData().volumeLayerMask);
            var baseline=Capture(camera,target,assets,false);
            float baselineEffective=VolumeManager.instance.stack.GetComponent<Bloom>().intensity.value;

            bloom.intensity.Override(configured);
            VolumeManager.instance.Update(camera.transform,camera.GetUniversalAdditionalCameraData().volumeLayerMask);
            var lit=Capture(camera,target,assets,false);
            float bloomAdded=RingBrightness(lit)-RingBrightness(baseline);
            var effective=VolumeManager.instance.stack.GetComponent<Bloom>();
            var data=camera.GetUniversalAdditionalCameraData();
            var diagnostics="\nBaseline effective: "+baselineEffective+", center: "+baseline.GetPixel(baseline.width/2,baseline.height/2)+"\nEffective bloom: "+effective.intensity.value+", threshold: "+effective.threshold.value+", active: "+effective.active+", enabled: "+effective.IsActive()+"\nCamera post: "+data.renderPostProcessing+", mask: "+data.volumeLayerMask.value+", HDR: "+camera.allowHDR+"\nCenter pixel: "+lit.GetPixel(lit.width/2,lit.height/2)+"\nReadback sRGB: "+target.sRGB+", colour space: "+QualitySettings.activeColorSpace+"\nPipeline: "+GraphicsSettings.currentRenderPipeline;
            var glow=Capture(camera,target,assets,true);
            float haloAdded=RingBrightness(glow)-RingBrightness(lit);
            Directory.CreateDirectory("TestResults");
            File.WriteAllText("TestResults/glow-readback.txt","Bloom added: "+bloomAdded+"\nRadial glow added: "+haloAdded+"\nIntensity: "+configured+diagnostics);
            File.WriteAllBytes("TestResults/glow-emitter.png",glow.EncodeToPNG());
            camera.targetTexture=null; assets.Dispose(); target.Release();
            Object.Destroy(target); Object.Destroy(baseline); Object.Destroy(lit); Object.Destroy(glow); Object.Destroy(root.gameObject);
            Assert.That(bloomAdded,Is.GreaterThan(.01f),"Bloom must visibly illuminate pixels outside the solid emitter");
            Assert.That(haloAdded,Is.GreaterThan(.02f),"Radial glows must remain conspicuous around the emitter");
            yield return null;
        }
    }
}
