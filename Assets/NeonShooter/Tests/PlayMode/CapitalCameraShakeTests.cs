using System.Collections;
using NUnit.Framework;
using NeonShooter.Unity;
using UnityEngine;
using UnityEngine.TestTools;

namespace NeonShooter.Tests
{
    public sealed class CapitalCameraShakeTests
    {
        [UnityTest]
        public IEnumerator VisibleCapitalShipRumblesUntilItLeavesOrFlightPauses()
        {
            var root=Object.FindAnyObjectByType<NeonGame>();
            if(root==null) root=new GameObject("Capital camera regression").AddComponent<NeonGame>();
            yield return null;
            root.Launch(); root.enabled=false;
            var g=root.Model;
            var camera=root.GetComponentInChildren<Camera>();
            var rest=new Vector3(0,0,-40);
            try
            {
                g.SpawnCapitalShip();
                var ship=g.CapitalShips[0];
                ship.Y=g.Height/2+14;
                g.Paused=false; root.SendMessage("Update");
                Assert.That(camera.transform.position,Is.EqualTo(rest),"No rumble before the hull enters");

                ship.Y=0;
                for(int frame=0;frame<5;frame++)
                {
                    g.Paused=false; root.SendMessage("Update");
                    Assert.That(Vector3.Distance(camera.transform.position,rest),Is.GreaterThan(.01f),"Visible hull must sustain camera shake");
                }
                g.Paused=true; root.SendMessage("Update");
                Assert.That(camera.transform.position,Is.EqualTo(rest),"Pause must stop the camera");
                g.Paused=false; ship.Y=-g.Height/2-14; root.SendMessage("Update");
                Assert.That(camera.transform.position,Is.EqualTo(rest),"No rumble after the hull leaves");
                g.CapitalShips.Clear(); root.SendMessage("Update");
                Assert.That(camera.transform.position,Is.EqualTo(rest));
            }
            finally { Object.Destroy(root.gameObject); }
            yield return null;
        }
    }
}
