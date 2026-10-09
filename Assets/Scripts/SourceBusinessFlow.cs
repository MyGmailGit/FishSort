using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
namespace FishSortLocal {
 public sealed partial class FishSortGame {
  SourceEconomy economy;ICountryProvider countryProvider=new UnitedStatesCountryProvider();IRuleConfigProvider configProvider=new DefaultRuleConfigProvider();
  LocalAdSimulator adSimulator;SourceAdCoordinator adCoordinator;IAdProvider adProviderOverride;
  ITimeLimitTaskProvider taskProvider=new TimeLimitTaskPlaceholder();int taskType=1,taskGeneration;bool taskPending;float nextTaskClick,nextOnlineClick;
  bool businessAcceptance,applicationFocused=true,applicationPaused,localWhiteBao;
  double completedBase,spinRewardValue;int completedLevel=-1,businessGeneration;
  bool completedForce,spinSelectionReady,flyScheduled;float nextFlyAt,flyStartedAt=-1;
  string Money(double value){return SourceEconomy.Currency(Data.economy.country)+" "+SourceEconomy.Display(value,Data.economy.country);}
  string OnlineDay {get{return DateTime.Now.ToString("MM/dd");}}
  void InitBusiness(){
   if(Data.economy==null){if(File.Exists(savePath))File.Copy(savePath,savePath+".pre-business-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"),false);Data.economy=new EconomyState{top=Data.demoBalance,passNum=Data.level};}
   var e=Data.economy;SourceEconomy.NormalizePending(e);if(!SourceEconomy.Finite(e.top)||!SourceEconomy.Finite(e.piggy)||e.top<0||e.piggy<0||e.top>=1e12||e.piggy>=1e12||!SourceEconomy.Finite(e.onlineRemaining)||e.onlineRemaining<0||e.onlineRemaining>120||!SourceEconomy.Finite(e.onlineReward)||e.onlineReward<0||e.spinUses<0||e.spinProgress<0||e.onlineIndex<0||e.onlineIndex>6)throw new Exception("Invalid economy save");
   if(e.pendingLevel!=null&&(e.pendingLevel.level<1||!SourceEconomy.Finite(e.pendingLevel.amount)||e.pendingLevel.amount<0))throw new Exception("Invalid pending level reward");
   if(e.pendingSpin!=null&&(string.IsNullOrEmpty(e.pendingSpin.receipt)||e.pendingSpin.index<0||e.pendingSpin.index>7||!SourceEconomy.Finite(e.pendingSpin.amount)||e.pendingSpin.amount<0))throw new Exception("Invalid pending spin reward");
   economy=new SourceEconomy(e,new SeededUnitRandom(Environment.TickCount),countryProvider,configProvider);if(!e.levelBaseReady){if(Board!=null&&e.levelBase>0)e.levelBaseReady=true;else economy.PrepareLevel();}economy.UpdateOnline(0,OnlineDay);SyncBalance();adSimulator=new LocalAdSimulator();adCoordinator=new SourceAdCoordinator(adProviderOverride??adSimulator);if(e.todayDay!=OnlineDay){e.todayDay=OnlineDay;e.todayPass=0;}RestoreCompletedContext();
  }
  public void SetCountryProvider(ICountryProvider provider){countryProvider=provider??new UnitedStatesCountryProvider();if(economy!=null)Data.economy.country=SourceEconomy.NormalizeCountry(countryProvider.GetCountry());}
  public void SetAdProvider(IAdProvider provider){adProviderOverride=provider??new SdkAdProviderPlaceholder();if(adCoordinator!=null)adCoordinator.Cancel();businessGeneration++;adCoordinator=new SourceAdCoordinator(adProviderOverride);if(modal=="ad")modal=CompletedModal;}
  LocalAdSimulator ActiveAdSimulator {get{return adProviderOverride==null?adSimulator:adProviderOverride as LocalAdSimulator;}}
  bool HasCompletedReward {get{return Data.economy.pendingLevel!=null&&!Data.economy.receipts.Contains("pass:"+Data.economy.pendingLevel.level);}}
  string CompletedModal {get{return Board!=null&&Board.Won&&HasCompletedReward?(Data.economy.pendingLevel.forced?"bonus":"victory"):"";}}
  void RestoreCompletedContext(){var p=Data.economy.pendingLevel;if(p==null){completedLevel=-1;completedBase=0;completedForce=false;return;}completedLevel=p.level;completedBase=p.amount;completedForce=p.forced;}
  void ClearCompletedContext(){Data.economy.pendingLevel=null;RestoreCompletedContext();}
  void CancelActiveAd(){if(adCoordinator!=null)adCoordinator.Cancel();businessGeneration++;modal=CompletedModal;Save();}
  void SyncBalance(){Data.demoBalance=(float)Data.economy.top;}
  void PrepareSpinPanel(){Data.economy.spinBase=economy.BaseReward(Data.economy.top);sourceSpinIndex=SourceEconomy.SpinIndex(UnityEngine.Random.Range(0,1000));spinRewardValue=Data.economy.spinBase*SourceEconomy.SpinMultipliers[sourceSpinIndex];spinSelectionReady=true;}
  void BusinessTick(){if(economy==null)return;if(applicationFocused&&!applicationPaused){double previous=Data.economy.onlineRemaining;economy.UpdateOnline(Time.unscaledDeltaTime,OnlineDay);if(previous>0&&Data.economy.onlineRemaining==0)Save();}if(Data.economy.todayDay!=OnlineDay){Data.economy.todayDay=OnlineDay;Data.economy.todayPass=0;Save();}
   if(Data.economy.passNum>=2&&!flyScheduled){flyScheduled=true;nextFlyAt=Time.unscaledTime+1;}
   if(flyScheduled){if(flyStartedAt<0&&Time.unscaledTime>=nextFlyAt)flyStartedAt=Time.unscaledTime;if(flyStartedAt>=0&&Time.unscaledTime-flyStartedAt>=11){flyStartedAt=-1;nextFlyAt=Time.unscaledTime+UnityEngine.Random.Range(15,21);}}
   if(Input.GetKeyDown(KeyCode.F8)&&modal!="spinplaying")Open("business");}
  void OnApplicationFocus(bool focused){applicationFocused=focused;if(!focused&&Board!=null&&Data!=null&&economy!=null)Save();}
  void ShowReward(RewardRecord record,string action){if(record==null){Notify("This local transaction was already credited.");return;}SyncBalance();lastRewardAmount=(float)record.raw;rewardAction=action;giftOpenedAt=rewardShownAt=Time.unscaledTime;modal="rewardtoast";Save();Sound("rewardTip");}
  string NewReceipt(string prefix){return prefix+":"+Guid.NewGuid().ToString("N");}
  void ClaimNewGift(){if(Data.economy.newGiftClaimed)return;Data.economy.newGiftClaimed=true;ShowReward(economy.Credit("new-gift","v2_novice",economy.AwardAmount("v2_novice")),"novice");}
  void StartSourceSpin(){
   if(Data.economy.pendingSpin!=null){RestorePendingSpin();return;}
   if(!economy.SpinReady){Notify("Complete "+(economy.SpinTarget-Data.economy.spinProgress)+" more levels.");return;}
   Data.economy.firstSpinUsed=true;Data.economy.spinUses++;Data.economy.limitTaskZhuanpan++;Data.economy.spinProgress=0;
   Data.economy.pendingSpin=new PendingSpinReward{active=true,receipt=NewReceipt("spin"),index=sourceSpinIndex,amount=spinRewardValue};Save();RestorePendingSpin();
  }
  void RestorePendingSpin(){var p=Data.economy.pendingSpin;if(p==null)return;if(Data.economy.receipts.Contains(p.receipt)){Data.economy.pendingSpin=null;Save();return;}sourceSpinIndex=p.index;spinRewardValue=p.amount;rewardPending=true;rewardAction="spin";rewardStart=Time.unscaledTime;spinFrom=0;sourceSpinLanded=false;spinTarget=-(2160-45*sourceSpinIndex);modal="spinplaying";Sound("turnBg");}
  void FinishSourceSpin(){if(!rewardPending||modal!="spinplaying"||Time.unscaledTime-rewardStart<PendingRewardDuration)return;var p=Data.economy.pendingSpin;if(p==null)return;rewardPending=false;var record=economy.Credit(p.receipt,"v2_spinReward",p.amount);Data.economy.pendingSpin=null;ShowReward(record,"spin");Save();}
  void ClaimSourceOnline(){economy.UpdateOnline(0,OnlineDay);ShowReward(economy.ClaimOnline(NewReceipt("online")),"online");}
  void CompleteSourceLevel(){
   if(!Board.Won)return;CancelPendingReward();int finished=Data.level;if(Data.economy.receipts.Contains("pass:"+finished)&&!holdEarlyAutoAdvance){LoadLevel(finished+1);return;}
   if(Data.economy.pendingLevel==null||Data.economy.pendingLevel.level!=finished)Data.economy.pendingLevel=new PendingLevelReward{active=true,level=finished,amount=Data.economy.levelBase,forced=economy.ForceVideo(finished)};RestoreCompletedContext();
   if(!Data.cleared.Contains(finished)){Data.cleared.Add(finished);Data.completed=Data.cleared.Count;Data.unlocked=Mathf.Max(Data.unlocked,Mathf.Min(350,finished+1));Data.economy.passNum=finished;economy.AdvancePass();Save();}
   if(finished<=3){var award=economy.Credit("pass:"+finished,"v2_pass",economy.AwardAmount("v2_pass",completedBase));SyncBalance();lastRewardAmount=award==null?0:(float)award.raw;if(!holdEarlyAutoAdvance){LoadLevel(finished+1);rewardAction="level";rewardShownAt=Time.unscaledTime;modal="rewardtoast";Sound("rewardTip");}else modal="victory";Save();}
   else {modal=completedForce?"bonus":"victory";bonusOpenedAt=Time.unscaledTime;Sound(completedForce?"boxout":"levelComplete");Save();}
  }
  void ClaimCompleted(bool doubled){
   if(!Board.Won||!HasCompletedReward)return;RestoreCompletedContext();
   if(Data.economy.receipts.Contains("pass:"+completedLevel)){LoadLevel(completedLevel+1);return;}
   if(doubled){RequestReward(completedForce?"bonus":"passDouble");return;}
   if(completedForce&&!localWhiteBao){RequestReward("forceClose");return;}
   string biz=completedForce?"v2_passd":"v2_pass";int nextLevel=completedLevel+1;var r=economy.Credit("pass:"+completedLevel,biz,economy.AwardAmount(biz,completedBase));LoadLevel(nextLevel);ShowReward(r,"level");
  }
  void BusinessRequest(string action){
   if(rewardPending||adCoordinator.Pending!=null)return;
   if(action=="online"){if(Time.unscaledTime<nextOnlineClick)return;nextOnlineClick=Time.unscaledTime+1;if(OnlineRemaining>0){Notify("The foreground countdown is still running.");return;}ClaimSourceOnline();return;}
   if(action=="spin"){
    if(!economy.SpinReady){Notify("Spin progress: "+Data.economy.spinProgress+"/"+economy.SpinTarget);return;}
    // A panel samples base and index once before the ad; callbacks keep this captured amount.
    if(!spinSelectionReady)PrepareSpinPanel();
    if(!Data.economy.firstSpinUsed){StartSourceSpin();return;}
   }
   if(action=="add"&&Data.economy.addUsedLevel==Data.level){Notify("One added branch per level.");return;}
   if((action=="bonus"||action=="passDouble"||action=="forceClose")&&!HasCompletedReward)return;
   if(action=="fly"&&Data.economy.passNum<2){Notify("Flying chest unlocks after the first completed level.");return;}
   var req=SourceAdCoordinator.Route(action);req.id=action=="bonus"||action=="passDouble"||action=="forceClose"?"pass:"+completedLevel:NewReceipt(action);req.level=Data.level;req.generation=businessGeneration;req.amount=action=="spin"?spinRewardValue:action=="bonus"||action=="passDouble"?2*completedBase:action=="forceClose"?completedBase:0;req.rate=action=="bonus"||action=="passDouble"?2:1;req.biz=action=="bonus"||action=="forceClose"?"v2_passd":action=="passDouble"?"v2_pass":action=="fly"?"v2_fly":"";
   rewardAction=action;rewardStart=Time.unscaledTime;
   adCoordinator.Begin(req,Time.unscaledTime,SourceAdCoordinator.Debounce(action),BusinessAdCompleted,()=>{
    modal="ad";
    // Original forced panel advances immediately, independently of asynchronous ad success.
    if(action=="bonus"||action=="forceClose")CreateLevelBoard(completedLevel+1);Save();
   });
  }
  void BusinessAdCompleted(AdOutcome outcome,AdRequest request){
   if(request.generation!=businessGeneration)return;
   if(outcome!=AdOutcome.EligibleClosed){modal=CompletedModal;Notify("Ad outcome: "+outcome+". No reward credited.");Save();return;}
   switch(request.action){
    case "undo":Data.undo+=3;modal="";Save();Notify("3 undo uses refilled.");return;
    case "add":if(Data.economy.addUsedLevel!=request.level&&Data.level==request.level&&Board.AddStand()){Data.economy.addUsedLevel=request.level;selected=-1;Save();}modal="";return;
    case "skip":if(Data.level==request.level){economy.AdvancePass();Data.unlocked=Mathf.Max(Data.unlocked,Mathf.Min(350,Data.level+1));LoadLevel(Data.level+1);}return;
    case "spin":StartSourceSpin();return;
   }
   double raw=request.action=="fly"?economy.AwardAmount("v2_fly"):request.biz=="v2_pass"?economy.AwardAmount(request.biz,request.amount):request.amount;if(request.action=="fly"){flyStartedAt=-1;nextFlyAt=Time.unscaledTime+UnityEngine.Random.Range(15,21);}var record=economy.Credit(request.id,request.biz,raw,request.rate);if(request.action=="passDouble")LoadLevel(request.level+1);ShowReward(record,"level");
  }
  public void SetTimeLimitTaskProvider(ITimeLimitTaskProvider provider){taskProvider=provider??new TimeLimitTaskPlaceholder();taskPending=false;taskGeneration++;}
  public void SetTimeLimitTaskType(int type){TimeLimitTaskRouting.Value(type,Data.economy);taskType=type;}
  void RequestTimeLimitTask(ITimeLimitTaskProvider provider){if(taskPending||Time.unscaledTime<nextTaskClick)return;nextTaskClick=Time.unscaledTime+.5f;taskPending=true;int generation=taskGeneration;string receipt=NewReceipt("local-task");bool consumed=false;Action<NativeRewardOutcome> callback=outcome=>{if(consumed||generation!=taskGeneration)return;consumed=true;taskPending=false;if(outcome==NativeRewardOutcome.Success)ShowReward(economy.Credit(receipt,"v2_limitTask",economy.AwardAmount("v2_limitTask")),"task");};try{provider.Show(taskType,TimeLimitTaskRouting.Value(taskType,Data.economy),callback);}catch{callback(NativeRewardOutcome.Failed);}}
  void SimulateTimeLimitTask(){RequestTimeLimitTask(new LocalTimeLimitTaskSimulator());}
  void DrawBusinessFly(){
   bool fixture=visualTest&&flyVisualTimeOverride.HasValue;
   if(modal!=""||(!fixture&&(flyStartedAt<0||visualTest)))return;
   float elapsed=fixture?flyVisualTimeOverride.Value:Time.unscaledTime-flyStartedAt;var pos=SourceFlyPosition(elapsed);DrawSourceFlyLayers(pos);
   RestoreInteractionMouse();if(GUI.Button(SourceFlyHitRect(pos),new GUIContent("","Flying reward"),GUIStyle.none)){Sound();RequestReward("fly");}
  }
  bool DrawBusinessModal(){
   if(modal!="ad"&&modal!="business"&&modal!="newgift")return false;
   Box(new Rect(0,0,W,H),new Color(0,.02f,.08f,.82f));Draw("resources_setbg",new Rect(60,450,600,930),ScaleMode.StretchToFill);
   if(modal=="ad"){
    var req=adCoordinator.Pending;if(req==null){modal="";return true;}
    var simulator=ActiveAdSimulator;
    Label(simulator!=null?"LOCAL AD SIMULATOR":"AD PROVIDER PENDING",new Rect(100,480,520,70),34);Label(req.action+" • "+req.kind+"\nPlacement "+req.nativePlacement+" / tool "+req.toolTag+"\n"+(simulator!=null?"No SDK or network is connected.":"Waiting for the selected provider callback."),new Rect(100,565,520,165),27);
    if(simulator!=null){if(ActionButton("Eligible close / reward",790))simulator.Complete(AdOutcome.EligibleClosed);if(ActionButton("Error / unavailable",890))simulator.Complete(AdOutcome.Failed);}
    if(ActionButton("Cancel / no reward",990))CancelActiveAd();
    Label("Original JS resolves on close.\nYour SDK adapter should qualify reward completion\nbefore sending EligibleClosed.",new Rect(95,1100,530,120),23);
   }else if(modal=="newgift"){
    Label("NEW PLAYER GIFT • DEMO",new Rect(90,500,540,70),33);Draw("resources_box",new Rect(210,625,300,265));Label(Money(50),new Rect(130,900,460,90),55);if(ActionButton("Collect once",1080))ClaimNewGift();
   }else{
    Label("LOCAL BUSINESS REVIEW",new Rect(90,470,540,70),33);Label("Country: "+Data.economy.country+" (replaceable provider)\nMain: "+Money(Data.economy.top)+"\nPiggy: "+Money(Data.economy.piggy)+"\nSpin "+Data.economy.spinProgress+"/"+economy.SpinTarget+" • "+(Data.economy.firstSpinUsed?"advertisement":"first free")+"\nOnline "+OnlineRemaining+"s • "+Money(Data.economy.onlineReward),new Rect(100,555,520,255),26);
    if(ActionButton("New gift (once)",840))ClaimNewGift();if(ActionButton("Flying chest • ad",930))RequestReward("fly");
    if(ActionButton("White-mode local assumption: "+localWhiteBao,1020))localWhiteBao=!localWhiteBao;
    Label("Time-limit task / withdrawal: native eligibility\nand settlement are unverified; no payout runs.",new Rect(100,1120,520,95),23);
    if(ActionButton("Task type "+taskType+" (1 / 2 / 3 / 4)",1210))taskType=taskType%4+1;
    if(ActionButton("Task provider (eligibility unverified)",1300))RequestTimeLimitTask(taskProvider);
    if(ActionButton("Simulate task success callback",1390))SimulateTimeLimitTask();if(ActionButton("Close",1490))CloseModal();
   }
   return true;
  }
 }
}
