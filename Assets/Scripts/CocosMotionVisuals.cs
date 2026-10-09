using UnityEngine;
using System;
namespace FishSortLocal {
 public sealed partial class FishSortGame {
  [Serializable] class SourceMotionFrame {public float x,y,scaleX,scaleY,angle,alpha;}
  [Serializable] class SourceRewardTrack {public string name;public SourceMotionFrame[] frames;}
  [Serializable] class SourceMotionData {public int fps;public float handDuration,handSpeed,rewardDuration;public float[] handFrames;public SourceRewardTrack[] rewardTracks;}
  [Serializable] class SourceRewardTween {public string name,group;public int index;public float x,y,delay,movieTime;}
  [Serializable] class SourceRewardTweenData {public float popupFlightStart,piggyStart,lastScale,rootDestroyAfter,contentScale,nativeToDesign,parallel1Base,parallel1PerIndex,parallel2Position,parallel2Scale,overshootPosition,overshootScale,settleScale,finalScale,nodeDestroyDelay;public SourceRewardTween[] tracks;public string[] drawOrder;}
  SourceRewardTweenData sourceRewardTweens;
  SourceMotionData sourceMotion;
  float lastGuideScale=1,lastSourceRewardTime=-1;int lastSourceRewardCount,lastSpinHandFrame=-1;
  float? sourceRewardTimeOverride;
  void LoadSourceMotion(){sourceMotion=JsonUtility.FromJson<SourceMotionData>(Resources.Load<TextAsset>("Animations/cocos-motion").text);sourceRewardTweens=JsonUtility.FromJson<SourceRewardTweenData>(Resources.Load<TextAsset>("Animations/reward-tweens").text);}
  float SourceGuideScale(float time){
   if(sourceMotion==null)return 1;
   float f=Mathf.Repeat(time*sourceMotion.handSpeed,sourceMotion.handDuration)*sourceMotion.fps;int a=Mathf.Clamp((int)f,0,sourceMotion.handFrames.Length-1),b=Mathf.Min(a+1,sourceMotion.handFrames.Length-1);
   return Mathf.Lerp(sourceMotion.handFrames[a],sourceMotion.handFrames[b],f-a);
  }
  void DrawSourceHintHand(){
   if(hint==null||modal!="")return;
   var fish=FishRect(hint[0],Mathf.Max(0,Board.Stands[hint[0]].fish.Count-1));float scale=SourceGuideScale(Time.unscaledTime);lastGuideScale=scale;var center=fish.center+new Vector2(33,36);
   Draw("GUI_tap",new Rect(center.x-43*scale,center.y-41*scale,86*scale,82*scale));
  }
  void DrawSourceSpinHand(bool header){
   float phase=visualTest ? (header?.9593581f:1.126029f) : Time.unscaledTime;var rect=header?new Rect(206,135,198,198):new Rect(304,880,198,198);
   DrawBaked("spinhand_newAnimation",rect,phase);
  }
  SourceMotionFrame SourceRewardFrame(SourceRewardTrack track,float time){
   float f=Mathf.Clamp(time*sourceMotion.fps,0,track.frames.Length-1);int a=(int)f,b=Mathf.Min(a+1,track.frames.Length-1);float t=f-a;var aa=track.frames[a];var bb=track.frames[b];
   return new SourceMotionFrame{x=Mathf.Lerp(aa.x,bb.x,t),y=Mathf.Lerp(aa.y,bb.y,t),scaleX=Mathf.Lerp(aa.scaleX,bb.scaleX,t),scaleY=Mathf.Lerp(aa.scaleY,bb.scaleY,t),angle=Mathf.Lerp(aa.angle,bb.angle,t),alpha=Mathf.Lerp(aa.alpha,bb.alpha,t)};
  }
  SourceMotionFrame SourceRewardTweenFrame(SourceRewardTween track,float time){
   var d=sourceRewardTweens;var frame=new SourceMotionFrame{x=0,y=0,scaleX=1,scaleY=1,alpha=1};
   float t=time-(track.group=="piggy"?d.piggyStart:0)-d.parallel1PerIndex*track.index;
   if(t<0)return frame;
   float posDuration=d.parallel1Base+(track.index%2)*d.parallel1PerIndex*track.index;
   float scaleDuration=d.parallel1Base+((track.index+1)%2)*d.parallel1PerIndex*track.index;
   float parallelDuration=Mathf.Max(posDuration,scaleDuration);
   if(t<parallelDuration){frame.x=track.x*d.overshootPosition*Mathf.Clamp01(t/posDuration);frame.y=track.y*d.overshootPosition*Mathf.Clamp01(t/posDuration);frame.scaleX=frame.scaleY=Mathf.Lerp(1,d.overshootScale,t/scaleDuration);return frame;}
   t-=parallelDuration;
   if(t<d.parallel2Scale){frame.x=Mathf.Lerp(track.x*d.overshootPosition,track.x,t/d.parallel2Position);frame.y=Mathf.Lerp(track.y*d.overshootPosition,track.y,t/d.parallel2Position);frame.scaleX=frame.scaleY=Mathf.Lerp(d.overshootScale,d.settleScale,t/d.parallel2Scale);return frame;}
   t-=d.parallel2Scale;frame.x=track.x;frame.y=track.y;frame.scaleX=frame.scaleY=d.settleScale;
   if(t<track.delay)return frame;
   t-=track.delay;
   // Target positions follow the corresponding HUD nodes in this local 720-wide layout.
   var destination=track.group=="piggy"?new Vector2(83,245):new Vector2(150,120);
   float sceneScale=d.contentScale*d.nativeToDesign;
   var target=new Vector2((destination.x-360)/sceneScale,(880-destination.y)/sceneScale);
   frame.x=Mathf.Lerp(track.x,target.x,t/track.movieTime);frame.y=Mathf.Lerp(track.y,target.y,t/track.movieTime);frame.scaleX=frame.scaleY=Mathf.Lerp(d.settleScale,d.finalScale,t/track.movieTime);
   if(t>=track.movieTime+d.nodeDestroyDelay)frame.alpha=0;
   return frame;
  }
  void DrawSourceRewardMotion(float time){
   lastSourceRewardTime=time;lastSourceRewardCount=0;if(sourceMotion==null||sourceRewardTweens==null||time<0||time>=sourceRewardTweens.rootDestroyAfter)return;
   var matrix=GUI.matrix;var color=GUI.color;float sceneScale=sourceRewardTweens.contentScale*sourceRewardTweens.nativeToDesign;
   foreach(string name in sourceRewardTweens.drawOrder){
    SourceMotionFrame frame=null;
    foreach(var track in sourceRewardTweens.tracks)if(track.name==name){frame=SourceRewardTweenFrame(track,time);break;}
    if(frame==null)foreach(var track in sourceMotion.rewardTracks)if(track.name=="content/"+name&&time<=sourceMotion.rewardDuration){frame=SourceRewardFrame(track,time);break;}
    if(frame==null||frame.alpha<=.001f)continue;
    var center=new Vector2(360+frame.x*sceneScale,880-frame.y*sceneScale);float w=78*sceneScale*frame.scaleX,h=71*sceneScale*frame.scaleY;
    try{GUI.matrix=matrix*Matrix4x4.Translate(center)*Matrix4x4.Rotate(Quaternion.Euler(0,0,-frame.angle))*Matrix4x4.Translate(-center);GUI.color=new Color(color.r,color.g,color.b,color.a*frame.alpha);Draw("resources_money",new Rect(center.x-w/2,center.y-h/2,w,h));lastSourceRewardCount++;}
    finally{GUI.matrix=matrix;GUI.color=color;}
   }
  }
 }
}
