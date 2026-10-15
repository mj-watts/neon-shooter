using System.Collections;
using NeonShooter.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace NeonShooter.Tests
{
    public sealed class MenuSoundtrackTests
    {
        [UnityTest]
        public IEnumerator MenuMusicStopsForFlightAndReturnsWithTheMenu()
        {
            var root=Object.FindAnyObjectByType<NeonGame>();
            if(root==null) root=new GameObject("Menu Audio Test").AddComponent<NeonGame>();
            yield return null;
            var menu=root.GetComponent<MenuSoundtrack>();
            Assert.That(menu.Active,Is.True); Assert.That(menu.Source.clip.name,Is.EqualTo("menu-soundtrack"));
            Assert.That(menu.Source.loop,Is.False);
            menu.SetMuted(true); Assert.That(menu.Source.mute,Is.True);
            root.Launch(); Assert.That(menu.Active,Is.False); Assert.That(menu.Source.isPlaying,Is.False);
            root.SendMessage("Leave"); Assert.That(menu.Active,Is.True);
            Object.Destroy(root.gameObject); yield return null;
        }
        [UnityTest]
        public IEnumerator NaturalCompletionHasASilentGapBeforeRestarting()
        {
            var host=new GameObject("Short Menu Track Test");
            var menu=host.AddComponent<MenuSoundtrack>();
            var clip=AudioClip.Create("Short track",4410,1,44100,false);
            menu.Source.clip=clip; menu.SetMuted(true); menu.Begin();
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(menu.Source.isPlaying,Is.False);
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.That(menu.Source.isPlaying,Is.False,"At least two seconds of silence after the track ends");
            double deadline=AudioSettings.dspTime+3;
            while(!menu.Source.isPlaying && AudioSettings.dspTime<deadline) yield return null;
            Assert.That(menu.Source.isPlaying,Is.True,"Track should restart within four seconds of ending");
            Object.Destroy(host); Object.Destroy(clip); yield return null;
        }
    }
}
