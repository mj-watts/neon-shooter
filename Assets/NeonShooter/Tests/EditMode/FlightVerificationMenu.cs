using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
namespace NeonShooter.Tests
{
    [InitializeOnLoad]
    public static class FlightVerificationMenu
    {
        static readonly TestRunnerApi Api;
        const string ModeKey="NeonShooter.FlightVerification";
        static FlightVerificationMenu()
        {
            Api=ScriptableObject.CreateInstance<TestRunnerApi>();
            Api.RegisterCallbacks(new Results());
        }
        [MenuItem("Neon Shooter/Verify Add-ons/Edit Mode")]
        public static void EditMode() { Run(TestMode.EditMode,"addons-editmode"); }
        [MenuItem("Neon Shooter/Verify Add-ons/Play Mode")]
        public static void PlayMode() { Run(TestMode.PlayMode,"addons-playmode"); }
        static void Run(TestMode mode,string name)
        {
            SessionState.SetString(ModeKey,name);
            Api.Execute(new ExecutionSettings(new Filter { testMode=mode,
                assemblyNames=new[]{mode==TestMode.EditMode?"NeonShooter.EditMode":"NeonShooter.PlayMode"} }));
        }
        sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) {}
            public void TestStarted(ITestAdaptor test) {}
            public void TestFinished(ITestResultAdaptor result) {}
            public void RunFinished(ITestResultAdaptor result)
            {
                string name=SessionState.GetString(ModeKey,"");
                if(name=="") return;
                Directory.CreateDirectory("TestResults");
                TestRunnerApi.SaveResultToFile(result,"TestResults/"+name+".xml");
                Debug.Log("NEON_ADDON_VERIFICATION: "+name+" passed="+result.PassCount+" failed="+result.FailCount);
                SessionState.EraseString(ModeKey);
            }
        }
    }
}
