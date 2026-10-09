using UnityEngine;
using System;

namespace FishSortLocal {
 public sealed partial class FishSortGame {
  bool rewardPending;
  float spinFrom,spinTarget;
  void CancelPendingReward(){rewardPending=false;rewardAction="";spinSelectionReady=false;if(adCoordinator!=null)adCoordinator.Cancel();businessGeneration++;taskGeneration++;taskPending=false;}
  void RequestReward(string action){BusinessRequest(action);}
  float CurrentSpinAngle(){
   if(modal!="spinplaying")return 0;
   float t=Mathf.Clamp01((Time.unscaledTime-rewardStart)/SourceSpinRotate);
   return Mathf.Lerp(spinFrom,spinTarget,SourceEaseInOut(t));
  }
  void FinishReward(){FinishSourceSpin();}
 }
}
