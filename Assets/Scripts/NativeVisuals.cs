using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
namespace FishSortLocal {
 public sealed partial class FishSortGame {
  bool DrawNativeModal(){
   if(modal=="withdraw"||modal=="withdrawhelp"||modal=="history"){
    DrawNativeWithdrawal();if(modal=="withdrawhelp")DrawNativeInstructions();if(modal=="history"){Box(new Rect(0,0,W,H),new Color(0,0,0,.65f));Draw("native_tubwoman",new Rect(40,530,640,760),ScaleMode.StretchToFill);Label("Local Demo History",new Rect(130,560,460,80),34);Label("Completed levels: "+Data.completed+"\nDemo points: "+SourceEconomy.Display(Data.economy.top,Data.economy.country)+"\n\nNo payment requests or transfers.",new Rect(100,770,520,280),32);if(IconButton("resources_close",new Rect(610,586,60,60),"Close"))modal="withdraw";}return true;
   }
   if(modal=="piggy"){DrawNativePiggy();return true;}
   if(modal=="rewardtoast"){DrawNativeReward();return true;}
   return false;
  }
  void FlatLabel(string value,Rect r,int size,Color color,TextAnchor alignment=TextAnchor.MiddleCenter){var style=new GUIStyle(text){fontSize=size,alignment=alignment};style.normal.textColor=color;GUI.Label(r,value,style);}
  void Tinted(string name,Rect r,Color color){var c=GUI.color;GUI.color=color;Draw(name,r,ScaleMode.StretchToFill);GUI.color=c;}
  void DrawNativeWithdrawal(){
   Box(new Rect(0,0,W,H),Color.black);Draw("native_lyricism",new Rect(0,60,W,H-60),ScaleMode.StretchToFill);
   Draw("resources_圆角矩形_4",new Rect(0,60,W,145),ScaleMode.StretchToFill);
   if(IconButton("resources_back",new Rect(30,90,84,84),"Back"))modal="";Label("Withdraw",new Rect(178,92,366,75),43);
   if(IconButton("native_hete",new Rect(565,111,49,49),"Instructions"))modal="withdrawhelp";if(IconButton("native_fallouts",new Rect(649,107,48,49),"Local history"))modal="history";
   Draw("native_many",new Rect(35,230,650,387),ScaleMode.StretchToFill);FlatLabel("My Balance • DEMO",new Rect(108,242,510,56),34,new Color(.34f,.08f,.15f));Label("$ "+SourceEconomy.Display(Data.economy.top,Data.economy.country),new Rect(89,347,540,104),66,new Color(1,.94f,.23f));
   DrawNine("native_pistic",new Rect(74,498,572,78),32,32,26);Draw("native_unechoic",new Rect(85,508,149,59));FlatLabel("Local demo — no account",new Rect(243,512,366,55),23,new Color(.31f,.09f,.12f));
   Draw("native_spraddle",new Rect(620,522,12,22));if(GUI.Button(new Rect(74,498,572,78),GUIContent.none,GUIStyle.none)){Sound();modal="withdrawhelp";}
   Draw("native_swilled",new Rect(31,644,658,108),ScaleMode.StretchToFill);Label("Exchange Cash • DEMO",new Rect(93,659,534,76),40);
   Draw("native_outstair",new Rect(38,729,644,599),ScaleMode.StretchToFill);
   float[] amounts={800,1500,3000};for(int i=0;i<3;i++){
    float y=750+i*188;Draw("native_twinight",new Rect(77,y,565,175),ScaleMode.StretchToFill);Label("$"+amounts[i].ToString("0.00"),new Rect(93,y+14,410,72),51,new Color(1,1,.23f),TextAnchor.MiddleLeft);Draw("resources_money",new Rect(91,y+108,51,45));
    Draw("native_flings",new Rect(158,y+104,320,47),ScaleMode.StretchToFill);float progress=Mathf.Clamp01(Data.demoBalance/amounts[i]);if(progress>0)Draw("native_dolesome",new Rect(160,y+108,316*progress,38),ScaleMode.StretchToFill);Label("$"+SourceEconomy.Display(Data.economy.top,Data.economy.country)+"/$"+amounts[i].ToString("0.00"),new Rect(171,y+108,303,38),30);
    if(GUI.Button(new Rect(77,y,565,175),GUIContent.none,GUIStyle.none)){Sound();modal="withdrawhelp";}
   }
   Draw("native_outstair",new Rect(26,1481,668,252),ScaleMode.StretchToFill);Draw("native_unwhole",new Rect(64,1517,74,74));Label("LOCAL DEMO REVIEW",new Rect(154,1510,465,76),34);FlatLabel("No ratings, payouts or linked account",new Rect(69,1609,592,63),27,Color.white,TextAnchor.MiddleLeft);FlatLabel("Payment branding is shown for local visual study.",new Rect(69,1670,588,55),21,Color.white,TextAnchor.MiddleLeft);
  }
  void DrawNativeInstructions(){
   Box(new Rect(0,0,W,H),new Color(0,0,0,.68f));Draw("native_tubwoman",new Rect(41,491,638,798),ScaleMode.StretchToFill);Label("Withdrawal Instructions",new Rect(132,542,462,69),31);
   if(IconButton("resources_close",new Rect(601,576,72,72),"Close")){Sound("closePanel");modal="withdraw";return;}
   string[] paragraphs={"1. This local demo has no cash value. The balance and exchange cards are visual previews for prototype review.","2. No payment account is connected. No email, identity, password or account information is collected.","3. Claiming a local reward only changes demo points saved on this Mac. No transfer is submitted or processed.","4. The original publisher's requirements, payment timing and device rules have not been verified. This build makes no payout promise."};
   float[] ys={687,815,943,1112};float[] hs={123,119,159,150};for(int i=0;i<4;i++)FlatLabel(paragraphs[i],new Rect(103,ys[i],517,hs[i]),26,Color.white,TextAnchor.UpperLeft);
  }
  void DrawNativePiggy(){
   Box(new Rect(0,0,W,H),new Color(0,0,0,.67f));Draw("native_retarder",new Rect(0,175,720,988),ScaleMode.StretchToFill);Label("Piggy Bank",new Rect(145,205,430,76),45);
   if(IconButton("resources_close",new Rect(638,244,75,75),"Close")){CloseModal();return;}
   Draw("native_sabana",new Rect(214,354,292,96),ScaleMode.StretchToFill);Label(Money(Data.economy.piggy),new Rect(225,355,270,62),43,new Color(.07f,.9f,.15f));
   DrawNine("native_locally",new Rect(58,868,466,135),24,24,24);FlatLabel("Native eligibility unknown\nLocal piggy balance only",new Rect(73,878,433,112),27,new Color(.02f,.27f,.46f),TextAnchor.UpperLeft);
   Draw("native_swizzled",new Rect(537,873,125,130));Draw("native_beshout",new Rect(552,899,96,94));
   Draw("native_fainest",new Rect(168,1018,386,115));if(TextButton("Cash Out • Demo",new Rect(171,1031,381,87),36))modal="withdrawhelp";
   Draw("native_outstair",new Rect(0,1189,720,383),ScaleMode.StretchToFill);Label("LOCAL STATISTICS (OFFLINE)",new Rect(26,1202,668,49),31,new Color(1,.92f,.45f));
   Draw("native_jurists",new Rect(26,1277,326,180),ScaleMode.StretchToFill);Draw("native_jurists",new Rect(368,1277,326,180),ScaleMode.StretchToFill);FlatLabel("Completed\nlevels",new Rect(48,1294,280,82),31,new Color(.04f,.39f,.49f));FlatLabel("Demo\npoints",new Rect(390,1294,280,82),31,new Color(.04f,.39f,.49f));Label(Data.completed.ToString(),new Rect(76,1380,221,58),37,new Color(1,.9f,.16f));Label("$"+SourceEconomy.Display(Data.economy.top,Data.economy.country),new Rect(397,1380,270,58),37,new Color(.05f,.92f,.15f));
   Draw("native_rupiahs",new Rect(65,1488,79,77));Label("LOCAL DEMO • NO PAYMENT",new Rect(151,1487,504,61),27);FlatLabel("No withdrawal or transfer has been submitted.",new Rect(158,1545,497,40),22,Color.white,TextAnchor.MiddleLeft);
  }
  void DrawNativeReward(){
   float elapsed=sourceRewardTimeOverride??(Time.unscaledTime-rewardShownAt);
   bool toolReward=rewardAction=="undo"||rewardAction=="add"||rewardAction=="skip";
   // The retrieved reward-tip prefab has one fixed-scale money image, not an enlarged pile state.
   float flightTime=elapsed-sourceRewardTweens.popupFlightStart;
   var oldColor=GUI.color;GUI.color=new Color(0,.03f,.05f,.88f);DrawNine("resources_rewardTipBg",new Rect(126,653,468,405),32,32,18);GUI.color=oldColor;
   if(toolReward){Draw(rewardAction=="undo"?"GUI_backstep1":rewardAction=="add"?"GUI_branch1":"GUI_nextvideo",new Rect(286,739,148,148));lastSourceRewardCount=0;}
   else if(rewardAction=="online"||rewardAction=="bonus"){DrawBaked("gift_dakai",new Rect(210,680,300,300),Time.unscaledTime-giftOpenedAt,false);}
   else Draw("native_beshout",new Rect(265,715,200,164));
   FlatLabel(toolReward?"REFILL RECEIVED":"CONGRATULATIONS",new Rect(157,909,406,51),33,Color.white);FlatLabel(toolReward?"+3 USES":"+$ "+lastRewardAmount.ToString("0.00"),new Rect(177,972,366,65),55,new Color(1,1,0));Label("LOCAL DEMO • NO CASH VALUE",new Rect(158,1066,404,37),19);
   // The newly added Canvas/MAX_ZINDEX rewardAnim root overlays the older tip root.
   if(!toolReward&&(!visualTest||referenceRewardExpanded))DrawSourceRewardMotion(flightTime);else lastSourceRewardCount=0;
   if(TextButton("Continue",new Rect(249,1130,222,57),27))ContinueReward();
  }
  // Pixel-preserving nine-slice for the tiny native stretchable panel textures.
  void DrawNine(string name,Rect r,int sx,int sy,float border){Texture2D t;if(!art.TryGetValue(name,out t)||t==null)return;float[] px={0,sx,t.width-sx,t.width};float[] py={0,sy,t.height-sy,t.height};float[] dx={r.x,r.x+border,r.xMax-border,r.xMax};float[] dy={r.y,r.y+border,r.yMax-border,r.yMax};float scaleX=GUI.matrix.m00,scaleY=GUI.matrix.m11;for(int i=0;i<4;i++){dx[i]=(Mathf.Round(dx[i]*scaleX+GUI.matrix.m03)-GUI.matrix.m03)/scaleX;dy[i]=(Mathf.Round(dy[i]*scaleY+GUI.matrix.m13)-GUI.matrix.m13)/scaleY;}for(int y=0;y<3;y++)for(int x=0;x<3;x++)GUI.DrawTextureWithTexCoords(new Rect(dx[x],dy[y],dx[x+1]-dx[x],dy[y+1]-dy[y]),t,new Rect(px[x]/t.width,1-py[y+1]/t.height,(px[x+1]-px[x])/t.width,(py[y+1]-py[y])/t.height),true);}
 }
}
