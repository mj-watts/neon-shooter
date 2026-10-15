using System;
using System.Collections.Generic;
using NeonShooter.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonShooter.Unity
{
    public sealed partial class NeonGame : MonoBehaviour
    {
        public ShooterGame Model { get; private set; }
        Camera gameCamera;
        VoxelAssets assets;
        FlightAudio audioPlayer;
        MenuSoundtrack menuMusic;
        float MenuPulse => !launched && !reducedMotion && menuMusic!=null?menuMusic.Pulse:0;
        Color MenuAccent => Color.Lerp(VoxelAssets.ColorOf("#ff3ba7"),VoxelAssets.ColorOf("#ffe85a"),MenuPulse);
        NeonBloom bloom;
        RunHistory history;
        bool launched,historyVisible,reducedMotion,pointerActive,saved;
        Vector3 lastPointer;
        int lastWidth,lastHeight,lastCountdown;
        float calloutUntil,shake,visualTime;
        const int MaxParticles=3200, MaxRings=96, MaxBlasts=32;
        string callout="";
        GUIStyle button;
        Font displayFont,bodyFont;
        float uiScale=1,uiWidth=1100,uiHeight=800;
        readonly OrbitalTrails orbitalTrails=new OrbitalTrails();
        public int OrbitalTrailCount => orbitalTrails.Points.Count;
        readonly List<Particle> particles=new List<Particle>();
        readonly List<Ring> rings=new List<Ring>();
        readonly List<Blast> blasts=new List<Blast>();
        struct Particle { public Vector3 Position,Velocity; public float Life,MaxLife,Size,Spin; public string Color; public bool Spark; }
        struct Ring { public Vector3 Position; public float Life,MaxLife,Delay,Size; public string Color; public float[] Drift,FadeAt; }
        struct Blast { public Vector3 Position; public float Life,MaxLife,Size; public string Color; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if(FindAnyObjectByType<NeonGame>()==null) new GameObject("Neon Shooter").AddComponent<NeonGame>();
        }
        void Awake()
        {
            Application.targetFrameRate=120; QualitySettings.vSyncCount=1;
            history=new RunHistory(); reducedMotion=PlayerPrefs.GetInt("neon-shooter.reduced-motion",0)==1;
            assets=new VoxelAssets(); Model=new ShooterGame(); Model.OnEvent+=Handle;
            gameCamera=Camera.main;
            if(gameCamera==null) gameCamera=new GameObject("Flight Camera").AddComponent<Camera>();
            var cameraObject=gameCamera.gameObject; cameraObject.transform.SetParent(transform);
            gameCamera.orthographic=true; gameCamera.nearClipPlane=.1f; gameCamera.farClipPlane=100;
            gameCamera.transform.rotation=Quaternion.identity;
            gameCamera.transform.position=new Vector3(0,0,-40); gameCamera.backgroundColor=VoxelAssets.ColorOf("#080318"); gameCamera.clearFlags=CameraClearFlags.SolidColor;
            gameCamera.allowHDR=true; gameCamera.allowMSAA=false;
            if(cameraObject.GetComponent<AudioListener>()==null) cameraObject.AddComponent<AudioListener>();
            bloom=cameraObject.AddComponent<NeonBloom>(); bloom.ReducedMotion=reducedMotion;
            audioPlayer=gameObject.AddComponent<FlightAudio>(); audioPlayer.SetMuted(PlayerPrefs.GetInt("neon-shooter.muted",0)==1);
            menuMusic=gameObject.AddComponent<MenuSoundtrack>(); menuMusic.SetMuted(audioPlayer.Muted); menuMusic.Begin();
            displayFont=Resources.Load<Font>("Fonts/OffBit101"); bodyFont=Resources.Load<Font>("Fonts/nohemi"); Resize();
        }
        void Resize()
        {
            lastWidth=Screen.width; lastHeight=Screen.height;
            float aspect=Screen.width/(float)Mathf.Max(1,Screen.height),height=Mathf.Max(32,24/aspect);
            gameCamera.orthographicSize=height/2; Model.SetPlayfield(height*aspect,height);
            UpdateUiViewport();
        }
        void UpdateUiViewport()
        {
            uiScale=Mathf.Clamp(Mathf.Min(Screen.width/1280f,Screen.height/800f),.35f,3);
            uiWidth=Screen.width/uiScale; uiHeight=Screen.height/uiScale;
        }
        public void Launch()
        {
            menuMusic.Stop(); bloom.MusicPulse=0;
            Model.Reset(); Model.ScenerySpeed=reducedMotion?.072:3.6; Resize(); launched=true; saved=false; historyVisible=false;
            particles.Clear(); rings.Clear(); blasts.Clear(); orbitalTrails.Clear(); bloom.BlastPulse=0; lastCountdown=0; pointerActive=false; audioPlayer.Begin();
        }
        void Leave()
        {
            if(launched && !saved && Model.Wave>0) { history.Record(Model.Score,Model.Wave,Model.Kills); saved=true; }
            launched=false; Model.Paused=false; historyVisible=false; audioPlayer.Stop(); menuMusic.Begin(); particles.Clear(); rings.Clear(); blasts.Clear(); orbitalTrails.Clear(); bloom.BlastPulse=0;
        }
        void Pause() { if(!launched || Model.Phase=="gameover") return; Model.Paused=!Model.Paused; audioPlayer.Pause(Model.Paused); }
        void OnApplicationFocus(bool focused) { if(!focused && launched && !Model.Paused && Model.Phase!="gameover") Pause(); }
        void OnApplicationPause(bool paused) { if(paused) OnApplicationFocus(false); }
        FlightInput ReadInput()
        {
            bool left=Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.O)||Input.GetKey(KeyCode.A);
            bool right=Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.P)||Input.GetKey(KeyCode.D);
            bool up=Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.W),down=Input.GetKey(KeyCode.DownArrow)||Input.GetKey(KeyCode.S);
            bool keyboard=left||right||up||down;
            var input=new FlightInput { Left=left,Right=right,Up=up,Down=down,Fire=Input.GetKey(KeyCode.Space),Missile=Input.GetKey(KeyCode.B) };
            if(Model.Paused || !launched) return input;
            Vector3 pointer=Input.mousePosition;
            bool inArena=pointer.y>88*uiScale && pointer.y<Screen.height-88*uiScale && pointer.x>=0 && pointer.x<=Screen.width;
            if(Input.touchCount>0)
            {
                var touch=Input.GetTouch(0); pointer=touch.position;
                inArena=pointer.y>90*uiScale && pointer.y<Screen.height-88*uiScale;
                if(inArena) { pointerActive=true; input.Fire=true; }
            }
            else
            {
                if(keyboard) pointerActive=false;
                else if((pointer-lastPointer).sqrMagnitude>1 && inArena) pointerActive=true;
                if(inArena && Input.GetMouseButton(0)) input.Fire=true;
            }
            lastPointer=pointer;
            if(pointerActive && inArena && !keyboard) { var world=gameCamera.ScreenToWorldPoint(new Vector3(pointer.x,pointer.y,40)); input.HasTarget=true; input.TargetX=world.x; input.TargetY=world.y; }
            return input;
        }
        void Update()
        {
            if(Screen.width!=lastWidth || Screen.height!=lastHeight) Resize();
            if(Input.GetKeyDown(KeyCode.Escape)) { if(historyVisible) historyVisible=false; else Pause(); }
            if(!launched && !historyVisible && Input.GetKeyDown(KeyCode.Return)) Launch();
            if(launched && Model.Phase=="gameover" && Input.GetKeyDown(KeyCode.Return)) Launch();
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
            if(launched && !Model.Paused)
            {
                Model.Advance(dt,ReadInput());
                if(Model.Phase=="countdown") { int value=(int)Math.Ceiling(3.8-Model.Elapsed); if(value!=lastCountdown) { lastCountdown=value; audioPlayer.Handle(new GameEvent { Type="countdown" }); } }
                if(Model.Lasers.Count>0) audioPlayer.Handle(new GameEvent { Type="laser" });
                visualTime+=dt; UpdateEffects(dt);
            }
            else if(!launched) { visualTime+=dt; Model.GroundTravel+=dt*(reducedMotion?.03:1); }
            shake=Mathf.Max(0,shake-dt*3);
            gameCamera.transform.position=new Vector3(reducedMotion || Model.Paused?0:Mathf.Sin(visualTime*71)*shake,reducedMotion || Model.Paused?0:Mathf.Cos(visualTime*83)*shake,-40);
        }
        void Handle(GameEvent e)
        {
            audioPlayer.Handle(e);
            switch(e.Type)
            {
                case "wave": Callout("WAVE "+Model.Wave+"  //  ENGAGE",2); break;
                case "waveClear": Callout(e.Perfect?"PERFECT WAVE  //  BONUS STAR":"WAVE CLEAR",2); break;
                case "powerup": Callout(PowerCatalog.Find(e.Power).Label,2); break;
                case "civilianIncoming": Callout("CIVILIAN INCOMING  //  HOLD YOUR FIRE",3); break;
                case "civilianHit": Callout("CIVILIAN HIT  //  -1,000",2); break;
                case "civilianRescued": Callout("CIVILIAN RESCUED  //  +10,000",3); break;
                case "capitalIncoming": Callout("CAPITAL SHIP INCOMING",3); break;
                case "laserGateTutorial": Callout("LASER GATE  //  DESTROY AN EMITTER",3); break;
                case "life": Callout("HULL REPAIRED",1.5f); break;
                case "gameover": if(!saved) { history.Record(Model.Score,Model.Wave,Model.Kills); saved=true; } break;
            }
            if(e.Type=="explosion" || e.Type.EndsWith("Explosion",StringComparison.Ordinal) || e.Type=="hit" || e.Type=="playerCrash")
            {
                if(!reducedMotion) shake=Mathf.Min(.3f,(float)e.Size*.06f);
                float size=Mathf.Clamp((float)e.Size,.4f,5.5f);
                Burst(e,Mathf.FloorToInt(size*(reducedMotion?24:e.Type=="gateExplosion"?60:100)),true);
                var at=new Vector3((float)e.X,(float)e.Y,-2);
                if(blasts.Count>=MaxBlasts) blasts.RemoveAt(0);
                blasts.Add(new Blast { Position=at,Size=size,Life=1.05f,MaxLife=1.05f,Color=e.Color??"pink" });
                int layers=reducedMotion?2:4;
                for(int i=0;i<layers;i++)
                {
                    if(rings.Count>=MaxRings) rings.RemoveAt(0);
                    float life=1.5f+i*.3f;
                    var drift=new float[48]; var fadeAt=new float[48];
                    for(int j=0;j<48;j++) { drift[j]=UnityEngine.Random.Range(.1f,.4f); fadeAt[j]=UnityEngine.Random.Range(.6f,1); }
                    rings.Add(new Ring { Position=at,Size=size*(2.8f+i*.8f),Life=life,MaxLife=life,Delay=i*.13f,Color=i==1?"gold":i==3?"cyan":e.Color??"pink",Drift=drift,FadeAt=fadeAt });
                }
                if(!reducedMotion) bloom.BlastPulse=Mathf.Min(.65f,bloom.BlastPulse+size*.12f);
            }
            else if(e.Type=="impact" || e.Type=="collect" || e.Type=="powerup") Burst(e,reducedMotion?2:8);
        }
        void Callout(string text,float duration) { callout=text; calloutUntil=Time.unscaledTime+duration; }
        void Burst(GameEvent e,int count,bool fireworks=false)
        {
            for(int i=0;i<count;i++)
            {
                if(particles.Count>=MaxParticles) particles.RemoveAt(0);
                float angle=i/(float)count*Mathf.PI*2+UnityEngine.Random.Range(-.08f,.08f);
                var direction=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                float life=fireworks?UnityEngine.Random.Range(1.2f,2.7f):UnityEngine.Random.Range(.25f,.6f);
                float speed=Mathf.Max(.3f,(float)e.Size)*UnityEngine.Random.Range(1f,8f);
                if(reducedMotion) { speed*=.4f; life*=.7f; }
                particles.Add(new Particle {
                    Position=new Vector3((float)e.X,(float)e.Y,-2.2f),Velocity=direction*speed,
                    Life=life,MaxLife=life,Size=UnityEngine.Random.Range(.12f,.34f),
                    Spin=UnityEngine.Random.Range(-180f,180f),
                    Color=i%5==0?"#fff9cc":fireworks && i%3==0?(e.Color=="cyan" || e.Color=="mint"?"pink":"cyan"):e.Color??"orange",Spark=fireworks
                });
            }
        }
        void UpdateEffects(float dt)
        {
            orbitalTrails.Update(Model.Orbs,Model.Player,dt,reducedMotion);
            for(int i=particles.Count-1;i>=0;i--)
            {
                var p=particles[i]; p.Life-=dt; p.Position+=p.Velocity*dt;
                p.Velocity*=Mathf.Exp(-dt*.65f);
                p.Velocity+=Vector3.down*(dt*(reducedMotion?.15f:1.15f));
                if(p.Life<=0) particles.RemoveAt(i); else particles[i]=p;
            }
            for(int i=rings.Count-1;i>=0;i--)
            {
                var r=rings[i];
                float activeTime=Mathf.Max(0,dt-r.Delay); r.Delay=Mathf.Max(0,r.Delay-dt); r.Life-=activeTime;
                if(r.Life<=0) rings.RemoveAt(i); else rings[i]=r;
            }
            for(int i=blasts.Count-1;i>=0;i--)
            {
                var b=blasts[i]; b.Life-=dt;
                if(b.Life<=0) blasts.RemoveAt(i); else blasts[i]=b;
            }
        }
        static Vector3 Position(Entity e,float z=0) { return new Vector3((float)e.X,(float)e.Y,z); }
        void Draw(string key,Entity e,float scale=1,float z=0,float angle=0) { assets.Draw(key,Position(e,z),Vector3.one*scale,angle,gameCamera); }
        void DrawBlock(string color,Vector3 at,Vector3 size,float angle=0) { assets.Draw("block"+color,at,size,angle,gameCamera); }
        void DrawRing(Vector3 at,float radius,string color,int count=40,float size=.10f)
        {
            for(int i=0;i<count;i++) { float a=i/(float)count*Mathf.PI*2; DrawBlock(color,at+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*radius,new Vector3(size,size,.05f),a*Mathf.Rad2Deg); }
        }
        void DrawBeam(Beam b)
        {
            var a=new Vector3((float)b.X,(float)b.Y,-1); var end=new Vector3((float)b.EndX,(float)b.Top,-1); var delta=end-a; float width=(float)b.Width;
            string color=b.Warning?"gold":b.Color;
            DrawBlock(color,(a+end)*.5f,new Vector3(width,delta.magnitude,.06f),-Mathf.Atan2(delta.x,delta.y)*Mathf.Rad2Deg);
            if(!b.Warning) assets.Glow(color,(a+end)*.5f+Vector3.forward*.15f,new Vector3(width*4+.35f,delta.magnitude*1.05f,1),gameCamera,.6f,-Mathf.Atan2(delta.x,delta.y)*Mathf.Rad2Deg);
            if(!b.Warning) { DrawBlock("gold",(a+end)*.5f+Vector3.back*.05f,new Vector3(width*.18f,delta.magnitude,.05f),-Mathf.Atan2(delta.x,delta.y)*Mathf.Rad2Deg); }
        }
        void LateUpdate()
        {
            bloom.MusicPulse=0; assets.Canyon.SetFloat("musicPulse",0);
            if(!launched) return;
            UpdateBackdrop();
            assets.Canyon.SetFloat("width",(float)Model.Width); assets.Canyon.SetFloat("height",(float)Model.Height);
            assets.Canyon.SetVector("travel",new Vector4((float)(Model.GroundTravel*CanyonTerrain.DeepTravelRate),(float)(Model.GroundTravel*CanyonTerrain.MiddleTravelRate),(float)(Model.GroundTravel*CanyonTerrain.NearTravelRate),0));
            assets.Canyon.SetVector("lateral",new Vector4((float)CanyonTerrain.LateralOffset(Model.Width,Model.Player.X,1),(float)CanyonTerrain.LateralOffset(Model.Width,Model.Player.X,2),(float)CanyonTerrain.LateralOffset(Model.Width,Model.Player.X,3),0));
            Graphics.DrawMesh(assets.Quad,Matrix4x4.TRS(new Vector3(0,0,25),Quaternion.identity,new Vector3((float)Model.Width,(float)Model.Height,1)),assets.Canyon,0,gameCamera,0,null,ShadowCastingMode.Off,false);
            foreach(var ship in Model.CapitalShips)
            {
                assets.Draw("capital",Position(ship,15),new Vector3((float)ship.Scale,1,1),0,gameCamera);
                foreach(var t in ship.Turrets)
                {
                    var at=new Vector3((float)(ship.X+t.X),(float)(ship.Y+t.Y),14);
                    if(!t.Destroyed) assets.Draw("capitalGun",at,Vector3.one,(float)t.Aim*Mathf.Rad2Deg,gameCamera);
                    else { DrawBlock("#090909",at,new Vector3(1.35f,1.25f,.15f)); DrawBlock("#70505a",at+new Vector3(Mathf.Sin((float)t.Age),.5f,0),new Vector3(.5f,.5f,.1f)); }
                }
            }
            foreach(var e in Model.GroundEnemies) DrawGround(e);
            foreach(var e in Model.GroundWrecks) DrawGround(e);
            foreach(var e in Model.AssaultFleet) DrawGround(e);
            foreach(var e in Model.Enemies) Draw(e.Heavy?"heavy":"enemy"+e.Variant,e,(float)(e.Invader?e.Scale*.52:e.Heavy?.728:.52),0);
            foreach(var e in Model.Civilians)
            {
                Draw("civilian",e,1,0); DrawRing(Position(e,1),1.3f,"mint",28,.07f);
                if(e.Hp<5) { assets.Glow("orange",Position(e,-.5f),Vector3.one*1.4f,gameCamera,.25f); for(int i=0;i<5-e.Hp;i++) DrawBlock("#090909",Position(e,-.1f)+new Vector3((i%2==0?1:-1)*.4f,-i*.15f,0),new Vector3(.3f,.22f,.08f)); }
            }
            foreach(var e in Model.FallingShips)
            {
                float progress=(float)(e.FallAge/e.FallDuration); bool civilian=e.Kind=="civilianFall";
                Draw(civilian?"civilian":e.Heavy?"heavy":"enemy"+e.Variant,e,(civilian?1:.52f)*(1-progress*.65f),progress*22,progress*360*e.Side);
                assets.Glow("orange",Position(e,progress*22-.1f),Vector3.one*(1-progress)*2,gameCamera,.6f);
            }
            foreach(var s in Model.Fleet) Draw("fleet"+s.Side,s,.341f,0,(float)s.Bank*Mathf.Rad2Deg);
            foreach(var p in Model.Pickups)
            {
                string color=p.Kind=="life"?"red":p.Kind=="powerup"?PowerCatalog.Find(p.Power).Color:"gold";
                Draw(p.Kind=="life"?"life":p.Kind=="powerup"?"power"+p.Power:"star",p,p.Value==10?1.8f:1,-.5f,reducedMotion?0:Mathf.Sin((float)p.Age*2)*12);
                assets.Glow(color,Position(p,.2f),Vector3.one*(p.Kind=="life"?2:1.6f),gameCamera,p.Kind=="life"?.7f:.38f);
            }
            foreach(var b in Model.Bullets)
            {
                float angle=-Mathf.Atan2((float)b.Vx,(float)b.Vy)*Mathf.Rad2Deg;
                string key=b.Kind=="missile"?"missile":b.Kind=="bomb"?"blockorange":!b.Friendly?"blockpink":b.Kind=="companion" || b.Kind=="fleet"?"blockcyan":b.Kind=="shrapnel" || b.Kind=="assault"?"blockorange":"shot"+(int)b.Strength;
                float scale=b.Kind=="missile"?1:b.Kind=="bomb"?.44f:!b.Friendly?.22f:key.StartsWith("block",StringComparison.Ordinal)?.15f:1;
                Draw(key,b,scale,-1,angle);
                string color=b.Kind=="missile" || b.Kind=="bomb"?"orange":b.Friendly?"cyan":"pink";
                assets.Glow(color,Position(b,-.5f),new Vector3(1.6f,b.Kind=="missile"?2.8f:1.6f,1),gameCamera,.65f);
            }
            foreach(var b in Model.Lasers) DrawBeam(b); foreach(var b in Model.GateLasers) DrawBeam(b); foreach(var b in Model.TurretLasers) DrawBeam(b);
            foreach(var point in orbitalTrails.Points)
            {
                float fade=(float)Math.Max(0,1-point.Age/point.Life);
                float size=(point.Particle?.07f:.22f)*fade;
                var at=Position(Model.Player,-.8f)+new Vector3((float)point.X,(float)point.Y,0);
                assets.Draw("orbTrail",at,new Vector3(size,size,.04f),0,gameCamera);
            }
            foreach(var e in Model.Orbs) assets.Draw("orbTrail",Position(e,-1),Vector3.one*.25f,0,gameCamera);
            foreach(var e in Model.Companions) { DrawBlock("cyan",Position(e,-1),Vector3.one*.28f); assets.Glow("cyan",Position(e,-.5f),Vector3.one,gameCamera); }
            if(Model.Phase!="crashed" && Model.Phase!="gameover")
            {
                float progress=Model.Phase=="dying"?Mathf.Clamp01((float)Model.Elapsed/2.4f):0;
                bool visible=Model.Player.Invulnerable<=0 || ((int)(visualTime*14)%2==0) || reducedMotion;
                if(visible) Draw("hero",Model.Player,.75f*(1-progress*.75f),-(float)Model.Player.Z,(float)Model.Player.Bank*Mathf.Rad2Deg);
                if(Model.Phase!="dying")
                {
                    assets.DrawExhaust(Position(Model.Player,0),Model.SpeedTime>0?1:0,visualTime,(float)Model.Player.Bank*Mathf.Rad2Deg,gameCamera);
                    if(Model.ShieldTime>0) DrawRing(Position(Model.Player,-1.5f),1.5f,"mint");
                }
            }
            foreach(var b in blasts)
            {
                float age=b.MaxLife-b.Life,fade=b.Life/b.MaxLife;
                // The coloured light outlives the hot core, like an aerial firework.
                float cloud=b.Size*(7+age*5);
                assets.Glow(b.Color,b.Position,new Vector3(cloud,cloud,1),gameCamera,fade*fade*.8f);
                assets.Glow(b.Color,b.Position,new Vector3(cloud*.45f,cloud*.45f,1),gameCamera,fade*.7f);
                float core=Mathf.Clamp01(1-age/(reducedMotion?.18f:.28f));
                assets.Glow("#fff9cc",b.Position,Vector3.one*b.Size*(1+core),gameCamera,core,0,true);
            }
            foreach(var p in particles)
            {
                float fade=Mathf.Clamp01(p.Life/p.MaxLife);
                float size=p.Size*(.4f+.6f*fade);
                assets.Glow(p.Color,p.Position,Vector3.one*size*4.5f,gameCamera,fade*.48f);
                assets.Glow(p.Color,p.Position,Vector3.one*size,gameCamera,fade,0,true);
                if(!p.Spark || reducedMotion) continue;
                // Sample a tapered, curved comet tail along the spark's recent path.
                float tailTime=Mathf.Min(.22f,p.MaxLife-p.Life);
                for(int j=1;j<=5;j++)
                {
                    float t=j/5f,behind=tailTime*t;
                    var at=p.Position-p.Velocity*behind+Vector3.down*(behind*behind*.575f);
                    assets.Glow(p.Color,at,Vector3.one*size*(2.7f-t*1.7f),gameCamera,fade*(1-t*.85f)*.55f,0,true);
                }
            }
            foreach(var r in rings)
            {
                if(r.Delay>0) continue;
                float age=r.MaxLife-r.Life,progress=age/r.MaxLife;
                float radius=.1f+r.Size*(1-(1-progress)*(1-progress));
                float cell=.14f+r.Size*.018f;
                for(int i=0;i<48;i++)
                {
                    if(progress>=r.FadeAt[i]) continue;
                    float angle=i/48f*Mathf.PI*2+r.Drift[i]*progress*.15f;
                    float spread=radius+r.Size*r.Drift[i]*progress*progress;
                    var at=r.Position+new Vector3(
                        Mathf.Round(Mathf.Cos(angle)*spread/cell)*cell+Mathf.Sin(age*1.6f+i/48f*Mathf.PI*2)*progress*.12f,
                        Mathf.Round(Mathf.Sin(angle)*spread/cell)*cell-age*age*.25f,0);
                    float size=cell*(1-progress*.6f);
                    float brightness=Mathf.Pow(1-progress,1.3f);
                    assets.Glow(r.Color,at,new Vector3(size*4,size*4,1),gameCamera,brightness*.5f);
                    assets.Glow(r.Color,at,new Vector3(size,size,1),gameCamera,brightness,0,false,true);
                }
            }
            assets.Flush(gameCamera);
        }
        void DrawGround(Entity e)
        {
            if(e.Destroyed) { DrawBlock("#090909",Position(e,7),new Vector3(1.5f,1.5f,.1f)); return; }
            float scale=e.Kind=="cart"?(float)e.Scale:1;
            Draw(e.Kind,e,scale,8,(float)e.Bank*Mathf.Rad2Deg);
            Draw(e.Kind+"gun",e,scale,7.5f,(float)e.Aim*Mathf.Rad2Deg);
        }
        void ToggleSound() { audioPlayer.SetMuted(!audioPlayer.Muted); menuMusic.SetMuted(audioPlayer.Muted); PlayerPrefs.SetInt("neon-shooter.muted",audioPlayer.Muted?1:0); }
        void ToggleMotion() { reducedMotion=!reducedMotion; bloom.ReducedMotion=reducedMotion; Model.ScenerySpeed=reducedMotion?.072:3.6; PlayerPrefs.SetInt("neon-shooter.reduced-motion",reducedMotion?1:0); }
        void OnDestroy() { if(Model!=null) Model.OnEvent-=Handle; assets?.Dispose(); DisposeUi(); }
    }
}
