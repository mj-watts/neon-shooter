using NeonShooter.Core;
using NeonShooter.Unity;
using NUnit.Framework;
using UnityEngine;
namespace NeonShooter.Tests
{
    public sealed class MenuMusicTests
    {
        [TestCase(0,2)] [TestCase(.5,3)] [TestCase(1,4)]
        public void RepeatWaitsFromTheEndOfTheTrack(double random,double gap)
        {
            var loop=new MenuMusicLoop(); loop.Start(10,5,random);
            Assert.That(loop.ShouldRestart(15),Is.False);
            Assert.That(loop.ShouldRestart(15+gap-.001),Is.False);
            Assert.That(loop.ShouldRestart(15+gap),Is.True);
            loop.Stop(); Assert.That(loop.ShouldRestart(100),Is.False);
        }
        [Test]
        public void ReactionFollowsAudioAndFallsBackToSilence()
        {
            var reaction=new MusicReaction(); var samples=new float[256];
            reaction.Advance(samples,.1f); Assert.That(reaction.Level,Is.Zero);
            for(int i=0;i<samples.Length;i++) samples[i]=i%2==0?.2f:-.2f;
            reaction.Advance(samples,.05f); Assert.That(reaction.Level,Is.GreaterThan(.5));
            System.Array.Clear(samples,0,samples.Length);
            for(int i=0;i<40;i++) reaction.Advance(samples,.05f);
            Assert.That(reaction.Level,Is.LessThan(.001));
        }
        [Test]
        public void SuppliedTrackImportsAsSeparateMenuAudio()
        {
            var clip=Resources.Load<AudioClip>("MenuAudio/menu-soundtrack");
            Assert.That(clip,Is.Not.Null); Assert.That(clip.length,Is.GreaterThan(30));
            Assert.That(Resources.LoadAll<AudioClip>("Audio").Length,Is.EqualTo(7));
        }
    }
}
