using System;
using System.Collections.Generic;
using System.Collections;
using NeonShooter.Core;
using UnityEngine;

namespace NeonShooter.Unity
{
    // Layout, colours, copy and fonts follow TermsShooter.vue and terms-shooter.css.
    public sealed partial class NeonGame
    {
        Font scoreFont,controlFont;
        Vector2 battleScroll;
        Texture2D pausedBackdrop;
        bool capturingBackdrop;
        int backdropWidth,backdropHeight;
        void UpdateBackdrop()
        {
            bool needed=launched && (Model.Paused || Model.Phase=="gameover");
            if(!needed || backdropWidth!=Screen.width || backdropHeight!=Screen.height)
            {
                if(pausedBackdrop!=null) { Destroy(pausedBackdrop); pausedBackdrop=null; }
            }
            if(needed && pausedBackdrop==null && !capturingBackdrop && !Application.isBatchMode)
                StartCoroutine(CaptureBackdrop());
        }
        IEnumerator CaptureBackdrop()
        {
            capturingBackdrop=true;
            yield return new WaitForEndOfFrame();
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            if(image!=null)
            {
                backdropWidth=Screen.width; backdropHeight=Screen.height;
                int width=Mathf.Max(1,image.width/4),height=Mathf.Max(1,image.height/4);
                var source=image.GetPixels32(); var pixels=new Color[width*height];
                for(int y=0;y<height;y++) for(int x=0;x<width;x++)
                {
                    Color sum=Color.clear;
                    for(int dy=0;dy<4;dy++) for(int dx=0;dx<4;dx++)
                        sum+=((Color)source[Mathf.Min(image.height-1,y*4+dy)*image.width+Mathf.Min(image.width-1,x*4+dx)]).linear;
                    pixels[y*width+x]=sum/16;
                }
                var buffer=new Color[pixels.Length];
                for(int pass=0;pass<3;pass++)
                {
                    for(int y=0;y<height;y++) for(int x=0;x<width;x++)
                    {
                        Color sum=Color.clear;
                        for(int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++)
                            sum+=pixels[Mathf.Clamp(y+dy,0,height-1)*width+Mathf.Clamp(x+dx,0,width-1)];
                        buffer[y*width+x]=sum/9;
                    }
                    var swap=pixels; pixels=buffer; buffer=swap;
                }
                pausedBackdrop=new Texture2D(width,height,TextureFormat.RGBAFloat,false,true) { filterMode=FilterMode.Bilinear };
                for(int i=0;i<pixels.Length;i++) pixels[i]=UiUpload(pixels[i]);
                pausedBackdrop.SetPixels(pixels); pausedBackdrop.Apply(); Destroy(image);
            }
            capturingBackdrop=false;
        }
        Texture2D roundedButton,headerGradient,footerGradient,vignette,shieldBar,buttonBackground;
        readonly Dictionary<string,Texture2D> solidTextures=new Dictionary<string,Texture2D>();
        readonly Dictionary<string,GUIStyle> textStyles=new Dictionary<string,GUIStyle>();
        static Color UiColor(string html) { Color c; return ColorUtility.TryParseHtmlString(html,out c)?c.linear:Color.white; }
        GUIStyle TextStyle(int size,string color,TextAnchor align=TextAnchor.UpperCenter,Font font=null)
        {
            font=font??displayFont;
            string key=size+color+align+(font!=null?font.name:"");
            if(!textStyles.TryGetValue(key,out var style))
            {
                style=new GUIStyle { font=font,fontSize=size,alignment=align,wordWrap=false,clipping=TextClipping.Overflow };
                style.normal.textColor=UiColor(color); textStyles.Add(key,style);
            }
            return style;
        }
        void Text(float x,float y,float w,float h,string text,int size=13,string color="#ffffff",TextAnchor align=TextAnchor.UpperCenter,Font font=null)
        {
            var style=TextStyle(size,color,align,font);
            float tracking=text=="SCORE" || text=="WAVE" || text=="SHIELDS" || text=="MISSILES"?size*.12f:
                text.StartsWith("SYSTEMS READY") || text=="PRESS LAUNCH WHEN READY" || text=="TAKE A BREATH. THE UNIVERSE CAN WAIT."?size*.1f:
                text=="YOU MADE IT." || text=="NOW MAKE IT OUT."?size*.01f:0;
            if(tracking==0) { GUI.Label(new Rect(x,y,w,h),text,style); return; }
            float length=tracking*Mathf.Max(0,text.Length-1);
            foreach(char c in text) length+=style.CalcSize(new GUIContent(c.ToString())).x;
            float left=align==TextAnchor.UpperLeft?x:align==TextAnchor.UpperRight?x+w-length:x+(w-length)/2;
            var glyphStyle=TextStyle(size,color,TextAnchor.UpperLeft,font);
            foreach(char c in text)
            {
                string glyph=c.ToString(); float advance=style.CalcSize(new GUIContent(glyph)).x;
                GUI.Label(new Rect(left,y,advance+4,h),glyph,glyphStyle); left+=advance+tracking;
            }
        }
        void GlowText(float x,float y,float w,float h,string text,int size,string color,Font font=null)
        {
            var old=GUI.color;
            foreach(float radius in new[]{1f,3f,6f})
                for(int i=0;i<8;i++)
                {
                    float angle=i*Mathf.PI/4;
                    GUI.color=new Color(1,1,1,radius<2?.005f:radius<4?.002f:.0005f);
                    Text(x+Mathf.Cos(angle)*radius,y+Mathf.Sin(angle)*radius,w,h,text,size,color,TextAnchor.UpperCenter,font);
                }
            GUI.color=old; Text(x,y,w,h,text,size,color,TextAnchor.UpperCenter,font);
        }
        // IMGUI floating-point textures use the fast gamma curve (without its
        // low-end linear segment). Invert that curve before the final sRGB write.
        // This also preserves dark purple instead of crushing it to black.
        static Color UiUpload(Color linear)
        {
            return new Color(Mathf.Pow((linear.r+.055f)/1.055f,2.4f),Mathf.Pow((linear.g+.055f)/1.055f,2.4f),Mathf.Pow((linear.b+.055f)/1.055f,2.4f),linear.a);
        }
        static Texture2D UiTexture(int width,int height,Func<int,int,Color> pixel)
        {
            var image=new Texture2D(width,height,TextureFormat.RGBAFloat,false,true) { wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear };
            var pixels=new Color[width*height];
            for(int y=0;y<height;y++) for(int x=0;x<width;x++) pixels[y*width+x]=UiUpload(pixel(x,y).linear);
            image.SetPixels(pixels); image.Apply(); return image;
        }
        void Styles()
        {
            if(roundedButton!=null) return;
            scoreFont=Resources.Load<Font>("Fonts/OffBit"); controlFont=Resources.Load<Font>("Fonts/NeuePower");
            button=ArcadeButtons.CreateStyle(controlFont);
            roundedButton=UiTexture(32,32,(x,y)=>{
                float dx=Mathf.Max(0,8-Mathf.Min(x,31-x)),dy=Mathf.Max(0,8-Mathf.Min(y,31-y));
                return new Color(1,1,1,Mathf.Clamp01(8.5f-Mathf.Sqrt(dx*dx+dy*dy)));
            });
            buttonBackground=UiTexture(32,32,(x,y)=>{
                float dx=Mathf.Max(0,8-Mathf.Min(x,31-x)),dy=Mathf.Max(0,8-Mathf.Min(y,31-y));
                var c=ColorUtility.TryParseHtmlString("#140128",out var tint)?tint:Color.black;
                c.a=Mathf.Clamp01(8.5f-Mathf.Sqrt(dx*dx+dy*dy)); return c;
            });
            button.normal.background=button.active.background=button.focused.background=buttonBackground;
            button.hover.background=roundedButton;
            button.border=new RectOffset(8,8,8,8);
            shieldBar=UiTexture(32,10,(x,y)=>new Color(1,1,1,x>=4-y*.4f && x<31-y*.4f?1:0));
            headerGradient=UiTexture(1,128,(x,y)=>new Color(8/255f,3/255f,24/255f,.86f*y/127));
            footerGradient=UiTexture(1,128,(x,y)=>new Color(8/255f,3/255f,24/255f,.86f*(1-y/127f)));
            vignette=UiTexture(128,128,(x,y)=>{
                float edge=Mathf.Min(Mathf.Min(x,127-x),Mathf.Min(y,127-y))/127f;
                return new Color(4/255f,1/255f,13/255f,.6f*Mathf.Pow(1-Mathf.Clamp01(edge/.16f),2));
            });
        }
        bool Button(float x,float y,float w,string text,bool primary=false)
        {
            var rect=new Rect(x,y,w,primary?49:44);
            bool hover=rect.Contains(Event.current.mousePosition);
            var old=GUI.backgroundColor;
            GUI.backgroundColor=Color.white;
            button.normal.background=hover?roundedButton:buttonBackground;
            foreach(var state in new[]{button.normal,button.hover,button.active,button.focused}) state.textColor=hover?UiColor("#140128"):UiColor("#caff60");
            bool clicked=GUI.Button(rect,text,button); GUI.backgroundColor=old; return clicked;
        }
        void SolidRect(Rect rect,string color,float alpha=1)
        {
            string key=color+alpha;
            if(!solidTextures.TryGetValue(key,out var image))
            {
                image=new Texture2D(1,1,TextureFormat.RGBAFloat,false,true);
                var tint=UiColor(color); tint.a=alpha; image.SetPixel(0,0,UiUpload(tint)); image.Apply(); solidTextures.Add(key,image);
            }
            var old=GUI.color; GUI.color=Color.white;
            GUI.DrawTexture(rect,image); GUI.color=old;
        }
        void OnGUI()
        {
            UpdateUiViewport(); Styles(); GUI.matrix=Matrix4x4.Scale(new Vector3(uiScale,uiScale,1));
            float width=uiWidth,height=uiHeight;
            if(!launched) DrawBriefing(width,height);
            else
            {
                GUI.DrawTexture(new Rect(0,0,width,height),vignette);
                GUI.DrawTexture(new Rect(0,0,width,205),headerGradient);
                Text(32,24,190,28,"NEON SHOOTER",21,"#ffffff",TextAnchor.UpperLeft);
                Text(32,55,190,14,"A F T E R  H O U R S",9,"#80dfff",TextAnchor.UpperLeft);
                if(Button(width-450,24,84,Model.Paused?"RESUME":"PAUSE")) Pause();
                if(Button(width-342,24,115,audioPlayer.Muted?"SOUND OFF":"SOUND ON")) ToggleSound();
                if(Button(width-203,24,171,"FLIGHT BRIEFING ↗")) Leave();
                float left=(width-Mathf.Min(760,width-64))/2,statsWidth=Mathf.Min(760,width-64);
                float unit=(statsWidth-72)/5;
                float[] centres={left+unit,left+2*unit+24+unit*.5f,left+3*unit+48+unit*.5f,left+4*unit+72+unit*.5f};
                string[] labels={"SCORE","WAVE","SHIELDS","MISSILES"};
                string[] values={Model.Score.ToString("D6"),Model.Wave.ToString("D2"),Model.Lives.ToString("D2"),Model.MissileCount.ToString("D2")};
                string[] colors={"#ffeaa7","#93f3ff","#93f3ff","#ff803b"};
                for(int i=0;i<4;i++)
                {
                    float w=i==0?unit*2:unit;
                    Text(centres[i]-w/2,89,w,24,labels[i],13,"#a396ba");
                    GlowText(centres[i]-w/2,106,w,70,values[i],i==0?Mathf.RoundToInt(Mathf.Clamp(width*.044f,40,62)):Mathf.RoundToInt(Mathf.Clamp(width*.04f,36,56)),colors[i],i==0?scoreFont:displayFont);
                    if(i<2)
                    {
                        Text(centres[i]-38,184,24,20,"PB",12,"#ff48c4");
                        Text(centres[i]-10,179,65,26,history.Data.battles.Count==0?"—":(i==0?history.Data.bestScore:history.Data.bestWave).ToString(),18,"#c4b5d6",TextAnchor.UpperLeft,scoreFont);
                    }
                }
                for(int i=0;i<3;i++)
                {
                    var tint=GUI.color; GUI.color=UiColor(i<Model.Lives?"#7bf1ff":"#3b294d");
                    GUI.DrawTexture(new Rect(centres[2]-36+i*28,175,22,7),shieldBar); GUI.color=tint;
                }
                DrawTimers(width,height);
                if(Model.FleetRadioTime>0)
                {
                    SolidRect(new Rect(16,height-116,350,70),"#081222",.93f);
                    Text(32,height-105,320,20,"FLEET COMMS · FOUR "+(Model.FleetRadioMessage=="Good job guys!"?"OUTBOUND":"INBOUND"),10,"#93f3ff",TextAnchor.UpperLeft);
                    Text(32,height-80,320,28,Model.FleetRadioMessage,16,"#d2fcff",TextAnchor.UpperLeft);
                }
                if(Time.unscaledTime<calloutUntil && !Model.Paused && Model.Phase!="countdown" && Model.Phase!="gameover")
                    GlowText(width/2-350,height*.26f,700,60,callout,25,"#ffe397");
                if(Model.Paused) Overlay("PAUSED","MOVE MOUSE OR O / P & W / S · SPACE / HOLD CLICK TO FIRE","RESUME FLIGHT →",Pause);
                else if(Model.Phase=="gameover") DrawGameOver(width,height);
                else if(Model.Phase=="countdown")
                    GlowText(0,height/2-125,width,250,Model.Message,Mathf.RoundToInt(Mathf.Clamp(width*.16f,90,220)),"#d2fcff");
                GUI.DrawTexture(new Rect(0,height-82,width,82),footerGradient);
                DrawControls(32,height-49,false);
                Text(width-180,height-43,148,20,"FULL WAVE = ★ 10",10,"#ffe3a2",TextAnchor.UpperRight);
                if(Input.touchSupported && Button(width-180,height-130,148,"MISSILE [B]")) Model.Advance(1.0/120,new FlightInput { Missile=true });
            }
            if(historyVisible) DrawHistory();
        }
        void DrawBriefing(float width,float height)
        {
            SolidRect(new Rect(0,0,width,height),"#080318");
            Text(32,24,220,28,"NEON SHOOTER / AFTER HOURS",12,"#caff60",TextAnchor.UpperLeft);
            float y=Mathf.Max(80,height/2-140);
            Text(0,y+8,width,25,"SYSTEMS READY. YOUR NEXT FLIGHT STARTS HERE.",11,"#b4a1ca");
            int size=Mathf.RoundToInt(Mathf.Clamp(width*.07f,44,95));
            Text(0,y+34,width,110,"YOU MADE IT.",size);
            GlowText(0,y+33+size*1.05f,width,110,"NOW MAKE IT OUT.",size,"#65e9ff");
            Text(0,y+255,width,35,"There’s a whole universe down here.",17,"#c1b2cf",TextAnchor.UpperCenter,bodyFont);
            DrawControls(width/2-255,y+311,true);
            if(Button(width/2-96,y+370,192,"LAUNCH FLIGHT ↗",true)) Launch();
            Text(0,y+453,width,20,"PRESS LAUNCH WHEN READY",10,"#ae9cc5");
            if(Button(32,height-65,130,audioPlayer.Muted?"SOUND OFF":"SOUND ON")) ToggleSound();
            if(Button(180,height-65,205,reducedMotion?"REDUCED MOTION ON":"REDUCED MOTION OFF")) ToggleMotion();
            if(Button(width-200,height-65,168,"PAST BATTLES")) historyVisible=true;
        }
        void DrawControls(float x,float y,bool launch)
        {
            int size=launch?11:10;
            string color=launch?"#c8bfd9":"#998dab";
            Action<string> label=text=>{
                float width=TextStyle(size,color).CalcSize(new GUIContent(text)).x;
                Text(x,y+7,width,24,text,size,color,TextAnchor.UpperLeft); x+=width+6;
            };
            Action<string> key=text=>{
                float width=TextStyle(size,"#ffffff").CalcSize(new GUIContent(text)).x+14;
                SolidRect(new Rect(x,y,width,24),"#b5c7ff",.25f);
                SolidRect(new Rect(x+1,y+1,width-2,22),"#100824",.9f);
                Text(x,y+6,width,20,text,size); x+=width+6;
            };
            label(launch?"MOUSE TO FLY":"MOUSE OR");
            key("O"); key("P"); label(launch?"LEFT / RIGHT":"MOVE");
            key("W"); key("S"); label(launch?"UP / DOWN":"↑↓");
            key("SPACE"); label("FIRE"); key("B"); label("MISSILE");
            if(!launch) { key("ESC"); label("PAUSE"); }
        }
        void DrawTimers(float width,float height)
        {
            var active=new List<(string key,string label,double time,double duration,string color)>();
            if(Model.WeaponTime>0) active.Add((Model.Weapon,Model.Weapon.ToUpperInvariant(),Model.WeaponTime,12,"#ff803b"));
            if(Model.ShieldTime>0) active.Add(("shield","SHIELD",Model.ShieldTime,8,"#54ffb5"));
            if(Model.SpeedTime>0) active.Add(("speed","BOOST",Model.SpeedTime,15,"#28eaff"));
            if(Model.OrbitTime>0) active.Add(("orbit","ORBIT x"+Model.OrbitCount,Model.OrbitTime,10,"#54ffb5"));
            if(Model.CompanionTime>0) active.Add((Model.CompanionMode,Model.CompanionMode.ToUpperInvariant(),Model.CompanionTime,12,"#8855ff"));
            if(Model.ScatterTime>0) active.Add(("scatter","SCATTER",Model.ScatterTime,12,"#ff803b"));
            if(Model.FleetTime>0) active.Add(("fleet","FLEET",Model.FleetTime,10,"#28eaff"));
            if(Model.AssaultTime>0) active.Add(("assault","ASSAULT",Model.AssaultTime,20,"#ff803b"));
            for(int i=0;i<active.Count;i++)
            {
                var entry=active[i]; float x=32,y=195+i*55;
                SolidRect(new Rect(x,y,220,40),"#0c071f",.85f);
                if(SpriteCatalog.Icons.TryGetValue(entry.key,out var rows))
                    for(int row=0;row<rows.Length;row++) for(int col=0;col<rows[row].Length;col++)
                        if(rows[row][col]!='.') SolidRect(new Rect(x+10+col*3,y+9+row*3,2.6f,2.6f),entry.color);
                Text(x+40,y+12,140,20,entry.label,11,entry.color,TextAnchor.UpperLeft);
                Text(x+177,y+10,36,20,Math.Ceiling(entry.time)+"s",13,entry.color);
                SolidRect(new Rect(x,y+38,220*Mathf.Clamp01((float)(entry.time/entry.duration)),2),entry.color);
            }
        }
        void Overlay(string text,string detail,string action,Action clicked)
        {
            if(capturingBackdrop) return;
            if(pausedBackdrop!=null) GUI.DrawTexture(new Rect(0,0,uiWidth,uiHeight),pausedBackdrop);
            SolidRect(new Rect(0,0,uiWidth,uiHeight),"#070316",.6f);
            float y=uiHeight/2-132;
            Text(0,y-24,uiWidth,25,"TAKE A BREATH. THE UNIVERSE CAN WAIT.",11,"#c4b5d6");
            GlowText(0,y,uiWidth,160,text,Mathf.RoundToInt(Mathf.Clamp(uiWidth*.1f,60,150)),"#d2fcff");
            if(Button(uiWidth/2-96,y+172,192,action,true)) clicked();
            if(Button(uiWidth/2-86,y+245,172,"BACK TO BRIEFING")) Leave();
        }
        void DrawGameOver(float width,float height)
        {
            if(capturingBackdrop) return;
            if(pausedBackdrop!=null) GUI.DrawTexture(new Rect(0,0,width,height),pausedBackdrop);
            SolidRect(new Rect(0,0,width,height),"#070316",.6f);
            float tableHeight=Mathf.Min(256,history.Data.battles.Count==0?40:64+history.Data.battles.Count*42);
            float shift=(tableHeight-106)/2+(800-height)/2;
            Text(0,142-shift,width,24,"THE CANYON WILL BE WAITING.",11,"#c4b5d6");
            GlowText(0,164-shift,width,140,"GAME OVER",Mathf.RoundToInt(Mathf.Clamp(width*.1f,60,150)),"#d2fcff");
            DrawBattleTable(width/2-290,334-shift,580);
            if(Button(width/2-103,438-shift+tableHeight,206,"ONE MORE FLIGHT ↗",true)) Launch();
            if(Button(width/2-86,511-shift+tableHeight,172,"BACK TO BRIEFING")) Leave();
        }
        void DrawBattleTable(float x,float y,float width)
        {
            Text(x,y,width,40,"Past battles",30,"#d2fcff",TextAnchor.UpperCenter,scoreFont);
            string[] headings={"Score","Wave","Enemies killed"};
            for(int i=0;i<3;i++) Text(x+i*width/3,y+50,width/3,25,headings[i],14,"#c4b5d6");
            int count=history.Data.battles.Count;
            float contentHeight=count==0?40:64+count*42;
            battleScroll=GUI.BeginScrollView(new Rect(x,y+80,width,Mathf.Min(uiHeight*.32f,350,contentHeight)),battleScroll,new Rect(0,0,width-16,contentHeight),false,false);
            if(count>0)
            {
                SolidRect(new Rect(0,0,width,64),"#10071b");
                var best=history.Data.battles.Find(record=>record.score==history.Data.bestScore);
                Text(0,8,width/3,20,"Personal best",12,"#b5ff00");
                Text(0,28,width/3,26,history.Data.bestScore.ToString(),18,"#b5ff00");
                Text(width/3,20,width/3,26,best==null?"—":best.wave.ToString(),18,"#b5ff00");
                Text(width*2/3,20,width/3,26,best==null?"—":best.kills.ToString(),18,"#b5ff00");
                for(float dash=0;dash<width;dash+=7) SolidRect(new Rect(dash,63,3,1),"#b5ff00",.5f);
            }
            for(int i=0;i<count;i++)
            {
                var record=history.Data.battles[i]; float row=64+i*42;
                string color=i==0?"#ffeaa7":"#d2fcff";
                Text(0,row+10,width/3,25,record.score.ToString(),18,color);
                Text(width/3,row+10,width/3,25,record.wave.ToString(),18,color);
                Text(width*2/3,row+10,width/3,25,record.kills.ToString(),18,color);
                for(float dash=0;dash<width;dash+=7) SolidRect(new Rect(dash,row+41,3,1),"#b5ff00",.5f);
            }
            if(count==0) Text(0,15,width,40,"Your first flight is waiting.",14,"#c4b5d6");
            GUI.EndScrollView();
        }
        void DrawHistory()
        {
            SolidRect(new Rect(0,0,uiWidth,uiHeight),"#070316",.96f);
            DrawBattleTable(uiWidth/2-290,uiHeight/2-240,580);
            if(Button(uiWidth/2-90,uiHeight-100,180,"BACK")) historyVisible=false;
        }
        void DisposeUi()
        {
            foreach(var image in new[]{roundedButton,headerGradient,footerGradient,vignette,shieldBar,buttonBackground}) if(image!=null) Destroy(image);
            foreach(var image in solidTextures.Values) Destroy(image); solidTextures.Clear();
            if(pausedBackdrop!=null) Destroy(pausedBackdrop);
            textStyles.Clear();
        }
    }
}
