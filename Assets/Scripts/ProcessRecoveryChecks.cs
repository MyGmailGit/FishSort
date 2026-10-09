using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
namespace FishSortLocal {
 [Serializable] public sealed class RecoveryExpected {public string kind,receipt;public int level,passNum,spinUses,spinCounter,sourceLevel;public double amount,top,piggy,remaining;}
 public sealed partial class FishSortGame {
  string restartStage;
  IEnumerator ProcessRecoveryCheck(){
   yield return null;var notes=new System.Collections.Generic.List<string>();Action<bool,string> check=(ok,message)=>{if(!ok){File.WriteAllLines(Path.Combine(testDir,restartStage+"-checks.txt"),notes.Concat(new[]{"FAIL "+message}));Debug.LogError("PROCESS_RECOVERY_FAIL "+message);Application.Quit(1);throw new Exception(message);}notes.Add("PASS "+message);};
   string expectedPath=Path.Combine(testDir,"expected.json");
   if(restartStage.StartsWith("prepare-")){
    string kind=restartStage.Substring(8);ResetRevisionFixture(kind=="normal"?4:kind=="forced"?5:kind=="online"?351:8);holdEarlyAutoAdvance=false;
    if(kind=="normal"||kind=="forced"){Data.economy.levelBase=3.17;CompleteConservedFixture();check(Board.Won&&HasCompletedReward&&modal==(kind=="forced"?"bonus":"victory"),"prepare genuine conserved completed board with pending reward");}
    if(kind=="spin"){Data.economy.spinProgress=economy.SpinTarget;sourceSpinIndex=4;spinRewardValue=9.51;spinSelectionReady=true;RequestReward("spin");check(modal=="spinplaying"&&Data.economy.pendingSpin!=null,"prepare active production spin before process exit");}
    if(kind=="online"){Data.economy.onlineRemaining=37.25;OnApplicationFocus(false);check(!applicationFocused,"prepare focus-loss save without advancing foreground clock");}
    var expected=new RecoveryExpected{kind=kind,level=Data.level,passNum=Data.economy.passNum,spinUses=Data.economy.spinUses,spinCounter=Data.economy.limitTaskZhuanpan,sourceLevel=Data.sourceLevel,top=Data.economy.top,piggy=Data.economy.piggy,remaining=Data.economy.onlineRemaining,amount=kind=="spin"?Data.economy.pendingSpin.amount:kind=="normal"||kind=="forced"?completedBase:0,receipt=kind=="spin"?Data.economy.pendingSpin.receipt:"pass:"+Data.level};
    File.WriteAllText(expectedPath,JsonUtility.ToJson(expected,true));Save();check(File.Exists(savePath),"prepare isolation save exists on disk");
   }else{
    var expected=JsonUtility.FromJson<RecoveryExpected>(File.ReadAllText(expectedPath));check(Data.economy.country=="us","fresh process restores default US provider");check(Data.economy.passNum==expected.passNum,"fresh process never advances completion progress twice");check(Data.economy.spinUses==expected.spinUses&&Data.economy.limitTaskZhuanpan==expected.spinCounter,"fresh process never consumes spin qualification/counter twice");
    if(restartStage=="resume"){
     check(Data.level==expected.level&&Data.sourceLevel==expected.sourceLevel,"fresh process restores actual and chosen source level");
     if(expected.kind=="normal"||expected.kind=="forced"){
      check(Board.Won&&HasCompletedReward&&completedBase==expected.amount&&modal==(expected.kind=="forced"?"bonus":"victory"),"fresh process restores original unclaimed amount and correct production panel");
      check(Data.economy.top==expected.top&&Data.economy.piggy==expected.piggy,"unclaimed reward does not credit automatically during boot");localWhiteBao=expected.kind=="forced";ClaimCompleted(false);check(Data.level==expected.level+1&&Data.economy.pendingLevel==null,"claim restored reward once and advance");
     }else if(expected.kind=="spin"){
      check(modal=="spinplaying"&&Data.economy.pendingSpin!=null&&Data.economy.pendingSpin.receipt==expected.receipt&&spinRewardValue==expected.amount,"fresh process resumes exact captured spin and receipt");CloseModal();check(modal=="spinplaying","recovered spin also blocks premature close");yield return new WaitForSecondsRealtime(9.7f);check(Data.economy.pendingSpin==null&&modal=="rewardtoast","recovered full-duration spin reaches real reward toast");FinishSourceSpin();
     }else{
      OnApplicationFocus(false);check(Data.economy.onlineRemaining<=expected.remaining&&expected.remaining-Data.economy.onlineRemaining<.1,"fresh process preserves focus-saved countdown, no offline accumulation");check(Data.level==351&&Data.sourceLevel>=150&&Data.sourceLevel<=348,"high actual level chosen resource restored across processes");Save();
     }
     if(expected.kind!="online"){check(Data.economy.ledger.Count(x=>x.id==expected.receipt)==1&&Data.economy.ledger.Last().raw==expected.amount,"fresh process credits exactly one captured receipt and raw amount");Save();}
    }else if(restartStage=="verify"){
     if(expected.kind!="online"){
      check(Data.economy.receipts.Count(x=>x==expected.receipt)==1&&Data.economy.ledger.Count(x=>x.id==expected.receipt)==1,"second restart retains exactly one payment record");check(Data.economy.pendingLevel==null&&Data.economy.pendingSpin==null,"second restart does not resurrect settled transaction");int count=Data.economy.ledger.Count;check(economy.Credit(expected.receipt,"v2_pass",expected.amount)==null&&Data.economy.ledger.Count==count,"duplicate transaction after second restart cannot pay again");
     }else{OnApplicationFocus(false);check(Data.level==351&&Data.sourceLevel==expected.sourceLevel&&Data.economy.ledger.Count==0,"second restart preserves selected high board without inventing reward");}
    }else throw new Exception("Unknown restart stage");
   }
   File.WriteAllLines(Path.Combine(testDir,restartStage+"-checks.txt"),notes);File.WriteAllText(Path.Combine(testDir,restartStage+"-state.json"),JsonUtility.ToJson(Data,true));Debug.Log("PROCESS_RECOVERY_PASS "+restartStage+" "+notes.Count);Application.Quit(0);
  }
 }
}
