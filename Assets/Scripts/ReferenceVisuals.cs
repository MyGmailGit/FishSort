using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
namespace FishSortLocal {
 public sealed partial class FishSortGame {
  float? bonusPointerXOverride;
  void DrawReferenceHeader(){
   Draw("resources_圆角矩形_4",new Rect(0,0,W,147),ScaleMode.StretchToFill);
   if(IconButton("resources_set",new Rect(0,0,160,150),"Settings"))Open("settings");
   Draw("resources_levelBg",new Rect(560,0,160,150),ScaleMode.StretchToFill);
   Label("LEVEL",new Rect(625,13,88,38),25);Label(Data.level.ToString(),new Rect(626,64,83,54),37,new Color(1,.9f,0));
   if(!backgroundInputBlocked&&GUI.Button(new Rect(625,0,95,145),GUIContent.none,GUIStyle.none)){Sound();Open("levels");}
   Draw("resources_11",new Rect(103,67,449,78),ScaleMode.StretchToFill);Draw("resources_money",new Rect(94,45,126,114));
   Label(Money(Data.economy.top),new Rect(214,75,172,51),37);
   Draw("resources_withDraw",new Rect(385,63,186,81),ScaleMode.StretchToFill);if(TextButton("withdraw",new Rect(385,70,186,62),30))Open("withdraw");
   if(IconButton("resources_piggybank",new Rect(12,148,145,140),"Piggy bank"))Open("piggy");DrawHudPig();Draw("resources_piggybankbg",new Rect(12,250,145,51),ScaleMode.StretchToFill);Label(SourceEconomy.Display(Data.economy.piggy,Data.economy.country),new Rect(21,257,126,32),22,new Color(.1f,1,.2f));
   DrawHudWheel();RestoreInteractionMouse();if(!backgroundInputBlocked&&GUI.Button(new Rect(220,157,135,133),GUIContent.none,GUIStyle.none)){Sound();Open("spin");}Draw("GUI_turnPro1",new Rect(206,251,167,52),ScaleMode.StretchToFill);Draw("GUI_turnPro2",new Rect(215,258,148,38),ScaleMode.StretchToFill);Label(Data.economy.spinProgress+"/"+economy.SpinTarget,new Rect(216,257,138,39),24);DrawSourceSpinHand(true);
   DrawHudOnline();RestoreInteractionMouse();if(!backgroundInputBlocked&&GUI.Button(new Rect(402,154,150,148),new GUIContent("","Online reward"),GUIStyle.none)){Sound();Open("gift");}if(OnlineRemaining>0)Label("Online\nRewards",new Rect(405,280,147,60),23);Label("DEMO",new Rect(411,341,136,24),15);
   Draw("resources_素材5",new Rect(588,159,123,126));DrawLoopArrows(new Rect(594,176,112,112));Draw("resources_money",new Rect(608,196,55,50));if(IconButton("native_unrainy",new Rect(648,222,41,53),"Themes"))Open("themes");
   
  }
  void DrawReferenceFooter(){
   float y=H-255;string[] icons={"GUI_branch1","GUI_backstep1","GUI_nextvideo"};
   for(int i=0;i<3;i++){var r=new Rect(35+i*230,y,190,196);if(IconButton(icons[i],r,i==0?"Add branch":i==1?"Undo":"Skip")){if(i==0)AddBranch();else if(i==1)Undo();else Skip();}if(i==1){Draw("GUI_组_20",new Rect(157+i*230,y+5,54,54));Label(Data.undo.ToString(),new Rect(160+i*230,y+7,48,48),31);}else{Draw("GUI_组_201",new Rect(129+i*230,y+8,100,49));}}
   if(TextButton("Hint",new Rect(570,H-295,130,44),22)&&modal=="")ShowHint();
   Label("Local demo points have no cash value",new Rect(45,H-33,630,30),17,new Color(.85f,.95f,1));
  }
  bool DrawReferenceModal(){
   if(modal!="settings"&&modal!="spin"&&modal!="spinplaying"&&modal!="bonus")return false;
   Box(new Rect(0,0,W,H),new Color(0,.02f,.08f,.72f));
   if(modal=="settings"){
    Draw("resources_setbg",new Rect(94,550,532,665),ScaleMode.StretchToFill);
    if(IconButton("resources_close",new Rect(576,527,75,75),"Close")){CloseModal();return true;}
    string[] labels={"Restart","Term Of Use","Privacy Policy"};
    for(int i=0;i<3;i++){var r=new Rect(181,647+i*146.5f,359,109);Draw("resources_settingsBtn",r);if(TextButton(labels[i],new Rect(r.x,r.y+(i==0?-2:1),r.width,r.height),37))modal=i==0?"restart":i==1?"terms":"privacy";}
    if(IconButton(Data.sound?"resources_soundOn":"resources_soundOff",new Rect(211,1090,102,102),"Sound")){Data.sound=!Data.sound;Save();}
    if(IconButton(Data.music?"resources_musicOn":"resources_musicOff",new Rect(408,1090,102,102),"Music")){Data.music=!Data.music;UpdateMusic();Save();}
    Label("LOCAL DEMO",new Rect(210,1237,300,31),19,new Color(.65f,.95f,1));
   }else if(modal=="spin"||modal=="spinplaying"){
    DrawWheel(wheelAngleOverride??CurrentSpinAngle());Draw("resources_turnBg",new Rect(25,541,670,811),ScaleMode.StretchToFill);
    bool wasEnabled=GUI.enabled;GUI.enabled=modal!="spinplaying";Draw("resources_btn",new Rect(271,850,178,209));if(TextButton(modal=="spinplaying"?"DEMO":"SPIN",new Rect(270,938,180,76),45,new Color(186/255f,5/255f,5/255f))){Sound("turnOut");RequestReward("spin");}GUI.enabled=wasEnabled;if(modal=="spin")DrawSourceSpinHand(false);
    if(IconButton("resources_close",new Rect(623,488,65,65),"Close")){CloseModal();return true;}
    Label(Data.economy.firstSpinUsed?"SIMULATED AD REQUIRED":"FIRST SPIN FREE",new Rect(120,1380,480,43),23);Label("Progress "+Data.economy.spinProgress+"/"+economy.SpinTarget+" • local demo",new Rect(100,1425,520,48),21);
   }else{
    Draw("resources_组_21",new Rect(12,361,696,154));FlatLabel("Lucky Bonus",new Rect(158,418,403,60),37,Color.white);
    Draw("resources_55",new Rect(20,537,680,673));Draw("resources_qw1",new Rect(129,597,464,415));Draw("resources_box",new Rect(217,662,286,263));
    Draw("resources_33",new Rect(50,1030,620,147),ScaleMode.StretchToFill);int index=SourceBonusMultiplier()==3?2:SourceBonusPosition()<0?(SourceBonusMultiplier()==2?1:0):(SourceBonusMultiplier()==2?3:4);int[] multi={1,2,3,2,1};int[] fontSizes={43,46,50,46,43};for(int i=0;i<5;i++)FlatLabel("x"+multi[i],new Rect(60+i*123,1091,123,68),fontSizes[i],Color.white);Draw("resources_22",new Rect(bonusPointerXOverride??SourceBonusPointer(),1016,55,104));
    bool claimed=Data.economy.receipts.Contains("pass:"+completedLevel);GUI.enabled=!claimed;var r=new Rect(182,1263,357,108);Draw("resources_btn22",r);Draw("resources_videoIcon",new Rect(236,1283,67,67));if(TextButton(claimed?"Claimed":"Claim",new Rect(294,1273,220,96),40))ClaimBonus(multi[index]);GUI.enabled=true;
    if(IconButton("resources_close",new Rect(612,521,75,75),"Close")){CloseModal();return true;}
    Label("LOCAL MULTIPLIER PREVIEW • NO CASH VALUE",new Rect(55,1435,610,42),20);
   }
   return true;
  }
  IEnumerator VisualCheck(){
   if(Environment.GetCommandLineArgs().Contains("--spine-visual-check")){yield return SourceSpineVisualCheck();yield break;}
   Screen.SetResolution(540,1320,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.6f);toastUntil=0;
   LoadLevel(3);modal="";yield return RecreateReferenceLevel3();yield return CaptureReference("MainLevel3");
   LoadLevel(4);yield return CaptureReference("MainLevel4");
   foreach(var pair in new[]{Tuple.Create("settings","Settings"),Tuple.Create("spin","LuckySpin"),Tuple.Create("bonus","LuckyBonus"),Tuple.Create("piggy","PiggyBank"),Tuple.Create("withdraw","Withdraw"),Tuple.Create("withdrawhelp","WithdrawalInstructions"),Tuple.Create("rewardtoast","RewardLevel4")}){
    bool level4=pair.Item1=="bonus"||pair.Item2=="RewardLevel4";LoadLevel(level4?4:3);modal="";
    if(!level4)yield return RecreateReferenceLevel3();
    rewardAction="level";lastRewardAmount=(float)Data.economy.levelBase;referenceRewardExpanded=true;sourceRewardTimeOverride=sourceRewardTweens.popupFlightStart+.35f;bonusPointerXOverride=268;modal=pair.Item1;wheelAngleOverride=0;if(pair.Item1=="spin")PrepareSpinPanel();yield return CaptureReference(pair.Item2);
   }
   LoadLevel(3);modal="";yield return RecreateReferenceLevel3();referenceRewardExpanded=false;sourceRewardTimeOverride=.4f;modal="rewardtoast";yield return CaptureReference("RewardLevel3");
   File.WriteAllText(Path.Combine(testDir,"visual-capture-info.json"),"{\"screenWidth\":"+Screen.width+",\"screenHeight\":"+Screen.height+",\"captureMultiplier\":2,\"designWidth\":720,\"designHeight\":1760,\"levelDataModified\":false,\"staticReferencePoseOnly\":true}");Debug.Log("VISUAL_REFERENCE_CAPTURE_PASS");Application.Quit(0);
  }
  IEnumerator RecreateReferenceLevel3(){
   int[][] path={new[]{1,0},new[]{2,1},new[]{3,2},new[]{1,3}};var histogram=Board.ConservedHistogram();var notes=new List<string>();
   foreach(var step in path){if(Board.MovableCount(step[0],step[1])==0)throw new Exception("Reference-state move is not legal");TapStand(step[0]);TapStand(step[1]);yield return WaitForBoardMotion();notes.Add("LEGAL "+step[0]+" -> "+step[1]+"; "+Board.Key());}
   if(!histogram.SequenceEqual(Board.ConservedHistogram())||Board.Stands[3].fish.Count!=3||Board.Stands[2].fish.Count!=1||Board.Stands[0].fish.Count!=0)throw new Exception("Reference level3 state mismatch");selected=3;File.WriteAllLines(Path.Combine(testDir,"level3-reference-legal-moves.txt"),notes);toastUntil=0;
  }
  IEnumerator CaptureReference(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(testDir,name+".png"),2);yield return new WaitForSecondsRealtime(.3f);}
 }
}
