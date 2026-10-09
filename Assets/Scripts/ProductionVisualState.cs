using UnityEngine;
namespace FishSortLocal {
 public sealed partial class FishSortGame {
  float rewardShownAt;bool referenceRewardExpanded;
  void DrawLoopArrows(Rect r){var basis=GUI.matrix;var pivot=new Vector3(r.center.x,r.center.y,0);float angle=visualTest?0:Time.unscaledTime*18;try{GUI.matrix=basis*Matrix4x4.Translate(pivot)*Matrix4x4.Rotate(Quaternion.Euler(0,0,angle))*Matrix4x4.Translate(-pivot);Draw("local_loopArrows",r);}finally{GUI.matrix=basis;}}
 }
}
