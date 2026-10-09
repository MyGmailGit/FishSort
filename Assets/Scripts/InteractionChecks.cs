using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
namespace FishSortLocal {
 [Serializable] public sealed class InteractionTelemetry {public int pid,level,sourceLevel,screenWidth,screenHeight,selected,spinProgress,spinUses;public string modal,appPath;public bool moving,spinPending;public double top,piggy;public BoardSnapshot board;public Vector2[] branchCenters;public RewardRecord[] ledger;}
 public sealed partial class FishSortGame {
  [Serializable] public sealed class InteractionEvent {public int sequence;public string type;public float x,y;}
  bool interactionAcceptance,interactionEventActive;Vector2 interactionMousePosition;
  void RestoreInteractionMouse(){if(interactionAcceptance&&interactionEventActive&&Event.current.isMouse)Event.current.mousePosition=interactionMousePosition;}
  Event BeginInteractionEvent(){if(!interactionAcceptance||Event.current.type!=EventType.Repaint)return null;string path=Path.Combine(testDir,"ui-event.json");if(!File.Exists(path))return null;var request=JsonUtility.FromJson<InteractionEvent>(File.ReadAllText(path));File.Delete(path);interactionEventActive=true;interactionMousePosition=new Vector2(request.x,request.y);var previous=new Event(Event.current);float scale=Mathf.Min(Screen.width/W,Screen.height/H);Event.current=new Event{type=(EventType)Enum.Parse(typeof(EventType),request.type),button=0,mousePosition=new Vector2(request.x,request.y)};File.WriteAllText(Path.Combine(testDir,"ui-event-ack.txt"),request.sequence.ToString());return previous;}
  void ShowInitialGift(){if(!Data.economy.newGiftSeen){Data.economy.newGiftSeen=true;modal="newgift";Save();}}
  IEnumerator InteractionCheck(){
   ShowInitialGift();float began=Time.unscaledTime;bool first=true;
   while(Time.unscaledTime-began<300){
    LayoutBranches();var snapshot=new InteractionTelemetry{pid=System.Diagnostics.Process.GetCurrentProcess().Id,level=Data.level,sourceLevel=Data.sourceLevel,screenWidth=Screen.width,screenHeight=Screen.height,selected=selected,spinProgress=Data.economy.spinProgress,spinUses=Data.economy.spinUses,modal=modal,appPath=Application.dataPath,moving=motionInProgress,spinPending=Data.economy.pendingSpin!=null,top=Data.economy.top,piggy=Data.economy.piggy,board=Board.Snapshot(),branchCenters=branchRects.Select(r=>new Vector2(r.center.x,r.y+15)).ToArray(),ledger=Data.economy.ledger.ToArray()};
    string path=Path.Combine(testDir,"interaction-state.json");File.WriteAllText(path+".tmp",JsonUtility.ToJson(snapshot,true));if(File.Exists(path))File.Replace(path+".tmp",path,null);else File.Move(path+".tmp",path);
    if(first){first=false;Debug.Log("INTERACTION_READY pid="+snapshot.pid);yield return Capture("interaction-startup");}
    string capture=Path.Combine(testDir,"capture-request.txt");if(File.Exists(capture)){string name=File.ReadAllText(capture).Trim();File.Delete(capture);yield return Capture(name);}
    string quit=Path.Combine(testDir,"quit-request.txt");if(File.Exists(quit)){string status=File.ReadAllText(quit).Trim();File.Delete(quit);Save();Debug.Log("INTERACTION_EXIT "+status);Application.Quit(status=="PASS"?0:1);yield break;}
    yield return new WaitForSecondsRealtime(.15f);
   }
   Debug.LogError("INTERACTION_TIMEOUT");Application.Quit(1);
  }
 }
}
