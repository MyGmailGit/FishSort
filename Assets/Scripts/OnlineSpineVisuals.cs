using UnityEngine;
using System;
using System.Collections.Generic;
namespace FishSortLocal {
 public sealed partial class FishSortGame {
  [Serializable] sealed class OnlineRegionUV {public float x,y,width,height;}
  [Serializable] sealed class OnlineRegion {public string slot;public Vector2[] points;public OnlineRegionUV uv;public Color tint;}
  [Serializable] sealed class OnlineRegionPose {public OnlineRegion[] regions;}
  [Serializable] sealed class OnlineRegionClip {public string key,texture;public OnlineRegionPose[] frames;}
  [Serializable] sealed class OnlineRegionData {public OnlineRegionClip[] clips;}
  readonly Dictionary<string,OnlineRegionClip> onlineRegionClips=new Dictionary<string,OnlineRegionClip>();
  readonly Dictionary<string,Texture2D> onlineRegionTextures=new Dictionary<string,Texture2D>();
  bool onlineVisualInitialized,onlineVisualReady;string onlineVisualCountry;
  float onlineVisualStarted;float? onlineVisualTimeOverride,flyVisualTimeOverride;
  string lastOnlineKey,lastFlyCountry;int lastOnlineFrame,lastOnlineRegionCount,lastFlyLayerCount;
  Vector2 lastOnlineOrigin,lastFlyPosition;Rect lastFlyHitRect;
  Texture2D sourceOnlineMaskTexture;float lastOnlineMaskFill;
  static string SourceVisualCountry(string country){switch(country){case "id":case "br":case "ru":case "de":case "kr":case "jp":case "in":return country;default:return "us";}}
  float OnlineVisualTime(){
   bool ready=OnlineRemaining==0;string country=SourceVisualCountry(Data.economy.country);
   if(!onlineVisualInitialized||ready!=onlineVisualReady||country!=onlineVisualCountry){onlineVisualInitialized=true;onlineVisualReady=ready;onlineVisualCountry=country;onlineVisualStarted=Time.unscaledTime;}
   return visualTest&&onlineVisualTimeOverride.HasValue?onlineVisualTimeOverride.Value:Time.unscaledTime-onlineVisualStarted;
  }
  bool EnsureAnimationTexture(string key){Texture2D texture;if(animationTextures.TryGetValue(key,out texture)&&texture!=null)return true;texture=Resources.Load<Texture2D>("Animations/"+key);if(texture==null)return false;animationTextures[key]=texture;return true;}
  static float SourceOnlineMaskFill(double remaining,int queueIndex){return Mathf.Clamp01((float)(remaining/(queueIndex==0?30:queueIndex==1?60:120)));}
  void DrawSourceOnlineMask(){
   lastOnlineMaskFill=SourceOnlineMaskFill(Data.economy.onlineRemaining,Data.economy.onlineIndex);if(lastOnlineMaskFill<=0)return;
   if(sourceOnlineMaskTexture==null)sourceOnlineMaskTexture=Resources.Load<Texture2D>("Animations/online_radial_mask");
   int frame=Mathf.RoundToInt(lastOnlineMaskFill*256);float s=.95f*NativeToDesign;float size=182*s;var origin=new Vector2(477+.542f*s,228-5.136f*s);
   GUI.DrawTextureWithTexCoords(new Rect(origin.x-size/2,origin.y-size/2,size,size),sourceOnlineMaskTexture,new Rect(frame%16/16f,1-(frame/16+1)/17f,1/16f,1/17f),true);
  }
  bool DrawOnlineSpine(Vector2 origin,float scale,float time,bool ready){
   string key="online_"+SourceVisualCountry(Data.economy.country)+"_"+(ready?"dakai":"daiji");BakedClip clip;
   if(!bakedClips.TryGetValue(key,out clip)||!EnsureAnimationTexture(key)){Debug.LogError("Missing source online clip "+key);return false;}
   float phase=Mathf.Repeat(time,clip.duration);int frame=Mathf.Clamp((int)(phase/clip.duration*clip.frames),0,clip.frames-1);
   lastOnlineKey=key;lastOnlineFrame=frame;lastOnlineOrigin=origin;lastOnlineRegionCount=0;
   float side=clip.worldSize*scale;DrawBaked(key,new Rect(origin.x-side/2,origin.y-side/2,side,side),time);
   if(!ready)return true;
   if(onlineRegionClips.Count==0){var data=JsonUtility.FromJson<OnlineRegionData>(Resources.Load<TextAsset>("Animations/online-region-frames").text);foreach(var item in data.clips)onlineRegionClips[item.key]=item;}
   OnlineRegionClip regions;if(!onlineRegionClips.TryGetValue(key,out regions)||regions.frames.Length!=clip.frames){Debug.LogError("Source online regions do not match "+key);return false;}
   Texture2D atlas;if(!onlineRegionTextures.TryGetValue(regions.texture,out atlas)){atlas=Resources.Load<Texture2D>("Animations/"+regions.texture);onlineRegionTextures[regions.texture]=atlas;}if(atlas==null){Debug.LogError("Missing source online region atlas "+key);return false;}
   // Source slots 2/1 (weighted meshes) precede every coin region. Preserve that order.
   var basis=GUI.matrix;var tint=GUI.color;
   foreach(var region in regions.frames[frame].regions){
    var p=region.points;var m=Matrix4x4.identity;
    m.m00=(p[1].x-p[0].x)*scale;m.m10=-(p[1].y-p[0].y)*scale;
    m.m01=(p[2].x-p[0].x)*scale;m.m11=-(p[2].y-p[0].y)*scale;
    m.m03=origin.x+p[0].x*scale;m.m13=origin.y-p[0].y*scale;
    try{GUI.matrix=basis*m;GUI.color=tint*region.tint;GUI.DrawTextureWithTexCoords(new Rect(0,0,1,1),atlas,new Rect(region.uv.x,region.uv.y,region.uv.width,region.uv.height),true);lastOnlineRegionCount++;}
    finally{GUI.matrix=basis;GUI.color=tint;}
   }
   return true;
  }
  static Vector2 SourceFlyPosition(float elapsed){
   var points=new[]{new Vector2(360,H+350*NativeToDesign),new Vector2(360-400*NativeToDesign,H*.75f),new Vector2(360+400*NativeToDesign,H*.5f),new Vector2(360-400*NativeToDesign,H*.25f),new Vector2(360+400*NativeToDesign,-350*NativeToDesign)};
   float distance=0;for(int i=1;i<5;i++)distance+=Vector2.Distance(points[i-1],points[i]);float travel=Mathf.Clamp01(elapsed/11)*distance;
   for(int i=1;i<5;i++){float leg=Vector2.Distance(points[i-1],points[i]);if(travel<=leg)return Vector2.Lerp(points[i-1],points[i],travel/leg);travel-=leg;}
   return points[4];
  }
  Rect SourceFlyHitRect(Vector2 origin){const float sourceScale=2.6f;float s=sourceScale*NativeToDesign;return new Rect(origin.x-50*s,origin.y+4*s-100*s,100*s,200*s);}
  void DrawSourceFlyLayers(Vector2 origin){
   float s=2.6f*NativeToDesign;string country=SourceVisualCountry(Data.economy.country);string suffix=country=="us"?"en":country=="de"?"ou":country=="in"?"yindu":country;
   float x=country=="ru"||country=="kr"||country=="jp"||country=="de"||country=="in"?1:0;
   Draw("GUI_qiqiu",new Rect(origin.x-44*s,origin.y-40*s-55*s,88*s,110*s),ScaleMode.StretchToFill);
   Draw("GUI_qi_"+suffix,new Rect(origin.x+(x-55)*s,origin.y+49*s-50*s,110*s,100*s),ScaleMode.StretchToFill);
   Draw("GUI_AD",new Rect(origin.x-21.5f*s,origin.y+71*s-21*s,43*s,42*s),ScaleMode.StretchToFill);
   lastFlyCountry=country;lastFlyLayerCount=3;lastFlyPosition=origin;lastFlyHitRect=SourceFlyHitRect(origin);
  }
 }
}
