using System;
using System.Collections.Generic;
using NeonShooter.Core;
using UnityEngine;
namespace NeonShooter.Unity
{
    public sealed class FlightAudio : MonoBehaviour
    {
        AudioSource music,voice,effects,alarm;
        readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        readonly Dictionary<string,float> last=new Dictionary<string,float>();
        readonly List<AudioClip> generated=new List<AudioClip>();
        public bool Muted { get; private set; }
        AudioSource Source(float volume) { var s=gameObject.AddComponent<AudioSource>(); s.playOnAwake=false; s.spatialBlend=0; s.volume=volume; return s; }
        void Awake()
        {
            music=Source(.3f); voice=Source(.5f); effects=Source(.35f); alarm=Source(.15f);
            foreach(var clip in Resources.LoadAll<AudioClip>("Audio")) clips[clip.name]=clip;
            music.loop=true; AudioClip track; if(clips.TryGetValue("soundtrack",out track)) music.clip=track;
            Synth("shot",91,62,.055f,.08f); Synth("enemyShot",65,42,.09f,.05f);
            Synth("missileLaunch",48,79,.25f,.09f); Synth("laser",67,64,.15f,.07f);
            Synth("collect",84,96,.12f,.13f); Synth("powerup",72,96,.35f,.15f);
            Synth("life",79,103,.3f,.15f); Synth("countdown",84,84,.12f,.13f);
            Synth("explosion",40,19,.65f,.4f,true); Synth("impact",70,50,.04f,.09f,true);
        }
        void Synth(string name,int midi,int end,float duration,float volume,bool noise=false)
        {
            const int rate=22050; var data=new float[(int)(duration*rate)]; double phase=0; var rng=new System.Random(37);
            for(int i=0;i<data.Length;i++)
            {
                double t=i/(double)data.Length,frequency=440*Math.Pow(2,(midi+(end-midi)*t-69)/12);
                phase+=frequency/rate; double tone=noise?(rng.NextDouble()*2-1)*.6+Math.Sin(phase*Math.PI*2)*.4:Math.Sign(Math.Sin(phase*Math.PI*2));
                data[i]=(float)(tone*volume*Math.Min(1,t*100)*Math.Pow(1-t,2));
            }
            var clip=AudioClip.Create(name,data.Length,1,rate,false); clip.SetData(data,0); clips[name]=clip; generated.Add(clip);
        }
        public void Begin() { Stop(); music.Play(); }
        public void Stop() { music.Stop(); voice.Stop(); effects.Stop(); alarm.Stop(); }
        public void Pause(bool paused) { foreach(var s in new[]{music,voice,effects,alarm}) { if(paused) s.Pause(); else s.UnPause(); } }
        public void SetMuted(bool value) { Muted=value; foreach(var s in new[]{music,voice,effects,alarm}) s.mute=value; }
        void Play(string key,AudioSource source) { AudioClip clip; if(clips.TryGetValue(key,out clip)) source.PlayOneShot(clip); }
        public void Handle(GameEvent e)
        {
            string key=e.Type; float cooldown=key=="shot"?.06f:key=="enemyShot"?.1f:key=="impact"?.09f:0;
            float time; if(last.TryGetValue(key,out time) && Time.unscaledTime-time<cooldown) return; last[key]=Time.unscaledTime;
            if(key=="gameover") { Stop(); Play("gameover",voice); return; }
            if(key=="capitalIncoming") { Play("bigship",voice); return; }
            if(key=="civilianIncoming") { Play("mayday",voice); return; }
            if(key=="civilianAlarm") { if(!alarm.isPlaying) Play("siren",alarm); return; }
            if(key=="powerup" && (e.Power=="fleet" || e.Power=="assault")) Play(UnityEngine.Random.value<.5f?"engaging":"engaging-2",voice);
            if(key.EndsWith("Explosion",StringComparison.Ordinal) || key=="hit" || key=="playerCrash") key="explosion";
            if(key=="armourImpact") key="impact";
            Play(key,effects);
        }
        void OnDestroy() { foreach(var clip in generated) Destroy(clip); }
    }
}
