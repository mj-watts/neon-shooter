using System;
using System.Linq;
using NeonShooter.Core;

namespace NeonShooter.Tests
{
    // Shared behavior checks run unchanged under .NET and the Unity Test Runner.
    public static class GameplayChecks
    {
        static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}"); }
        static void Near(double expected, double actual, double tolerance = .001) { if (Math.Abs(expected - actual) > tolerance) throw new Exception($"Expected {expected}, got {actual}"); }
        static void True(bool value) { if (!value) throw new Exception("Condition was false"); }
        static ShooterGame Game() { var g = new ShooterGame(() => .5); g.StartWave(); g.SupplyCooldown = g.GroundCooldown = g.GateCooldown = g.CapitalCooldown = g.CivilianCooldown = 9999; return g; }
        static void Advance(ShooterGame g, double seconds, FlightInput input = null) { for (int i = 0; i < (int)Math.Round(seconds * 120); i++) g.Update(1.0 / 120, input ?? new FlightInput()); }
        public static void Run(Action<string, Action> test)
        {
            test("assault cars are smaller, faster and independently paced", () => {
                var g=Game(); g.CollectPowerup("assault");
                True(g.AssaultFleet.All(car=>car.Speed>=(g.Height+2)/10 && car.Scale<.9));
                Equal(6,g.AssaultFleet.Select(car=>car.Speed).Distinct().Count());
                var car=g.AssaultFleet[0]; car.Y=0; double x=car.X;
                Advance(g,.5); True(Math.Abs(car.Bank)>.001); True(Math.Abs(car.X-x)>.02);
            });
            test("assault cars steer away from a vehicle directly ahead", () => {
                var g=Game(); g.CollectPowerup("assault");
                var car=g.AssaultFleet[0]; car.Y=0;
                var obstacle=g.AssaultFleet[1]; obstacle.Side=car.Side; obstacle.X=car.X; obstacle.Y=1.8; obstacle.Speed=.01;
                double before=Math.Abs(car.X-obstacle.X);
                Advance(g,.2); True(Math.Abs(car.X-obstacle.X)>before+.03);
                True(Math.Abs(car.Bank)<.3);
            });
            test("orbital trails emit blocks and particles, freeze, expire and clear", () => {
                var trails=new OrbitalTrails(); var g=Game(); g.CollectPowerup("orbit");
                for(int i=0;i<60;i++) { g.Update(1.0/120,new FlightInput()); trails.Update(g.Orbs,g.Player,1.0/120); }
                True(trails.Points.Any(p=>p.Particle)); True(trails.Points.Any(p=>!p.Particle));
                True(trails.Points.Count<=OrbitalTrails.Capacity);
                double age=trails.Points[0].Age; trails.Update(g.Orbs,g.Player,0); Near(age,trails.Points[0].Age);
                trails.Update(new Entity[0],g.Player,1); Equal(0,trails.Points.Count);
                trails.Update(g.Orbs,g.Player,.05); trails.Clear(); Equal(0,trails.Points.Count);
            });
            test("ground driving sees the shader's buildings and trees", () => {
                int obstacles=0,clear=0;
                for(int x=-12;x<=12;x++) for(int y=-20;y<=20;y++)
                    if(GroundDriving.SceneryObstacle(x*.7,y*.7)) obstacles++; else clear++;
                True(obstacles>0); True(clear>obstacles);
            });
            test("every browser add-on has a Unity pickup icon", () => {
                Equal(12,PowerCatalog.All.Length);
                foreach(var power in PowerCatalog.All) { True(SpriteCatalog.Icons.ContainsKey(power.Key)); var g=Game(); g.CollectPowerup(power.Key); g.Update(.01,new FlightInput { Fire=true }); }
            });
            test("initial lives, missiles and phase", () => { var g = new ShooterGame(); Equal(3, g.Lives); Equal(3, g.MissileCount); Equal("countdown", g.Phase); });
            test("countdown starts first wave at 3.8 seconds", () => { var g = new ShooterGame(); Advance(g, 3.7); Equal(0, g.Wave); Advance(g, .2); Equal(1, g.Wave); });
            test("pause freezes the simulation", () => { var g = Game(); g.Paused = true; Advance(g, 1); Near(0, g.Clock); });
            test("keyboard movement speed", () => { var g = Game(); Advance(g, .25, new FlightInput { Right = true }); Near(4, g.Player.X); });
            test("diagonal movement is normalized", () => { var g = Game(); double y = g.Player.Y; Advance(g, .25, new FlightInput { Right = true, Up = true }); Near(4, Math.Sqrt(g.Player.X*g.Player.X + Math.Pow(g.Player.Y-y, 2))); });
            test("movement clamps to playfield", () => { var g = Game(); Advance(g, 2, new FlightInput { Right = true, Down = true }); Near(g.BoundX, g.Player.X); Near(g.Bottom, g.Player.Y); });
            test("pointer movement is rate limited", () => { var g = Game(); g.Update(.01, new FlightInput { HasTarget = true, TargetX = 10, TargetY = 0 }); Near(.22, g.Player.X); });
            test("resize clamps player", () => { var g = Game(); g.Player.X = 10; g.SetPlayfield(12, 20); Near(4.9, g.Player.X); });
            test("guns fire a twin volley", () => { var g = Game(); g.Update(.001, new FlightInput { Fire = true }); Equal(2, g.Bullets.Count(b => b.Friendly)); });
            test("missile fires on press edge", () => { var g = Game(); Advance(g, .1, new FlightInput { Missile = true }); Equal(2, g.MissileCount); g.Update(.01, new FlightInput()); g.Update(.01, new FlightInput { Missile = true }); Equal(1, g.MissileCount); });
            test("empty missile stock cannot fire", () => { var g = Game(); g.MissileCount = 0; g.Update(.01, new FlightInput { Missile = true }); Equal(0, g.Bullets.Count); });
            test("machine gun starts at strength two", () => { var g = Game(); g.CollectPowerup("machine"); Equal(2, g.WeaponStrength); Near(12, g.WeaponTime); });
            test("repeated weapons stack to strength three", () => { var g = Game(); for(int i=0;i<5;i++) g.CollectPowerup("laser"); Equal(3, g.WeaponStrength); });
            test("weapon switch resets strength", () => { var g = Game(); g.CollectPowerup("mega"); g.CollectPowerup("laser"); Equal(1, g.WeaponStrength); });
            test("expired weapon returns to guns", () => { var g = Game(); g.CollectPowerup("machine"); g.WeaponTime = .005; g.Update(.01, new FlightInput()); Equal("guns", g.Weapon); Equal(1, g.WeaponStrength); });
            test("idle missiles circle continuously inside the visible screen", () => {
                var g=Game(); g.Enemies.Clear(); g.Phase="intermission"; g.Elapsed=-100;
                g.Update(.001,new FlightInput { Missile=true }); var rocket=g.Bullets.Single();
                Advance(g,4); double min=100,max=0,turn=0;
                double previous=Math.Atan2(rocket.Y,rocket.X);
                for(int frame=0;frame<960;frame++) {
                    g.Update(1.0/120,new FlightInput());
                    True(g.Bullets.Contains(rocket)); True(Math.Abs(rocket.X)<g.Width/2); True(Math.Abs(rocket.Y)<g.Height/2);
                    double radius=Math.Sqrt(rocket.X*rocket.X+rocket.Y*rocket.Y); min=Math.Min(min,radius); max=Math.Max(max,radius);
                    double angle=Math.Atan2(rocket.Y,rocket.X); turn+=Math.Atan2(Math.Sin(angle-previous),Math.Cos(angle-previous)); previous=angle;
                }
                True(max-min<.5); True(Math.Abs(turn)>Math.PI*2);
            });
            test("missiles reach turrets on both outer scenery shelves", () => {
                foreach(double width in new[]{24.0,51.2}) foreach(int side in new[]{-1,1}) {
                    var g=Game(); g.SetPlayfield(width,32); g.Enemies.Clear(); g.Phase="intermission"; g.Elapsed=-100; g.ScenerySpeed=0;
                    g.SpawnGroundEnemy("turret"); var turret=g.GroundEnemies.Single(); turret.Side=side; turret.Y=0; turret.Cooldown=999;
                    g.Update(.001,new FlightInput { Missile=true });
                    Advance(g,4); True(!g.GroundEnemies.Contains(turret));
                }
            });
            test("missiles wait for a visible enemy then acquire it", () => {
                var g=Game(); g.SetPlayfield(24,20); g.Enemies.Clear(); g.Phase="intermission"; g.Elapsed=-100; g.ScenerySpeed=0;
                g.SpawnGroundEnemy("turret"); var turret=g.GroundEnemies.Single(); turret.Y=11; turret.Cooldown=999;
                g.Update(.001,new FlightInput { Missile=true }); var rocket=g.Bullets.Single();
                Advance(g,3); True(g.GroundEnemies.Contains(turret)); True(Math.Abs(rocket.Y)<g.Height/2);
                turret.Y=0; Advance(g,4); True(!g.GroundEnemies.Contains(turret));
            });
            test("missiles hit visible ground targets above the old fixed height", () => {
                var g=Game(); g.SetPlayfield(51.2,48); g.Enemies.Clear(); g.Phase="intermission"; g.Elapsed=-100; g.ScenerySpeed=0;
                g.SpawnGroundEnemy("turret"); var turret=g.GroundEnemies.Single(); turret.Y=20; turret.Cooldown=999;
                g.Update(.001,new FlightInput());
                g.Bullets.Add(new Entity { Kind="missile",Friendly=true,X=turret.X,Y=19.5,Vy=18,Life=double.PositiveInfinity });
                Advance(g,.2); True(!g.GroundEnemies.Contains(turret));
            });
            test("missiles are capped at ten", () => { var g = Game(); for(int i=0;i<20;i++) g.CollectPowerup("missile"); Equal(10, g.MissileCount); });
            test("invalid pickup ignored", () => { var g = Game(); g.CollectPowerup("unknown"); Equal("guns", g.Weapon); });
            test("shield prevents regular damage", () => { var g = Game(); g.CollectPowerup("shield"); g.DamagePlayer(); Equal(3, g.Lives); });
            test("laser gate pierces shield", () => { var g = Game(); g.CollectPowerup("shield"); g.DamagePlayer(true); Equal(2, g.Lives); });
            test("invulnerability prevents double damage", () => { var g = Game(); g.DamagePlayer(); g.DamagePlayer(true); Equal(2, g.Lives); });
            test("hit clears hostile bullets only", () => { var g = Game(); g.Bullets.Add(new Entity { Friendly = true }); g.Bullets.Add(new Entity()); g.DamagePlayer(); Equal(1, g.Bullets.Count); True(g.Bullets[0].Friendly); });
            test("orbital guard stacks to three", () => { var g = Game(); for(int i=0;i<5;i++) g.CollectPowerup("orbit"); Equal(3, g.OrbitCount); g.DamagePlayer(); Equal(3, g.Lives); });
            test("orbital guard expires", () => { var g = Game(); g.CollectPowerup("orbit"); g.OrbitTime = .005; g.Update(.01, new FlightInput()); Equal(0, g.OrbitCount); });
            test("wingmen spawn two support orbs", () => { var g = Game(); g.CollectPowerup("wingmen"); g.Update(.01, new FlightInput()); Equal(2, g.Companions.Count); });
            test("spread support fires six shots", () => { var g = Game(); g.CollectPowerup("spread"); g.Update(.01, new FlightInput { Fire = true }); Equal(6, g.Bullets.Count(b => b.Kind == "companion")); });
            test("scatter emits five bombs", () => { var g = Game(); g.CollectPowerup("scatter"); g.Update(.01, new FlightInput { Fire = true }); Equal(5, g.Bullets.Count(b => b.Kind == "bomb")); });
            test("fleet has four distinct formation slots", () => { var g = Game(); g.CollectPowerup("fleet"); Equal(4, g.Fleet.Count); Equal(4, g.Fleet.Select(s => s.Side).Distinct().Count()); });
            test("assault fleet has six vehicles", () => { var g = Game(); g.CollectPowerup("assault"); Equal(6, g.AssaultFleet.Count); Near(20, g.AssaultTime); });
            test("speed boost increases scenery and movement", () => { var g = Game(); g.CollectPowerup("speed"); Advance(g, .7); True(g.SceneryVelocity > 8); });
            test("laser weapon creates two beams", () => { var g = Game(); g.CollectPowerup("laser"); g.Update(.01, new FlightInput { Fire = true }); Equal(2, g.Lasers.Count); });
            test("mega weapon creates wide beam", () => { var g = Game(); g.CollectPowerup("mega"); g.Update(.01, new FlightInput { Fire = true }); Equal(1, g.Lasers.Count); Near(1.2, g.Lasers[0].Width); });
            test("kills drop score stars without awarding score", () => { var g = Game(); var e = g.Enemies[0]; g.DestroyEnemy(e, true); Equal(1, g.Kills); Equal(0, g.Score); Equal(1, g.Pickups.Count); });
            test("heavy ship drops a life pickup", () => { var g = Game(); g.StartWave(); var e = g.Enemies.First(s => s.Heavy); g.DestroyEnemy(e, true); True(g.Pickups.Any(p => p.Kind == "life")); });
            test("escaping does not count as a kill", () => { var g = Game(); g.DestroyEnemy(g.Enemies[0], false); Equal(0, g.Kills); Equal(1, g.Escaped); });
            test("perfect wave drops ten point star", () => { var g = Game(); foreach(var e in g.Enemies.ToArray()) g.DestroyEnemy(e, true); g.Update(.01, new FlightInput()); Equal("intermission", g.Phase); True(g.Pickups.Any(p => p.Value == 10)); });
            test("imperfect wave gives no bonus star", () => { var g = Game(); foreach(var e in g.Enemies.ToArray()) g.DestroyEnemy(e, false); g.Update(.01, new FlightInput()); True(!g.Pickups.Any(p => p.Value == 10)); });
            test("intermission starts next wave", () => { var g = Game(); g.Enemies.Clear(); g.Update(.01, new FlightInput()); Advance(g, 3.1); Equal(2, g.Wave); });
            test("score pickup awards its value", () => { var g = Game(); g.Pickups.Add(new Entity { X=g.Player.X, Y=g.Player.Y, Value=10 }); g.Update(.001, new FlightInput()); Equal(10, g.Score); });
            test("life pickup restores one life", () => { var g = Game(); g.Lives=2; g.Pickups.Add(new Entity { X=g.Player.X, Y=g.Player.Y, Kind="life" }); g.Update(.001, new FlightInput()); Equal(3, g.Lives); });
            test("life pickup cannot exceed three", () => { var g = Game(); g.Pickups.Add(new Entity { X=g.Player.X, Y=g.Player.Y, Kind="life" }); g.Update(.001, new FlightInput()); Equal(3, g.Lives); });
            test("civilian hits incur 1000 point penalty and cooldown", () => { var g = Game(); g.SpawnCivilian(); var c = g.Civilians[0]; c.Y=0; g.HitCivilian(c); g.HitCivilian(c); Equal(-1000, g.Score); Near(4, c.Hp); });
            test("civilian safe passage awards 10000", () => { var g = Game(); g.SpawnCivilian(); g.Civilians[0].Y=-17.999; g.Update(.01, new FlightInput()); Equal(10000, g.Score); Equal(1, g.Rescues); });
            test("capital ship has six destructible turrets", () => { var g = Game(); g.SpawnCapitalShip(); Equal(6, g.CapitalShips[0].Turrets.Count); });
            test("capital ship schedules 100 invaders", () => { var g = Game(); g.SpawnCapitalShip(); g.StartWave(); Equal(100, g.Enemies.Count); True(g.Enemies.All(e=>e.Invader)); });
            test("laser gate requires both emitters", () => { var g = Game(); g.SpawnLaserGate(); Equal(1, g.GateLasers.Count); g.DestroyGroundEnemy(g.GroundEnemies[0]); Equal(0, g.GateLasers.Count); });
            test("death finishes with crash and gameover", () => { var g = Game(); g.Lives=1; g.DamagePlayer(); Equal("dying", g.Phase); Advance(g, 2.5); Equal("crashed", g.Phase); Advance(g, 1); Equal("gameover", g.Phase); });
            test("reset clears score and restores stock", () => { var g = Game(); g.Score=500; g.CollectPowerup("fleet"); g.Reset(); Equal(0,g.Score); Equal(0,g.Fleet.Count); Equal(3,g.Lives); });
            test("simulation accumulator caps frame delta", () => { var a=Game(); var b=Game(); a.Advance(5,new FlightInput { Right=true }); b.Advance(.1,new FlightInput { Right=true }); Near(a.Player.X,b.Player.X); });
            test("simulation is consistent at 30 and 60 fps", () => { var a=Game(); var b=Game(); for(int i=0;i<30;i++) a.Advance(1.0/30,new FlightInput { Right=true }); for(int i=0;i<60;i++) b.Advance(1.0/60,new FlightInput { Right=true }); Near(a.Player.X,b.Player.X); Near(a.Clock,b.Clock); });
            test("tanks and every ground turret spawn and remain on the top scenery shelf", () => {
                foreach(int side in new[]{-1,1})
                {
                    var g=new ShooterGame(()=>side<0?.25:.75); g.StartWave();
                    g.GroundCooldown=g.GateCooldown=g.CapitalCooldown=g.CivilianCooldown=9999; g.Player.Invulnerable=100;
                    g.SpawnGroundEnemy("tank"); g.SpawnGroundEnemy("turret"); g.SpawnLaserGate();
                    foreach(double seconds in new[]{0.0,.5,2.0})
                    {
                        Advance(g,seconds);
                        foreach(var e in g.GroundEnemies)
                        {
                            double x=e.Side*(e.X+CanyonTerrain.LateralOffset(g.Width,g.Player.X,3))/(g.Width*.5);
                            double edge=.79+CanyonTerrain.Wall(e.Y+g.GroundTravel*CanyonTerrain.NearTravelRate,e.Side*.7+4);
                            True(x>edge && x<1);
                        }
                    }
                }
            });
            test("turrets and tanks follow the top canyon shelf lateral shift", () => {
                var g=Game(); g.SpawnGroundEnemy("turret"); g.SpawnGroundEnemy("tank"); g.Player.Invulnerable=100;
                g.Update(.001,new FlightInput()); var before=g.GroundEnemies.Select(e=>e.X).ToArray();
                g.Player.X=6; g.Update(.001,new FlightInput());
                for(int i=0;i<before.Length;i++) Near(-g.Width*6*.0125,g.GroundEnemies[i].X-before[i],.01);
            });
            test("fixed ground installations remain on the same scrolling surface", () => {
                var g=Game(); g.SpawnGroundEnemy("turret"); g.Player.Invulnerable=100;
                g.Update(.001,new FlightInput()); var turret=g.GroundEnemies[0]; double surface=turret.Y+g.GroundTravel*CanyonTerrain.NearTravelRate;
                Advance(g,.5); Near(surface,turret.Y+g.GroundTravel*CanyonTerrain.NearTravelRate);
            });
            test("assault vehicles follow the nearest canyon shelf lateral shift", () => {
                var g=Game(); g.CollectPowerup("assault"); g.Update(.001,new FlightInput()); var before=g.AssaultFleet.Select(e=>e.X).ToArray();
                g.Player.X=6; g.Update(.001,new FlightInput());
                for(int i=0;i<before.Length;i++) Near(-g.Width*6*.0125,g.AssaultFleet[i].X-before[i],.01);
            });
            test("laser gates and wrecks share the installation lateral shift", () => {
                var g=Game(); g.Player.Invulnerable=100; g.SpawnLaserGate(); g.Update(.001,new FlightInput());
                double beamX=g.GateLasers[0].X; var left=g.GroundEnemies[0];
                g.Player.X=6; g.Update(.001,new FlightInput()); Near(-g.Width*6*.0125,g.GateLasers[0].X-beamX,.01);
                g.DestroyGroundEnemy(left); double wreckX=left.X;
                g.Player.X=-4; g.Update(.001,new FlightInput()); Near(g.Width*10*.0125,left.X-wreckX,.01);
            });
            test("all twelve supply types are reachable", () => { var powers=Enumerable.Range(0,10000).Select(i=>PowerCatalog.Supply(i/10000.0)).Distinct().Count(); Equal(12,powers); });
        }
    }
}
