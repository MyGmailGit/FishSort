using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace FishSortLocal {
 public sealed partial class FishSortGame : MonoBehaviour {
  const float W=720,H=1760;
  static readonly int[] FishResourceIndex={7,4,0,9,2,5,6,1,8,3};
  bool visualTest, backgroundInputBlocked;
  public PuzzleModel Board {get;private set;}
  public SaveData Data {get;private set;}
  readonly Dictionary<string,Texture2D> art=new Dictionary<string,Texture2D>();
  [Serializable] class RigPart {public string texture;public float x,y,w,h;}
  [Serializable] class Rig {public RigPart[] parts;}
  Rig[] rigs=new Rig[10];
  [Serializable] class TextureIndex{public string[] names;}
  [Serializable] class BakedClip{public string key;public int frames,columns,rows;public float duration,worldSize;}
  [Serializable] class BakedClips{public BakedClip[] clips;}
  readonly Dictionary<string,Texture2D> animationTextures=new Dictionary<string,Texture2D>();
  readonly Dictionary<string,BakedClip> bakedClips=new Dictionary<string,BakedClip>();
  float loadProgress, giftOpenedAt;
  int lastGiftFrame;
  float lastRewardAmount;
  Font gameFont;
  Texture2D panel,button,white,gradient;
  AudioSource music,sfx;
  GUIStyle text;
  int selected=-1, themePage=0, levelPage=0;
  int OnlineRemaining {get{return Data.economy==null?30:Mathf.CeilToInt((float)Data.economy.onlineRemaining);}}
  string modal="", toast="Select an end group, then a branch with free slots.", rewardAction="", jump="1";
  float toastUntil=8, rewardStart, transitionUntil;
  int[] hint;
  bool automation, repairTest;
  float? wheelAngleOverride;
  Vector3 lastWheelCenter;
  string testDir, savePath;
  Rect[] branchRects;
  List<Tuple<int,Rect,Rect>> flying=new List<Tuple<int,Rect,Rect>>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot() {if(FindAnyObjectByType<FishSortGame>()==null)new GameObject("Fish Sort Local").AddComponent<FishSortGame>();}
  IEnumerator Start() {
   QualitySettings.vSyncCount=0;Application.runInBackground=true;Application.targetFrameRate=60;Debug.Log("LOCAL_LOAD_BEGIN");
   var args=Environment.GetCommandLineArgs();visualTest=args.Contains("--visual-test");interactionAcceptance=args.Contains("--interaction-check");businessAcceptance=args.Any(x=>new[]{"--business-check","--functional-check","--final-check","--repair-test","--prototype-test","--restart-check"}.Contains(x));repairTest=businessAcceptance;automation=visualTest||businessAcceptance||interactionAcceptance;if(automation)AudioListener.volume=0;holdEarlyAutoAdvance=businessAcceptance;
   int ri=Array.IndexOf(args,"--restart-stage");restartStage=ri>=0&&ri+1<args.Length?args[ri+1]:null;
   int ti=Array.IndexOf(args,"--evidence");testDir=ti>=0 && ti+1<args.Length?args[ti+1]:Path.Combine(Application.persistentDataPath,"Evidence");
   savePath=automation?Path.Combine(testDir,"test-save.json"):Path.Combine(Application.persistentDataPath,"progress-v1.json");
   if(automation) Directory.CreateDirectory(testDir);
   white=Texture2D.whiteTexture;
   foreach(var name in new[]{"main_loading","main_logo","main_sliderbg","main_sliderbar"})art[name]=Resources.Load<Texture2D>("Art/"+name);
   gameFont=Resources.Load<Font>("gameManiafont");yield return null;
   var textureIndex=JsonUtility.FromJson<TextureIndex>(Resources.Load<TextAsset>("texture-index").text);int loaded=0;bool captured=false;
   foreach(var name in textureIndex.names){var request=Resources.LoadAsync<Texture2D>("Art/"+name);yield return request;art[name]=(Texture2D)request.asset;loadProgress=.8f*(++loaded)/(float)textureIndex.names.Length;if(loaded%50==0)Debug.Log("LOCAL_TEXTURES "+loaded);if(repairTest&&!captured&&loadProgress>.1f){captured=true;yield return Capture("00-real-loading-progress");}}
   var clipIndex=JsonUtility.FromJson<BakedClips>(Resources.Load<TextAsset>("Animations/clips").text);
   foreach(var clip in clipIndex.clips){
    bakedClips[clip.key]=clip;
    // Keep all country metadata, preload the default us body only; other countries load when selected.
    if(!clip.key.StartsWith("online_")||clip.key.StartsWith("online_us_")){var request=Resources.LoadAsync<Texture2D>("Animations/"+clip.key);yield return request;animationTextures[clip.key]=(Texture2D)request.asset;}
    loadProgress=.8f+.18f*bakedClips.Count/clipIndex.clips.Length;
   }
   yield return null;
   gameFont=Resources.Load<Font>("gameManiafont");for(int i=0;i<10;i++)rigs[i]=JsonUtility.FromJson<Rig>(Resources.Load<TextAsset>("Rigs/fish"+i).text);
   white=Texture2D.whiteTexture; panel=Rounded(new Color(.02f,.12f,.25f,.96f));button=Rounded(new Color(.07f,.53f,.68f,1));gradient=Gradient();
   music=gameObject.AddComponent<AudioSource>();music.clip=Resources.Load<AudioClip>("Audio/audio_bg");music.loop=true;music.volume=.27f;
   sfx=gameObject.AddComponent<AudioSource>();sfx.volume=.5f;
   Data=new SaveData(); LoadSave(); InitBusiness(); if(Board==null)LoadLevel(Data.level,false);
   LoadSourceMotion();loadProgress=1;Debug.Log("LOCAL_LOAD_READY");UpdateMusic();if(Board.Won)CompleteLevel();RestorePendingSpin();
   if(automation)StartCoroutine(interactionAcceptance?InteractionCheck():restartStage!=null?ProcessRecoveryCheck():visualTest?VisualCheck():BusinessCheck());else ShowInitialGift();
  }
  Texture2D Rounded(Color c) {var t=new Texture2D(64,64);for(int y=0;y<64;y++)for(int x=0;x<64;x++){float dx=Mathf.Max(12-x,0,x-51),dy=Mathf.Max(12-y,0,y-51);var p=c;p.a*=Mathf.Clamp01(13-Mathf.Sqrt(dx*dx+dy*dy));t.SetPixel(x,y,p);}t.Apply();return t;}
  Texture2D Gradient(){var t=new Texture2D(1,128);for(int i=0;i<128;i++)t.SetPixel(0,i,Color.Lerp(new Color(0,.02f,.1f,.65f),new Color(0,.08f,.2f,0),i/127f));t.Apply();return t;}
  void UpdateMusic(){if(Data.music){if(!music.isPlaying)music.Play();}else music.Pause();}
  void Sound(string clip="button"){lastSoundRequested=clip;if(Data.sound){var c=Resources.Load<AudioClip>("Audio/audio_"+clip);if(c!=null)sfx.PlayOneShot(c);}}
  void Notify(string s){toast=s;toastUntil=Time.unscaledTime+4;}
  static int SourceLevelFor(int level){return level<=350?level:UnityEngine.Random.Range(150,349);}
  void CreateLevelBoard(int level){ResetBoardMotion();Data.level=Math.Max(1,level);Data.sourceLevel=SourceLevelFor(Data.level);var asset=Resources.Load<TextAsset>("Levels/Level_"+Data.sourceLevel.ToString("000"));Board=new PuzzleModel(JsonUtility.FromJson<LevelConfig>(asset.text));selected=-1;hint=null;flying.Clear();transitionUntil=0;jump=Data.level.ToString();if(economy!=null){ClearCompletedContext();Data.economy.addUsedLevel=-1;Data.economy.passNum=Data.level;economy.PrepareLevel();}}
  public void LoadLevel(int level,bool save=true){CancelPendingReward();CreateLevelBoard(level);modal="";if(save)Save();}
  void LoadSave(){
   if(!File.Exists(savePath))return;
   try{
    var d=JsonUtility.FromJson<SaveData>(File.ReadAllText(savePath));
    if(d==null||(d.version!=1&&d.version!=2)||d.level<1||d.level>=int.MaxValue||d.theme<1||d.theme>20||d.unlocked<1||d.unlocked>350||d.undo<0||d.add<0||d.skip<0||d.demoBalance<0||float.IsNaN(d.demoBalance)||float.IsInfinity(d.demoBalance))throw new Exception("Invalid save");
    if(d.economy!=null)SourceEconomy.NormalizePending(d.economy);
    if(d.economy!=null&&(!SourceEconomy.Finite(d.economy.top)||!SourceEconomy.Finite(d.economy.piggy)||d.economy.top<0||d.economy.piggy<0||d.economy.top>=1e12||d.economy.piggy>=1e12||!SourceEconomy.Finite(d.economy.onlineRemaining)||d.economy.onlineRemaining<0||d.economy.onlineRemaining>120||!SourceEconomy.Finite(d.economy.onlineReward)||d.economy.onlineReward<0||d.economy.spinUses<0||d.economy.spinProgress<0||d.economy.onlineIndex<0||d.economy.onlineIndex>6))throw new Exception("Invalid economy save");
    if(d.economy!=null&&((d.economy.pendingLevel!=null&&(d.economy.pendingLevel.level<1||!SourceEconomy.Finite(d.economy.pendingLevel.amount)||d.economy.pendingLevel.amount<0))||(d.economy.pendingSpin!=null&&(string.IsNullOrEmpty(d.economy.pendingSpin.receipt)||d.economy.pendingSpin.index<0||d.economy.pendingSpin.index>7||!SourceEconomy.Finite(d.economy.pendingSpin.amount)||d.economy.pendingSpin.amount<0))))throw new Exception("Invalid pending reward save");
    if(d.sourceLevel==0)d.sourceLevel=d.level<=350?d.level:SourceLevelFor(d.level);
    if((d.level<=350&&d.sourceLevel!=d.level)||(d.level>350&&(d.sourceLevel<150||d.sourceLevel>348)))throw new Exception("Invalid source level");
    bool legacy=d.version==1;PuzzleModel candidate=null;
    if(d.board!=null){
     candidate=new PuzzleModel(d.board);var asset=Resources.Load<TextAsset>("Levels/Level_"+d.sourceLevel.ToString("000"));var original=new PuzzleModel(JsonUtility.FromJson<LevelConfig>(asset.text));var expected=original.ConservedHistogram();
     if(!candidate.ConservedHistogram().SequenceEqual(expected))throw new Exception("Saved fish plus cleared ledger differ from level");
     var history=new List<BoardSnapshot>();foreach(var item in d.history??new List<BoardSnapshot>()){
      var prior=new PuzzleModel(item);if(!prior.ConservedHistogram().SequenceEqual(expected))throw new Exception("Invalid undo conservation");if(legacy)prior.ResolveCompletedGroups();var normalized=prior.Snapshot();normalized.transferFrom=item.transferFrom;normalized.transferTo=item.transferTo;normalized.transferCount=item.transferCount;history.Add(normalized);
     }
     // Resume a saved transfer/completion from a stable state; its full pre-move undo snapshot remains.
     candidate.ResolveCompletedGroups();foreach(var item in history.AsEnumerable().Reverse())candidate.History.Push(item);
    }
    d.cleared=d.cleared??new List<int>();d.bonusClaimed=d.bonusClaimed??new List<int>();if(d.cleared.Any(x=>x<1||x>=int.MaxValue)||d.bonusClaimed.Any(x=>x<1||x>=int.MaxValue))throw new Exception("Invalid level reward records");
    if(legacy)File.Copy(savePath,savePath+".pre-v7-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"),false);
    d.version=2;d.completed=d.cleared.Distinct().Count();Data=d;Board=candidate;ResetBoardMotion();jump=d.level.ToString();
    if(legacy&&Board!=null)Save();
   }catch(Exception e){Notify("Save could not be read. A fresh board was loaded.");Debug.LogWarning("Save recovery: "+e.Message);try{File.Copy(savePath,savePath+".corrupt-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss"),false);}catch{}}
  }
  public void Save(){try{Data.board=Board.Snapshot();Data.history=Board.History.Take(100).ToList();Directory.CreateDirectory(Path.GetDirectoryName(savePath));File.WriteAllText(savePath+".tmp",JsonUtility.ToJson(Data,true));if(File.Exists(savePath))File.Replace(savePath+".tmp",savePath,null);else File.Move(savePath+".tmp",savePath);}catch(Exception e){Notify("Progress could not be saved.");Debug.LogError("Save failed: "+e.Message);}}
  void OnApplicationPause(bool paused){if(Board==null)return;applicationPaused=paused;if(paused)Save();}
  void OnApplicationQuit(){if(Board!=null)Save();}
  void Update(){if(Board==null)return;BusinessTick();UpdateSourceSpin();if(modal=="spinplaying"&&rewardPending&&Time.unscaledTime-rewardStart>=PendingRewardDuration)FinishReward();if(Input.GetKeyDown(KeyCode.Escape)){if(modal!="")CloseModal();else{selected=-1;hint=null;}}if(Input.GetKeyDown(KeyCode.R)&&modal=="")Open("restart");if(Input.GetKeyDown(KeyCode.Z)&&modal=="")Undo();if(Input.GetKeyDown(KeyCode.H)&&modal=="")ShowHint();if(Input.GetKeyDown(KeyCode.F12))ScreenCapture.CaptureScreenshot(Path.Combine(Application.persistentDataPath,"screenshot-"+DateTime.Now.ToString("yyyyMMddHHmmss")+".png"));}
  void Open(string name){CancelPendingReward();selected=-1;hint=null;modal=name;if(name=="spin"&&economy!=null)PrepareSpinPanel();if(name=="bonus")bonusOpenedAt=Time.unscaledTime;}
  void CloseModal(){if(modal=="spinplaying")return;if(modal=="gift"||modal=="bonus")Sound("boxout");else Sound("closePanel");if(modal=="bonus"&&HasCompletedReward){ClaimCompleted(false);return;}CancelPendingReward();modal=CompletedModal;}
  void ContinueReward(){Sound("closePanel");CancelPendingReward();modal=CompletedModal;}
  void LayoutBranches(){branchRects=new Rect[Board.Stands.Count];for(int i=0;i<Board.Stands.Count;i++){bool left=i%2==0;branchRects[i]=new Rect(left?0:390,864.6667f-BranchSourceY(Board.Stands.Count,i)*NativeToDesign,330,100);}}
  Rect FishRect(int i,int slot){var r=branchRects[i];bool left=Board.Stands[i].side<0;return new Rect(left?12+slot*69:636-slot*69,r.y-42,76,76);}
  public void TapStand(int i){
   if(modal!=""||motionInProgress||i<0||i>=Board.Stands.Count)return;
   if(selected<0){if(Board.Stands[i].fish.Count==0){Notify("Choose a branch with fish first.");return;}selected=i;PlayFishCry(Board.Stands[i].fish.Last());return;}
   if(selected==i){selected=-1;return;}
   int from=selected,n=Board.MovableCount(from,i);
   if(n==0){Notify("This branch has no free slots.");selected=Board.Stands[i].fish.Count>0?i:-1;if(selected>=0)PlayFishCry(Board.Stands[i].fish.Last());return;}
   BeginBoardTransfer(from,i,n);
  }
  public void CompleteLevel(){CompleteSourceLevel();}
  public void Undo(){if(motionInProgress)return;if(Data.undo<=0){RequestReward("undo");return;}if(Board.History.Count==0){Notify("No move to undo.");return;}var after=Board.Snapshot();var undoRecord=Board.History.Peek();if(Board.Undo()){ResetBoardMotion();BeginUndoAnimation(after,undoRecord);Data.undo--;ClearCompletedContext();selected=-1;hint=null;if(modal=="victory"||modal=="bonus"||modal=="stuck")modal="";Save();Sound();}}
  void AddBranch(){if(motionInProgress)return;RequestReward("add");}
  void Skip(){if(motionInProgress)return;RequestReward("skip");}
  void ShowHint(){hint=Board.Hint(Board.Stands.Count<=6?3000:200);if(hint==null)Notify("No legal move. Undo, restart or add a branch.");else Notify("Move the outlined branch to the other outline.");}
  void OnGUI(){if(Board==null){DrawLoading();return;}float scale=Mathf.Min(Screen.width/W,Screen.height/H);float offsetX=(Screen.width-W*scale)/2,offsetY=(Screen.height-H*scale)/2;GUI.matrix=Matrix4x4.TRS(new Vector3(offsetX,offsetY,0),Quaternion.identity,new Vector3(scale,scale,1));var previousEvent=BeginInteractionEvent();GUI.color=Color.white;Draw("GUI_"+Data.theme,new Rect(0,0,W,H),ScaleMode.ScaleAndCrop);text=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,wordWrap=true,fontSize=28,font=gameFont};text.normal.textColor=Color.white;
   GUI.enabled=true;backgroundInputBlocked=modal!="";Header();LayoutBranches();DrawBoard();DrawSourceHintHand();BottomBar();backgroundInputBlocked=false;DrawBusinessFly();if(Time.unscaledTime<toastUntil&&modal==""){Box(new Rect(80,1450,560,64),new Color(.01f,.13f,.27f,.9f));Label(toast,new Rect(90,1455,540,54),23);}
   if(modal!="")DrawModal();if(previousEvent!=null){interactionEventActive=false;Event.current=previousEvent;}
  }
  void Header(){DrawReferenceHeader();}
  void DrawBoard(){for(int i=0;i<Board.Stands.Count;i++){Rect r=branchRects[i];if(hint!=null&&(hint[0]==i||hint[1]==i))Box(new Rect(r.x+2,r.y-49,r.width-4,125),new Color(1,.83f,.1f,.23f));bool left=Board.Stands[i].side<0;var old=GUI.matrix;ApplyBranchWobble(i);var wobble=GUI.matrix;if(!left)GUI.matrix=wobble*Matrix4x4.TRS(new Vector3(720,0,0),Quaternion.identity,new Vector3(-1,1,1));Draw("GUI_branch3",new Rect(0,r.y+17,330,85));GUI.matrix=wobble;
   if(!collectingStands.Contains(i))for(int j=0;j<Board.Stands[i].fish.Count;j++){int f=Board.Stands[i].fish[j];var fr=FishRect(i,j);bool inFlight=motionInProgress&&(flying.Any(t=>t.Item3==fr)||undoFlights.Any(t=>t.Item3==fr));if(!inFlight)Fish(f,fr,IsSelectedSlot(i,j),false,!left);}
   GUI.matrix=old;RestoreInteractionMouse();if(modal==""&&GUI.Button(new Rect(r.x,r.y-48,r.width,126),GUIContent.none,GUIStyle.none))TapStand(i);
  }DrawBoardMotion();DrawUndoAnimation();}
  void BottomBar(){DrawReferenceFooter();}
  void DrawModal(){if(DrawBusinessModal()||DrawNativeModal()||DrawReferenceModal())return;var modalBasis=GUI.matrix;GUI.matrix=modalBasis*Matrix4x4.Translate(new Vector3(0,240,0));Box(new Rect(0,-240,W,H),new Color(0,.02f,.08f,.72f));Rect p=new Rect(70,285,580,690);Draw("resources_setbg",p,ScaleMode.StretchToFill);if(modal!="ad"&&modal!="victory"&&IconButton("resources_close",new Rect(588,251,65,65),"Close")){CloseModal();GUI.matrix=modalBasis;return;}
   switch(modal){
    case "settings":Title("SETTINGS");if(ActionButton("Music: "+(Data.music?"ON":"OFF"),400)){Data.music=!Data.music;UpdateMusic();Save();}if(ActionButton("Sound: "+(Data.sound?"ON":"OFF"),480)){Data.sound=!Data.sound;Save();}if(ActionButton("Restart level",560))modal="restart";if(ActionButton("How to play",640))modal="help";if(ActionButton("Privacy • local build",720))modal="privacy";if(ActionButton("Terms • local build",800))modal="terms";break;
    case "restart":Title("RESTART LEVEL?");Label("Your current arrangement and moves will reset.\nTools and demo balance are kept.",new Rect(105,435,510,170),29);if(ActionButton("Restart",695)){LoadLevel(Data.level);Notify("Level restarted.");}if(ActionButton("Keep playing",795))modal="";break;
    case "stuck":Title("NO LEGAL MOVE");Label("Under this local prototype rule set,\nno branch-to-branch move is available.\n\nUndo, add a branch or restart.\nThis does not claim the original game\nhas the same failure condition.",new Rect(105,405,510,245),27);if(ActionButton("Undo",685)){modal="";Undo();}if(ActionButton("Add branch",765)){modal="";AddBranch();}if(ActionButton("Restart",845))modal="restart";break;
    case "help":Title("HOW TO PLAY");Label("1. Tap a branch to select its end group.\n2. Tap a branch with free slots; colors may differ.\n3. Four of one kind swim away. Clear all fish.\n\nUndo, add a branch or use a hint if stuck.\nKeyboard: Z undo • H hint • R restart\nEscape closes panels.",new Rect(110,400,500,400),29);if(ActionButton("Play",825))modal="";break;
    case "privacy":Title("PRIVACY • LOCAL BUILD");Label("This reference build stores board progress, settings and demo points on this Mac.\n\nNo accounts, analytics, advertisements, payments or server connections are included.\n\nThese are local-build statements; the original publisher policy is unverified.",new Rect(110,410,500,365),27);if(ActionButton("About this build",825))modal="about";break;
    case "terms":Title("TERMS • LOCAL BUILD");Label("For local research and prototype review.\n\nThird-party artwork and audio remain owned by their respective rights holders. They are not licensed here for commercial use.\n\nDemo points have no cash value. Original online policies remain unverified.",new Rect(110,410,500,365),27);if(ActionButton("About this build",825))modal="about";break;
    case "about":Title("LOCAL REFERENCE BUILD");Label("Unity 6000.4.6f1\n350 locally loaded levels\n\nProgress is stored on this Mac.\nNo data is sent. No ads, accounts,\npayments or third-party server calls.\n\nThird-party assets are used for local study.\nCore rules follow recovered source.",new Rect(110,400,500,400),27);if(ActionButton("Close",825))modal="";break;
    case "withdraw":DrawWithdrawal();break;
    case "withdrawhelp":Title("WITHDRAWAL HELP");Label("LOCAL SIMULATION ONLY\n\n1. Demo points have no cash value.\n2. No PayPal or payment account is linked.\n3. No transfer will be submitted.\n4. Your board and settings stay on this Mac.\n\nThe original service rules are unverified.",new Rect(110,410,500,390),29);if(ActionButton("Back to demo",835))modal="withdraw";break;
    case "history":Title("DEMO HISTORY");Label("No real transactions.\n\nCompleted levels: "+Data.completed+"\nLocal demo points: "+Data.demoBalance.ToString("0.00")+"\n\nNo payment has been requested or sent.",new Rect(115,450,490,270),31);if(ActionButton("Back",835))modal="withdraw";break;
    case "piggy":Title("PIGGY BANK • DEMO");Draw("pig_图层 885",new Rect(205,405,310,240));Label("DEMO $ "+Data.demoBalance.ToString("0.00"),new Rect(120,642,480,55),35);Label("Native eligibility unknown\nLocal piggy wallet only\nNo cash value or payout.",new Rect(120,710,480,105),27);if(ActionButton("Cash out • demo info",835))modal="withdrawhelp";break;
    case "gift":Title("ONLINE REWARD • DEMO");DrawOnlineSpine(new Vector2(360,570),.7f,OnlineVisualTime(),OnlineRemaining==0);Label((OnlineRemaining>0?OnlineRemaining+"s until local reward":"Local reward ready")+"\nNo network connection required.",new Rect(105,665,510,130),29);GUI.enabled=OnlineRemaining==0;if(ActionButton("Simulate reward",825))RequestReward("online");GUI.enabled=true;break;
    case "spin":Title("LUCKY SPIN • DEMO");DrawWheel(wheelAngleOverride??Time.unscaledTime*18);Label("Progress "+Data.economy.spinProgress+"/"+economy.SpinTarget+"\nLocal client-rule preview",new Rect(110,738,500,80),25);if(ActionButton("Simulate spin reward",845)){Sound("turnOut");RequestReward("spin");}break;
    case "victory":Title(completedForce?"FORCED REWARD • DEMO":"GAME VICTORY");Draw("resources_xuanzhuanguang",new Rect(180,388,360,360));Draw("resources_money",new Rect(263,450,195,175));Label("All fish sorted!\n"+Money(completedBase),new Rect(120,634,480,110),35);Label("Client rules • local values only",new Rect(110,757,500,50),24);if(ActionButton("2x reward • simulated ad",798))ClaimCompleted(true);if(ActionButton("Base reward / next level",895))ClaimCompleted(false);break;
    case "bonus":DrawBonus();break;
    case "rewardtoast":Title("CONGRATULATIONS");if(rewardAction=="online"||rewardAction=="bonus")DrawBaked("gift_dakai",new Rect(180,375,360,360),Time.unscaledTime-giftOpenedAt,false);else Draw("resources_Moremoney",new Rect(210,420,300,245));Label("Local simulated reward received.\nDEMO $ "+Data.demoBalance.ToString("0.00")+" total\nNo cash value.",new Rect(120,685,480,135),30);if(ActionButton("Continue",845))ContinueReward();break;
    case "themes":DrawThemes();break;
    case "levels":DrawLevels();break;
   }
   GUI.matrix=modalBasis;
  }
  void DrawWithdrawal(){Box(new Rect(0,0,720,1280),new Color(.07f,.35f,.65f));Box(new Rect(0,0,720,122),new Color(.03f,.2f,.4f));if(TextButton("< Back",new Rect(25,32,145,65),30))modal="";Label("WITHDRAW • DEMO",new Rect(177,32,360,65),32);if(TextButton("?",new Rect(580,32,50,65),40))modal="withdrawhelp";if(TextButton("History",new Rect(605,113,105,50),20))modal="history";
   Box(new Rect(35,192,650,225),new Color(.32f,.12f,.65f));Label("DEMO BALANCE",new Rect(70,220,580,60),28);Label("$ "+Data.demoBalance.ToString("0.00"),new Rect(70,282,580,85),56);Label("Local points • no cash value",new Rect(70,359,580,40),23);Label("Payment account: not connected",new Rect(35,440,650,65),27);Box(new Rect(35,528,650,55),new Color(.72f,.12f,.27f));Label("EXCHANGE CASH • DISPLAY ONLY",new Rect(45,528,630,55),27);
   float[] amounts={800,1500,3000};for(int i=0;i<3;i++){Rect r=new Rect(35,622+i*153,650,130);Draw("resources_piggybankbg",r,ScaleMode.StretchToFill);Label("$"+amounts[i]+"  •  DEMO CARD",new Rect(r.x+20,r.y+12,610,50),33);Label("No payout available",new Rect(r.x+20,r.y+62,610,38),24);if(GUI.Button(r,GUIContent.none,GUIStyle.none))modal="withdrawhelp";}Label("No real funds, accounts or transfers.\nNo testimonials or earnings claims.",new Rect(35,1118,650,90),26);}
  void DrawBonus(){Title("LUCKY BONUS • DEMO");DrawBaked("gift_daiji",new Rect(210,375,300,300),Time.unscaledTime);Draw("resources_qipao",new Rect(185,365,350,325));Draw("resources_33",new Rect(100,665,520,115),ScaleMode.StretchToFill);int index=SourceBonusMultiplier()==3?2:SourceBonusPosition()<0?(SourceBonusMultiplier()==2?1:0):(SourceBonusMultiplier()==2?3:4);int[] multi={1,2,3,2,1};for(int i=0;i<5;i++)Label("x"+multi[i],new Rect(102+i*103,703,102,62),32);Box(new Rect(100+index*103,660,8,125),new Color(1,.58f,.06f));Label("Local multiplier preview • no cash value",new Rect(105,793,510,45),23);bool claimed=Data.bonusClaimed.Contains(Data.level);GUI.enabled=!claimed;if(ActionButton(claimed?"Already claimed":"Claim simulated x"+multi[index],845)){ClaimBonus(multi[index]);}GUI.enabled=true;}
  void ClaimBonus(int multiplier){ClaimCompleted(true);}
  void DrawThemes(){Title("THEMES");for(int k=0;k<6;k++){int n=themePage*6+k+1;if(n>20)break;float x=105+(k%3)*177,y=393+(k/3)*215;Draw("GUI_"+n,new Rect(x,y,155,175),ScaleMode.ScaleAndCrop);if(n==Data.theme)Box(new Rect(x,y,155,175),new Color(.8f,1,.2f,.25f));if(GUI.Button(new Rect(x,y,155,175),GUIContent.none,GUIStyle.none)){Data.theme=n;Save();Sound();}Label(n==Data.theme?"Selected":n.ToString(),new Rect(x,y+174,155,35),22);}if(TextButton("<",new Rect(110,850,90,60),36))themePage=Mathf.Max(0,themePage-1);Label((themePage+1)+" / 4",new Rect(210,855,300,50),27);if(TextButton(">",new Rect(520,850,90,60),36))themePage=Mathf.Min(3,themePage+1);}
  void DrawLevels(){Title("LEVEL BROWSER");Label("Prototype review: any level can be opened",new Rect(100,372,520,50),23);for(int k=0;k<25;k++){int n=levelPage*25+k+1;if(n>350)break;Rect r=new Rect(105+(k%5)*103,443+(k/5)*72,96,62);Box(r,Data.cleared.Contains(n)?new Color(.1f,.5f,.3f):new Color(.07f,.29f,.43f));if(TextButton(n.ToString(),r,27)){LoadLevel(n);}}
   if(TextButton("<",new Rect(105,817,80,50),35))levelPage=Mathf.Max(0,levelPage-1);Label((levelPage+1)+" / 14",new Rect(210,817,300,50),25);if(TextButton(">",new Rect(530,817,80,50),35))levelPage=Mathf.Min(13,levelPage+1);jump=GUI.TextField(new Rect(110,887,120,50),jump,3);if(TextButton("Open level",new Rect(250,887,350,50),26)){int n;if(int.TryParse(jump,out n)&&n>=1&&n<=350)LoadLevel(n);else Notify("Enter a level from 1 to 350.");}}
  void DrawWheel(float angle){var basis=GUI.matrix;var pivot=new Vector3(360,981,0);try{GUI.matrix=basis*Matrix4x4.Translate(pivot)*Matrix4x4.Rotate(Quaternion.Euler(0,0,angle))*Matrix4x4.Translate(-pivot);lastWheelCenter=GUI.matrix.MultiplyPoint3x4(pivot);Draw("resources_转盘",new Rect(84,705,552,552));for(int i=0;i<8;i++){GUI.matrix=basis*Matrix4x4.Translate(pivot)*Matrix4x4.Rotate(Quaternion.Euler(0,0,angle+i*45))*Matrix4x4.Translate(-pivot);Label(Data.economy==null?"DEMO":SourceEconomy.Display(Data.economy.spinBase*SourceEconomy.SpinMultipliers[i],Data.economy.country),new Rect(290,742,140,30),20,new Color(1,.2f,.2f));Draw("resources_Moremoney",new Rect(319,782,82,70));}}finally{GUI.matrix=basis;}}
  void Title(string s){Label(s,new Rect(93,314,534,65),35,new Color(1,.87f,.38f));}
  bool ActionButton(string s,float y){Draw("resources_btn22",new Rect(115,y,490,65),ScaleMode.StretchToFill);return TextButton(s,new Rect(115,y,490,65),27);}
  void Draw(string name,Rect r,ScaleMode mode=ScaleMode.ScaleToFit){if(Data!=null&&Data.economy!=null&&(name=="resources_money"||name=="resources_Moremoney"))name=SourceEconomy.MoneyAsset(Data.economy.country,name=="resources_Moremoney");Texture2D t;if(art.TryGetValue(name,out t))GUI.DrawTexture(r,t,mode,true);}
  void Fish(int f,Rect r,bool glow=false,bool swimming=false,bool facingLeft=false){var originalMatrix=GUI.matrix;if(facingLeft){var p=new Vector3(r.center.x,r.center.y,0);GUI.matrix=GUI.matrix*Matrix4x4.Translate(p)*Matrix4x4.Scale(new Vector3(-1,1,1))*Matrix4x4.Translate(-p);}try{f=FishResourceIndex[f];string key="fish"+f+"_"+(glow?"xuanzhong":swimming?"youdong":"daiji");BakedClip metadata;float world=bakedClips.TryGetValue(key,out metadata)&&metadata.worldSize>0?metadata.worldSize:256f/1.25f;float size=r.width*world/140f;var view=new Rect(r.center.x-size/2,r.center.y-size/2,size,size);if(DrawBaked(key,view,Time.unscaledTime+(swimming?0:f*.07f)))return;if(glow){Draw("fish"+f,r);return;}float scale=r.width/140;foreach(var p in rigs[f].parts)Draw(p.texture,new Rect(r.center.x+p.x*scale,r.center.y+p.y*scale,p.w*scale,p.h*scale));}finally{GUI.matrix=originalMatrix;}}
  bool DrawBaked(string key,Rect r,float time,bool loop=true){BakedClip clip;Texture2D texture;if(!bakedClips.TryGetValue(key,out clip)||!animationTextures.TryGetValue(key,out texture))return false;float phase=loop?Mathf.Repeat(time,clip.duration):Mathf.Clamp(time,0,clip.duration-.0001f);int frame=Mathf.Clamp((int)(phase/clip.duration*clip.frames),0,clip.frames-1);if(key=="gift_dakai")lastGiftFrame=frame;if(key=="spinhand_newAnimation")lastSpinHandFrame=frame;if(key=="hudwheel_zhuandong")lastHudWheelFrame=frame;if(key=="hudpig_jinbi")lastHudPigFrame=frame;GUI.DrawTextureWithTexCoords(r,texture,new Rect((frame%clip.columns)/(float)clip.columns,1-(frame/clip.columns+1)/(float)clip.rows,1f/clip.columns,1f/clip.rows),true);return true;}
  void DrawLoading(){float scale=Mathf.Min(Screen.width/W,Screen.height/H);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-W*scale)/2,(Screen.height-H*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));GUI.color=Color.white;Draw("main_loading",new Rect(0,0,W,H),ScaleMode.ScaleAndCrop);Draw("main_logo",new Rect(90,95,540,320));text=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,wordWrap=true,font=gameFont,fontSize=26};Draw("main_sliderbg",new Rect(145,950,430,65),ScaleMode.StretchToFill);GUI.BeginGroup(new Rect(153,955,414*loadProgress,55));Draw("main_sliderbar",new Rect(0,0,414,55),ScaleMode.StretchToFill);GUI.EndGroup();Label("Loading local resources "+Mathf.RoundToInt(loadProgress*100)+"%",new Rect(105,1019,510,55),25);}

  void Box(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,white);GUI.color=old;}
  void Label(string s,Rect r,int size=28,Color? color=null,TextAnchor alignment=TextAnchor.MiddleCenter,Color? outline=null){var oldAlignment=text.alignment;text.alignment=alignment;text.fontSize=size;text.normal.textColor=outline??new Color(.04f,.06f,.09f,.95f);float o=1.25f;foreach(var v in new[]{new Vector2(-o,0),new Vector2(o,0),new Vector2(0,-o),new Vector2(0,o),new Vector2(-o,-o),new Vector2(o,-o),new Vector2(-o,o),new Vector2(o,o)})GUI.Label(new Rect(r.x+v.x,r.y+v.y,r.width,r.height),s,text);text.normal.textColor=color??Color.white;GUI.Label(r,s,text);text.alignment=oldAlignment;}
  bool TextButton(string s,Rect r,int size=28,Color? outline=null){RestoreInteractionMouse();Label(s,r,size,null,TextAnchor.MiddleCenter,outline);bool debugEvent=interactionAcceptance&&Event.current.isMouse;if(debugEvent)Debug.Log("GUI_INPUT before text="+s+" type="+Event.current.type+" pos="+Event.current.mousePosition+" rect="+r+" contains="+r.Contains(Event.current.mousePosition)+" blocked="+backgroundInputBlocked+" enabled="+GUI.enabled+" hot="+GUIUtility.hotControl);bool clicked=!backgroundInputBlocked&&GUI.Button(r,GUIContent.none,GUIStyle.none);if(debugEvent)Debug.Log("GUI_INPUT after text="+s+" clicked="+clicked+" type="+Event.current.type+" hot="+GUIUtility.hotControl);if(clicked)Sound();return clicked;}
  bool IconButton(string name,Rect r,string tooltip){Draw(name,r);RestoreInteractionMouse();bool clicked=!backgroundInputBlocked&&GUI.Button(r,new GUIContent("",tooltip),GUIStyle.none);if(clicked&&name!="resources_close")Sound();return clicked;}
  IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(testDir,name+".png"));yield return new WaitForSecondsRealtime(.2f);}

 }
}
