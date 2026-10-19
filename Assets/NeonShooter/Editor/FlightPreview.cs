using System;
using System.IO;
using System.Linq;
using NeonShooter.Core;
using NeonShooter.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace NeonShooter.Editor
{
    // Explicit CLI/menu preview run; no work is scheduled unless Start is invoked.
    [InitializeOnLoad]
    public static class FlightPreview
    {
        const string StageKey="NeonShooter.PreviewStage", TimeKey="NeonShooter.PreviewTime";
        static FlightPreview() { EditorApplication.update+=Update; }
        [MenuItem("Neon Shooter/Capture UI and Flight Previews")]
        public static void Start()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/NeonShooter.unity");
            var type=TypeCache.GetTypesDerivedFrom<EditorWindow>().First(t=>t.Name=="GameView");
            if(Environment.GetCommandLineArgs().Contains("-neonPreviewExit"))
                foreach(var existing in Resources.FindObjectsOfTypeAll(type)) ((EditorWindow)existing).Close();
            var window=(EditorWindow)ScriptableObject.CreateInstance(type);
            window.titleContent=new GUIContent("Neon Flight Preview"); window.Show(); window.position=new Rect(80,80,1280,820); SetReferenceSize(window); window.Focus();
            SessionState.SetString("NeonShooter.PendingCapture",""); SessionState.SetInt(StageKey,1); Delay(3); EditorApplication.isPaused=false; EditorApplication.isPlaying=true;
        }
        static void SetReferenceSize(EditorWindow window)
        {
            // Fixed render resolution is essential on Retina displays: window
            // dimensions are logical points, screenshots contain physical pixels.
            var flags=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy;
            var assembly=window.GetType().Assembly;
            var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var sizes=sizesType.GetProperty("instance",flags).GetValue(null);
            var getGroup=sizesType.GetMethod("GetGroup",flags);
            var group=getGroup.Invoke(sizes,new[]{Enum.Parse(getGroup.GetParameters()[0].ParameterType,"Standalone")});
            var sizeType=assembly.GetType("UnityEditor.GameViewSize");
            var kind=assembly.GetType("UnityEditor.GameViewSizeType");
            var size=Activator.CreateInstance(sizeType,flags,null,new[]{Enum.Parse(kind,"FixedResolution"),(object)1280,800,"Browser comparison 1280x800"},null);
            var groupType=group.GetType();
            int index=(int)groupType.GetMethod("GetTotalCount",flags).Invoke(group,null);
            groupType.GetMethod("AddCustomSize",flags).Invoke(group,new[]{size});
            window.GetType().GetProperty("selectedSizeIndex",flags).SetValue(window,index);
        }
        static void Capture(string path)
        {
            if(File.Exists(path)) File.Delete(path);
            SessionState.SetString("NeonShooter.PendingCapture",path);
            ScreenCapture.CaptureScreenshot(path);
        }
        static void Delay(float seconds) { SessionState.SetFloat(TimeKey,(float)EditorApplication.timeSinceStartup+seconds); }
        static void Update()
        {
            int stage=SessionState.GetInt(StageKey,0);
            if(stage!=0 && EditorApplication.isPlaying)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                foreach(var view in Resources.FindObjectsOfTypeAll<EditorWindow>())
                    if(view.GetType().Name=="GameView") view.Repaint();
            }
            string pending=SessionState.GetString("NeonShooter.PendingCapture","");
            if(pending!="")
            {
                if(!File.Exists(pending)) return;
                SessionState.SetString("NeonShooter.PendingCapture","");
            }
            if(stage==0 || !EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup<SessionState.GetFloat(TimeKey,0)) return;
            if(EditorApplication.isPaused) { EditorApplication.isPaused=false; Delay(1); return; }
            var root=UnityEngine.Object.FindAnyObjectByType<NeonGame>(); if(root==null || root.Model==null) return;
            Application.runInBackground=true;
            Directory.CreateDirectory("TestResults");
            bool menuAudio=Environment.GetCommandLineArgs().Contains("-neonMenuAudioVerify") && (stage==1 || stage==7 || stage==8);
            root.GetComponent<FlightAudio>().SetMuted(!menuAudio);
            bool parity=Environment.GetCommandLineArgs().Contains("-neonVisualParity");
            if(parity && stage>=3 && stage!=12)
            {
                root.Model.Enemies.Clear(); root.Model.Bullets.Clear(); root.Model.Pickups.Clear();
                root.Model.GroundTravel=0; root.Model.Player.X=0; root.Model.Player.Y=-10.5;
                root.Model.Wave=2; root.Model.Phase="playing"; root.Model.Elapsed=0;
                var history=(RunHistory)typeof(NeonGame).GetField("history",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(root);
                history.Data.battles.Clear(); history.Data.bestScore=0; history.Data.bestWave=0;
                // A hidden, inert enemy keeps normal wave-completion logic idle.
                root.Model.Enemies.Add(new Entity { Id=99999,X=1000,Y=1000,Age=-100,FireAt=1000,Hp=1000 });
                typeof(NeonGame).GetField("calloutUntil",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(root,0f);
            }
            switch(stage)
            {
                case 1:
                    Debug.Log("VISUAL_REFERENCE: resolution="+Screen.width+"x"+Screen.height+" activeColorSpace="+QualitySettings.activeColorSpace+" configured="+PlayerSettings.colorSpace);
                    if(Environment.GetCommandLineArgs().Contains("-neonMenuAudioVerify"))
                    {
                        root.GetComponent<MenuSoundtrack>().SetMuted(false);
                        SessionState.SetInt(StageKey,7); Delay(1); break;
                    }
                    Capture("TestResults/briefing-ui.png");
                    SessionState.SetInt(StageKey,2); Delay(1); break;
                case 7:
                    Debug.Log("MENU_AUDIO_REACTION: pulse="+root.GetComponent<MenuSoundtrack>().Pulse+" playing="+root.GetComponent<MenuSoundtrack>().Source.isPlaying);
                    Capture("TestResults/briefing-ui.png");
                    SessionState.SetInt(StageKey,8); Delay(.3f); break;
                case 8:
                    Debug.Log("MENU_AUDIO_REACTION: pulse="+root.GetComponent<MenuSoundtrack>().Pulse);
                    Capture("TestResults/menu-music-ui.png");
                    SessionState.SetInt(StageKey,2); Delay(.5f); break;
                case 2:
                    root.Launch(); root.Model.StartWave();
                    if(parity) { root.Model.Reset(); root.Model.Phase="playing"; root.Model.Wave=2; } root.Model.Player.Invulnerable=100;
                    if(!parity) { root.Model.CollectPowerup("shield"); root.Model.CollectPowerup("orbit"); root.Model.CollectPowerup("orbit"); root.Model.CollectPowerup("wingmen"); }
                    foreach(var enemy in root.Model.Enemies) enemy.Age=1.5;
                    if(!parity) for(int side=-1;side<=1;side+=2) root.Model.Pickups.Add(new Entity { Id=9000+side,Kind="life",X=side*4,Y=-1 });
                    SessionState.SetInt(StageKey,3); Delay(2); break;
                case 3:
                    root.Model.Paused=false; Capture("TestResults/flight-ui.png");
                    SessionState.SetInt(StageKey,parity?9:4); Delay(1); break;
                case 9:
                    foreach(var entry in new[]{(-9f,3f,"pink"),(5f,7f,"cyan"),(10f,-3f,"gold")})
                        root.SendMessage("Handle",new GameEvent { Type="explosion",X=entry.Item1,Y=entry.Item2,Size=1.5,Color=entry.Item3 });
                    SessionState.SetInt(StageKey,10); Delay(.4f); break;
                case 10:
                    Capture("TestResults/explosions-ui.png");
                    SessionState.SetInt(StageKey,14); Delay(.55f); break;
                case 14:
                    Capture("TestResults/fireworks-trails-ui.png");
                    SessionState.SetInt(StageKey,4); Delay(.5f); break;
                case 4:
                    root.Model.Paused=true; SessionState.SetInt(StageKey,5); Delay(.5f); break;
                case 5:
                    Capture("TestResults/paused-ui.png");
                    SessionState.SetInt(StageKey,parity?11:6); Delay(1); break;
                case 11:
                    root.Model.Paused=false;
                    root.SendMessage("UpdateEffects",3f);
                    foreach(var entry in new[]{(-9f,3f,"pink"),(5f,7f,"cyan"),(10f,-3f,"gold")})
                        root.SendMessage("Handle",new GameEvent { Type="explosion",X=entry.Item1,Y=entry.Item2,Size=1.5,Color=entry.Item3 });
                    SessionState.SetInt(StageKey,13); Delay(.4f); break;
                case 13:
                    root.Model.Phase="gameover";
                    var records=(RunHistory)typeof(NeonGame).GetField("history",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(root);
                    records.Data.battles.Add(new BattleRecord { score=0,wave=2,kills=0 }); records.Data.bestScore=0; records.Data.bestWave=2;
                    SessionState.SetInt(StageKey,12); Delay(.5f); break;
                case 12:
                    Capture("TestResults/gameover-ui.png");
                    SessionState.SetInt(StageKey,6); Delay(1); break;
                case 6:
                    SessionState.SetInt(StageKey,0); EditorApplication.isPlaying=false;
                    Debug.Log("NEON_PREVIEWS_CAPTURED: briefing-ui.png, flight-ui.png, paused-ui.png");
                    if(Environment.GetCommandLineArgs().Contains("-neonPreviewExit")) EditorApplication.Exit(0);
                    break;
            }
        }
    }
}
