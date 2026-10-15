using System;
using System.Collections.Generic;

namespace NeonShooter.Core
{
    // Offsets follow the hero like the browser's OrbTrails; particles drift off the ribbon.
    public sealed class OrbitalTrails
    {
        public const int Capacity=384;
        public sealed class Point
        {
            public double X,Y,Age,Life,Vx,Vy;
            public bool Particle;
        }
        public readonly List<Point> Points=new List<Point>();
        double accumulator;
        public void Update(IEnumerable<Entity> orbs,Entity hero,double dt,bool reducedMotion)
        {
            if(dt<=0) return;
            for(int i=Points.Count-1;i>=0;i--)
            {
                var p=Points[i]; p.Age+=dt; p.X+=p.Vx*dt; p.Y+=p.Vy*dt;
                if(p.Age>=p.Life) Points.RemoveAt(i);
            }
            double interval=reducedMotion?1.0/30:1.0/90;
            accumulator+=dt;
            while(accumulator>=interval)
            {
                accumulator-=interval;
                foreach(var orb in orbs)
                {
                    double x=orb.X-hero.X,y=orb.Y-hero.Y;
                    // Step back around the orbit for sub-frame samples at lower frame rates.
                    double angle=-accumulator*7,c=Math.Cos(angle),s=Math.Sin(angle);
                    double px=x*c-y*s,py=x*s+y*c;
                    Points.Add(new Point { X=px,Y=py,Age=accumulator,Life=reducedMotion?.12:.35 });
                    Points.Add(new Point { X=px,Y=py,Age=accumulator,Life=reducedMotion?.18:.48,
                        Vx=px*.3-py*.12,Vy=py*.3+px*.12,Particle=true });
                }
            }
            if(Points.Count>Capacity) Points.RemoveRange(0,Points.Count-Capacity);
        }
        public void Clear() { Points.Clear(); accumulator=0; }
    }
}
