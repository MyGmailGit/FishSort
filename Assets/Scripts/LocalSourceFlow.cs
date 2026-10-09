using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
namespace FishSortLocal {
 public sealed partial class FishSortGame {
  // Historical regression fixtures hold early auto-advance; production always follows the recovered early-level route.
  bool holdEarlyAutoAdvance;
  int sourceSpinIndex,lastCryType=-1;
  bool sourceSpinLanded;
  string lastSoundRequested="";
  float bonusOpenedAt;
  const float SourceSpinRotate=7f,SourceSpinLanding=.5f,SourceSpinBlink=1f,SourceSpinHold=1f;
  float PendingRewardDuration {get{return rewardAction=="spin"?SourceSpinRotate+SourceSpinLanding+SourceSpinBlink+SourceSpinHold:0;}}
  static readonly int[] SourceSpinMultipliers={10,1,5,2,3,7,1,2};
  static int SourceSpinIndex(int draw){
   if(draw<0||draw>=1000)throw new ArgumentOutOfRangeException("draw");
   double total=0;double[] weights={0,.3,0,.2,.1,0,.3,.1};
   for(int i=0;i<weights.Length;i++){total+=weights[i];if(draw<=total*1000)return i;}return 7;
  }
  static float SourceEaseInOut(float t){t=Mathf.Clamp01(t);return t<.5f?.5f*Mathf.Pow(2*t,4):1-.5f*Mathf.Pow(2*(1-t),4);}
  void UpdateSourceSpin(){if(modal=="spinplaying"&&rewardPending&&!sourceSpinLanded&&Time.unscaledTime-rewardStart>=SourceSpinRotate+SourceSpinLanding){sourceSpinLanded=true;Sound("turnGet");}}
  static bool DefaultForceVideo(int completedLevel){int min=completedLevel<=50?1:completedLevel<=100?51:101;int interval=completedLevel<=50?5:completedLevel<=100?4:3;return completedLevel!=min&&(completedLevel+1-min)%interval==0;}
  static int SourceOnlineInterval(int index){return index<=0?30:index==1?60:120;}
  int SelectedSuffixCount(int branch){var fish=Board.Stands[branch].fish;if(fish.Count==0)return 0;int n=1;while(n<fish.Count&&fish[fish.Count-1-n]==fish[fish.Count-1])n++;return n;}
  bool IsSelectedSlot(int branch,int slot){return selected==branch&&slot>=Board.Stands[branch].fish.Count-SelectedSuffixCount(branch);}
  void PlayFishCry(int type){lastCryType=type;Sound("BirdTweet"+(Mathf.Min(type,8)+1));}
  float SourceBonusPosition(){return -395*Mathf.Cos(Mathf.PI*(Time.unscaledTime-bonusOpenedAt)/1.2f);}
  static int SourceBonusMultiplierAt(float x){return x<=-170?1:x<=-58?2:x<=58?3:x<=170?2:1;}
  int SourceBonusMultiplier(){return SourceBonusMultiplierAt(SourceBonusPosition());}
  float SourceBonusPointer(){return 335+SourceBonusPosition()*492/790f;}
  float BranchSourceY(int count,int index){float start=count==2?0:count%2==0?Mathf.Floor(count/2f-1)*260/2:260/2f+Mathf.Floor(count/2f-1)*260/2;return count%2==0?start-Mathf.Floor(index/2f)*260:start-index*260/2;}
  float BranchWobbleAngle(int branch){
   float latest=-1;for(int i=0;i<flying.Count;i++)if(movingTarget==branch){float arrived=transferStartedAt+.1f*i+transferLegs[i]+SourceLandingDuration;if(Time.unscaledTime>=arrived)latest=Mathf.Max(latest,arrived);}
   if(latest<0)return 0;float t=Time.unscaledTime-latest;
   float[] times={0,.2f,.35f,.5f,.65f,.75f},angles={0,1,-.5f,.8f,-.2f,0};
   for(int i=1;i<times.Length;i++)if(t<=times[i])return -Mathf.Lerp(angles[i-1],angles[i],(t-times[i-1])/(times[i]-times[i-1]));return 0;
  }
  void ApplyBranchWobble(int branch){float angle=BranchWobbleAngle(branch);if(angle==0)return;var p=new Vector3(Board.Stands[branch].side<0?17.6f:702.4f,branchRects[branch].y+62,0);GUI.matrix=GUI.matrix*Matrix4x4.Translate(p)*Matrix4x4.Rotate(Quaternion.Euler(0,0,angle))*Matrix4x4.Translate(-p);}
  readonly List<Tuple<int,Rect,Rect>> undoFlights=new List<Tuple<int,Rect,Rect>>();
  float undoFlightStart,undoFlightEnd;
  // Keep conserved full snapshots; do not reproduce the recovered StepData's immediate-isMove record omission.
  void BeginUndoAnimation(BoardSnapshot after,BoardSnapshot before){
   undoFlights.Clear();if(after.stands.Count!=before.stands.Count||before.transferCount<=0)return;LayoutBranches();int from=before.transferFrom,to=before.transferTo,n=before.transferCount;
   bool cleared=after.clearedFish[before.stands[from].fish.Last()]>before.clearedFish[before.stands[from].fish.Last()];
   Func<int,Rect> origin=(slot)=>{var r=FishRect(to,slot);if(cleared){var offset=sourceCollectOffsets[slot]*NativeToDesign;r.x+=before.stands[to].side<0?offset.x:-offset.x;r.y-=offset.y;}return r;};
   for(int j=0;j<n;j++){int slot=before.stands[from].fish.Count-1-j;undoFlights.Add(Tuple.Create(before.stands[from].fish[slot],origin(before.stands[to].fish.Count+j),FishRect(from,slot)));}
   if(cleared)for(int slot=0;slot<before.stands[to].fish.Count;slot++)undoFlights.Add(Tuple.Create(before.stands[to].fish[slot],origin(slot),FishRect(to,slot)));
   if(undoFlights.Count==0)return;undoFlightStart=Time.unscaledTime;undoFlightEnd=undoFlightStart;
   for(int i=0;i<undoFlights.Count;i++)undoFlightEnd=Mathf.Max(undoFlightEnd,undoFlightStart+.1f*(undoFlights.Count-i)+Vector2.Distance(undoFlights[i].Item2.center,undoFlights[i].Item3.center)/NativeToDesign/100*.15f+.3f+.75f);
   motionInProgress=true;transitionUntil=undoFlightEnd;StartCoroutine(FinishUndoAnimation(Board,Data.level,movementGeneration));
  }
  IEnumerator FinishUndoAnimation(PuzzleModel board,int level,int generation){while(Time.unscaledTime<undoFlightEnd){if(!CurrentMovement(board,level,generation))yield break;yield return null;}if(!CurrentMovement(board,level,generation))yield break;undoFlights.Clear();motionInProgress=false;transitionUntil=0;Save();}
  void DrawUndoAnimation(){for(int i=0;i<undoFlights.Count;i++){var f=undoFlights[i];float t=Time.unscaledTime-undoFlightStart-.1f*(undoFlights.Count-i),leg=Vector2.Distance(f.Item2.center,f.Item3.center)/NativeToDesign/100*.15f;Rect r=f.Item2;if(t>=0){r.x=Mathf.Lerp(f.Item2.x,f.Item3.x,Mathf.Clamp01(t/leg));r.y=Mathf.Lerp(f.Item2.y,f.Item3.y-50*NativeToDesign,Mathf.Clamp01(t/leg));if(t>=leg)r.y=Mathf.Lerp(f.Item3.y-50*NativeToDesign,f.Item3.y,(t-leg)/.3f);}Fish(f.Item1,r,false,true,f.Item3.x<f.Item2.x);}}
 }
}
