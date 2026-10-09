using UnityEngine;
namespace FishSortLocal {
 public sealed partial class FishSortGame {
  int lastHudWheelFrame=-1,lastHudPigFrame=-1;
  void DrawHudPig(){DrawBaked("hudpig_jinbi",new Rect(-54,50,272,272),visualTest?0:Time.unscaledTime);}
  void DrawHudWheel(){DrawBaked("hudwheel_zhuandong",new Rect(220,157,135,135),visualTest?0:Time.unscaledTime);}
  void DrawHudOnline(){
   // GamePool onlineTask scale .95; country skeleton offset (-2.794,-9.626), scale .4.
   var origin=new Vector2(477-2.794f*.95f*NativeToDesign,228+9.626f*.95f*NativeToDesign);
   DrawOnlineSpine(origin,.4f*.95f*NativeToDesign,OnlineVisualTime(),OnlineRemaining==0);
   DrawSourceOnlineMask();
   Label(OnlineRemaining>0?OnlineRemaining+"s":Money(Data.economy.onlineReward),new Rect(413,224,130,35),OnlineRemaining>0?23:19);
  }
 }
}
