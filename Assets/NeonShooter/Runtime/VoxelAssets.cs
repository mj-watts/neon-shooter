using System;
using System.Collections.Generic;
using NeonShooter.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonShooter.Unity
{
    // Each silhouette becomes one vertex-colored mesh, shared by every instance.
    public sealed class VoxelAssets : IDisposable
    {
        readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
        readonly Dictionary<string,Material> glows=new Dictionary<string,Material>();
        sealed class Batch
        {
            public Mesh Mesh;
            public Material Material;
            public readonly List<Matrix4x4> Matrices=new List<Matrix4x4>();
        }
        readonly Dictionary<string,Batch> batches=new Dictionary<string,Batch>();
        readonly Matrix4x4[] instanceMatrices=new Matrix4x4[1023];
        public readonly Material Solid, Canyon,Exhaust;
        public readonly Mesh Quad;
        readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();
        readonly List<Color> colors=new List<Color>();
        readonly List<int> triangles=new List<int>();
        public static Color ColorOf(string value)
        {
            switch(value) { case "blue":value="#289dff";break;case "cyan":value="#28eaff";break;case "mint":value="#54ffb5";break;case "red":value="#ff2020";break;case "pink":value="#ff27cc";break;case "orange":value="#ff803b";break;case "gold":value="#ffe569";break;case "violet":value="#8855ff";break; }
            Color c; return ColorUtility.TryParseHtmlString(value,out c)?c.linear:Color.white;
        }

        public VoxelAssets()
        {
            Solid=new Material(Resources.Load<Shader>("Shaders/Voxel")); Solid.enableInstancing=true; Canyon=new Material(Resources.Load<Shader>("Shaders/Canyon"));
            Exhaust=new Material(Resources.Load<Shader>("Shaders/Exhaust"));
            Quad=new Mesh { name="Unit quad" };
            Quad.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
            Quad.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)}; Quad.triangles=new[]{0,1,2,0,2,3}; Quad.RecalculateBounds();
            BuildSprites();
        }
        void Begin() { vertices.Clear(); normals.Clear(); colors.Clear(); triangles.Clear(); }
        void Box(float x,float y,float z,float w,float h,float depth,Color color)
        {
            var p=new Vector3(x,y,z); var e=new Vector3(w,h,depth)*.5f;
            Vector3[] corners={ p+new Vector3(-e.x,-e.y,-e.z),p+new Vector3(e.x,-e.y,-e.z),p+new Vector3(e.x,e.y,-e.z),p+new Vector3(-e.x,e.y,-e.z),p+new Vector3(-e.x,-e.y,e.z),p+new Vector3(e.x,-e.y,e.z),p+new Vector3(e.x,e.y,e.z),p+new Vector3(-e.x,e.y,e.z) };
            int[][] faces={new[]{0,3,2,1},new[]{4,5,6,7},new[]{0,4,7,3},new[]{1,2,6,5},new[]{0,1,5,4},new[]{3,7,6,2}};
            Vector3[] ns={Vector3.back,Vector3.forward,Vector3.left,Vector3.right,Vector3.down,Vector3.up};
            for(int f=0;f<6;f++) { int start=vertices.Count; foreach(int i in faces[f]) { vertices.Add(corners[i]); colors.Add(color); normals.Add(ns[f]); } triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3}); }
        }
        Mesh End(string key)
        {
            var mesh=new Mesh { name=key,indexFormat=IndexFormat.UInt32 }; mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetNormals(normals); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds(); meshes.Add(key,mesh); return mesh;
        }
        void Pixels(string[] rows,float cell,Dictionary<char,string> palette,float brightness=1)
        {
            for(int r=0;r<rows.Length;r++) for(int c=0;c<rows[r].Length;c++)
            {
                char symbol=rows[r][c]; if(symbol=='.') continue;
                string html; palette.TryGetValue(symbol,out html); Color color=ColorOf(html??"cyan")*brightness;
                float z=symbol=='G'?-.16f:symbol=='B'?-.08f:0;
                Box((c-(rows[r].Length-1)*.5f)*cell,((rows.Length-1)*.5f-r)*cell,z,cell*.88f,cell*.88f,cell*.72f,color);
            }
        }
        Dictionary<char,string> Palette(string main="pink",bool hero=false)
        { return new Dictionary<char,string> { {'B',"#293ecb"},{'V',"#8138cd"},{'P',hero?"#ed35d7":main},{'C',"#32d8ef"},{'G',"#ffbf32"},{'O',"#fa7437"},{'X',main} }; }
        void BuildSprites()
        {
            Begin(); Box(0,0,0,1,1,1,ColorOf("mint")*.95f); End("orbTrail");
            Begin(); Pixels(SpriteCatalog.Hero,.26f,Palette("cyan",true)); End("hero");
            for(int i=0;i<3;i++) { Begin(); Pixels(SpriteCatalog.Enemies[i],.29f,Palette(i==1?"orange":"pink")); End("enemy"+i); }
            Begin(); Pixels(SpriteCatalog.Enemies[2],.29f,Palette("gold")); End("heavy");
            foreach(var pair in SpriteCatalog.Fleet) { Begin(); Pixels(pair.Value,.26f,SpriteCatalog.FleetPalettes[pair.Key]); End("fleet"+pair.Key); }
            foreach(var p in PowerCatalog.All)
            {
                Begin(); Pixels(SpriteCatalog.Icons[p.Key],.14f,new Dictionary<char,string> { {'X',p.Color} },1.8f);
                for(int s=-1;s<=1;s+=2) { Box(s*.68f,0,.08f,.05f,1.4f,.05f,ColorOf(p.Color)); Box(0,s*.68f,.08f,1.4f,.05f,.05f,ColorOf(p.Color)); } End("power"+p.Key);
            }
            Begin(); Pixels(SpriteCatalog.Heart,.15f,Palette("red"),1.8f); End("life");
            Begin(); Pixels(SpriteCatalog.Star,.11f,Palette("gold"),1.8f); End("star");
            Begin(); Box(0,0,0,.12f,.42f,.09f,ColorOf("blue")*2); End("shot1");
            Begin(); Box(0,0,0,.14f,.42f,.09f,ColorOf("orange")*2); End("shot2");
            Begin(); Box(0,0,0,.24f,.42f,.09f,ColorOf("violet")*2); Box(0,0,-.08f,.08f,.34f,.03f,Color.white*2); End("shot3");
            foreach(string color in new[]{"cyan","mint","pink","orange","gold","red","violet","#090909","#70505a"}) { Begin(); Box(0,0,0,1,1,1,ColorOf(color)*1.8f); End("block"+color); }
            foreach(string color in new[]{"blue","cyan","mint","pink","orange","gold","red","violet"}) { Begin(); Box(0,0,0,.9f,.9f,.03f,ColorOf(color)*3); End("ring"+color); }
            Begin(); Pixels(new[]{"..X..",".XXX.",".XXX.",".XXX.","XXXXX","X.X.X","..X.."},.09f,Palette("orange"),2); End("missile");
            Begin(); Pixels(new[]{"..WWWWW..",".WCCCCCW.","WWCCCCCWW","GWCCCCCWG","GGWWWWWGG","YGGWWWGGY",".GGGWGGG.","..GGWGG..","...W.W..."},.2f,new Dictionary<char,string>{{'W',"#b3dacd"},{'G',"#44d99d"},{'C',"#153846"},{'Y',"#ffc954"}});
            Box(0,.36f,-.16f,.3f,.29f,.08f,ColorOf("#ffd69d")); Box(0,.52f,-.18f,.34f,.08f,.04f,ColorOf("mint"));
            for(int s=-1;s<=1;s+=2) Box(s*.065f,.4f,-.22f,.035f,.04f,.02f,ColorOf("#2a111c"));
            Box(.24f,.4f,-.16f,.08f,.3f,.04f,ColorOf("mint")); End("civilian");
            foreach(string kind in new[]{"turret","tank","laserTurret","cart"}) BuildGround(kind);
            Begin();
            for(int row=0;row<48;row++)
            {
                int half=row<8?3+row/2:row>41?10:13;
                for(int col=-half;col<=half;col++)
                {
                    bool edge=Math.Abs(col)==half,spine=Math.Abs(col)<=2,hangar=row>14 && row<31 && Math.Abs(col)<5,bridge=row>5 && row<10 && Math.Abs(col)<3,engine=row>43 && (Math.Abs(col)<3 || Math.Abs(col)>7);
                    string color=engine?"#ff853d":bridge?"#8ac9ce":edge && row%4==0?"#d67849":hangar?"#171629":spine?"#67627a":edge?"#7b7289":row%6==0 || col%6==0?"#343044":"#4b475c";
                    Box(col*.5f,11.75f-row*.5f,spine?-.12f:0,.47f,.47f,spine?.2f:.1f,ColorOf(color)*.32f);
                }
            }
            End("capital");
            Begin(); Box(0,0,0,1.35f,1.25f,.15f,ColorOf("#282438")*.32f); for(int s=-1;s<=1;s+=2) Box(s*.3f,.5f,-.15f,.2f,1.2f,.12f,ColorOf("#93899f")*.32f); End("capitalGun");
        }
        void BuildGround(string kind)
        {
            bool tank=kind=="tank",cart=kind=="cart";
            var palette=new Dictionary<char,string>{{'D',cart?"#463322":"#1d1638"},{'B',tank?"#287b67":"#504470"},{'E',tank?"#7bf9a5":kind=="laserTurret"?"red":"#ffa257"},{'G',cart?"#b7f6ff":tank?"#c7ff87":kind=="laserTurret"?"#ff6975":"#ffe098"},{'T',"#2c2641"},{'S',"#d6a45c"},{'H',"#ffe1a0"},{'R',"#72665a"}};
            Begin(); Pixels(cart?new[]{"..HHH..","..SSS..",".RSSSR.","..S.S..","..S.S..",".RSSSR.","..SSS..","..HHH.."}:tank?new[]{"TT.BBB.TT","TEBBBBBET","TTBBBBBTT","TEBGGGBET","TTBBBBBTT","TEBBBBBET","TT.BBB.TT"}:new[]{"..EEEEE..",".EBBBBBE.","EBBDBDBBE","EBDBBBDBE","EBBBGBBBE","EBDBBBDBE","EBBDBDBBE",".EBBBBBE.","..EEEEE.."},.2f,palette); End(kind);
            Begin(); Pixels(new[]{"..G..","..G..","..E..",".BBB.","EBGBE",".BBB.","..E.."},.2f,palette); End(kind+"gun");
        }
        public void Draw(string key,Vector3 position,Vector3 scale,float angle,Camera camera)
        { Queue(key,meshes[key],Solid,Matrix4x4.TRS(position,Quaternion.Euler(0,0,angle),scale)); }
        void Queue(string key,Mesh mesh,Material material,Matrix4x4 matrix)
        {
            Batch batch;
            if(!batches.TryGetValue(key,out batch)) { batch=new Batch { Mesh=mesh,Material=material }; batches.Add(key,batch); }
            batch.Matrices.Add(matrix);
        }
        public void Flush(Camera camera)
        {
            foreach(var batch in batches.Values)
            {
                for(int offset=0;offset<batch.Matrices.Count;offset+=1023)
                {
                    int count=Math.Min(1023,batch.Matrices.Count-offset);
                    batch.Matrices.CopyTo(offset,instanceMatrices,0,count);
                    Graphics.DrawMeshInstanced(batch.Mesh,0,batch.Material,instanceMatrices,count,null,ShadowCastingMode.Off,false,0,camera,LightProbeUsage.Off);
                }
                batch.Matrices.Clear();
            }
        }
        public void Glow(string color,Vector3 position,Vector3 scale,Camera camera,float alpha=.5f,float angle=0,bool spark=false,bool block=false)
        {
            // Fading glows share 32 intensity levels instead of allocating a new
            // material for every spark and every frame.
            alpha=Mathf.Round(Mathf.Clamp01(alpha)*32)/32;
            if(alpha<=0) return;
            string key=(block?"block":spark?"spark":"halo")+color+alpha;
            Material m; if(!glows.TryGetValue(key,out m)) { m=new Material(Resources.Load<Shader>(block?"Shaders/ExplosionBlock":spark?"Shaders/Spark":"Shaders/Glow")); m.enableInstancing=true; Color c=ColorOf(color)*(spark?2:3); c.a=alpha; m.SetColor("_Color",c); glows.Add(key,m); }
            Queue("glow"+key,Quad,m,Matrix4x4.TRS(position,Quaternion.Euler(0,0,angle),scale));
        }
        public void DrawExhaust(Vector3 position,float boost,float time,float angle,Camera camera)
        {
            Exhaust.SetFloat("boost",boost); Exhaust.SetFloat("time",time);
            var rotation=Quaternion.Euler(0,0,angle);
            Graphics.DrawMesh(Quad,Matrix4x4.TRS(position+rotation*Vector3.down*3,rotation,new Vector3(1.2f,4.65f,1)),Exhaust,0,camera,0,null,ShadowCastingMode.Off,false);
            Glow("#783fff",position+rotation*Vector3.down*.74f,new Vector3(1.875f,2.1f,1),camera,.28f,angle);
            Glow("#3eeeff",position+rotation*Vector3.down*.74f,new Vector3(.75f,1.2f,1),camera,.45f,angle);
            Glow("#e5ffff",position+rotation*Vector3.down*.74f,new Vector3(.26f,.49f,1),camera,.7f,angle);
        }
        public void Dispose()
        {
            foreach(var m in meshes.Values) UnityEngine.Object.Destroy(m); foreach(var m in glows.Values) UnityEngine.Object.Destroy(m);
            UnityEngine.Object.Destroy(Exhaust); UnityEngine.Object.Destroy(Solid); UnityEngine.Object.Destroy(Canyon); UnityEngine.Object.Destroy(Quad);
        }
    }
}
