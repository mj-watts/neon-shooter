using System.Collections;
using System.IO;
using NUnit.Framework;
using NeonShooter.Core;
using NeonShooter.Unity;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace NeonShooter.Tests
{
    public sealed class SceneryScrollTests
    {
        [UnityTest]
        public IEnumerator LegacyMotionPreferenceCannotSlowScenery()
        {
            const string key="neon-shooter.reduced-motion";
            bool existed=PlayerPrefs.HasKey(key);
            int previous=PlayerPrefs.GetInt(key);
            var existing=Object.FindAnyObjectByType<NeonGame>();
            if(existing!=null) Object.Destroy(existing.gameObject);
            yield return null;
            PlayerPrefs.SetInt(key,1);
            var root=new GameObject("Legacy scenery preference regression").AddComponent<NeonGame>();
            try
            {
                root.Launch();
                Assert.That(root.Model.ScenerySpeed,Is.EqualTo(3.6),"Old preferences must not slow flight scenery");
            }
            finally
            {
                if(existed) PlayerPrefs.SetInt(key,previous); else PlayerPrefs.DeleteKey(key);
                Object.Destroy(root.gameObject);
            }
            yield return null;
        }

        static Texture2D Capture(NeonGame root, Camera camera, RenderTexture target)
        {
            root.SendMessage("LateUpdate");
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
            var previous=RenderTexture.active;
            RenderTexture.active=target;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
            image.Apply(); RenderTexture.active=previous;
            return image;
        }

        static int DownwardShift(Texture2D before, Texture2D after, float left, float right)
        {
            var a=before.GetPixels32(); var b=after.GetPixels32();
            double best=double.MaxValue; int shift=0;
            for(int candidate=-100;candidate<=100;candidate++)
            {
                double error=0;
                for(int y=150;y<before.height-150;y+=2)
                    for(int x=(int)(left*before.width);x<(int)(right*before.width);x+=2)
                    {
                        var p=a[y*before.width+x]; var q=b[(y-candidate)*before.width+x];
                        error+=System.Math.Abs(p.r-q.r)+System.Math.Abs(p.g-q.g)+System.Math.Abs(p.b-q.b);
                    }
                if(error<best) { best=error; shift=candidate; }
            }
            return shift;
        }

        [UnityTest]
        public IEnumerator SceneryRendersDownwardAtDifferentDepthSpeeds()
        {
            var root=Object.FindAnyObjectByType<NeonGame>();
            if(root==null) root=new GameObject("Scenery regression").AddComponent<NeonGame>();
            yield return null;
            root.Launch(); root.enabled=false; root.Model.Paused=true;
            var camera=root.GetComponentInChildren<Camera>();
            var target=new RenderTexture(960,600,24,RenderTextureFormat.ARGB32);
            camera.targetTexture=target; camera.orthographicSize=16;
            root.Model.SetPlayfield(51.2,32); root.Model.Player.X=0;
            Texture2D before=null,after=null;
            try
            {
                root.Model.GroundTravel=0;
                before=Capture(root,camera,target);
                yield return null;
                root.Model.GroundTravel=4;
                after=Capture(root,camera,target);
                Directory.CreateDirectory("TestResults");
                File.WriteAllBytes("TestResults/scenery-before.png",before.EncodeToPNG());
                File.WriteAllBytes("TestResults/scenery-after.png",after.EncodeToPNG());
                float pixelsPerUnit=target.height/32f;
                var layers=new[]{("floor",.55f,.60f,CanyonTerrain.DeepTravelRate*.45),
                    ("deep",.745f,.755f,CanyonTerrain.DeepTravelRate),
                    ("middle",.84f,.86f,CanyonTerrain.MiddleTravelRate),
                    ("near",.96f,.98f,CanyonTerrain.NearTravelRate)};
                foreach(var layer in layers)
                {
                    int shift=DownwardShift(before,after,layer.Item2,layer.Item3);
                    Debug.Log("SCENERY_SCROLL: "+layer.Item1+" downward pixels="+shift);
                    Assert.That(shift,Is.EqualTo(4*layer.Item4*pixelsPerUnit).Within(2),layer.Item1+" must scroll down at its own depth speed");
                }
            }
            finally
            {
                camera.targetTexture=null; target.Release(); Object.Destroy(target);
                if(before!=null) Object.Destroy(before);
                if(after!=null) Object.Destroy(after);
                Object.Destroy(root.gameObject);
            }
            yield return null;
        }
    }
}
