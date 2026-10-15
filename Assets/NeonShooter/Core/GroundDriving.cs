using System;
using System.Collections.Generic;

namespace NeonShooter.Core
{
    // Use the same parcel hashes and hills as the canyon shader to see trees and roofs.
    public static class GroundDriving
    {
        static double Fract(double x) { return x-Math.Floor(x); }
        static double Mod(double x,double size) { return x-Math.Floor(x/size)*size; }
        static double Hash(double x,double y) { return Fract(Math.Sin(x*127.1+y*311.7)*43758.5453); }
        static double Mix(double a,double b,double t) { return a+(b-a)*t; }
        static double Noise(double x,double y)
        {
            double ix=Math.Floor(x),iy=Math.Floor(y),fx=Fract(x),fy=Fract(y);
            fx=fx*fx*(3-2*fx); fy=fy*fy*(3-2*fy);
            return Mix(Mix(Hash(ix,iy),Hash(ix+1,iy),fx),Mix(Hash(ix,iy+1),Hash(ix+1,iy+1),fx),fy);
        }
        static double Height(double x,double y)
        { x*=.065; y*=.065; return Noise(x,y)*.58+Noise(x*2.1+7.3,y*2.1+7.3)*.28+Noise(x*4.3+19.7,y*4.3+19.7)*.14; }
        public static bool SceneryObstacle(double x,double y)
        {
            const double layer=3;
            double dx=Math.Floor(x/12),dy=Math.Floor(y/12),lx=Mod(x+6,12)-6,ly=Mod(y+6,12)-6;
            if(Math.Abs(lx)<.3) return false;
            double seed=Hash(dx+layer*17,dy+layer*17);
            double lot=Hash(Math.Floor((lx+6)/2.4)+dx*5+layer*31,Math.Floor((ly+6)/2.4)+dy*5+layer*31);
            double px=Mod(lx+6,2.4)-1.2,py=Mod(ly+6,2.4)-1.2;
            if(seed>.68 && lot>.28 && Math.Abs(px)<.28+lot*.26+.28 && Math.Abs(py)<.24+Fract(lot*7)*.35+.28) return true;
            return Height(x+layer*13,y+layer*13)>.49 && lot>.5 &&
                Math.Abs(px-(Fract(lot*3)-.5)*.8)<.56 && Math.Abs(py-(Fract(lot*7)-.5)*.8)<.56;
        }
        public static double SteeringTarget(Entity car,double lane,double width,double travel,double lateral,IEnumerable<Entity> traffic)
        {
            double desired=lane+Math.Sin(car.Age*1.5+car.Id*2.1)*.22;
            double boundary=.79+CanyonTerrain.Wall(car.Y+travel,car.Side*.7+4);
            double min=width*.5*boundary+.38,max=width*.5-.38;
            double best=car.X,bestCost=double.MaxValue;
            for(int step=-3;step<=3;step++)
            {
                double x=car.Side*Math.Max(min,Math.Min(max,car.Side*(desired+step*.3)+car.Side*lateral))-lateral;
                double cost=Math.Abs(x-desired)*.7+Math.Abs(x-car.X)*.15;
                for(int look=1;look<=4;look++)
                {
                    double y=car.Y+look*.65;
                    if(SceneryObstacle(x+lateral,y+travel)) cost+=2.0/look;
                    foreach(var other in traffic)
                        if(other!=car && other.Y>car.Y-.2 && Math.Abs(other.Y-y)<1.05 && Math.Abs(other.X-x)<.85) cost+=3.0/look;
                }
                if(cost<bestCost) { bestCost=cost; best=x; }
            }
            return best;
        }
    }
}
