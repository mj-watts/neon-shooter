using NeonShooter.Core;
using UnityEngine;
namespace NeonShooter.Unity
{
    /// <summary>Menu-only playback and audio analysis. Gameplay owns a separate audio player.</summary>
    public sealed class MenuSoundtrack : MonoBehaviour
    {
        AudioSource source;
        readonly MenuMusicLoop loop=new MenuMusicLoop();
        readonly MusicReaction reaction=new MusicReaction();
        readonly float[] samples=new float[256];
        public float Pulse => Mathf.Clamp01(reaction.Level*.3f+reaction.Beat*.7f);
        public bool Active { get; private set; }
        public AudioSource Source => source;
        void Awake()
        {
            source=gameObject.AddComponent<AudioSource>(); source.playOnAwake=false;
            source.loop=false; source.spatialBlend=0; source.volume=.45f;
            source.clip=Resources.Load<AudioClip>("MenuAudio/menu-soundtrack");
        }
        public void Begin()
        {
            Stop(); if(source.clip==null) return;
            Active=true; PlayTrack();
        }
        void PlayTrack()
        {
            double start=AudioSettings.dspTime+.05;
            source.PlayScheduled(start);
            loop.Start(start,source.clip.length,Random.value);
        }
        public void Stop()
        {
            Active=false; loop.Stop(); reaction.Reset(); if(source!=null) source.Stop();
        }
        public void SetMuted(bool muted) { source.mute=muted; }
        void Update()
        {
            if(!Active) return;
            if(loop.ShouldRestart(AudioSettings.dspTime)) PlayTrack();
            System.Array.Clear(samples,0,samples.Length);
            if(source.isPlaying && !source.mute) source.GetOutputData(samples,0);
            reaction.Advance(samples,Mathf.Min(Time.unscaledDeltaTime,.1f));
        }
        void OnDisable() { Stop(); }
    }
}
