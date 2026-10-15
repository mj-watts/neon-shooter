using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NeonShooter.Core;
public static class BrowserParity
{
    static void Near(double a,double b,string label) { if(Math.Abs(a-b)>.000001) throw new Exception(label+": "+a+" != "+b); }
    public static int Check()
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures/browser-reference.json")));
        int count=0;
        foreach(var scenario in doc.RootElement.GetProperty("rules").EnumerateArray())
        {
            var g=new ShooterGame(()=>.5);
            foreach(var action in scenario.GetProperty("actions").EnumerateArray()) g.CollectPowerup(action.GetString());
            var e=scenario.GetProperty("expected");
            var pairs=new (string,double)[]{("wave",g.Wave),("score",g.Score),("kills",g.Kills),("lives",g.Lives),("missiles",g.MissileCount),("strength",g.WeaponStrength),("weaponTime",g.WeaponTime),("shield",g.ShieldTime),("speed",g.SpeedTime),("orbits",g.OrbitCount),("orbitTime",g.OrbitTime),("companionTime",g.CompanionTime),("scatterTime",g.ScatterTime),("fleet",g.Fleet.Count),("fleetTime",g.FleetTime),("assault",g.AssaultFleet.Count),("assaultTime",g.AssaultTime)};
            foreach(var pair in pairs) Near(e.GetProperty(pair.Item1).GetDouble(),pair.Item2,pair.Item1);
            if(e.GetProperty("phase").GetString()!=g.Phase || e.GetProperty("weapon").GetString()!=g.Weapon || e.GetProperty("companionMode").GetString()!=g.CompanionMode) throw new Exception("Browser state differs");
            count++;
        }
        foreach(var sample in doc.RootElement.GetProperty("canyon").EnumerateArray())
        { Near(sample.GetProperty("expected").GetDouble(),CanyonTerrain.Wall(sample.GetProperty("y").GetDouble(),sample.GetProperty("phase").GetDouble()),"canyon"); count++; }
        foreach(var power in doc.RootElement.GetProperty("powers").EnumerateObject())
        {
            var p=PowerCatalog.Find(power.Name); if(p==null) throw new Exception("Missing "+power.Name);
            Near(power.Value.GetProperty("weight").GetDouble(),p.Weight,p.Key+" weight");
            if(power.Value.TryGetProperty("duration",out var duration)) Near(duration.GetDouble(),p.Duration,p.Key+" duration");
            if(power.Value.GetProperty("label").GetString()!=p.Label || power.Value.GetProperty("color").GetString()!=p.Color) throw new Exception("Power metadata differs: "+p.Key);
            count++;
        }
        Console.WriteLine($"PASS {count} browser-reference parity scenarios"); return count;
    }
}
