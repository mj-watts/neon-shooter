using System;
using System.IO;
using System.Linq;
using NeonShooter.Unity;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NeonShooter.Editor
{
    public static class ProjectTools
    {
        const string ScenePath="Assets/Scenes/NeonShooter.unity";
        [MenuItem("Neon Shooter/Open Game Scene")]
        public static void OpenScene() { EditorSceneManager.OpenScene(ScenePath); }
        [MenuItem("Neon Shooter/Validate Project")]
        public static void Validate()
        {
            UniversalPipelineSetup.Ensure();
            foreach(var shader in Resources.LoadAll<Shader>("Shaders"))
            {
                var errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").ToArray();
                if(errors.Length>0) throw new Exception(shader.name+": "+string.Join("; ",errors.Select(m=>m.message)));
            }
            if(Resources.LoadAll<AudioClip>("Audio").Length!=7) throw new Exception("Expected all seven source audio assets.");
            OpenScene();
            if(UnityEngine.Object.FindAnyObjectByType<NeonGame>()==null) throw new Exception("Scene is missing NeonGame.");
            Debug.Log("NEON_PROJECT_VALIDATED: scene, scripts, shaders and seven audio imports are available.");
        }
        [MenuItem("Neon Shooter/Build macOS")]
        public static void BuildMac()
        {
            Validate(); Directory.CreateDirectory("Builds/macOS");
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath},locationPathName="Builds/macOS/Neon Shooter.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.None });
            if(result.summary.result!=BuildResult.Succeeded) throw new Exception("Build failed: "+result.summary.result);
            Debug.Log("NEON_BUILD_SUCCEEDED: "+result.summary.outputPath);
        }
    }
}
