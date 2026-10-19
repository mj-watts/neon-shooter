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
    public sealed class GroundShelfTests
    {
        [UnityTest]
        public IEnumerator GroundDefensesRenderOnTopShelf()
        {
            var root=Object.FindAnyObjectByType<NeonGame>();
            if(root==null) root=new GameObject("Ground shelf regression").AddComponent<NeonGame>();
            yield return null;
            root.Launch(); root.enabled=false;
            var g=root.Model;
            g.StartWave(); g.Enemies.Clear(); g.GroundEnemies.Clear();
            g.SetPlayfield(51.2,32); g.Player.X=0;
            g.GroundCooldown=g.GateCooldown=g.CapitalCooldown=g.CivilianCooldown=9999;
            g.SpawnGroundEnemy("tank"); g.SpawnGroundEnemy("turret"); g.SpawnLaserGate();
            g.GroundEnemies[0].Side=-1; g.GroundEnemies[0].Y=5;
            g.GroundEnemies[1].Side=1; g.GroundEnemies[1].Y=5;
            g.GroundEnemies[2].Y=g.GroundEnemies[3].Y=-3;
            g.Update(.001,new FlightInput()); g.Paused=true;
            var camera=root.GetComponentInChildren<Camera>();
            var target=new RenderTexture(1280,800,24,RenderTextureFormat.ARGB32);
            Texture2D image=null;
            var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=target; camera.orthographicSize=16;
                root.SendMessage("LateUpdate");
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
                RenderTexture.active=target;
                image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
                Directory.CreateDirectory("TestResults");
                File.WriteAllBytes("TestResults/ground-top-shelf.png",image.EncodeToPNG());
                foreach(var e in g.GroundEnemies)
                {
                    double edge=.79+CanyonTerrain.Wall(e.Y+g.GroundTravel*CanyonTerrain.NearTravelRate,e.Side*.7+4);
                    Assert.That(e.Side*e.X/(g.Width*.5),Is.GreaterThan(edge).And.LessThan(1));
                }
                Assert.That(g.GateLasers[0].X,Is.EqualTo(g.GroundEnemies[2].X));
                Assert.That(g.GateLasers[0].EndX,Is.EqualTo(g.GroundEnemies[3].X));
            }
            finally
            {
                RenderTexture.active=previous; camera.targetTexture=null; target.Release();
                Object.Destroy(target); if(image!=null) Object.Destroy(image);
                Object.Destroy(root.gameObject);
            }
            yield return null;
        }
    }
}
