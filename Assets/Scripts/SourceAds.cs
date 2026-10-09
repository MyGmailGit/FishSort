using System;
using System.Collections.Generic;
namespace FishSortLocal {
 public enum AdKind {Rewarded,Interstitial}
 public enum AdOutcome {EligibleClosed,Failed,Cancelled,Unavailable}
 public sealed class AdRequest {public string id,action,nativePlacement,toolTag;public AdKind kind;public int generation,level;public double amount,rate=1;public string biz;}
 public interface IAdProvider {void Show(AdRequest request,Action<AdOutcome> completed);void Cancel();}
 // Intentionally empty SDK integration point. A future adapter translates SDK callbacks to the explicit outcome contract.
 public sealed class SdkAdProviderPlaceholder:IAdProvider {public void Show(AdRequest request,Action<AdOutcome> completed){completed(AdOutcome.Unavailable);}public void Cancel(){}}
 public sealed class LocalAdSimulator:IAdProvider {
  Action<AdOutcome> callback;public AdRequest Current {get;private set;}
  public void Show(AdRequest request,Action<AdOutcome> completed){Current=request;callback=completed;}
  public void Complete(AdOutcome result){var done=callback;callback=null;Current=null;if(done!=null)done(result);}
  public void Cancel(){callback=null;Current=null;}
 }
 public sealed class SourceAdCoordinator {
  readonly IAdProvider provider;int token;readonly Dictionary<string,double> next=new Dictionary<string,double>();
  public AdRequest Pending {get;private set;}
  public SourceAdCoordinator(IAdProvider p){provider=p;}
  public bool Begin(AdRequest req,double now,double debounce,Action<AdOutcome,AdRequest> done,Action accepted=null){double until;if(Pending!=null||(next.TryGetValue(req.action,out until)&&now<until))return false;next[req.action]=now+debounce;Pending=req;int current=++token;bool consumed=false;Action<AdOutcome> callback=result=>{if(consumed||current!=token||Pending!=req)return;consumed=true;Pending=null;done(result,req);};if(accepted!=null)accepted();try{provider.Show(req,callback);}catch{callback(AdOutcome.Failed);}return true;}
  public void Cancel(){++token;Pending=null;provider.Cancel();}
  public static AdRequest Route(string action){var r=new AdRequest{action=action,kind=AdKind.Rewarded,nativePlacement="10",toolTag="0"};switch(action){case "add":r.nativePlacement="20";r.toolTag="1";break;case "undo":r.nativePlacement="20";r.toolTag="2";break;case "skip":r.nativePlacement="20";r.toolTag="3";break;case "spin":r.nativePlacement="30";break;case "bonus":case "passDouble":r.nativePlacement="0";break;case "forceClose":r.kind=AdKind.Interstitial;r.nativePlacement="";break;case "fly":break;default:throw new ArgumentException("Unknown ad action");}return r;}
  public static double Debounce(string action){return action=="passDouble"?1:action=="bonus"?3:2;}
 }
}
