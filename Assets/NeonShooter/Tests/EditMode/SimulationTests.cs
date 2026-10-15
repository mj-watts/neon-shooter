using System.Collections.Generic;
using NUnit.Framework;
namespace NeonShooter.Tests
{
    public sealed class SimulationTests
    {
        public static IEnumerable<TestCaseData> Cases()
        {
            var cases=new List<TestCaseData>();
            GameplayChecks.Run((name,action)=>cases.Add(new TestCaseData(action).SetName(name)));
            return cases;
        }
        [TestCaseSource(nameof(Cases))] public void Gameplay(System.Action check) { check(); }
    }
}
