using System;
namespace NeonShooter.Core
{
    /// <summary>DSP timeline: the silent gap starts at the actual end of the clip.</summary>
    public sealed class MenuMusicLoop
    {
        bool active;
        double restartAt;
        public void Start(double start,double duration,double randomUnit)
        {
            active=true; restartAt=start+duration+2+Math.Max(0,Math.Min(1,randomUnit))*2;
        }
        public bool ShouldRestart(double now) { return active && now>=restartAt; }
        public void Stop() { active=false; }
    }
    public sealed class MusicReaction
    {
        public float Level { get; private set; }
        public float Beat { get; private set; }
        float average;
        public void Advance(float[] samples,float delta)
        {
            double power=0;
            foreach(float sample in samples) power+=sample*sample;
            float energy=samples.Length==0?0:(float)Math.Min(1,Math.Sqrt(power/samples.Length)*5);
            float attack=energy>Level?35:7;
            Level+=(energy-Level)*(1-(float)Math.Exp(-attack*delta));
            average+=(energy-average)*(1-(float)Math.Exp(-2*delta));
            Beat=Math.Max(Beat*(float)Math.Exp(-9*delta),Math.Min(1,Math.Max(0,energy-average)*4));
        }
        public void Reset() { Level=0; Beat=0; average=0; }
    }
}
