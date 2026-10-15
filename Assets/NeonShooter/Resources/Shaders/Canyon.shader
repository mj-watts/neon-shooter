Shader "NeonShooter/Canyon"
{
 SubShader {
  Tags { "Queue"="Background" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
  Pass {
   Tags { "LightMode"="SRPDefaultUnlit" }
   Cull Off ZWrite Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
   CBUFFER_START(UnityPerMaterial)
   float3 travel,lateral;
   float height,width,musicPulse;
   CBUFFER_END
   struct Attributes { float4 vertex:POSITION; float2 texcoord:TEXCOORD0; };
   static const float3 floorColor=float3(0.01161225,0.00402472,0.03071344);
   static const float3 deepColor=float3(0.01764195,0.00518152,0.05780543);
   static const float3 middleColor=float3(0.02624122,0.00604883,0.11697067);
   static const float3 nearColor=float3(0.03433981,0.00651209,0.17464740);
   static const float3 deepEdge=float3(0.05951124,0.01444384,0.16513219);
   static const float3 middleEdge=float3(0.11193243,0.05126946,0.52099557);
   static const float3 nearEdge=float3(0.26635560,0.03189603,0.46207700);
   static const float3 deepContour=float3(0.15292615,0.06301002,0.36625260);
   static const float3 middleContour=float3(0.18447499,0.10461648,0.60382734);
   static const float3 nearContour=float3(0.39157248,0.08650046,0.65140564);
   float2 modulo(float2 x,float y) { return x-y*floor(x/y); }
   v2f vert(Attributes v) { v2f o; o.pos=TransformObjectToHClip(v.vertex.xyz); o.uv=v.texcoord; return o; }

  float wallHash(float index, float phase){return frac(sin(index*127.1+phase*311.7)*43758.5453);}
  float wallSegment(float y,float spacing,float phase){
    float samplePosition=y/spacing,index=floor(samplePosition);
    return lerp(wallHash(index,phase),wallHash(index+1.,phase),frac(samplePosition))*2.-1.;
  }
  float wall(float y,float phase){return wallSegment(y,6.,phase)*.045+wallSegment(y,2.,phase+4.)*.014;}


  float terrainHash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
  float terrainBox(float2 p,float2 centre,float2 extent,float aa){
    float2 d=abs(p-centre)-extent;
    return 1.-smoothstep(0.,aa,max(d.x,d.y));
  }
  float terrainNoise(float2 samplePosition){
    float2 cell=floor(samplePosition),f=frac(samplePosition);
    float2 blend=f*f*(3.-2.*f);
    return lerp(lerp(terrainHash(cell),terrainHash(cell+float2(1.,0.)),blend.x),
      lerp(terrainHash(cell+float2(0.,1.)),terrainHash(cell+float2(1.,1.)),blend.x),blend.y);
  }
  float terrainHeight(float2 p){
    // Connected hills and valleys; contour lines follow a shared elevation field.
    p*=.065;
    return terrainNoise(p)*.58+terrainNoise(p*2.1+7.3)*.28+terrainNoise(p*4.3+19.7)*.14;
  }
  float terrainContour(float elevation,float intervals,float thickness){
    float level=elevation*intervals;
    float distanceToLine=abs(frac(level+.5)-.5);
    float aa=max(fwidth(level),.0001);
    return 1.-smoothstep(aa*thickness*.35,aa*thickness,distanceToLine);
  }
  float3 terrainMap(float3 fill,float3 ink,float2 samplePosition,float intervals){
    float elevation=terrainHeight(samplePosition);
    float minor=terrainContour(elevation,intervals,1.);
    float major=terrainContour(elevation,intervals/4.,1.7);
    float3 relief=fill*(.76+elevation*.5);
    return lerp(relief,ink,max(minor*.32,major*.62));
  }
  float3 terrainDetails(float3 color,float2 samplePosition,float layer){
    // Derivatives must use continuous world coordinates, before mod/floor and
    // conditional parcels. Wrapped coordinates create screen-wide seam artifacts.
    float aa=max(max(fwidth(samplePosition.x),fwidth(samplePosition.y)),.0001);
    float2 district=floor(samplePosition/12.);
    float2 local=modulo(samplePosition+6.,12.)-6.;
    float seed=terrainHash(district+layer*17.);
    float3 asphalt=float3(.028,.014,.045);
    float roadX=1.-smoothstep(.18,.24,abs(local.x));
    // Long cross-arena road strips read as stray horizontal lines in flight.
    float road=roadX*step(.28,seed);
    color=lerp(color,asphalt,road*.85);
    float marking=(1.-smoothstep(.012,.035,abs(local.x)))*step(.48,frac((local.x+local.y)*.65));
    color=lerp(color,float3(.11,.07,.18),marking*road*.65);
    float2 parcel=floor((local+6.)/2.4);
    float2 p=modulo(local+6.,2.4)-1.2;
    float lot=terrainHash(parcel+district*5.+layer*31.);
    if(terrainHeight(samplePosition+layer*13.)>.49&&lot>.5&&roadX<.1){
      // Four stepped tree crowns and a tiny trunk / offset block shadow.
        float2 tree=p-float2(frac(lot*3.)-.5,frac(lot*7.)-.5)*.8;
        float shadow=terrainBox(tree,float2(.12,-.12),float2(.32,.28),aa);
        float crown=max(terrainBox(tree,float2(0.,0.),float2(.30,.14),aa),terrainBox(tree,float2(0.,0.),float2(.14,.30),aa));
        color=lerp(color,float3(.008,.004,.017),shadow*.8);
        color=lerp(color,lerp(float3(.012,.08,.045),float3(.026,.11,.065),lot),crown*.85);
        color=lerp(color,float3(.045,.18,.08),terrainBox(tree,float2(-.08,.08),float2(.08,.08),aa)*.6);
    }
    // Districts alternate empty rock, small settlements, and denser city blocks.
    if(seed>.68&&lot>.28&&roadX<.1){
      float2 extent=float2(.28+lot*.26,.24+frac(lot*7.)*.35);
      float shadow=terrainBox(p,float2(.13,-.14),extent+.06,aa);
      float roof=terrainBox(p,float2(0.,0.),extent,aa);
      float inset=terrainBox(p,float2(0.,0.),max(extent-.065,float2(.05,.05)),aa);
      float3 trim=lerp(float3(.09,.04,.15),float3(.034,.11,.13),frac(lot*9.));
      color=lerp(color,float3(.004,.002,.012),shadow*.85);
      color=lerp(color,trim,roof*.8);
      color=lerp(color,float3(.025,.012,.045)*(1.+lot*.5),inset*.9);
      float skylight=terrainBox(p,float2(-extent.x*.35,extent.y*.25),float2(.045,.10),aa);
      color=lerp(color,float3(.23,.13,.035),skylight*.75);
    }
    return color;
  }

float3 shelf(float3 under, float3 fill, float3 edge, float x, float boundary, float thickness, float2 samplePosition, float cellSize, float3 contourColor, float layer){
            float mask=smoothstep(boundary-.002,boundary+.002,x);
            float3 mapped=terrainMap(fill,contourColor,samplePosition+layer*5.,28./sqrt(cellSize/.6));
            mapped=terrainDetails(mapped,samplePosition,layer);
            float3 color=lerp(under,mapped,mask);
            // Keep each shelf's contours attached to its own scrolling rock surface.
            float shadow=exp(-abs(x-(boundary-.012))*65.)*(1.-mask);
            color*=1.-shadow*.2;
            float rim=1.-smoothstep(thickness,thickness+.0015,abs(x-boundary));
            color=lerp(color,edge,rim*.85);
            return color;
          }
          float4 frag(v2f i):SV_Target {
 float2 vUv=i.uv;
            float side=vUv.x<.5?-1.:1.;
            float x=abs(vUv.x*2.-1.);
            float y=(vUv.y-.5)*height;
            float deep=.43+wall(y+travel.x,side*.9);
            float middle=.61+wall(y+travel.y,side*1.4+2.);
            float near=.79+wall(y+travel.z,side*.7+4.);
            float worldX=(vUv.x-.5)*width;
            float3 color=floorColor*(.94+.06*cos(x*3.));
            float2 floorPoint=float2(worldX,y+travel.x*.45);
            color=terrainMap(color,deepContour*.65,floorPoint,36.);
            color=terrainDetails(color,floorPoint,0.);
            color=shelf(color,deepColor,deepEdge,x+lateral.x*side*2./width,deep,.0015,float2(worldX+lateral.x,y+travel.x),.6,deepContour,1.);
            color=shelf(color,middleColor,middleEdge,x+lateral.y*side*2./width,middle,.0025,float2(worldX+lateral.y,y+travel.y),1.2,middleContour,2.);
            color=shelf(color,nearColor,nearEdge,x+lateral.z*side*2./width,near,.0018,float2(worldX+lateral.z,y+travel.z),2.4,nearContour,3.);
            return float4(color,1);
          }
 ENDHLSL
 }
 }
}
