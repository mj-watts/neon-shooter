using System;
using System.Collections.Generic;

namespace NeonShooter.Core
{
    public sealed class FlightInput
    {
        public bool Left, Right, Up, Down, Fire, Missile, HasTarget;
        public double TargetX, TargetY;
    }

    // Simulation data contains no engine objects. Coordinates match the browser's XY world.
    public class Entity
    {
        public int Id, Side, Row, Column, Pattern, Variant, Value = 1, GateId;
        public string Kind = "", Power = "", Support = "";
        public double X, Y, Z = 1, Vx, Vy, Age, Origin, Hp = 1, FireAt, Cooldown;
        public double Life = 8, Fuse, Damage = 1, Strength = 1, Scale = 1, Bank, Invulnerable;
        public double Aim, EntryY, PreviousY, MissileCooldown, FollowDelay, Speed;
        public double HitCooldown, AlarmCooldown, LaserTime, FallAge, FallDuration = 4, FallX, FallY, FallBank;
        public bool Friendly, Heavy, Invader, Departing, Destroyed, Collected, Exploded, Warning, EnteredView;
        public List<Entity> Turrets = new List<Entity>();
    }

    public sealed class Beam
    {
        public int Id;
        public double X, Y, EndX, Top, Width = .1, Damage = 18, Strength = 1;
        public string Color = "cyan";
        public bool Warning;
    }

    public sealed class GameEvent
    {
        public string Type, Power, Color;
        public double X, Y, Size;
        public bool Perfect, Fatal;
    }

    public sealed class PowerDefinition
    {
        public readonly string Key, Label, Color;
        public readonly double Duration;
        public readonly int Weight;
        public PowerDefinition(string key, string label, string color, double duration, int weight)
        { Key=key; Label=label; Color=color; Duration=duration; Weight=weight; }
    }

    public static class PowerCatalog
    {
        public static readonly PowerDefinition[] All = {
            new PowerDefinition("machine", "MACHINE GUN", "gold", 12, 11),
            new PowerDefinition("laser", "TWIN LASERS", "cyan", 12, 11),
            new PowerDefinition("missile", "MISSILE +1", "orange", 0, 17),
            new PowerDefinition("shield", "SHIELD", "mint", 8, 11),
            new PowerDefinition("speed", "SPEED BOOST", "violet", 15, 11),
            new PowerDefinition("orbit", "ORBITAL GUARD", "mint", 10, 30),
            new PowerDefinition("wingmen", "WING ORBS", "cyan", 12, 8),
            new PowerDefinition("spread", "TRIPLE WING ORBS", "pink", 12, 8),
            new PowerDefinition("scatter", "SCATTER BOMBS", "orange", 12, 8),
            new PowerDefinition("mega", "MEGA LASER", "gold", 12, 8),
            new PowerDefinition("fleet", "CALL IN THE FLEET", "cyan", 10, 8),
            new PowerDefinition("assault", "ASSAULT FLEET", "orange", 20, 8)
        };
        public static PowerDefinition Find(string key) { return Array.Find(All, p => p.Key == key); }
        public static string Supply(double roll)
        {
            int total=0, threshold=0;
            foreach(var p in All) total+=p.Weight;
            foreach(var p in All) { threshold+=p.Weight; if(roll*total<threshold) return p.Key; }
            return "mega";
        }
    }

    public static class CanyonTerrain
    {
        public const double DeepTravelRate=.36, MiddleTravelRate=.67, NearTravelRate=1.08;
        public static double LateralOffset(double width,double horizontal,int layer)
        { return width*horizontal*(layer==1?.003:layer==2?.007:.0125); }

        static double Fract(double v) { return v-Math.Floor(v); }
        static double Hash(double i,double phase) { return Fract(Math.Sin(i*127.1+phase*311.7)*43758.5453); }
        static double Segment(double y,double spacing,double phase)
        { double p=y/spacing,i=Math.Floor(p),t=Fract(p); return (Hash(i,phase)*(1-t)+Hash(i+1,phase)*t)*2-1; }
        public static double Wall(double y,double phase) { return Segment(y,6,phase)*.045+Segment(y,2,phase+4)*.014; }
    }
}
