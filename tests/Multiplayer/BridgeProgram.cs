using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using MPAPI;
using MPAPI.Interfaces;
using MPAPI.Interfaces.Packets;
using RearrangedS060;
using RearrangedS060.Multiplayer;
using UnityEngine;

static class Program
{
    static int checks,cases;
    static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    static byte[] Bytes(BoundedPacket packet){using var stream=new MemoryStream();using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))packet.Serialize(writer);return stream.ToArray();}
    static T Read<T>(byte[] bytes)where T:BoundedPacket,new(){var packet=new T();using var reader=new BinaryReader(new MemoryStream(bytes));packet.Deserialize(reader);return packet;}
    static T Wire<T>(T packet)where T:BoundedPacket,new()=>Read<T>(Bytes(packet));
    sealed class Player:IPlayer {public byte PlayerId{get;set;}public bool IsHost{get;set;}public bool IsLoaded{get;set;}=true;public string Username{get;set;}public string DisplayName=>Username;}
    sealed class Server:IServer
    {
        public readonly Dictionary<Type,Delegate> Handlers=new();
        public readonly List<IPlayer> Connected=new();
        public readonly List<(BoundedPacket packet,IPlayer player)> Sent=new();
        public readonly List<IPlayer> Kicked=new();
        public readonly List<(string message,IPlayer player)> Whispers=new();
        public event Action<IPlayer> OnPlayerReady,OnPlayerDisconnected,OnPlayerConnected;
        public IEnumerable<IPlayer> Players=>Connected;
        public void RegisterSerializablePacket<T>(Action<T,IPlayer> callback)where T:ISerializablePacket=>Handlers[typeof(T)]=callback;
        public void SendSerializablePacketToPlayer<T>(T packet,IPlayer player)where T:ISerializablePacket=>Sent.Add(((BoundedPacket)(object)packet,player));
        public IPlayer GetPlayer(byte id)=>Connected.FirstOrDefault(p=>p.PlayerId==id);
        public void KickPlayer(IPlayer player){Kicked.Add(player);Disconnect(player);}
        public void SendWhisperChatMessage(string message,IPlayer player)=>Whispers.Add((message,player));
        public void Receive<T>(T packet,IPlayer player)where T:BoundedPacket,new()=>((Action<T,IPlayer>)Handlers[typeof(T)])(Wire(packet),player);
        public void Ready(IPlayer player)=>OnPlayerReady?.Invoke(player);
        public void Connect(IPlayer player){Connected.Add(player);OnPlayerConnected?.Invoke(player);}
        public void Disconnect(IPlayer player){Connected.Remove(player);OnPlayerDisconnected?.Invoke(player);}
        public WelcomePacket Welcome(IPlayer player)=>Sent.Where(p=>p.player==player).Select(p=>p.packet).OfType<WelcomePacket>().Last();
        public int States(IPlayer player)=>Sent.Count(p=>p.player==player&&p.packet is StatePacket);
    }
    sealed class Client:IClient
    {
        public bool IsConnected{get;set;}=true;
        public readonly Dictionary<Type,Delegate> Handlers=new();
        public readonly List<BoundedPacket> Sent=new();
        public void RegisterSerializablePacket<T>(Action<T> callback)where T:ISerializablePacket=>Handlers[typeof(T)]=callback;
        public void SendSerializablePacketToServer<T>(T packet)where T:ISerializablePacket=>Sent.Add((BoundedPacket)(object)packet);
        public void Receive<T>(T packet)where T:BoundedPacket,new()=>((Action<T>)Handlers[typeof(T)])(Wire(packet));
    }
    static HelloPacket Hello()=>new(){Protocol=S060MultiplayerSync.Protocol,Identity=S060MultiplayerSync.BuildIdentity};
    static void Request(Server server,RequestPacket request,IPlayer player){Time.unscaledTime+=.6f;server.Receive(request,player);}
    static WelcomePacket Welcome(long revision=1,bool canChange=true)=>new(){Protocol=S060MultiplayerSync.Protocol,Identity=S060MultiplayerSync.BuildIdentity,Accepted=true,CanChange=canChange,PolicyRevision=revision,Message="fixture permission"};
    static void PacketTests()
    {
        cases++;
        var hello=Wire(new HelloPacket{Protocol=7,Identity="unicode-\u00e9-\u65e5"});
        Check(hello.Valid&&hello.Protocol==7&&hello.Identity=="unicode-\u00e9-\u65e5","Hello binary round trip");
        var welcome=Wire(new WelcomePacket{Protocol=1,Identity="same-build",Accepted=true,CanChange=false,PolicyRevision=long.MaxValue,Message="locked"});
        Check(welcome.Valid&&welcome.Protocol==1&&welcome.Identity=="same-build"&&welcome.Accepted&&!welcome.CanChange&&welcome.PolicyRevision==long.MaxValue&&welcome.Message=="locked","Welcome binary round trip");
        var state=Wire(new StatePacket{Json="{\"car\":\"A\"}"});Check(state.Valid&&state.Json=="{\"car\":\"A\"}","State binary round trip");
        var request=Wire(new RequestPacket{CarGuid="A-guid",Arrangement=47,ExpectedRevision=long.MaxValue,Epoch=long.MaxValue});
        Check(request.Valid&&request.CarGuid=="A-guid"&&request.Arrangement==47&&request.ExpectedRevision==long.MaxValue&&request.Epoch==long.MaxValue,"Request binary round trip retains revision and epoch");
        Check(Wire(new RequestResultPacket{Message="refused"}).Message=="refused","Result binary round trip");
        Check(Wire(new ApplyFailurePacket{Message="native geometry failed"}).Message=="native geometry failed","Replica-failure binary round trip");
        foreach(var packet in new BoundedPacket[]{Hello(),Welcome(),new StatePacket{Json="payload"},new RequestPacket{CarGuid="guid",Arrangement=47,ExpectedRevision=12},new RequestResultPacket{Message="result"},new ApplyFailurePacket{Message="failure"}})
        {
            cases++;var bytes=Bytes(packet);
            for(int length=0;length<bytes.Length;length++)
            {
                var truncated=(BoundedPacket)Activator.CreateInstance(packet.GetType());
                using var reader=new BinaryReader(new MemoryStream(bytes.Take(length).ToArray()));truncated.Deserialize(reader);
                Check(!truncated.Valid,packet.GetType().Name+" rejects every truncated prefix at "+length);
            }
        }
        cases++;
        Check(Wire(new HelloPacket{Identity=new string('a',512)}).Valid,"Hello exact byte limit");
        Check(!Wire(new HelloPacket{Identity=new string('a',513)}).Valid,"Hello oversize identity refused");
        Check(!Wire(new HelloPacket{Identity=new string('\u00e9',257)}).Valid,"Hello caps UTF8 bytes, not characters");
        Check(Wire(new StatePacket{Json=new string('a',65536)}).Valid,"State exact byte limit");
        Check(!Wire(new StatePacket{Json=new string('a',65537)}).Valid,"State oversize JSON refused");
        Check(!Wire(new RequestPacket{CarGuid=new string('a',129)}).Valid,"Request oversize GUID refused");
        Check(!Wire(new WelcomePacket{Identity="x",Message=new string('a',2049)}).Valid,"Welcome oversize message refused");
        Check(!Wire(new RequestResultPacket{Message=new string('a',2049)}).Valid,"Result oversize message refused");
        Check(!Wire(new ApplyFailurePacket{Message=new string('a',2049)}).Valid,"Replica-failure oversize message refused");
        foreach(var bytes in new[]{new byte[]{0xff,0xff,0xff,0xff,0xff},new byte[]{0xff,0xff,0xff,0xff,0x0f},new byte[]{0x80},new byte[]{2,0xc0,0xaf},new byte[]{3,0x61}})
            Check(!Read<StatePacket>(bytes).Valid,"Malformed size/UTF8/truncated payload refused without throwing");
        var reused=Wire(new StatePacket{Json="valid"});using(var reader=new BinaryReader(new MemoryStream(new byte[]{2,1})))reused.Deserialize(reader);
        Check(!reused.Valid,"Failed second Deserialize cannot retain Valid flag");
    }
    static void SinglePlayerTests()
    {
        cases++;Bridge.Init();int hosts=S060MultiplayerSync.Hosts,clients=S060MultiplayerSync.Clients;
        MultiplayerAPI.Instance.IsSinglePlayer=true;
        var server=new Server();MultiplayerAPI.StartServer(server);
        var loopback=new Client();MultiplayerAPI.StartClient(loopback);Bridge.Tick();
        Check(S060MultiplayerSync.Hosts==hosts&&S060MultiplayerSync.Clients==clients,"Native internal single-player server and loopback do not enter multiplayer roles");
        Check(S060MultiplayerSync.DrawNetworkGUI==null&&S060MultiplayerSync.BroadcastState==null&&S060MultiplayerSync.SendRequest==null,"Internal single-player mode installs no host controls or packet delegates");
        Check(server.Handlers.Count==0&&loopback.Handlers.Count==0&&loopback.Sent.Count==0,"Single-player internal network has no S282 packets or handshake");
        MultiplayerAPI.StopClient();MultiplayerAPI.StopServer();MultiplayerAPI.Instance.IsSinglePlayer=false;
    }
    static void StartupRetryTests()
    {
        cases++;
        MultiplayerAPI.Instance.FailCompatibility=true;
        bool threw=false;try{Bridge.Init();}catch(InvalidOperationException){threw=true;}
        Check(threw,"API registration failure is surfaced to the optional loader");
        GameObject.FailComponent=true;threw=false;try{Bridge.Init();}catch(InvalidOperationException){threw=true;}
        Check(threw,"Failed API initialization can retry rather than returning false success");
        Check(UnityEngine.Object.Destroyed==1,"Failed runner initialization cleans up its own persistent GameObject");
        Check(MultiplayerAPI.LifecycleSubscribers==0,"Failed startup detaches every native lifecycle callback before retry");
        Bridge.Init();
        Check(GameObject.Components==1,"Retry after runner failure creates one working runner");
        Check(MultiplayerAPI.LifecycleSubscribers==4,"Successful retry installs one copy of each native lifecycle callback");
    }
    static void HostTests()
    {
        cases++;int previousCalls=MultiplayerAPI.Instance.CompatibilityCalls;Bridge.Init();Bridge.Init();Check(MultiplayerAPI.Instance.CompatibilityCalls==previousCalls,"Successful bridge initialization is idempotent after failed startup retries");
        var server=new Server();var a=new Player{PlayerId=1,Username="Alice"};var b=new Player{PlayerId=2,Username="Bob"};server.Connected.AddRange(new[]{a,b});
        MultiplayerAPI.StartServer(server);Check(S060MultiplayerSync.Hosts==1&&S060MultiplayerSync.DrawNetworkGUI!=null,"Server starts host authority and host UI");
        var request=new RequestPacket{CarGuid="car",Arrangement=19,ExpectedRevision=4,Epoch=S060MultiplayerSync.SessionEpoch};int requests=S060MultiplayerSync.HostRequests;
        Request(server,request,a);Check(S060MultiplayerSync.HostRequests==requests&&server.Sent.Last().packet is RequestResultPacket,"Unverified guest cannot call host mutation");
        server.Ready(a);Check(server.States(a)==0,"Ready event alone does not bypass handshake");
        var badProtocol=new Player{PlayerId=3,Username="WrongProtocol"};var badBuild=new Player{PlayerId=4,Username="WrongBuild"};server.Connect(badProtocol);server.Connect(badBuild);
        server.Receive(new HelloPacket{Protocol=99,Identity=S060MultiplayerSync.BuildIdentity},badProtocol);Check(!server.Welcome(badProtocol).Accepted&&server.States(badProtocol)==0,"Protocol mismatch gets no state snapshot");
        server.Receive(new HelloPacket{Protocol=S060MultiplayerSync.Protocol,Identity="other-build"},badBuild);Check(!server.Welcome(badBuild).Accepted,"Build mismatch refused");
        Time.unscaledTime+=1.1f;Bridge.Tick();Check(server.Kicked.Contains(badProtocol)&&server.Kicked.Contains(badBuild),"Mismatched packages are disconnected through native kick API after message grace");
        server.Receive(Hello(),a);server.Receive(Hello(),b);Check(server.Welcome(a).Accepted&&server.Welcome(a).CanChange&&server.States(a)==2,"Verified guest gets permission and full snapshot");
        Request(server,request,a);Check(S060MultiplayerSync.HostRequests==requests+1,"Verified permitted guest reaches core mutation validator");
        cases++;GUILayout.Values["Alice may change arrangements"]=false;S060MultiplayerSync.DrawNetworkGUI();GUILayout.Values.Clear();
        Check(!server.Welcome(a).CanChange&&server.Welcome(b).CanChange,"Per-player lock updates only selected permission");
        int states=server.States(a);Request(server,request,a);Check(S060MultiplayerSync.HostRequests==requests+1&&server.States(a)==states,"Locked request refused without a full snapshot replay");
        S060MultiplayerSync.BroadcastState("latest");Check(server.Sent.Count(p=>p.packet is StatePacket s&&s.Json=="latest")==2,"Locked guest still receives accepted state broadcasts");
        Request(server,request,b);Check(S060MultiplayerSync.HostRequests==requests+2,"Other guest retains radio access");
        GUILayout.Values["Allow guests to change arrangements"]=false;S060MultiplayerSync.DrawNetworkGUI();GUILayout.Values.Clear();
        Check(!server.Welcome(a).CanChange&&!server.Welcome(b).CanChange,"Global guest lock reaches all verified guests");
        Request(server,request,b);Check(S060MultiplayerSync.HostRequests==requests+2,"Global guest lock enforced at host boundary");
        GUILayout.Values["Allow guests to change arrangements"]=true;S060MultiplayerSync.DrawNetworkGUI();GUILayout.Values.Clear();
        Check(!server.Welcome(a).CanChange&&server.Welcome(b).CanChange,"Unlock global control preserves individual lock");
        cases++;server.Disconnect(a);var reused=new Player{PlayerId=1,Username="Carol"};server.Connected.Add(reused);server.Ready(reused);
        Check(server.States(reused)==0,"Recycled player ID requires a fresh handshake");
        server.Receive(Hello(),reused);Check(server.Welcome(reused).CanChange,"Recycled player ID does not inherit departed player's lock");
        server.Receive(new HelloPacket{Protocol=99,Identity="wrong"},b);int sent=server.States(b);S060MultiplayerSync.BroadcastState("post-reject");Check(server.States(b)==sent,"Newly rejected build loses broadcast membership");
        cases++;var silent=new Player{PlayerId=7,Username="Silent"};server.Connect(silent);server.Ready(silent);float readyAt=Time.unscaledTime;Time.unscaledTime=readyAt+44;Bridge.Tick();Check(!server.Kicked.Contains(silent),"Missing handshake has full 45 second ready grace");
        Time.unscaledTime=readyAt+45.1f;Bridge.Tick();Check(server.Kicked.Contains(silent),"Ready same-ID client without a handshake is disconnected after 45 seconds");
        var departed=new Player{PlayerId=8,Username="Departed"};server.Connect(departed);server.Ready(departed);server.Disconnect(departed);var fresh=new Player{PlayerId=8,Username="Fresh"};server.Connect(fresh);server.Receive(Hello(),fresh);Time.unscaledTime=100;Bridge.Tick();Check(!server.Kicked.Contains(fresh),"Departed player's pending timeout cannot kick recycled compatible player ID");
        cases++;server.Receive(new ApplyFailurePacket{Message="native apply failed"},fresh);requests=S060MultiplayerSync.HostRequests;Request(server,request,fresh);Check(S060MultiplayerSync.HostRequests==requests,"Guest reporting failed native replica immediately loses mutation access");Time.unscaledTime+=1.1f;Bridge.Tick();Check(server.Kicked.Contains(fresh),"Failed native replica disconnects through native server kick API");
        int ended=S060MultiplayerSync.Ended;MultiplayerAPI.StopServer();Check(S060MultiplayerSync.DrawNetworkGUI==null&&S060MultiplayerSync.BroadcastState==null&&S060MultiplayerSync.Ended==ended+1,"Server stop releases callbacks and core session");
    }
    static void ClientTests()
    {
        cases++;var client=new Client();MultiplayerAPI.StartClient(client);Check(S060MultiplayerSync.Clients==1&&S060MultiplayerSync.SendRequest!=null,"Client starts locked handshake boundary");
        S060MultiplayerSync.SendRequest("car",19,3);Check(client.Sent.Count==0,"Client cannot send mutation before acceptance");
        Time.unscaledTime=0;Bridge.Tick();Bridge.Tick();Check(client.Sent.OfType<HelloPacket>().Count()==1,"Hello sends immediately without duplicate tick spam");
        Time.unscaledTime=2;Bridge.Tick();Check(client.Sent.OfType<HelloPacket>().Count()==2,"Unanswered handshake retries");
        int received=S060MultiplayerSync.Received.Count;client.Receive(new StatePacket{Json="early"});Check(S060MultiplayerSync.Received.Count==received,"Unordered state buffered before matching welcome");
        client.Receive(Welcome());Check(S060MultiplayerSync.CanChange&&S060MultiplayerSync.Received.Last()=="early","Matching welcome unlocks permission and drains early snapshot");
        S060MultiplayerSync.SendRequest("car",47,91);var sent=client.Sent.OfType<RequestPacket>().Last();Check(sent.CarGuid=="car"&&sent.Arrangement==47&&sent.ExpectedRevision==91&&sent.Epoch==S060MultiplayerSync.SessionEpoch,"Client forwards expected state revision and current epoch");
        client.Receive(Welcome(3,false));client.Receive(Welcome(2,true));Check(!S060MultiplayerSync.CanChange,"Stale permission revision cannot undo host lock");
        client.Receive(new RequestResultPacket{Message="useful refusal"});Check(S060MultiplayerSync.Status=="useful refusal","Host request result reaches client status");
        int rejected=S060MultiplayerSync.Rejects,accepted=S060MultiplayerSync.Accepts;S060MultiplayerSync.ReportFailure(new string('x',3000));
        Check(S060MultiplayerSync.Rejects==rejected+1&&client.Sent.OfType<ApplyFailurePacket>().Last().Message.Length<=503,"Replica failure locks client and sends bounded host notification");
        client.Receive(Welcome(4,true));Check(S060MultiplayerSync.Accepts==accepted&&!S060MultiplayerSync.CanChange,"Delayed welcome cannot unlock failed replica before reconnect");
        int ended=S060MultiplayerSync.Ended;MultiplayerAPI.StopClient();Check(S060MultiplayerSync.SendRequest==null&&S060MultiplayerSync.Ended==ended+1,"Client stop releases request callback and session");
        cases++;client=new Client();MultiplayerAPI.StartClient(client);received=S060MultiplayerSync.Received.Count;
        rejected=S060MultiplayerSync.Rejects;client.Receive(new StatePacket{Json="untrusted-early"});var wrong=Welcome();wrong.Identity="wrong-build";client.Receive(wrong);
        client.Receive(new StatePacket{Json="untrusted-late"});S060MultiplayerSync.SendRequest("car",19,3);Check(S060MultiplayerSync.Rejects==rejected+1&&S060MultiplayerSync.Received.Count==received&&client.Sent.Count==0,"Mismatched host clears queued state and blocks later state/requests");
        MultiplayerAPI.StopClient();
        cases++;client=new Client();MultiplayerAPI.StartClient(client);received=S060MultiplayerSync.Received.Count;
        for(int n=0;n<1025;n++)client.Receive(new StatePacket{Json="early-"+n});
        client.Receive(Welcome());Check(client.Sent.OfType<HelloPacket>().Count()==1,"Early snapshot overflow explicitly requests fresh snapshot after welcome");
        client.Receive(new StatePacket{Json="fresh-snapshot"});Check(S060MultiplayerSync.Received.Last()=="fresh-snapshot","Client accepts replacement snapshot after bounded overflow");
        MultiplayerAPI.StopClient();
        cases++;client=new Client();MultiplayerAPI.StartClient(client);
        for(int n=0;n<1025;n++)client.Receive(new StatePacket{Json="early-"+n});
        client.Receive(new StatePacket{Json="invalid-after-overflow"});
        S060MultiplayerSync.OnReceiveState=json=>S060MultiplayerSync.ReportFailure("fixture invalid host state");
        client.Receive(Welcome());S060MultiplayerSync.OnReceiveState=null;
        Check(client.Sent.OfType<ApplyFailurePacket>().Count()==1&&!client.Sent.OfType<HelloPacket>().Any(),"Failed early replica reports once without retrying an overflow resnapshot");
        MultiplayerAPI.StopClient();
    }
    static void TrafficAndSessionTests()
    {
        cases++;Time.unscaledTime=200;var server=new Server();MultiplayerAPI.StartServer(server);
        var player=new Player{PlayerId=10,Username="Eve"};
        server.Receive(Hello(),player);Check(server.Sent.Count==0,"Premature hello before native connected callback is ignored for safe retry");
        server.Connect(player);server.Receive(Hello(),player);Check(server.Welcome(player).Accepted,"Hello after native connected callback accepts normally");
        var request=new RequestPacket{CarGuid="car",Arrangement=19,ExpectedRevision=4,Epoch=S060MultiplayerSync.SessionEpoch};
        int requests=S060MultiplayerSync.HostRequests,states=server.States(player),results=server.Sent.Count;
        server.Receive(request,player);for(int n=0;n<1000;n++)server.Receive(request,player);
        Check(S060MultiplayerSync.HostRequests==requests+1,"Burst of 1001 requests reaches core only once within half-second window");
        Check(server.Sent.Count<=results+2&&server.States(player)==states,"Rate-limit notices are bounded and request spam sends no state snapshots");
        cases++;Time.unscaledTime+=.6f;GUILayout.Values["Eve may change arrangements"]=false;S060MultiplayerSync.DrawNetworkGUI();GUILayout.Values.Clear();
        server.Receive(request,player);Check(S060MultiplayerSync.HostRequests==requests+1&&server.States(player)==states,"Permission denial never amplifies into a full snapshot");
        GUILayout.Values["Eve may change arrangements"]=true;S060MultiplayerSync.DrawNetworkGUI();GUILayout.Values.Clear();
        Time.unscaledTime+=.6f;S060MultiplayerSync.HostRequestResult=false;
        var stale=new RequestPacket{CarGuid="car",Arrangement=19,ExpectedRevision=3,Epoch=S060MultiplayerSync.SessionEpoch};
        int targeted=S060MultiplayerSync.TargetedSnapshots.Count;server.Receive(stale,player);
        Check(S060MultiplayerSync.TargetedSnapshots.Count==targeted+1&&server.States(player)==states+1,"Stale revision sends only the requested car's current state");
        Time.unscaledTime+=.6f;server.Receive(stale,player);Check(server.States(player)==states+1,"Repeated stale request cannot amplify targeted recovery traffic");
        cases++;Time.unscaledTime+=5f;states=server.States(player);requests=S060MultiplayerSync.HostRequests;
        var oldEpoch=new RequestPacket{CarGuid="car",Arrangement=19,ExpectedRevision=4,Epoch=S060MultiplayerSync.SessionEpoch-1};
        server.Receive(oldEpoch,player);Check(S060MultiplayerSync.HostRequests==requests&&server.States(player)==states+2,"Old epoch never mutates and gets one bounded current snapshot");
        Time.unscaledTime+=.6f;server.Receive(oldEpoch,player);Check(server.States(player)==states+2,"Old-epoch recovery cannot replay all cars repeatedly");
        states=server.States(player);for(int n=0;n<1000;n++)server.Receive(Hello(),player);
        Check(server.States(player)==states,"Repeated matching hellos cannot serialize snapshots immediately");
        Time.unscaledTime+=2f;Bridge.Tick();Check(server.States(player)==states,"Pending overflow resnapshot obeys full-snapshot interval");
        Time.unscaledTime+=3f;Bridge.Tick();Check(server.States(player)==states+2,"Throttled hello recovery is queued rather than lost");
        S060MultiplayerSync.HostRequestResult=true;
        cases++;var wrong=new Player{PlayerId=11,Username="Wrong"};server.Connect(wrong);
        server.Receive(new HelloPacket{Protocol=99,Identity="wrong"},wrong);float mismatchAt=Time.unscaledTime;
        for(int n=0;n<50;n++){Time.unscaledTime=mismatchAt+.9f;server.Receive(new HelloPacket{Protocol=99,Identity="wrong"},wrong);}
        Time.unscaledTime=mismatchAt+1.2f;Bridge.Tick();Check(server.Kicked.Contains(wrong),"Repeated wrong-build hellos cannot postpone a scheduled disconnect");
        cases++;var canonical=new Player{PlayerId=12,Username="FreshWrapper"};server.Connect(canonical);
        var packetWrapper=new Player{PlayerId=12,Username="FreshWrapper"};
        server.Receive(new HelloPacket{Protocol=99,Identity="wrong"},packetWrapper);Time.unscaledTime+=1.1f;Bridge.Tick();
        Check(server.Kicked.Contains(canonical)&&!server.Kicked.Contains(packetWrapper),"Delayed kick uses native cached player identity when packet callback has a fresh wrapper");
        cases++;var second=new Server();MultiplayerAPI.StartServer(second);requests=S060MultiplayerSync.HostRequests;
        server.Receive(request,player);server.Receive(Hello(),player);
        Check(S060MultiplayerSync.HostRequests==requests&&second.Sent.Count==0,"Old server packet handlers cannot feed a replacement session");
        var current=new Player{PlayerId=10,Username="NewEve"};second.Connect(current);second.Receive(Hello(),current);
        Check(second.Welcome(current).CanChange,"Replacement server does not inherit old permissions or request limits");
        MultiplayerAPI.Instance.IsSinglePlayer=true;MultiplayerAPI.StartServer(new Server());
        Check(S060MultiplayerSync.DrawNetworkGUI==null&&S060MultiplayerSync.BroadcastState==null,"Transition back to native single player removes host callbacks");
        MultiplayerAPI.StopServer();MultiplayerAPI.Instance.IsSinglePlayer=false;
        cases++;var oldClient=new Client();MultiplayerAPI.StartClient(oldClient);var newClient=new Client();MultiplayerAPI.StartClient(newClient);
        int accepts=S060MultiplayerSync.Accepts,rejects=S060MultiplayerSync.Rejects,received=S060MultiplayerSync.Received.Count;
        oldClient.Receive(Welcome(999));oldClient.Receive(new StatePacket{Json="old-session"});
        Check(S060MultiplayerSync.Accepts==accepts&&S060MultiplayerSync.Received.Count==received,"Old client welcome/state cannot initialize a replacement connection");
        newClient.Receive(Welcome());oldClient.Receive(new WelcomePacket{Protocol=99,Identity="wrong",PolicyRevision=1000});
        newClient.Receive(new StatePacket{Json="new-session"});
        Check(S060MultiplayerSync.Rejects==rejects&&S060MultiplayerSync.Received.Last()=="new-session","Old client rejection cannot break current accepted session");
        MultiplayerAPI.StopClient();
    }
    static int Main(string[] args)
    {
        PacketTests();StartupRetryTests();SinglePlayerTests();HostTests();ClientTests();TrafficAndSessionTests();
        var report=new{passed=true,checks,cases,compiledProduction=new[]{"Bridge.cs","Packets.cs"},limitations="Actual bridge and packet source compiled unchanged against managed MPAPI, Unity and core-boundary doubles. This validates codec and adapter policy, not the real transport, native player identity, live simulation or Unity rendering."};
        File.WriteAllText(args[0],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"Bridge/packet validation passed {checks} checks across {cases} cases.");return 0;
    }
}
