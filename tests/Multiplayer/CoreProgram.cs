using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using RearrangedS060;
using UnityEngine;
using UnityModManagerNet;
static class Program
{
    static int checks;
    static void Check(bool v,string label){checks++;if(!v)throw new Exception(label);}
    static TrainCar Car(string guid=null)
    {
        var go=new GameObject();go.transform.MeshReady=true;
        var car=go.AddComponent<TrainCar>();car.CarGUID=guid??Guid.NewGuid().ToString();car.ID="S060";
        go.AddComponent<LocoMeshSplitter.MeshLoaders.S060MeshLoader>();go.AddComponent<S060Lifecycle>();CarSpawner.Instance.AllCars.Add(car);return car;
    }
    static void Tick(){Time.unscaledTime+=1;S060MultiplayerSync.Tick();}
    static string State(TrainCar car,int id,long revision,long epoch,bool removed=false)=>JsonConvert.SerializeObject(new NetworkArrangement{CarGuid=car.CarGUID,Arrangement=id,Revision=revision,Epoch=epoch,Removed=removed});
    static void Reset(){S060MultiplayerSync.EndSession();CarSpawner.Instance=new CarSpawner();S060Conversion.Active.Clear();UnityModManager.CurrentMod=null;Time.unscaledTime+=1;}
    static void Main(string[] args)
    {
        Reset();int reads=CarSpawner.InstanceReads;
        S060MultiplayerSync.Initialize();Tick();Check(!S060MultiplayerSync.InSession&&S060MultiplayerSync.IsSimulationAuthority,"Absent MP preserves offline authority");
        Check(CarSpawner.InstanceReads==reads,"Main-menu optional startup does not query world singleton");
        S060MultiplayerSync.Draw();Check(GUILayout.Labels.Count==0,"Offline UI has no multiplayer status");
        UnityModManager.CurrentMod=new UnityModManager.ModEntry{Active=false};Tick();Check(!S060MultiplayerSync.InSession,"Disabled MP stays offline");
        var car=Car();var life=car.GetComponent<S060Lifecycle>();
        for(int id=0;id<=8;id++) {var choice=S060NetworkSelection.Decode(id);Check(S060NetworkSelection.Encode(choice)==id,"All nine wire choices round-trip "+id);}
        Check(S060NetworkSelection.Decode(0).IsStock&&!S060NetworkSelection.Decode(8).IsStock,"Stock and short wheelbase remain distinct");
        Check(!S060NetworkSelection.IsSupported(-1)&&!S060NetworkSelection.IsSupported(9),"Unsupported wire IDs refused");
        Check(S060MultiplayerSync.RequestChange(car,S060NetworkSelection.Decode(8),out _)&&life.Attempts==1,"Offline manual selection uses native lifecycle");
        S060MultiplayerSync.BeginClient();int sent=0;S060MultiplayerSync.SendRequest=(g,i,r)=>{Check(g==car.CarGUID&&i==7,"Guest request contains S060 GUID and Shay ID");sent++;};
        Check(!S060MultiplayerSync.IsSimulationAuthority&&S060MultiplayerSync.DeferAutomatic(car),"Guest has no simulation/random authority");
        Check(!S060MultiplayerSync.BeforeSwitch(car),"Guest direct local mutation blocked");
        Check(!S060MultiplayerSync.RequestChange(car,S060NetworkSelection.Decode(7),out _),"Unverified guest request blocked");
        S060MultiplayerSync.AcceptClient(false,"locked");Check(!S060MultiplayerSync.RequestChange(car,S060NetworkSelection.Decode(7),out _),"Locked guest request blocked");
        S060MultiplayerSync.AcceptClient(true,"allowed");Check(S060MultiplayerSync.RequestChange(car,S060NetworkSelection.Decode(7),out _)&&sent==1&&life.Attempts==1,"Allowed guest sends without local mutation");
        S060MultiplayerSync.RejectClient("mismatch");Check(!S060MultiplayerSync.RequestChange(car,S060Selection.Stock,out _)&&S060MultiplayerSync.IsClient,"Mismatch fails closed");
        Reset();car=Car();life=car.GetComponent<S060Lifecycle>();S060MultiplayerSync.BeginHost();var packets=new List<string>();S060MultiplayerSync.BroadcastState=packets.Add;
        Check(S060MultiplayerSync.TryHostRequest(car.CarGUID,8,0,out _),"Host accepts short wheelbase");
        Check(S060MultiplayerSync.RevisionFor(car.CarGUID)==1&&packets.Count==1,"Host publishes one revision");
        S060MultiplayerSync.OnSwitchFailed(car);Check(S060MultiplayerSync.RevisionFor(car.CarGUID)==1&&packets.Count==1,"Refused unchanged selection does not broadcast a spurious conversion");
        Check(!S060MultiplayerSync.TryHostRequest(car.CarGUID,4,0,out _)&&life.Attempts==1,"Stale concurrent request rejected before mutation");
        Check(!S060MultiplayerSync.TryHostRequest("not-a-guid",4,0,out _)&&!S060MultiplayerSync.TryHostRequest(car.CarGUID,9,1,out _),"Invalid IDs rejected");
        car.Moving=true;Check(!S060MultiplayerSync.TryHostRequest(car.CarGUID,4,1,out _)&&S060MultiplayerSync.RevisionFor(car.CarGUID)==1,"Host operating-condition refusal changes no state");car.Moving=false;
        Check(S060MultiplayerSync.TryHostRequest(car.CarGUID,0,1,out _)&&S060Conversion.Current(car).Selection.IsStock,"Stock request is not short wheelbase");
        var pending=Car();pending.GetComponent<S060Lifecycle>().HasPending=true;Tick();Check(S060MultiplayerSync.RevisionFor(pending.CarGUID)==0,"Pending saved/random selection not published as stock");
        pending.GetComponent<S060Lifecycle>().HasPending=false;Tick();Check(S060MultiplayerSync.RevisionFor(pending.CarGUID)==1,"Ready stock included in late-join snapshot");
        long epoch=S060MultiplayerSync.SessionEpoch;string snapshot=State(car,4,3,epoch);
        CarSpawner.Instance.Delete(car);Check(JsonConvert.DeserializeObject<NetworkArrangement>(S060MultiplayerSync.SnapshotFor(car.CarGUID).Single()).Removed,"Native deletion sends tombstone");
        Reset();car=Car(JsonConvert.DeserializeObject<NetworkArrangement>(snapshot).CarGuid);life=car.GetComponent<S060Lifecycle>();car.Moving=true;car.BogiesReady=false;
        S060MultiplayerSync.BeginClient();S060MultiplayerSync.AcceptClient(true,"allowed");S060MultiplayerSync.ReceiveState(snapshot);Tick();Check(life.Attempts==0,"Late join waits for native rig");
        S060MultiplayerSync.ReceiveState(State(car,7,2,epoch));Check(S060MultiplayerSync.RevisionFor(car.CarGUID)==3,"Out-of-order older state ignored");
        S060MultiplayerSync.ReceiveState(JsonConvert.SerializeObject(new NetworkArrangement{Epoch=epoch,Reset=true}));Check(S060MultiplayerSync.RevisionFor(car.CarGUID)==3,"Late reset cannot erase same-epoch data");
        car.BogiesReady=true;Tick();Check(life.Attempts==1&&life.SawRemote&&life.Restoring&&S060Conversion.Current(car).Selection.Arrangement==S060Arrangement.Meyer,"Moving late join applies host Meyer in remote scope");
        Tick();Check(life.Attempts==1&&!S060MultiplayerSync.ApplyingRemote,"Same revision is idempotent and remote scope cleared");
        S060MultiplayerSync.ReceiveState(State(car,7,4,epoch));Tick();Check(life.Attempts==2&&!life.Restoring&&S060Conversion.Current(car).Selection.Arrangement==S060Arrangement.Shay,"Later revision replaces an existing layout");
        S060MultiplayerSync.ReceiveState(State(car,4,5,epoch,true));S060MultiplayerSync.ReceiveState(State(car,4,4,epoch));Tick();Check(life.Attempts==2,"Deletion tombstone rejects delayed resurrection");
        S060MultiplayerSync.ReceiveState(State(car,8,1,epoch+1));Tick();Check(life.Attempts==3&&S060MultiplayerSync.RevisionFor(car.CarGUID)==1,"New epoch replaces old revision space");
        S060MultiplayerSync.ReceiveState(State(car,7,100,epoch));Tick();Check(life.Attempts==3,"Old epoch ignored even with larger revision");
        car.isExploded=true;S060MultiplayerSync.ReceiveState(State(car,0,2,epoch+1));for(int i=0;i<130;i++)Tick();Check(life.Attempts==3&&S060MultiplayerSync.IsClient,"Wreck waits without readiness timeout");car.isExploded=false;Tick();Check(life.Attempts==4&&S060Conversion.Current(car).Selection.IsStock,"Repaired wreck receives latest host layout");
        var reused=Car(car.CarGUID);CarSpawner.Instance.AllCars.Remove(car);Tick();Check(reused.GetComponent<S060Lifecycle>().Attempts==1,"Same GUID on a new native instance reapplies");
        var reports=new List<string>();S060MultiplayerSync.ReportFailure=reports.Add;
        S060MultiplayerSync.ReceiveState("{\"Epoch\":1,\"UnknownField\":2}");Check(reports.Count==1&&!S060MultiplayerSync.IsSimulationAuthority&&!S060MultiplayerSync.BeforeSwitch(reused),"Malformed host payload reports and locks session");
        Reset();car=Car();car.BogiesReady=false;S060MultiplayerSync.BeginClient();S060MultiplayerSync.AcceptClient(true,"ok");S060MultiplayerSync.ReportFailure=reports.Add;S060MultiplayerSync.ReceiveState(State(car,5,1,1));for(int i=0;i<122;i++)Tick();Check(reports.Count==2,"Missing native readiness times out with report");
        Reset();S060MultiplayerSync.BeginHost();long firstEpoch=S060MultiplayerSync.SessionEpoch;
        for(int i=0;i<S060MultiplayerSync.MaxRetainedTombstones;i++){car=Car();S060MultiplayerSync.OnApplied(car,S060Selection.Stock);CarSpawner.Instance.Delete(car);}
        Check(S060MultiplayerSync.SessionEpoch>firstEpoch&&S060MultiplayerSync.Snapshot().Count()==1,"Deletion compaction advances epoch and bounds tombstones");
        S060MultiplayerSync.EndSession();Check(S060MultiplayerSync.IsSimulationAuthority&&!S060MultiplayerSync.InSession,"Disconnect restores offline policy");
        Directory.CreateDirectory(Path.GetDirectoryName(args[0]));File.WriteAllText(args[0],JsonConvert.SerializeObject(new{passed=true,checks,limits="Production sync and selection code; managed Unity/UMM and native conversion boundary doubles. No live peers or transport."},Formatting.Indented));Console.WriteLine($"S060 core: {checks} checks passed");
    }
}
