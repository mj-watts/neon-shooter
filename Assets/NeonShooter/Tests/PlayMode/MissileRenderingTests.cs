using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using NeonShooter.Unity;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace NeonShooter.Tests
{
    public sealed class MissileRenderingTests
    {
        [UnityTest]
        public IEnumerator MissilesHaveMatteBlocksAndRearThrustAtEveryHeading()
        {
            var host=new GameObject("Missile render verification");
            var camera=host.AddComponent<Camera>();
            camera.orthographic=true; camera.orthographicSize=3;
            camera.transform.position=new Vector3(0,0,-10);
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.025f,.03f,.055f);
            host.AddComponent<NeonBloom>();
            var target=new RenderTexture(1200,800,24,RenderTextureFormat.ARGB32);
            camera.targetTexture=target;
            var assets=new VoxelAssets();
            var previous=RenderTexture.active;
            Texture2D image=null;
            try
            {
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                var batches=(IDictionary)typeof(VoxelAssets).GetField("batches",flags).GetValue(assets);
                var meshes=(Dictionary<string,Mesh>)typeof(VoxelAssets).GetField("meshes",flags).GetValue(assets);
                for(int i=0;i<8;i++)
                {
                    float radians=i*Mathf.PI/4;
                    var velocity=new Vector2(Mathf.Sin(radians),Mathf.Cos(radians));
                    var position=new Vector3((i%4-1.5f)*2,(i/4==0?1:-1)*1.3f,0);
                    assets.DrawMissile(position,velocity,.125f,camera);
                    Assert.That(batches.Count,Is.EqualTo(2),"Only solid missile and block exhaust batches; no halo");
                    foreach(DictionaryEntry entry in batches)
                    {
                        var batch=entry.Value;
                        var matrices=(List<Matrix4x4>)batch.GetType().GetField("Matrices").GetValue(batch);
                        var matrix=matrices[matrices.Count-1];
                        Assert.That(Vector3.Dot(matrix.MultiplyVector(Vector3.up).normalized,velocity),Is.GreaterThan(.999f));
                        if((string)entry.Key!="missile")
                            Assert.That(Vector3.Dot(matrix.MultiplyPoint3x4(Vector3.zero)-position,velocity),Is.LessThan(-.35f),"Thrust sits behind the tail");
                    }
                }
                foreach(var mesh in meshes.Values)
                    if(mesh.name=="missile" || mesh.name=="missileThrust")
                        foreach(var color in mesh.colors)
                            Assert.That(Mathf.Max(color.r,color.g,color.b),Is.LessThan(.9f),"Missile blocks stay below bloom threshold");
                Assert.That(meshes["missile"].bounds.size.y,Is.GreaterThan(meshes["missile"].bounds.size.x*1.6f));
                yield return null;
                assets.Flush(camera);
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
                RenderTexture.active=target;
                image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
                Directory.CreateDirectory("TestResults");
                File.WriteAllBytes("TestResults/missile-headings.png",image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=previous; camera.targetTexture=null; target.Release();
                assets.Dispose(); Object.Destroy(target); if(image!=null) Object.Destroy(image); Object.Destroy(host);
            }
            yield return null;
        }
    }
}
