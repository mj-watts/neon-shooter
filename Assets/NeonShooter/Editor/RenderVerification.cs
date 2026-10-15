using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using NeonShooter.Unity;

namespace NeonShooter.Editor
{
    // Compile the actual shader passes for the Editor's active graphics API.
    public static class RenderVerification
    {
        [MenuItem("Neon Shooter/Report Shader Compilation")]
        public static void Report()
        {
            Directory.CreateDirectory("TestResults");
            var lines=new System.Collections.Generic.List<string>();
            lines.Add("Editor: "+Application.unityVersion);
            lines.Add("Pipeline: "+(GraphicsSettings.currentRenderPipeline?.GetType().Name??"Built-in"));
            foreach(var shader in Resources.LoadAll<Shader>("Shaders"))
            {
                var material=new Material(shader);
                for(int pass=0;pass<material.passCount;pass++) ShaderUtil.CompilePass(material,pass,true);
                var errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").ToArray();
                lines.Add(shader.name+": supported="+shader.isSupported+", errors="+errors.Length);
                foreach(var error in errors) lines.Add(error.file+":"+error.line+" "+error.message);
                UnityEngine.Object.DestroyImmediate(material);
            }
            File.WriteAllLines("TestResults/render-verification.txt",lines);
            Debug.Log("NEON_RENDER_VERIFICATION: "+string.Join(" | ",lines));
        }
    }
}
