using System;
using System.Collections.Generic;
using System.Linq;

namespace NeonShooter.Core
{
    /// <summary>Native C# port of the browser simulation; no Unity or JavaScript runtime required.</summary>
    public sealed class ShooterGame
    {
        readonly Func<double> random;
        public event Action<GameEvent> OnEvent;
        public string Phase, Weapon, CompanionMode, FleetRadioMessage;
        public bool Paused, MissilePressed, InvaderWavePending;
        public int Wave, Score, Kills, Lives, MissileCount, WeaponStrength, OrbitCount, Rescues, Escaped, WaveKills, WaveSize;
        public double Elapsed, Clock, FireCooldown, WeaponTime, ShieldTime, SpeedTime, SpeedBlend;
        public double OrbitTime, CompanionTime, CompanionCooldown, ScatterTime, ScatterCooldown;
        public double FleetTime, FleetRadioTime, FleetCooldown, AssaultTime, AssaultCooldown;
        public double SupplyCooldown, CivilianCooldown, CapitalCooldown, GroundCooldown, GateCooldown;
        public double Width = 24, Height = 32, BoundX = 10.5, Bottom = -13, Top = 12.5, GroundTravel;
        public double ScenerySpeed = 3.6;
        public Entity Player = new Entity();
        public readonly List<Entity> Enemies = new List<Entity>(), Bullets = new List<Entity>(), Pickups = new List<Entity>();
        public readonly List<Entity> Civilians = new List<Entity>(), FallingShips = new List<Entity>();
        public readonly List<Entity> GroundEnemies = new List<Entity>(), GroundWrecks = new List<Entity>(), CapitalShips = new List<Entity>();
        public readonly List<Entity> Orbs = new List<Entity>(), Companions = new List<Entity>(), Fleet = new List<Entity>(), AssaultFleet = new List<Entity>();
        public readonly List<Beam> Lasers = new List<Beam>(), GateLasers = new List<Beam>(), TurretLasers = new List<Beam>();
        readonly List<Entity> fleetHistory = new List<Entity>();
        int serial, groundEncounter, gateTutorialCount;
        double fleetFollowTime, lastKillX, lastKillY;
        Entity playerFall;
        public ShooterGame(Func<double> random = null) { var rng=new Random(); this.random=random ?? rng.NextDouble; Reset(); }
        public static double Clamp(double n,double min,double max) { return Math.Max(min,Math.Min(max,n)); }
        static double Distance(Entity a,Entity b) { return Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y)); }
        static double Decay(double n,double dt) { return Math.Max(0,n-dt); }
        public double SpeedIntensity { get { return SpeedBlend*SpeedBlend*(3-2*SpeedBlend); } }
        public double SceneryVelocity { get { return ScenerySpeed*(1+SpeedIntensity*1.35); } }
        public bool SceneryStopped { get { return Phase=="dying" || Phase=="crashed" || Phase=="gameover"; } }
        public double SceneryMotion { get { return Phase=="dying" ? Math.Pow(Math.Max(0,1-Elapsed/1.2),2) : SceneryStopped ? 0 : 1; } }
        public string Message { get { return Phase=="countdown" ? (Elapsed<.8 ? "3" : Math.Max(1,3-(int)Math.Floor(Elapsed-.8)).ToString()) : Phase=="intermission" ? "NEXT WAVE IN " + Math.Max(1,(int)Math.Ceiling(3-Elapsed)) : ""; } }
        void Emit(string type,Entity at=null,double size=1,string color="orange",string power=null,bool perfect=false,bool fatal=false)
        { OnEvent?.Invoke(new GameEvent { Type=type,X=at?.X??0,Y=at?.Y??0,Size=size,Color=color,Power=power,Perfect=perfect,Fatal=fatal }); }
        public void Reset()
        {
            Phase="countdown"; Paused=false; Elapsed=Clock=GroundTravel=0; Wave=Score=Kills=Rescues=WaveKills=Escaped=serial=groundEncounter=gateTutorialCount=0;
            Lives=3; MissileCount=3; Weapon="guns"; WeaponStrength=1; CompanionMode="wingmen"; FleetRadioMessage="";
            FireCooldown=WeaponTime=ShieldTime=SpeedTime=SpeedBlend=OrbitTime=CompanionTime=CompanionCooldown=ScatterTime=ScatterCooldown=0;
            FleetTime=FleetRadioTime=FleetCooldown=AssaultTime=AssaultCooldown=fleetFollowTime=0; OrbitCount=0;
            MissilePressed=InvaderWavePending=false; playerFall=null; lastKillX=0; lastKillY=3;
            SupplyCooldown=2.5+random()*1.5; CivilianCooldown=30+random()*15; CapitalCooldown=22+random()*10; GroundCooldown=5+random()*3; GateCooldown=12;
            foreach(var list in new[] { Enemies,Bullets,Pickups,Civilians,FallingShips,GroundEnemies,GroundWrecks,CapitalShips,Orbs,Companions,Fleet,AssaultFleet,fleetHistory }) list.Clear();
            Lasers.Clear(); GateLasers.Clear(); TurretLasers.Clear();
            Player=new Entity { X=0,Y=Clamp(-10.5,Bottom,Top),Z=1 };
        }
        public void SetPlayfield(double width,double height)
        {
            Width=Math.Max(4,width); Height=Math.Max(4,height); BoundX=Width/2-1.1; Bottom=-Height/2+1.1; Top=Height/2-1.1;
            Player.X=Clamp(Player.X,-BoundX,BoundX); Player.Y=Clamp(Player.Y,Bottom,Top);
        }
        public void StartWave()
        {
            Wave++; Phase="playing"; Elapsed=0; WaveKills=Escaped=0; Enemies.Clear();
            if(InvaderWavePending)
            {
                InvaderWavePending=false; WaveSize=100; double spacing=BoundX*1.85/19;
                for(int i=0;i<100;i++) Enemies.Add(new Entity { Id=++serial,Invader=true,Column=i%20,Row=i/20,Origin=(i%20-9.5)*spacing,X=(i%20-9.5)*spacing,Y=Height/2+2+i/20*Math.Min(2,Height*.06),Scale=Math.Min(.7,spacing/1.8),Variant=i/20%3,FireAt=5+i%20*.24 });
            }
            else
            {
                int columns=4+(int)(random()*7); WaveSize=columns*3;
                for(int i=0;i<WaveSize;i++)
                {
                    int kind=(Wave-1)%3;
                    if(i>=(int)Math.Ceiling(WaveSize/2.0) && Wave%2==0) kind=(kind+1)%3;
                    var e=new Entity { Id=++serial,X=(i%columns-(columns-1)/2.0)*1.65,Y=Height/2+2+i/columns*2.2,Origin=(i%columns-(columns-1)/2.0)*1.65,Row=i/columns,Pattern=(Wave-1)%4,Variant=kind,Hp=1+Math.Floor(Math.Min(Wave-1,8)/3.0),FireAt=2.1+i%4*.48 };
                    if(Wave%3==2 && i==WaveSize/2) { e.Heavy=true; e.Variant=2; e.Hp=12+Math.Floor(Wave*.6); }
                    Enemies.Add(e);
                }
            }
            Emit("wave");
        }
        public void DestroyEnemy(Entity e,bool credited)
        {
            if(!Enemies.Remove(e)) return;
            if(!credited) { Escaped++; return; }
            Kills++; WaveKills++; lastKillX=e.X; lastKillY=Clamp(e.Y,-10,10);
            Pickups.Add(new Entity { Id=++serial,X=e.X,Y=e.Y });
            if(e.Heavy) Pickups.Add(new Entity { Id=++serial,Kind="life",X=e.X+.6,Y=e.Y });
            bool falling=e.Heavy || Enemies.Count==0;
            double roll=falling ? 1 : random();
            if(falling || roll<.28)
            {
                e.FallX=e.X; e.FallY=e.Y; e.FallAge=0; e.FallDuration=falling?4:1.4; e.Kind=falling?"enemyFall":"flyaway"; e.Side=roll<.14?-1:1;
                FallingShips.Add(e); Emit(falling?"enemyFalling":"enemyFlyaway",e,e.Heavy?4.8:1.5);
            }
            else Emit("explosion",e,1.5,e.Variant==1?"pink":"orange");
        }
        public void SpawnSupply()
        {
            string power=PowerCatalog.Supply(random());
            Pickups.Add(new Entity { Id=++serial,Kind="powerup",Power=power,X=(random()-.5)*14,Y=14 });
            SupplyCooldown=7+random()*4; Emit("supply",power:power);
        }
        public void SpawnCivilian()
        {
            if(Civilians.Count>0) { CivilianCooldown=2; return; }
            double origin=(random()-.5)*14;
            Civilians.Add(new Entity { Id=++serial,Kind="civilian",X=origin,Origin=origin,Y=18,Hp=5 });
            CivilianCooldown=45+random()*20; Emit("civilianIncoming");
        }
        public void HitCivilian(Entity c)
        {
            if(c.HitCooldown>0 || Math.Abs(c.Y)>16 || !Civilians.Contains(c)) return;
            c.Hp--; c.HitCooldown=.35; Score-=1000; Emit("civilianHit",c,fatal:c.Hp<=0);
            if(c.Hp==1) { c.AlarmCooldown=.9; Emit("civilianAlarm",c); }
            if(c.Hp<=0) { Civilians.Remove(c); c.Kind="civilianFall"; c.FallAge=0; c.FallX=c.X; c.FallY=c.Y; FallingShips.Add(c); Emit("civilianKilled",c); }
        }
        public void SpawnCapitalShip()
        {
            if(CapitalShips.Count>0) return;
            InvaderWavePending=true;
            double width=Math.Min(14,Width*.8);
            var s=new Entity { Id=++serial,Kind="capital",X=(random()-.5)*Math.Max(0,Width-width)*.35,Y=Height/2+14,Scale=width/14 };
            for(int row=0;row<3;row++) for(int side=-1;side<=1;side+=2) s.Turrets.Add(new Entity { Id=++serial,X=side*width*.28,Y=-6+row*6,Hp=4,Cooldown=.8+row*.4+(side+1)/2.0*.25 });
            CapitalShips.Add(s); Emit("capitalIncoming");
        }
        IEnumerable<Entity> CapitalTargets()
        {
            foreach(var ship in CapitalShips) foreach(var t in ship.Turrets) if(!t.Destroyed)
                yield return new Entity { Id=t.Id,X=ship.X+t.X,Y=ship.Y+t.Y,Age=ship.Age,Turrets=new List<Entity>{t} };
        }
        void DamageCapital(Entity target,double damage)
        {
            var t=target.Turrets[0]; if(t.Destroyed) return;
            t.Hp=Math.Max(0,t.Hp-damage);
            if(t.Hp==0) { t.Destroyed=true; Emit("explosion",target,1.1); }
        }
        public void SpawnGroundEnemy(string kind=null)
        {
            if(GroundEnemies.Count>=3) { GroundCooldown=2; return; }
            kind=kind??(++groundEncounter%3==0?"tank":"turret"); int side=random()<.5?-1:1;
            GroundEnemies.Add(new Entity { Id=++serial,Kind=kind,Side=side,X=GroundLane(side,Height/2+2,kind,0),Y=Height/2+2,Hp=kind=="tank"?1:5,Cooldown=1.1 });
            GroundCooldown=8+random()*5; Emit("groundIncoming");
        }
        public void SpawnLaserGate()
        {
            if(GroundEnemies.Any(e=>e.GateId!=0)) { GateCooldown=3; return; }
            int id=++serial;
            for(int side=-1;side<=1;side+=2) GroundEnemies.Add(new Entity { Id=++serial,GateId=id,Kind="laserTurret",Side=side,X=GroundLane(side,Height/2+2,"laserTurret",0),Y=Height/2+2,Hp=5,Aim=-side*Math.PI/2 });
            GateCooldown=20+random()*8; RefreshGates();
        }
        void RefreshGates()
        {
            GateLasers.Clear();
            foreach(var left in GroundEnemies.Where(e=>e.GateId!=0 && e.Side==-1))
            {
                var right=GroundEnemies.Find(e=>e.GateId==left.GateId && e.Side==1);
                if(right!=null) GateLasers.Add(new Beam { Id=left.GateId,X=left.X,EndX=right.X,Y=left.Y,Top=right.Y,Width=1.2,Color="red" });
            }
        }
        public void DestroyGroundEnemy(Entity e)
        {
            if(!GroundEnemies.Remove(e)) return;
            if(e.GateId!=0) { e.Destroyed=true; GroundWrecks.Add(e); RefreshGates(); }
            TurretLasers.RemoveAll(b=>b.Id==e.Id);
            Pickups.Add(new Entity { Id=++serial,X=e.X,Y=e.Y,Value=e.Kind=="tank"?3:1 });
            Emit(e.GateId!=0?"gateExplosion":"groundExplosion",e,e.Kind=="tank"?3.8:2.2);
        }
        public void CollectPowerup(string power)
        {
            if(PowerCatalog.Find(power)==null) return;
            if(power=="machine" || power=="laser" || power=="mega")
            {
                WeaponStrength=power=="mega"?3:Weapon==power && WeaponTime>0?Math.Min(3,WeaponStrength+1):power=="machine"?2:1;
                Weapon=power; WeaponTime=12; FireCooldown=0;
            }
            else if(power=="missile") MissileCount=Math.Min(10,MissileCount+1);
            else if(power=="shield") ShieldTime=8;
            else if(power=="speed") SpeedTime=15;
            else if(power=="orbit") { OrbitCount=Math.Min(3,OrbitCount+1); OrbitTime=10; }
            else if(power=="wingmen" || power=="spread") { CompanionMode=power; CompanionTime=12; CompanionCooldown=0; }
            else if(power=="scatter") { ScatterTime=12; ScatterCooldown=0; }
            else if(power=="fleet")
            {
                FleetTime=10; FleetCooldown=fleetFollowTime=0; FleetRadioTime=3; FleetRadioMessage="Roger, on way!"; Fleet.Clear(); fleetHistory.Clear(); fleetHistory.Add(new Entity { Age=0,X=Player.X,Y=Player.Y,Bank=Player.Bank });
                foreach(int side in new[]{-2,-1,1,2}) Fleet.Add(new Entity { Id=++serial,Side=side,FollowDelay=.08*Math.Abs(side)+(side>0?.02:0),X=Player.X+side*2.4,Y=Math.Min(Bottom-4,Player.Y-5),EntryY=Math.Min(Bottom-4,Player.Y-5),MissileCooldown=(side+2)*.15 });
            }
            else if(power=="assault")
            {
                AssaultTime=20; AssaultCooldown=0; AssaultFleet.Clear();
                foreach(int side in new[]{-3,-2,-1,1,2,3})
                { double y=-Height/2-1-(Math.Abs(side)-1)*2.4; AssaultFleet.Add(new Entity { Id=++serial,Kind="cart",Side=Math.Sign(side),Y=y,X=AssaultLane(Math.Sign(side),y),MissileCooldown=(side+3)*.08,Speed=(Height+2)/8*(1+side*.035),Scale=.8,Origin=CanyonTerrain.LateralOffset(Width,Player.X,3) }); }
            }
            Emit("powerup",Player,power:power);
        }
        public void DamagePlayer(bool pierceDefenses=false)
        {
            if(Lives<=0 || Player.Invulnerable>0 || (!pierceDefenses && (ShieldTime>0 || OrbitTime>0))) return;
            Lives--; Player.Invulnerable=2; Bullets.RemoveAll(b=>!b.Friendly); Emit("hit",Player,2.4,fatal:Lives==0);
            if(Lives>0) return;
            Phase="dying"; Elapsed=0; Player.Invulnerable=0; playerFall=new Entity { X=Player.X,Y=Player.Y,Bank=Player.Bank };
            Bullets.Clear(); Lasers.Clear(); GateLasers.Clear(); TurretLasers.Clear(); Orbs.Clear(); Companions.Clear();
            OrbitTime=CompanionTime=ShieldTime=SpeedTime=SpeedBlend=0; OrbitCount=MissileCount=0;
            Enemies.RemoveAll(e=>e.Age<0 || e.Y>Height/2+1); Emit("playerFalling",Player,2.4);
        }
        Entity Shot(double x,double y,double vx,double vy,string kind="",double life=1.5,bool friendly=true,double damage=1)
        {
            var b=new Entity { Id=++serial,X=x,Y=y,Vx=vx,Vy=vy,Kind=kind,Life=life,Friendly=friendly,Damage=damage,Strength=damage };
            Bullets.Add(b); return b;
        }
        bool VisibleMissileTarget(Entity e) { return !e.Destroyed && e.Age>=0 && Math.Abs(e.X)<=Width/2 && Math.Abs(e.Y)<=Height/2; }
        void Blast(Entity b,double radius,bool missile)
        {
            if(b.Exploded) return;
            b.Exploded=true; b.Life=-1; Emit(missile?"missileExplosion":"scatterExplosion",b,missile?3.4:1.7);
            foreach(var e in Enemies.ToArray()) if(e.Age>=0 && (!missile || VisibleMissileTarget(e)) && Distance(e,b)<radius) DestroyEnemy(e,true);
            foreach(var e in GroundEnemies.ToArray()) if((missile?VisibleMissileTarget(e):Math.Abs(e.Y)<=16) && Distance(e,b)<radius) DestroyGroundEnemy(e);
            foreach(var e in CapitalTargets()) if((!missile || VisibleMissileTarget(e)) && Math.Abs(e.Y)<=Height/2 && Distance(e,b)<radius) DamageCapital(e,e.Turrets[0].Hp);
            foreach(var shot in Bullets) if(!shot.Friendly && Distance(shot,b)<radius) shot.Life=-1;
            if(!missile)
            {
                var c=Civilians.Find(e=>Math.Abs(e.Y)<=16 && Distance(e,b)<radius); if(c!=null) HitCivilian(c);
                for(int i=0;i<8;i++) { double a=i/8.0*Math.PI*2; Shot(b.X,b.Y,Math.Sin(a)*16,Math.Cos(a)*16,"shrapnel",.7); }
            }
        }
        public static bool LaserHits(Beam b,Entity e,double radius)
        {
            return b.Top>b.Y && e.Y>=b.Y && e.Y<=b.Top && Math.Abs(e.X-(b.X+(e.Y-b.Y)*(b.EndX-b.X)/(b.Top-b.Y)))<radius+b.Width/2;
        }
        double AssaultLane(int side,double y) { return side*Width*.5*((.79+CanyonTerrain.Wall(y+GroundTravel*CanyonTerrain.NearTravelRate,side*.7+4)+1)/2)-CanyonTerrain.LateralOffset(Width,Player.X,3); }
        void SupportMissile(Entity s,string source,double remaining,double interval,double dt,double angle=0)
        {
            s.MissileCooldown-=dt; if(s.MissileCooldown>1e-8) return; s.MissileCooldown=interval;
            Shot(s.X,s.Y+.8,Math.Sin(angle)*18,Math.Cos(angle)*18,"missile",Math.Min(3,remaining)).Support=source; Emit("missileLaunch",s);
        }
        void UpdateAddons(double dt,bool firing)
        {
            OrbitTime=Decay(OrbitTime,dt); CompanionTime=Decay(CompanionTime,dt); ScatterTime=Decay(ScatterTime,dt);
            if(OrbitTime==0) OrbitCount=0;
            Orbs.Clear(); Companions.Clear();
            for(int i=0;i<OrbitCount;i++) { double a=Clock*7+i/(double)OrbitCount*Math.PI*2; Orbs.Add(new Entity { Id=-10-i,X=Player.X+Math.Cos(a)*1.8,Y=Player.Y+Math.Sin(a)*1.8 }); }
            if(CompanionTime>0) for(int side=-1;side<=1;side+=2) Companions.Add(new Entity { Id=-20+side,X=Player.X+side*1.7,Y=Player.Y+.2 });
            CompanionCooldown-=dt; ScatterCooldown-=dt;
            if(firing && Companions.Count>0 && CompanionCooldown<=0)
            {
                CompanionCooldown=.16;
                foreach(var s in Companions) foreach(int degrees in CompanionMode=="spread"?new[]{-2,0,2}:new[]{0}) { double a=degrees*Math.PI/180; Shot(s.X,s.Y+.4,Math.Sin(a)*42,Math.Cos(a)*42,"companion",Math.Max(1.5,Height/42+.2)); }
                Emit("shot",Player);
            }
            if(firing && ScatterTime>0 && ScatterCooldown<=0)
            {
                ScatterCooldown=.8;
                foreach(int degrees in new[]{-40,-20,0,20,40}) { double a=degrees*Math.PI/180; Shot(Player.X,Player.Y+.9,Math.Sin(a)*12,Math.Cos(a)*12,"bomb",1.2).Fuse=.65; }
                Emit("missileLaunch",Player);
            }
            double remaining=FleetTime; FleetTime=Decay(FleetTime,dt); FleetRadioTime=Decay(FleetRadioTime,dt);
            if(FleetTime<1e-8) FleetTime=0;
            if(remaining>0 && FleetTime==0 && Fleet.Count>0) { FleetRadioMessage="Good job guys!"; FleetRadioTime=3; }
            if(Fleet.Count>0)
            {
                fleetFollowTime+=dt; fleetHistory.Add(new Entity { Age=fleetFollowTime,X=Player.X,Y=Player.Y,Bank=Player.Bank });
                while(fleetHistory.Count>2 && fleetHistory[1].Age<fleetFollowTime-.2) fleetHistory.RemoveAt(0);
                FleetCooldown-=dt; bool fire=FleetCooldown<=0; if(fire) FleetCooldown=.18;
                foreach(var s in Fleet)
                {
                    s.PreviousY=s.Y; s.Age+=dt; if(FleetTime==0) s.Departing=true;
                    if(s.Departing) { s.Y+=dt*32; s.Bank=0; }
                    else
                    {
                        double target=fleetFollowTime-s.FollowDelay; int index=fleetHistory.FindIndex(e=>e.Age>=target); index=Math.Max(0,index);
                        var before=fleetHistory[Math.Max(0,index-1)]; var after=fleetHistory[index];
                        double t=after.Age>before.Age?Clamp((target-before.Age)/(after.Age-before.Age),0,1):0;
                        double x=before.X+(after.X-before.X)*t, y=before.Y+(after.Y-before.Y)*t;
                        double ease=1-Math.Pow(1-Math.Min(1,s.Age/1.1),3);
                        s.X=x+s.Side*2.4; s.Y=s.EntryY+(y-1.4*Math.Abs(s.Side)-s.EntryY)*ease; s.Bank=before.Bank+(after.Bank-before.Bank)*t;
                    }
                    if(!s.Departing && s.Age>=1.1) SupportMissile(s,"fleet",FleetTime,1.2,dt);
                    if(fire && s.Age>=1.1) Shot(s.X,s.Y+.8,0,42,"fleet",Math.Max(1.5,Height/42+.2));
                }
            }
            if(AssaultTime<=0) return;
            AssaultTime=Decay(AssaultTime,dt); if(AssaultTime<1e-8) { AssaultTime=0; AssaultFleet.Clear(); return; }
            AssaultCooldown-=dt; bool assaultFire=AssaultCooldown<=0; if(assaultFire) AssaultCooldown=.24;
            foreach(var s in AssaultFleet)
            {
                s.Age+=dt;
                s.Y+=s.Speed*(1+Math.Sin(s.Age*1.3+s.Id)*.08)*dt;
                if(s.Y>Height/2+2 && AssaultTime>3) { s.Y=-Height/2-2; s.X=AssaultLane(s.Side,s.Y); s.Vx=0; }
                double lateral=CanyonTerrain.LateralOffset(Width,Player.X,3);
                s.X-=lateral-s.Origin; s.Origin=lateral;
                var traffic=AssaultFleet.Concat(GroundEnemies).Concat(GroundWrecks);
                double steering=GroundDriving.SteeringTarget(s,AssaultLane(s.Side,s.Y),Width,GroundTravel*CanyonTerrain.NearTravelRate,lateral,traffic);
                double desiredVx=Clamp((steering-s.X)*4,-2.4,2.4);
                s.Vx+=(desiredVx-s.Vx)*(1-Math.Exp(-dt*7)); s.X+=s.Vx*dt;
                s.Bank=Clamp(-Math.Atan2(s.Vx,s.Speed),-.24,.24);
                var target=Enemies.Where(e=>e.Age>=0 && e.Y>s.Y && e.Y<=Height/2).OrderBy(e=>Distance(e,s)).FirstOrDefault();
                double a=target!=null?Math.Atan2(target.X-s.X,target.Y-s.Y):0; s.Aim=-a;
                if(Math.Abs(s.Y)<Height/2) SupportMissile(s,"assault",AssaultTime,.6,dt,a);
                if(assaultFire) Shot(s.X,s.Y+.8,Math.Sin(a)*36,Math.Cos(a)*36,"assault",Height/36+.2);
            }
        }
        double GroundLane(int side,double y,string kind,double age)
        {
            // Centre installations on the top shelf, using the shader's near wall and parallax.
            double edge=.79+CanyonTerrain.Wall(y+GroundTravel*CanyonTerrain.NearTravelRate,side*.7+4);
            double lane=(edge+1)/2+(kind=="tank"?Math.Sin(age*.75)*.025:0);
            return side*Width*.5*lane-CanyonTerrain.LateralOffset(Width,Player.X,3);
        }
        void UpdateGround(double dt)
        {
            TurretLasers.Clear(); GroundCooldown-=dt; GateCooldown-=dt;
            if(GroundCooldown<=0) SpawnGroundEnemy(); if(GateCooldown<=0) SpawnLaserGate();
            foreach(var e in GroundEnemies.Concat(GroundWrecks).ToArray())
            {
                e.Age+=dt; e.Y-=dt*SceneryVelocity*CanyonTerrain.NearTravelRate;
                e.X=GroundLane(e.Side,e.Y,e.Kind,e.Age);
                if(e.Destroyed) continue;
                if(e.GateId!=0)
                {
                    e.Aim=-e.Side*Math.PI/2;
                    if(!e.EnteredView && e.Y<=Height/2-1)
                    { var pair=GroundEnemies.FindAll(t=>t.GateId==e.GateId); pair.ForEach(t=>t.EnteredView=true); if(pair.Count==2 && gateTutorialCount<2) { gateTutorialCount++; Emit("laserGateTutorial"); } }
                    continue;
                }
                if(e.LaserTime>0)
                {
                    e.LaserTime-=dt;
                    if(e.LaserTime<=0) { if(e.Warning) { e.Warning=false; e.LaserTime=.25; Emit("laser",e); } else { e.Cooldown=4; continue; } }
                }
                else
                {
                    e.Aim=Math.Atan2(Player.Y-e.Y,Player.X-e.X)-Math.PI/2; e.Cooldown-=dt;
                    if(Math.Abs(e.Y)<15 && e.Cooldown<=0)
                    {
                        if(e.Kind=="turret" && Wave>=3) { e.Warning=true; e.LaserTime=1.2; }
                        else { e.Cooldown=e.Kind=="tank"?2.1:1.6; double a=e.Aim+Math.PI/2; Shot(e.X+Math.Cos(a)*.9,e.Y+Math.Sin(a)*.9,Math.Cos(a)*9,Math.Sin(a)*9,life:8,friendly:false); Emit("groundShot",e); }
                    }
                }
                if(e.LaserTime>0)
                {
                    double a=e.Aim+Math.PI/2,dx=Math.Cos(a),dy=Math.Sin(a),length=Math.Sqrt(Width*Width+Height*Height);
                    var b=new Beam { Id=e.Id,X=e.X+dx*.9,Y=e.Y+dy*.9,EndX=e.X+dx*length,Top=e.Y+dy*length,Width=e.Warning?.12:.65,Color=e.Warning?"gold":"red",Warning=e.Warning };
                    TurretLasers.Add(b);
                    if(!e.Warning) { double bx=b.EndX-b.X,by=b.Top-b.Y,t=Clamp(((Player.X-b.X)*bx+(Player.Y-b.Y)*by)/(bx*bx+by*by),0,1); if(Math.Sqrt(Math.Pow(Player.X-b.X-t*bx,2)+Math.Pow(Player.Y-b.Y-t*by,2))<.45+b.Width/2) DamagePlayer(); }
                }
            }
            GroundEnemies.RemoveAll(e=>e.Y<-Height/2-3); GroundWrecks.RemoveAll(e=>e.Y<-Height/2-3); RefreshGates();
        }
        void UpdateCapital(double dt)
        {
            if(CapitalShips.Count==0) { CapitalCooldown-=dt; if(CapitalCooldown<=0) SpawnCapitalShip(); }
            foreach(var ship in CapitalShips)
            {
                ship.Age+=dt; ship.Y-=dt*5.8;
                foreach(var t in ship.Turrets)
                {
                    if(t.Destroyed) { t.Age+=dt; continue; }
                    double x=ship.X+t.X,y=ship.Y+t.Y; t.Aim=Math.Atan2(Player.Y-y,Player.X-x)-Math.PI/2; t.Cooldown-=dt;
                    if(Math.Abs(y)>Height/2-1 || Math.Abs(x)>Width/2-.5 || t.Cooldown>0) continue;
                    t.Cooldown=2.4+random()*.6; t.Life=ship.Age+.12; double a=t.Aim+Math.PI/2;
                    Shot(x+Math.Cos(a)*1.1,y+Math.Sin(a)*1.1,Math.Cos(a)*8,Math.Sin(a)*8,life:8,friendly:false); Emit("enemyShot");
                }
            }
            if(CapitalShips.RemoveAll(s=>s.Y+12<-Height/2-2)>0) CapitalCooldown=35+random()*20;
        }
        void UpdateSupportContacts()
        {
            var contacts=new List<Entity>(Orbs); if(OrbitTime>0) contacts.Add(Player);
            foreach(var orb in contacts)
            {
                foreach(var e in Enemies.ToArray()) if(e.Age>=0 && Distance(orb,e)<(e.Heavy?1.3:.95)) DestroyEnemy(e,true);
                foreach(var e in GroundEnemies.ToArray()) if(Math.Abs(e.Y)<=16 && Distance(orb,e)<1) DestroyGroundEnemy(e);
                foreach(var b in Bullets) if(!b.Friendly && Distance(orb,b)<.7) b.Life=-1;
                foreach(var c in Civilians.ToArray()) if(Distance(orb,c)<1) HitCivilian(c);
            }
            foreach(var s in Fleet.Where(e=>e.Departing))
            {
                foreach(var e in Enemies.ToArray()) if(e.Age>=0 && Math.Abs(e.X-s.X)<(e.Heavy?1.5:1.1) && e.Y>=s.PreviousY-1.1 && e.Y<=s.Y+1.1) DestroyEnemy(e,true);
                foreach(var e in GroundEnemies.ToArray()) if(Math.Abs(e.X-s.X)<1.2 && e.Y>=s.PreviousY-1.2 && e.Y<=s.Y+1.2) DestroyGroundEnemy(e);
                foreach(var b in Bullets) if(!b.Friendly && Math.Abs(b.X-s.X)<.9 && b.Y>=s.PreviousY-.9 && b.Y<=s.Y+.9) b.Life=-1;
            }
            Fleet.RemoveAll(s=>s.Y>Height/2+3);
        }
        void UpdateLasers(double dt)
        {
            foreach(var b in Lasers)
            {
                var c=Civilians.Where(e=>LaserHits(b,e,.85)).OrderBy(e=>e.Y).FirstOrDefault();
                if(c!=null) { HitCivilian(c); b.EndX=b.X+(c.Y-b.Y)*(b.EndX-b.X)/(b.Top-b.Y); b.Top=c.Y; }
            }
            foreach(var e in Enemies.Concat(GroundEnemies).ToArray())
            {
                if(e.Age<0 || Math.Abs(e.Y)>Height/2) continue;
                double radius=e.Invader?.75*e.Scale:e.Heavy?1:.85;
                double damage=Lasers.Where(b=>LaserHits(b,e,radius)).Select(b=>b.Damage).DefaultIfEmpty(0).Max()*dt;
                if(damage<=0) continue; e.Hp-=damage; e.HitCooldown-=dt;
                if(e.HitCooldown<=0) { e.HitCooldown=.12; Emit("impact",e,.25); }
                if(e.Hp<=0) { if(GroundEnemies.Contains(e)) DestroyGroundEnemy(e); else DestroyEnemy(e,true); }
            }
            foreach(var target in CapitalTargets())
            { double damage=Lasers.Where(b=>LaserHits(b,target,.85)).Select(b=>b.Damage).DefaultIfEmpty(0).Max()*dt; if(damage>0) DamageCapital(target,damage); }
        }
        void UpdateBullets(double dt)
        {
            foreach(var b in Bullets.ToArray())
            {
                if(b.Life<=0) continue;
                if(b.Kind=="missile")
                {
                    var target=Enemies.Concat(GroundEnemies).Concat(CapitalTargets()).Where(VisibleMissileTarget).OrderBy(e=>Distance(e,b)).FirstOrDefault();
                    double vx,vy;
                    if(target!=null)
                    {
                        double dx=target.X-b.X,dy=target.Y-b.Y,length=Math.Max(.001,Math.Sqrt(dx*dx+dy*dy));
                        vx=dx/length*24; vy=dy/length*24;
                    }
                    else
                    {
                        // Tangential flight with a gentle radial correction makes a continuous
                        // circle, rather than chasing a moving point and sticking to inner walls.
                        double distance=Math.Sqrt(b.X*b.X+b.Y*b.Y),radius=Math.Min(Width,Height)*.3;
                        double nx=distance>.001?b.X/distance:Math.Cos(b.Id*2.4),ny=distance>.001?b.Y/distance:Math.Sin(b.Id*2.4);
                        double radial=Clamp((radius-distance)*3,-13,13);
                        vx=-ny*13+nx*radial; vy=nx*13+ny*radial;
                    }
                    double steer=Math.Min(1,dt*(target!=null?10:5));
                    b.Vx+=(vx-b.Vx)*steer; b.Vy+=(vy-b.Vy)*steer;
                }
                b.X+=b.Vx*dt; b.Y+=b.Vy*dt; b.Life-=dt;
                if(b.Kind=="missile")
                {
                    double xLimit=Math.Max(.5,Width/2-.5),yLimit=Math.Max(.5,Height/2-.5);
                    b.X=Clamp(b.X,-xLimit,xLimit); b.Y=Clamp(b.Y,-yLimit,yLimit);
                }
                if(b.Kind=="bomb") { b.Fuse-=dt; if(b.Fuse<=0) { Blast(b,2.2,false); continue; } }
                if(b.Friendly)
                {
                    var c=Civilians.Find(e=>Math.Abs(e.Y)<=16 && Distance(e,b)<.85);
                    if(c!=null && b.Kind!="missile") { b.Life=-1; if(b.Kind=="bomb") Blast(b,2.2,false); else HitCivilian(c); continue; }
                    var target=CapitalTargets().FirstOrDefault(e=>(b.Kind!="missile" || VisibleMissileTarget(e)) && Math.Abs(e.Y)<=Height/2 && Distance(e,b)<.85);
                    if(target==null) target=GroundEnemies.Find(e=>(b.Kind=="missile"?VisibleMissileTarget(e):Math.Abs(e.Y)<=16) && Distance(e,b)<.85);
                    if(target==null) target=Enemies.Find(e=>e.Age>=0 && (b.Kind!="missile" || VisibleMissileTarget(e)) && Distance(e,b)<(e.Invader?.15+.6*e.Scale:e.Heavy?.85:.6));
                    if(target==null) continue;
                    if(b.Kind=="missile" || b.Kind=="bomb") Blast(b,b.Kind=="missile"?4.8:2.2,b.Kind=="missile");
                    else
                    {
                        b.Life=-1; Emit("impact",b,.3);
                        if(target.Turrets.Count>0) DamageCapital(target,b.Damage);
                        else { target.Hp-=b.Damage; if(target.Hp<=0) { if(GroundEnemies.Contains(target)) DestroyGroundEnemy(target); else DestroyEnemy(target,true); } }
                    }
                }
                else
                {
                    var cart=AssaultFleet.Find(e=>Distance(e,b)<.7);
                    if(cart!=null) { b.Life=-1; AssaultFleet.Remove(cart); Emit("explosion",cart,1.2); }
                    else if(Distance(b,Player)<.36) { b.Life=-1; DamagePlayer(); if(Lives<=0) return; }
                }
            }
            Bullets.RemoveAll(b=>b.Life<=0 || Math.Abs(b.X)>Math.Max(16,Width/2+2) || Math.Abs(b.Y)>Math.Max(21,Height/2+2));
        }
        public void Advance(double delta,FlightInput input)
        {
            for(double remaining=Clamp(delta,0,.1);remaining>.00001;remaining-=1.0/120) Update(Math.Min(remaining,1.0/120),input);
        }
        public void Update(double delta,FlightInput input)
        {
            if(Paused || Phase=="gameover") return;
            double dt=Clamp(delta,0,.05); if(dt==0) return; input=input??new FlightInput(); Clock+=dt; Elapsed+=dt;
            if(SceneryStopped)
            {
                foreach(var e in Enemies.Concat(GroundEnemies).Concat(CapitalShips).Concat(FallingShips)) e.Y-=dt*18;
                Enemies.RemoveAll(e=>e.Y<-Height/2-3); GroundEnemies.RemoveAll(e=>e.Y<-Height/2-3); CapitalShips.RemoveAll(e=>e.Y<-Height/2-14); FallingShips.RemoveAll(e=>e.Y<-Height/2-3);
                if(Phase=="dying")
                {
                    GroundTravel+=dt*ScenerySpeed*SceneryMotion;
                    double p=Math.Min(1,Elapsed/2.4),c=Math.Min(1,Elapsed/.4),ease=c*c*(3-2*c);
                    Player.X=playerFall.X*(1-ease); Player.Y=playerFall.Y*(1-ease); Player.Z=1-25*p*p; Player.Bank=playerFall.Bank+p*Math.PI*2;
                    if(p==1) { Phase="crashed"; Elapsed=0; Emit("playerCrash",Player,5.5); }
                }
                else if(Phase=="crashed" && Elapsed>=.8) { Phase="gameover"; Emit("gameover"); }
                return;
            }
            SpeedTime=Decay(SpeedTime,dt); SpeedBlend=Clamp(SpeedBlend+(SpeedTime>0?dt/.65:-dt/.85),0,1); GroundTravel+=dt*SceneryVelocity;
            foreach(var e in FallingShips.ToArray())
            {
                e.FallAge+=dt; double p=Math.Min(1,e.FallAge/e.FallDuration);
                if(e.Kind=="flyaway") { e.X=e.FallX+e.Side*2.1*p; e.Y=e.FallY-.7*p; }
                else { e.X=e.FallX+Math.Sin(p*Math.PI)*.6; e.Y=Math.Max(-Height/2+.6,e.FallY-3.5*p*p); }
                if(p==1) { FallingShips.Remove(e); Emit("explosion",e,e.Kind=="flyaway"?1.5:e.Heavy?3:2); }
            }
            if(Phase=="countdown") { if(Elapsed>=3.8) StartWave(); return; }
            bool missile=input.Missile && !MissilePressed; MissilePressed=input.Missile;
            CivilianCooldown-=dt; if(CivilianCooldown<=0) SpawnCivilian();
            foreach(var c in Civilians.ToArray())
            {
                c.HitCooldown=Decay(c.HitCooldown,dt); c.Age+=dt; c.Y-=dt*2.1; c.X=c.Origin+Math.Sin(c.Age*.65)*.35;
                if(c.Hp==1) { c.AlarmCooldown-=dt; if(c.AlarmCooldown<=0) { c.AlarmCooldown+=.9; Emit("civilianAlarm",c); } }
                if(c.Y<=-18) { Rescues++; Score+=10000; Civilians.Remove(c); Emit("civilianRescued",c); }
            }
            WeaponTime=Decay(WeaponTime,dt); ShieldTime=Decay(ShieldTime,dt);
            if(WeaponTime==0) { Weapon="guns"; WeaponStrength=1; }
            SupplyCooldown-=dt; if(SupplyCooldown<=0) SpawnSupply(); Lasers.Clear();
            double dx=(input.Right?1:0)-(input.Left?1:0),dy=(input.Up?1:0)-(input.Down?1:0),mag=Math.Sqrt(dx*dx+dy*dy); if(mag>0) { dx/=mag; dy/=mag; }
            double intent=input.HasTarget?Clamp((input.TargetX-Player.X)/.6,-1,1):dx,speed=1+SpeedIntensity*.7;
            Player.Bank+=(-intent*Math.PI/36-Player.Bank)*Math.Min(1,dt*20*speed); double sin=Math.Sin(Player.Bank),cos=Math.Cos(Player.Bank),previousY=Player.Y;
            if(input.HasTarget) { Player.X+=Clamp(input.TargetX-Player.X,-22*speed*dt,22*speed*dt); Player.Y+=Clamp(input.TargetY-Player.Y,-22*speed*dt,22*speed*dt); }
            else { Player.X+=dx*16*speed*dt; Player.Y+=dy*16*speed*dt; }
            Player.X=Clamp(Player.X,-BoundX,BoundX); Player.Y=Clamp(Player.Y,Bottom,Top); Player.Invulnerable=Decay(Player.Invulnerable,dt); FireCooldown-=dt;
            if(input.Fire && (Weapon=="guns" || Weapon=="machine") && FireCooldown<=0)
            {
                FireCooldown=Weapon=="machine"?.035:.085;
                for(int side=-1;side<=1;side+=2) Shot(Player.X+side*.24*cos-.8*sin,Player.Y+side*.24*sin+.8*cos,-sin*42,cos*42,life:Math.Max(1.5,Height/42+.2),damage:WeaponStrength);
                Emit("shot",Player);
            }
            if(missile && MissileCount>0)
            {
                MissileCount--; var rockets=Bullets.Where(b=>b.Kind=="missile" && b.Support=="").ToArray(); if(rockets.Length>=12) rockets[0].Life=-1;
                Shot(Player.X-.9*sin,Player.Y+.9*cos,-sin*18,cos*18,"missile",double.PositiveInfinity); Emit("missileLaunch",Player);
            }
            if(input.Fire && (Weapon=="laser" || Weapon=="mega"))
            {
                foreach(int side in Weapon=="mega"?new[]{0}:new[]{-1,1})
                { double x=Player.X+side*.48*cos-.8*sin,y=Player.Y+side*.48*sin+.8*cos; Lasers.Add(new Beam { X=x,Y=y,Top=Height/2,EndX=x-(Height/2-y)*sin/cos,Width=Weapon=="mega"?1.2:.1*WeaponStrength,Damage=18*WeaponStrength,Strength=WeaponStrength,Color=Weapon=="mega"?"violet":"cyan" }); }
            }
            UpdateAddons(dt,input.Fire); UpdateGround(dt); if(Lives<=0) return; UpdateCapital(dt);
            foreach(var e in Enemies.ToArray())
            {
                e.Age+=dt; if(e.Age<0) continue;
                e.Y=Height/2+2+e.Row*(e.Invader?Math.Min(2,Height*.06):2.2)-e.Age*(e.Invader?(Height+10)/24:3.62+Math.Max(0,Wave-1)*.3);
                e.X=e.Origin+Math.Sin(e.Age*(e.Invader?.8:1.15)+e.Pattern*.4)*BoundX*(e.Invader?.04:.06);
                if(e.Y<(e.Invader?-Height/2-2:-17)) { DestroyEnemy(e,false); continue; }
                bool canFire=!e.Invader || !Enemies.Any(o=>o.Column==e.Column && o.Row<e.Row);
                if(canFire && e.Age>e.FireAt && e.Y<Math.Min(13,Height/2-1) && e.Y>Player.Y+1.5)
                {
                    e.FireAt=e.Age+(e.Invader?6:Math.Max(.9,2.5-Wave*.12)); double a=Math.Atan2(Player.Y-e.Y,Player.X-e.X);
                    foreach(double offset in !e.Invader && e.Variant==2 && Wave>2?new[]{-.22,0,.22}:new[]{0.0}) Shot(e.X,e.Y-.6,Math.Cos(a+offset)*7.5,Math.Sin(a+offset)*7.5,life:8,friendly:false);
                    Emit("enemyShot",e);
                }
                if(Distance(e,Player)<(e.Invader?.35+e.Scale*.45:e.Heavy?1.05:.8)) DamagePlayer(); if(Lives<=0) return;
            }
            UpdateSupportContacts(); UpdateLasers(dt); UpdateBullets(dt); if(Lives<=0) return;
            foreach(var b in GateLasers)
            {
                double before=previousY-(b.Y+dt*SceneryVelocity*CanyonTerrain.NearTravelRate),after=Player.Y-b.Y,clearance=b.Width/2+.45;
                if(Math.Abs(after)>=clearance && before*after>0) continue;
                Player.Y=b.Y+(before>0?1:-1)*clearance; DamagePlayer(true);
            }
            if(Lives<=0) return;
            foreach(var p in Pickups)
            {
                p.Age+=dt; p.Y-=dt*(p.Kind=="powerup"?4:1.8); double d=Distance(p,Player);
                if(d<3.2) { p.X+=(Player.X-p.X)/Math.Max(d,.01)*dt*11; p.Y+=(Player.Y-p.Y)/Math.Max(d,.01)*dt*11; }
                if(Distance(p,Player)>=.8) continue;
                p.Collected=true;
                if(p.Kind=="life") { Lives=Math.Min(3,Lives+1); Emit("life",p); }
                else if(p.Kind=="powerup") CollectPowerup(p.Power);
                else { Score+=p.Value; Emit("collect",p,p.Value==10?1.5:.45,"gold"); }
            }
            Pickups.RemoveAll(p=>p.Collected || p.Y<-18 || p.Age>=16);
            if(Phase=="playing" && Enemies.Count==0)
            {
                Phase="intermission"; Elapsed=0; bool perfect=WaveKills==WaveSize && Escaped==0;
                if(perfect) Pickups.Add(new Entity { Id=++serial,X=lastKillX,Y=lastKillY,Value=10 });
                Emit("waveClear",new Entity { X=lastKillX,Y=lastKillY },perfect?3:1.5,perfect:perfect);
            }
            else if(Phase=="intermission" && Elapsed>=3) StartWave();
        }
    }
}
