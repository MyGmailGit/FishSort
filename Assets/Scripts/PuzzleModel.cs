using System;
using System.Collections.Generic;
using System.Linq;

namespace FishSortLocal {
 [Serializable] public class StandData { public int type; public int numSlot; }
 [Serializable] public class StandConfig { public int side; public int[] idBirds; public StandData standData; }
 [Serializable] public class LevelConfig { public StandConfig[] standConfig; }
 [Serializable] public class Stand { public int side; public List<int> fish = new List<int>(); public Stand Copy() { return new Stand {side=side,fish=new List<int>(fish)}; } }
 [Serializable] public class BoardSnapshot { public List<Stand> stands = new List<Stand>(); public int moves; public int[] clearedFish; public int transferFrom=-1,transferTo=-1,transferCount; }
 public sealed class PuzzleModel {
  public const int Capacity = 4; // Recovered GamePool uses literal 4; standData.numSlot does not control transfer capacity.
  public List<Stand> Stands = new List<Stand>();
  public int Moves;
  public int[] ClearedFish = new int[10];
  public readonly Stack<BoardSnapshot> History = new Stack<BoardSnapshot>();
  public PuzzleModel(LevelConfig level) {
   if(level == null || level.standConfig == null || level.standConfig.Length < 2) throw new ArgumentException("Invalid level");
   foreach(var s in level.standConfig) {
    if(s.idBirds == null || s.idBirds.Length > Capacity || s.idBirds.Any(x=>x<0||x>9)) throw new ArgumentException("Invalid fish");
    Stands.Add(new Stand{side=Stands.Count%2==0?-1:1,fish=new List<int>(s.idBirds)});
   }
  }
  public PuzzleModel(BoardSnapshot b) { Restore(b); }
  public BoardSnapshot Snapshot() { return new BoardSnapshot {stands=Stands.Select(s=>s.Copy()).ToList(),moves=Moves,clearedFish=(int[])ClearedFish.Clone()}; }
  public void Restore(BoardSnapshot b) {
   if(b==null || b.stands==null || b.stands.Count<2 || b.stands.Count>14 || b.moves<0 || b.stands.Any(s=>s==null || s.fish==null || s.fish.Count>4 || s.fish.Any(x=>x<0||x>9))) throw new ArgumentException("Invalid saved board");
   var cleared=b.clearedFish==null||b.clearedFish.Length==0?new int[10]:b.clearedFish;
   if(cleared.Length!=10||cleared.Any(n=>n<0||n%Capacity!=0)||cleared.Sum()+b.stands.Sum(x=>x.fish.Count)>56)throw new ArgumentException("Invalid cleared fish ledger");
   Stands=b.stands.Select((s,i)=>new Stand{side=i%2==0?-1:1,fish=new List<int>(s.fish)}).ToList(); Moves=b.moves;ClearedFish=(int[])cleared.Clone();
  }
  public bool Complete(int i) { var a=Stands[i].fish; return a.Count==Capacity && a.All(x=>x==a[0]); }
  public bool Won { get { return Stands.All(s=>s.fish.Count==0); } }
  public bool HasLegalMove {get {for(int a=0;a<Stands.Count;a++)for(int b=0;b<Stands.Count;b++)if(MovableCount(a,b)>0)return true;return false;}}
  public int MovableCount(int from,int to) {
   if(from<0||to<0||from>=Stands.Count||to>=Stands.Count||from==to) return 0;
   var a=Stands[from].fish; var b=Stands[to].fish;
   if(a.Count==0 || b.Count>=Capacity || Complete(from)) return 0;
   int color=a[a.Count-1];
   int count=1; while(count<a.Count && a[a.Count-1-count]==color) count++;
   return Math.Min(count,Capacity-b.Count);
  }
  public int Move(int from,int to) {
   int n=MovableCount(from,to); if(n==0) return 0;
   var prior=Snapshot();prior.transferFrom=from;prior.transferTo=to;prior.transferCount=n;History.Push(prior); var a=Stands[from].fish; var b=Stands[to].fish;
   b.AddRange(a.GetRange(a.Count-n,n)); a.RemoveRange(a.Count-n,n); Moves++; return n;
  }
  // Called once moving fish and the completion flight have settled; never during transfer.
  public List<int> ResolveCompletedGroups() {
   var groups=new List<int>();for(int i=0;i<Stands.Count;i++)if(Complete(i)){ClearedFish[Stands[i].fish[0]]+=Capacity;Stands[i].fish.Clear();groups.Add(i);}return groups;
  }
  public int[] ConservedHistogram(){var n=Histogram();for(int i=0;i<n.Length;i++)n[i]+=ClearedFish[i];return n;}
  public bool Undo() { if(History.Count==0) return false; Restore(History.Pop()); return true; }
  public bool AddStand() { if(Stands.Count>=14) return false; History.Push(Snapshot()); Stands.Add(new Stand{side=Stands.Count%2==0?-1:1}); return true; }
  public string Key() { return string.Join("/", Stands.Select(s=>string.Join(",",s.fish))); }
  public int[] Histogram() { int[] counts=new int[10]; foreach(var s in Stands) foreach(int f in s.fish) counts[f]++; return counts; }
  // Bounded breadth-first hint: small boards receive a solving move; large boards get a legal heuristic move.
  public int[] Hint(int budget=20000) {
   if(Won) return null;
   var queue=new Queue<Tuple<PuzzleModel,int[]>>(); var visited=new HashSet<string>();
   queue.Enqueue(Tuple.Create(new PuzzleModel(Snapshot()),(int[])null)); visited.Add(Key());
   int explored=0;
   while(queue.Count>0 && explored++<budget) {
    var item=queue.Dequeue(); var board=item.Item1;
    for(int a=0;a<board.Stands.Count;a++) for(int b=0;b<board.Stands.Count;b++) {
     int n=board.MovableCount(a,b); if(n==0) continue;
     if(board.Stands[b].fish.Count==0 && board.Stands[a].fish.Distinct().Count()==1) continue;
     var next=new PuzzleModel(board.Snapshot()); next.Move(a,b);next.ResolveCompletedGroups(); var first=item.Item2??new[]{a,b};
     if(next.Won) return first;
     if(visited.Add(next.Key())) queue.Enqueue(Tuple.Create(next,first));
    }
   }
   int[] best=null; int score=-999;
   for(int a=0;a<Stands.Count;a++) for(int b=0;b<Stands.Count;b++) {int n=MovableCount(a,b); if(n==0)continue; int v=n*3+(Stands[b].fish.Count>0?5:0)+(Stands[b].fish.Count+n==4?10:0)-(Stands[a].fish.Count==n?2:0); if(v>score){score=v;best=new[]{a,b};}}
   return best;
  }
 }
 [Serializable] public class SaveData {
  public int version=2, level=1, unlocked=1, theme=1, completed=0, moves, undo=2, add=0, skip=0;
  public float demoBalance=0;
  public EconomyState economy;
  public int sourceLevel;
  public bool music=true,sound=true;
  public BoardSnapshot board;
  public List<BoardSnapshot> history = new List<BoardSnapshot>();
  public List<int> cleared = new List<int>();
  public List<int> bonusClaimed = new List<int>();
  public long nextOnlineUtc;
  public int onlineQueueIndex;
 }
}
