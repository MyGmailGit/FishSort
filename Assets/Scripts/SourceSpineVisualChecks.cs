using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
namespace FishSortLocal {
 public sealed partial class FishSortGame {
  sealed class VisualCountryProvider:ICountryProvider {readonly string country;public VisualCountryProvider(string value){country=value;}public string GetCountry(){return country;}}
  [Serializable] sealed class OnlineVisualEvidence {public string country,state,key,capture;public int frame,coinRegions;public float phase;public Vector2 origin;public double top,piggy;}
  [Serializable] sealed class FlyVisualEvidence {public string country,capture;public int layers;public float elapsed;public Vector2 position;public Rect hit;}
  [Serializable] sealed class SpineVisualEvidence {public OnlineVisualEvidence[] online;public FlyVisualEvidence[] fly;public int assertions;public bool allPass,isolated,originalGameScreenshot;public string scope;}
  IEnumerator SourceSpineVisualCheck(){
   var checks=new List<string>();var online=new List<OnlineVisualEvidence>();var fly=new List<FlyVisualEvidence>();
   Action<bool,string> check=(ok,name)=>{checks.Add((ok?"PASS ":"FAIL ")+name);File.WriteAllLines(Path.Combine(testDir,"source-visual-checks.txt"),checks);if(!ok)throw new Exception("Source visual check failed: "+name);};
   Screen.SetResolution(540,1320,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.6f);LoadLevel(3);modal="";toastUntil=0;
   double top=Data.economy.top,piggy=Data.economy.piggy;int ledger=Data.economy.ledger.Count;string originalBoard=Board.Key();
   check(Data.economy.country=="us","production country defaults to us");check(bakedClips.Count==51,"35 prior clips preserved and 16 source online clips available");
   string[] countries={"us","br","id","ru","de","kr","jp","in"};
   foreach(string country in countries){
    SetCountryProvider(new VisualCountryProvider(country));
    foreach(bool ready in new[]{false,true}){
     // Isolated visual fixture; readiness uses the unchanged production UpdateOnline path.
     Data.economy.onlineDay=OnlineDay;Data.economy.onlineRemaining=30;Data.economy.onlineReward=0;
     if(ready)economy.UpdateOnline(30,OnlineDay);
     onlineVisualTimeOverride=.6f;yield return new WaitForEndOfFrame();string expected="online_"+country+"_"+(ready?"dakai":"daiji");
     check(lastOnlineKey==expected,country+" source readiness selects "+expected);check(EnsureAnimationTexture(expected),country+" actual body texture loaded");
     check(Math.Abs(bakedClips[expected].duration-(ready?3.0667:3))<.0001,country+" source loop duration retained");
     check(ready?lastOnlineRegionCount>=16:lastOnlineRegionCount==0,country+" original coin attachment visibility respected");
     string capture="online-"+country+"-"+(ready?"ready":"countdown");yield return CaptureReference(capture);
     online.Add(new OnlineVisualEvidence{country=country,state=ready?"ready":"countdown",key=lastOnlineKey,frame=lastOnlineFrame,coinRegions=lastOnlineRegionCount,phase=.6f,origin=lastOnlineOrigin,top=Data.economy.top,piggy=Data.economy.piggy,capture=capture+".png"});
    }
    Data.economy.onlineRemaining=30;Data.economy.onlineReward=0;flyVisualTimeOverride=4.4f;yield return new WaitForEndOfFrame();
    check(lastFlyCountry==country&&lastFlyLayerCount==3,country+" flight uses balloon/country/AD source layers");check(lastFlyHitRect.Contains(lastFlyPosition),country+" source button hit area follows flight");
    string flight="fly-"+country;yield return CaptureReference(flight);fly.Add(new FlyVisualEvidence{country=country,capture=flight+".png",layers=lastFlyLayerCount,elapsed=4.4f,position=lastFlyPosition,hit=lastFlyHitRect});flyVisualTimeOverride=null;
    check(Data.economy.top==top&&Data.economy.piggy==piggy&&Data.economy.ledger.Count==ledger&&Board.Key()==originalBoard,country+" visual rendering does not credit money or alter board");
   }
   check(onlineRegionClips.Count==8&&onlineRegionClips.Values.All(c=>c.frames.All(p=>p.regions.All(r=>r.points.Length==4&&r.uv.width>0&&r.uv.height>0&&r.uv.x>=0&&r.uv.y>=0&&r.uv.x+r.uv.width<=1.00001&&r.uv.y+r.uv.height<=1.00001))),"all eight original region atlases deserialize valid quad/UV data");
   SetCountryProvider(new VisualCountryProvider("mx"));onlineVisualTimeOverride=.6f;yield return new WaitForEndOfFrame();check(lastOnlineKey=="online_us_daiji","other supported country uses source default visual");
   check(Vector2.Distance(SourceFlyPosition(0),new Vector2(360,H+350*NativeToDesign))<.001f&&Vector2.Distance(SourceFlyPosition(11),new Vector2(360+400*NativeToDesign,-350*NativeToDesign))<.001f,"existing eleven-second source flight endpoints retained");
   SetCountryProvider(new UnitedStatesCountryProvider());onlineVisualTimeOverride=null;float began=Time.unscaledTime;int first=-1;bool animated=false;
   Data.economy.onlineRemaining=30;Data.economy.onlineReward=0;yield return new WaitForEndOfFrame();
   while(Time.unscaledTime-began<.4f){yield return new WaitForEndOfFrame();if(first<0)first=lastOnlineFrame;else if(lastOnlineFrame!=first)animated=true;}
   check(animated,"production clock animates source countdown clip without phase override");
   check(Math.Abs(SourceOnlineMaskFill(15,0)-.5f)<.0001&&Math.Abs(SourceOnlineMaskFill(30,1)-.5f)<.0001&&Math.Abs(SourceOnlineMaskFill(60,2)-.5f)<.0001,"source radial fill follows unchanged30/60/120 intervals");
   Data.economy.onlineRemaining=15;onlineVisualTimeOverride=.6f;yield return CaptureReference("online-us-half-countdown");check(sourceOnlineMaskTexture!=null&&lastOnlineMaskFill>.45f&&lastOnlineMaskFill<.51f,"actual HUD renders partially depleted source radial mask");onlineVisualTimeOverride=null;
   economy.UpdateOnline(Data.economy.onlineRemaining,OnlineDay);yield return new WaitForEndOfFrame();check(lastOnlineKey=="online_us_dakai"&&lastOnlineFrame<=1,"real local readiness transition restarts source opening loop");
   onlineVisualTimeOverride=.6f;yield return CaptureReference("online-us-live-ready");onlineVisualTimeOverride=1.75f;yield return CaptureReference("online-us-late-coin-flight");
   check(lastOnlineRegionCount<17,"late source attachment removals are reflected in actual render");
   onlineVisualTimeOverride=null;flyVisualTimeOverride=null;Save();
   var evidence=new SpineVisualEvidence{online=online.ToArray(),fly=fly.ToArray(),assertions=checks.Count,allPass=true,isolated=true,originalGameScreenshot=false,scope="Actual local Unity render using source-derived sampled meshes and original atlas regions; accelerated isolated readiness fixture. Not macOS window capture or original device recording."};File.WriteAllText(Path.Combine(testDir,"source-visual-summary.json"),JsonUtility.ToJson(evidence,true));Debug.Log("SOURCE_SPINE_VISUAL_PASS "+checks.Count);Application.Quit(0);
  }
 }
}
