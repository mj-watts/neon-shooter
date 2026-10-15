using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using NeonShooter.Unity;
using NeonShooter.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
namespace NeonShooter.Tests
{
    public sealed class FlightSmokeTests
    {
        [UnityTest]
        public IEnumerator SceneStartsAndRendersEveryEncounter()
        {
            var root=Object.FindAnyObjectByType<NeonGame>();
            if(root==null) root=new GameObject("Test Flight").AddComponent<NeonGame>();
            yield return null;
            var camera=root.GetComponentInChildren<Camera>();
            Assert.That(camera,Is.Not.Null);
            Assert.That(Object.FindObjectsByType<AudioListener>().Count(l=>l.enabled),Is.EqualTo(1));
            foreach(var shader in Resources.LoadAll<Shader>("Shaders")) Assert.That(shader.isSupported,Is.True,shader.name);
            Assert.That(Resources.LoadAll<AudioClip>("Audio").Length,Is.EqualTo(7));
            root.Launch();
            var g=root.Model;
            for(int i=0;i<40;i++) g.Advance(.1,new FlightInput());
            Assert.That(g.Wave,Is.EqualTo(1));
            foreach(var p in PowerCatalog.All) g.CollectPowerup(p.Key);
            g.SpawnCivilian(); g.SpawnCapitalShip(); g.SpawnGroundEnemy("tank"); g.SpawnLaserGate();
            g.Player.Invulnerable=100;
            foreach(var enemy in g.Enemies) enemy.Age=1.8;
            g.Civilians[0].Y=4;
            g.CapitalShips[0].Y=12;
            foreach(var ground in g.GroundEnemies) ground.Y=6;
            var target=new RenderTexture(1280,800,24,RenderTextureFormat.ARGB32);
            camera.targetTexture=target;
            g.SetPlayfield(32.0*target.width/target.height,32);
            // Let the rotating guards leave a visible ribbon and the cars enter the arena.
            for(int frame=0;frame<90;frame++) { g.Paused=false; yield return null; }
            Assert.That(root.OrbitalTrailCount,Is.GreaterThan(0),"Rotating guards must render block and particle trails");
            Assert.That(g.AssaultFleet.All(car=>car.Scale==.8),Is.True);
            root.SendMessage("LateUpdate");
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
            var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
            RenderTexture.active=previous;
            Directory.CreateDirectory("TestResults");
            File.WriteAllBytes("TestResults/flight-urp.png",image.EncodeToPNG());
            var pixels=image.GetPixels32();
            int pink=pixels.Count(p=>p.r>220 && p.g<30 && p.b>220);
            Assert.That(pink,Is.LessThan(pixels.Length/20),"Magenta error shader covered the flight view");
            Assert.That(pixels.Select(p=>(p.r/8,p.g/8,p.b/8)).Distinct().Count(),Is.GreaterThan(30),"Flight frame was empty or unrendered");
            // A second frame isolates the new trails and drivers from encounter effects.
            g.Enemies.Clear(); g.CapitalShips.Clear(); g.GroundEnemies.Clear(); g.GroundWrecks.Clear();
            g.Civilians.Clear(); g.Bullets.Clear(); g.Pickups.Clear();
            g.ShieldTime=g.CompanionTime=0; g.Player.Y=-2;
            g.Phase="intermission"; g.Elapsed=-100;
            g.SupplyCooldown=g.GroundCooldown=g.GateCooldown=g.CapitalCooldown=g.CivilianCooldown=100;
            g.CollectPowerup("orbit"); g.CollectPowerup("orbit");
            for(int i=0;i<g.AssaultFleet.Count;i++) g.AssaultFleet[i].Y=-3+(i%3)*3;
            for(int frame=0;frame<60;frame++) { g.Paused=false; yield return null; }
            Assert.That(root.OrbitalTrailCount,Is.GreaterThan(20));
            root.SendMessage("LateUpdate");
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
            RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
            RenderTexture.active=previous;
            File.WriteAllBytes("TestResults/addons-trails-and-cars.png",image.EncodeToPNG());
            camera.targetTexture=null;
            target.Release(); Object.Destroy(target); Object.Destroy(image);
            g.Paused=true; double clock=g.Clock;
            yield return null;
            Assert.That(g.Clock,Is.EqualTo(clock));
            g.Paused=false;
            LogAssert.NoUnexpectedReceived();
            Object.Destroy(root.gameObject);
            yield return null;
        }
    }
}
