using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
namespace FishSortLocal {
 public sealed partial class FishSortGame {
  bool motionInProgress;
  int movementGeneration,movingTarget=-1;
  float transferStartedAt,transferLandingAt,collectionStartedAt;
  readonly List<float> transferLegs=new List<float>();
  readonly List<Tuple<int,Rect,Rect>> collectionFlights=new List<Tuple<int,Rect,Rect>>();
  readonly HashSet<int> collectingStands=new HashSet<int>();
  static readonly Vector2[] sourceCollectOffsets={new Vector2(1126,513),new Vector2(1026,653),new Vector2(956,498),new Vector2(851,473)};
  const float NativeToDesign=720f/1080f,SourceLandingDuration=.3f,SourceWobbleDuration=.75f,SourceCollectDuration=1f;
  void ResetBoardMotion(){movementGeneration++;motionInProgress=false;movingTarget=-1;transitionUntil=0;flying.Clear();transferLegs.Clear();collectionFlights.Clear();collectingStands.Clear();undoFlights.Clear();}
  void BeginBoardTransfer(int from,int to,int count){
   ResetBoardMotion();LayoutBranches();int before=Board.Stands[from].fish.Count,targetBefore=Board.Stands[to].fish.Count;
   transferStartedAt=Time.unscaledTime;transferLandingAt=transferStartedAt;movingTarget=to;
   for(int j=0;j<count;j++){
    int slot=before-1-j;var start=FishRect(from,slot);var end=FishRect(to,targetBefore+j);
    flying.Add(Tuple.Create(Board.Stands[from].fish[slot],start,end));
    float leg=Vector2.Distance(start.center,end.center)/NativeToDesign/100f*.15f;transferLegs.Add(leg);transferLandingAt=Mathf.Max(transferLandingAt,transferStartedAt+.1f*j+leg+SourceLandingDuration);
   }
   Board.Move(from,to);selected=-1;hint=null;motionInProgress=true;
   transitionUntil=transferLandingAt+SourceWobbleDuration+(Board.Complete(to)?SourceCollectDuration:0);
   Sound("Fly");Save();StartCoroutine(FinishBoardTransfer(Board,Data.level,movementGeneration,to));
  }
  bool CurrentMovement(PuzzleModel board,int level,int generation){return Board==board&&Data.level==level&&movementGeneration==generation;}
  IEnumerator FinishBoardTransfer(PuzzleModel board,int level,int generation,int target){
   float settleAt=transferLandingAt+SourceWobbleDuration;
   while(Time.unscaledTime<settleAt){if(!CurrentMovement(board,level,generation))yield break;yield return null;}
   if(!CurrentMovement(board,level,generation))yield break;
   flying.Clear();
   if(board.Complete(target)){
    collectingStands.Add(target);collectionStartedAt=Time.unscaledTime;
    for(int slot=0;slot<4;slot++){
     var start=FishRect(target,slot);var offset=sourceCollectOffsets[slot]*NativeToDesign;
     var end=new Rect(start.x+(board.Stands[target].side<0?offset.x:-offset.x),start.y-offset.y,start.width,start.height);
     collectionFlights.Add(Tuple.Create(board.Stands[target].fish[slot],start,end));
    }
    StartCoroutine(DelayedCollectSound(board,level,generation));transitionUntil=collectionStartedAt+SourceCollectDuration;
    while(Time.unscaledTime<transitionUntil){if(!CurrentMovement(board,level,generation))yield break;yield return null;}
    if(!CurrentMovement(board,level,generation))yield break;
    board.ResolveCompletedGroups();
   }
   collectionFlights.Clear();collectingStands.Clear();motionInProgress=false;movingTarget=-1;transitionUntil=0;Save();
   if(board.Won)CompleteLevel();else if(!board.HasLegalMove&&modal=="")modal="stuck";
  }
  IEnumerator DelayedCollectSound(PuzzleModel board,int level,int generation){yield return new WaitForSecondsRealtime(.7f);if(CurrentMovement(board,level,generation))Sound("happyCollect"+UnityEngine.Random.Range(1,3));}
  IEnumerator WaitForBoardMotion(){while(motionInProgress)yield return null;yield return new WaitForEndOfFrame();}
  void DrawBoardMotion(){
   if(!motionInProgress)return;
   for(int i=0;i<flying.Count;i++){
    var f=flying[i];float elapsed=Time.unscaledTime-transferStartedAt-.1f*i,leg=transferLegs[i];Rect rect=f.Item2;
    if(elapsed>0){float progress=Mathf.Clamp01(elapsed/Mathf.Max(.001f,leg));rect.x=Mathf.Lerp(f.Item2.x,f.Item3.x,progress);rect.y=Mathf.Lerp(f.Item2.y,f.Item3.y-50*NativeToDesign,progress);
     if(elapsed>=leg)rect.y=Mathf.Lerp(f.Item3.y-50*NativeToDesign,f.Item3.y,(elapsed-leg)/SourceLandingDuration);
    }
    var matrix=GUI.matrix;if(elapsed>=leg+SourceLandingDuration)ApplyBranchWobble(movingTarget);Fish(f.Item1,rect,false,elapsed>=0&&elapsed<leg+SourceLandingDuration+SourceWobbleDuration,elapsed>=leg+SourceLandingDuration?Board.Stands[movingTarget].side>0:f.Item3.x<f.Item2.x);GUI.matrix=matrix;
   }
   foreach(var f in collectionFlights){float progress=Mathf.Clamp01((Time.unscaledTime-collectionStartedAt)/SourceCollectDuration);Fish(f.Item1,new Rect(Mathf.Lerp(f.Item2.x,f.Item3.x,progress),Mathf.Lerp(f.Item2.y,f.Item3.y,progress),f.Item2.width,f.Item2.height),false,true,f.Item3.x<f.Item2.x);}
  }
 }
}
