using System;
namespace FishSortLocal {
 public enum NativeRewardOutcome {Success,Failed,Cancelled,Unavailable}
 public interface ITimeLimitTaskProvider {void Show(int type,string value,Action<NativeRewardOutcome> completed);}
 public static class TimeLimitTaskRouting {
  public static string Value(int type,EconomyState state){switch(type){case 1:return state.passNum.ToString(System.Globalization.CultureInfo.InvariantCulture);case 2:return "0";case 3:return state.limitTaskDisBlock.ToString(System.Globalization.CultureInfo.InvariantCulture);case 4:return state.limitTaskZhuanpan.ToString(System.Globalization.CultureInfo.InvariantCulture);default:throw new ArgumentOutOfRangeException("type");}}
 }
 public sealed class TimeLimitTaskPlaceholder:ITimeLimitTaskProvider {public void Show(int type,string value,Action<NativeRewardOutcome> completed){completed(NativeRewardOutcome.Unavailable);}}
 // Business callback fixture only. It asserts no native eligibility and submits nothing to the original service.
 public sealed class LocalTimeLimitTaskSimulator:ITimeLimitTaskProvider {
  public NativeRewardOutcome next=NativeRewardOutcome.Success;
  public void Show(int type,string value,Action<NativeRewardOutcome> completed){completed(next);}
 }
}
