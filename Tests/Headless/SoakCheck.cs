using System;
using System.Linq;
using NeonShooter.Core;
public static class SoakCheck
{
    public static void Run()
    {
        var rng=new Random(42017); var g=new ShooterGame(rng.NextDouble); g.SetPlayfield(48,32); g.StartWave();
        g.Player.Invulnerable=10000;
        int highestBullets=0,highestEnemies=0;
        for(int frame=0;frame<36000;frame++)
        {
            if(frame%1200==0) foreach(var p in PowerCatalog.All) g.CollectPowerup(p.Key);
            g.Update(1.0/120,new FlightInput { Fire=true,Missile=frame%180==0,HasTarget=true,TargetX=Math.Sin(frame/600.0)*15,TargetY=-9+Math.Sin(frame/1200.0)*2 });
            highestBullets=Math.Max(highestBullets,g.Bullets.Count); highestEnemies=Math.Max(highestEnemies,g.Enemies.Count);
            foreach(var e in g.Enemies.Concat(g.Bullets).Concat(g.Civilians).Concat(g.GroundEnemies).Concat(g.Pickups).Concat(g.Fleet).Concat(g.AssaultFleet))
                if(double.IsNaN(e.X) || double.IsInfinity(e.X) || double.IsNaN(e.Y) || double.IsInfinity(e.Y)) throw new Exception("Non-finite entity position after "+frame+" steps");
            if(g.Bullets.Count>1500 || g.Pickups.Count>1000 || g.Enemies.Count>100 || g.FallingShips.Count>200) throw new Exception("Encounter data grew beyond expected bounds");
        }
        if(g.Wave<3 || g.Kills==0) throw new Exception("Sustained flight did not progress");
        Console.WriteLine($"PASS five-minute simulated flight: wave {g.Wave}, {g.Kills} kills, peaks {highestBullets} projectiles / {highestEnemies} enemies");
    }
}
