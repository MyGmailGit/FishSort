using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
namespace FishSortLocal {
 public interface ICountryProvider { string GetCountry(); }
 // Replace only this provider with the user's country API adapter later.
 public sealed class UnitedStatesCountryProvider:ICountryProvider {public string GetCountry(){return "us";}}
 public interface IRuleConfigProvider { RuleConfig GetConfig(); }
 public sealed class DefaultRuleConfigProvider:IRuleConfigProvider {public RuleConfig GetConfig(){return RuleConfig.Default();}}
 public interface IUnitRandom {double Next();}
 public sealed class SeededUnitRandom:IUnitRandom {readonly Random random;public SeededUnitRandom(int seed){random=new Random(seed);}public double Next(){return random.NextDouble();}}
 [Serializable] public sealed class MoneyRule {public double start,end,min,max; public MoneyRule(double s,double e,double l,double h){start=s;end=e;min=l;max=h;}}
 [Serializable] public sealed class ForceRule {public int min,max,interval;public ForceRule(int a,int b,int c){min=a;max=b;interval=c;}}
 [Serializable] public sealed class RuleConfig {
  public MoneyRule[] mc;public ForceRule[] pac;public string provenance="compiled client defaults";
  public static RuleConfig Default(){return new RuleConfig{mc=new[]{new MoneyRule(0,40,2,4),new MoneyRule(40,50,1,3),new MoneyRule(50,60,1,2),new MoneyRule(60,70,.5,1.2),new MoneyRule(70,80,.2,.4),new MoneyRule(80,90,.1,.2),new MoneyRule(90,95,.04,.08),new MoneyRule(95,100,.01,.03),new MoneyRule(100,200,4,6),new MoneyRule(200,250,2,3),new MoneyRule(250,280,1,2),new MoneyRule(280,290,.1,.3),new MoneyRule(290,300,.05,.1),new MoneyRule(300,-1,1,2)},pac=new[]{new ForceRule(1,50,5),new ForceRule(51,100,4),new ForceRule(101,-1,3)}};}
  public bool Valid(){if(mc==null||pac==null||mc.Length==0||pac.Length==0)return false;foreach(var r in mc)if(r==null||!SourceEconomy.Finite(r.start)||!SourceEconomy.Finite(r.end)||!SourceEconomy.Finite(r.min)||!SourceEconomy.Finite(r.max)||r.start<0||r.min<0||r.max<r.min||r.max>1000000||(r.end!=-1&&r.end<r.start))return false;foreach(var r in pac)if(r==null||r.min<1||r.interval<1||(r.max!=-1&&r.max<r.min))return false;return true;}
 }
 [Serializable] public sealed class EconomyState {
  public int schema=2,passNum=1,spinProgress,spinUses,onlineIndex,addUsedLevel=-1,todayPass,limitTaskDisBlock,limitTaskZhuanpan;
  public double top,piggy,levelBase,spinBase,onlineRemaining=30,onlineReward;
  public bool firstSpinUsed,newGiftSeen,newGiftClaimed;
  public string onlineDay="",todayDay="",country="us",configProvenance="compiled client defaults";
  public RuleConfig config;
  public bool levelBaseReady;
  public PendingLevelReward pendingLevel;
  public PendingSpinReward pendingSpin;
  public List<string> receipts=new List<string>();
  public List<RewardRecord> ledger=new List<RewardRecord>();
 }
 [Serializable] public sealed class PendingLevelReward {public bool active;public int level;public double amount;public bool forced;}
 [Serializable] public sealed class PendingSpinReward {public bool active;public string receipt;public int index;public double amount;}
 [Serializable] public sealed class RewardRecord {
  public string id,biz;public double raw,rate,topAdd,piggyAdd,topAfter,piggyAfter;
  public string country; // Display profile only. Raw/native reward is never multiplied by country rate.
 }
 public sealed class SourceEconomy {
  public readonly EconomyState State;readonly IUnitRandom random;RuleConfig rules;
  public SourceEconomy(EconomyState state,IUnitRandom rng,ICountryProvider countries,IRuleConfigProvider configs){State=state??new EconomyState();random=rng;State.country=NormalizeCountry(countries.GetCountry());var defaults=configs.GetConfig();rules=defaults!=null&&defaults.Valid()?defaults:RuleConfig.Default();if(configs is DefaultRuleConfigProvider&&State.config!=null&&State.config.Valid())rules=State.config;State.config=rules;State.configProvenance=rules.provenance;State.receipts=State.receipts??new List<string>();State.ledger=State.ledger??new List<RewardRecord>();}
  public static void NormalizePending(EconomyState state){if(state.pendingLevel!=null&&!state.pendingLevel.active)state.pendingLevel=null;if(state.pendingSpin!=null&&!state.pendingSpin.active)state.pendingSpin=null;}
  public static bool Finite(double x){return !double.IsNaN(x)&&!double.IsInfinity(x);}
  // JS Number.toFixed(2): nearest decimal hundredth of the exact IEEE double, ties away for nonnegative input.
  public static double FixedTwo(double x){if(!Finite(x)||x<0||x>=1e15)throw new ArgumentOutOfRangeException("x");ulong bits=(ulong)BitConverter.DoubleToInt64Bits(x);int exponent=(int)((bits>>52)&2047);ulong mantissa=bits&0xfffffffffffffUL;int shift=exponent==0?-1074:exponent-1075;if(exponent!=0)mantissa|=1UL<<52;BigInteger numerator=new BigInteger(mantissa)*100,denominator=BigInteger.One;if(shift>=0)numerator<<=shift;else denominator<<=-shift;BigInteger rem;var q=BigInteger.DivRem(numerator,denominator,out rem);if(rem*2>=denominator)q++;return (double)q/100;}
  public static double FloorTwo(double x){return FixedTwo(Math.Floor(x*100)/100);}
  public static double RandomInteger(double unit,double min,double max){if(!Finite(unit)||unit<0||unit>=1||max<min)throw new ArgumentOutOfRangeException("unit");return Math.Floor(unit*(max-min+1)+min);}
  public MoneyRule RuleFor(double top){foreach(var r in rules.mc)if(top>=r.start&&top<=r.end)return r;return rules.mc[rules.mc.Length-1];}
  public double BaseReward(double top){var r=RuleFor(top);return FixedTwo(RandomInteger(random.Next(),r.min*1000,r.max*1000)/1000.0);}
  public bool OverrideConfig(RuleConfig incoming){if(incoming==null||!incoming.Valid())return false;rules=incoming;State.config=incoming;State.configProvenance=incoming.provenance;return true;}
  public bool ApplyConfigFields(MoneyRule[] mc,ForceRule[] pac,string provenance){return OverrideConfig(new RuleConfig{mc=mc??rules.mc,pac=pac??rules.pac,provenance=provenance});}
  public bool ForceVideo(int level){var r=rules.pac[rules.pac.Length-1];foreach(var f in rules.pac)if(level>=f.min&&level<=f.max){r=f;break;}return level!=r.min&&(level+1-r.min)%r.interval==0;}
  public int SpinTarget {get{return State.spinUses==0?2:3;}}
  public bool SpinReady {get{return State.spinProgress>=SpinTarget;}}
  public void AdvancePass(){State.passNum++;State.todayPass++;State.spinProgress=Math.Min(SpinTarget,State.spinProgress+1);}
  public void PrepareLevel(){State.levelBase=BaseReward(State.top);State.levelBaseReady=true;}
  public static readonly int[] SpinMultipliers={10,1,5,2,3,7,1,2};
  public static int SpinIndex(int draw){if(draw<0||draw>=1000)throw new ArgumentOutOfRangeException("draw");double cumulative=0;double[] weights={0,.3,0,.2,.1,0,.3,.1};for(int i=0;i<8;i++){cumulative+=weights[i];if(draw<=cumulative*1000)return i;}return 7;}
  public double AwardAmount(string biz,double preset=0){if(biz=="v2_novice")return 50;if(biz=="v2_pass")return State.passNum==2?26.72:State.passNum==3?38.64:State.passNum==4?42.32:preset;if(biz=="v2_passd"||biz=="v2_spinReward"||biz=="v2_onlineReard")return preset;if(biz=="v2_limitTask"&&State.top<=730)return 50;return BaseReward(State.top);}
  public RewardRecord Credit(string id,string biz,double raw,double rate=1,int skipFlag=0){if(skipFlag==1||string.IsNullOrEmpty(id)||State.receipts.Contains(id))return null;if(!Finite(raw)||raw<0||raw>1e9||!Finite(rate)||rate<=0)throw new ArgumentOutOfRangeException("reward");double pig=FloorTwo(.1*raw),top=raw-pig;if(pig<.01)pig=.01;if(top<.01)top=.01;var record=new RewardRecord{id=id,biz=biz,raw=raw,rate=rate,topAdd=top,piggyAdd=pig,topAfter=FloorTwo(State.top+top),piggyAfter=FloorTwo(State.piggy+pig),country=State.country};State.top=record.topAfter;State.piggy=record.piggyAfter;State.receipts.Add(id);State.ledger.Add(record);return record;}
  public void UpdateOnline(double elapsed,string day){if(State.onlineDay!=day){State.onlineDay=day;State.onlineIndex=0;State.onlineRemaining=30;State.onlineReward=0;}State.onlineRemaining=Math.Max(0,State.onlineRemaining-Math.Max(0,elapsed));if(State.onlineRemaining==0&&State.onlineReward==0)State.onlineReward=1.5*BaseReward(State.top);}
  public RewardRecord ClaimOnline(string id){if(State.onlineRemaining>0||State.onlineReward<=0)return null;var r=Credit(id,"v2_onlineReard",State.onlineReward);if(r==null)return null;State.onlineReward=0;State.onlineIndex=Math.Min(6,State.onlineIndex+1);State.onlineRemaining=State.onlineIndex==1?60:120;return r;}
  public static string NormalizeCountry(string value){value=(value??"us").ToLowerInvariant();return Array.IndexOf(new[]{"ru","kr","jp","de","mx","cl","co","pe","id","us","br","pk","en","ar","in"},value)>=0?value:"us";}
  public static double CountryRate(string c){switch(c){case "br":return 10;case "id":return 10000;case "ru":return 100;case "kr":return 1000;case "jp":return 200;case "in":return 100;default:return 1;}}
  public static string MoneyAsset(string c,bool large){switch(c){case "br":return large?"resources_BRMoremoney":"resources_BRmoney";case "id":return large?"resources_IDMoremoney":"resources_IDmoney";case "ru":return large?"resources_RUMoremoney":"resources_RUmoney";case "kr":return large?"resources_kr2":"resources_kr1";case "jp":return large?"resources_jp2":"resources_jp1";case "de":return large?"resources_ouyuan2":"resources_ouyuan1";case "in":return large?"resources_yindu2":"resources_yindu1";default:return large?"resources_Moremoney":"resources_money";}}
  public static string Currency(string c){switch(c){case "br":return "R$";case "id":return "Rp";case "ru":return "₽";case "kr":return "₩";case "jp":return "Ұ";case "de":return "€";case "in":return "₹";default:return "$";}}
  public static string Display(double raw,string country){double shown=FloorTwo(raw*CountryRate(country));if(country=="id"||country=="ru")return Math.Floor(shown).ToString("#,0",CultureInfo.InvariantCulture).Replace(',','.');if(country=="jp"||country=="kr"||country=="in")return Math.Floor(shown).ToString("#,0",CultureInfo.InvariantCulture);if(country=="us"||country=="en")return shown.ToString(shown==Math.Floor(shown)?"0.00":"0.##",CultureInfo.InvariantCulture);return shown.ToString("0.##",CultureInfo.InvariantCulture);}
 }
}
