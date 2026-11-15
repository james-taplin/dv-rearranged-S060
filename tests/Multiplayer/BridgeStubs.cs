// External boundaries only. Bridge.cs and Packets.cs are compiled unchanged.
using System;
using System.Collections.Generic;
using System.Linq;

namespace MPAPI.Interfaces.Packets
{
    public interface ISerializablePacket { void Serialize(System.IO.BinaryWriter writer); void Deserialize(System.IO.BinaryReader reader); }
}
namespace MPAPI.Types { public enum MultiplayerCompatibility { All } }
namespace MPAPI.Interfaces
{
    using MPAPI.Interfaces.Packets;
    public interface IPlayer { byte PlayerId { get; } bool IsHost { get; } bool IsLoaded{get;} string Username { get; } string DisplayName { get; } }
    public interface IServer
    {
        event Action<IPlayer> OnPlayerReady;
        event Action<IPlayer> OnPlayerDisconnected;
        event Action<IPlayer> OnPlayerConnected;
        IEnumerable<IPlayer> Players { get; }
        void RegisterSerializablePacket<T>(Action<T,IPlayer> callback) where T:ISerializablePacket;
        void SendSerializablePacketToPlayer<T>(T packet,IPlayer player) where T:ISerializablePacket;
        IPlayer GetPlayer(byte id);
        void KickPlayer(IPlayer player);
        void SendWhisperChatMessage(string message,IPlayer player);
    }
    public interface IClient
    {
        bool IsConnected { get; }
        void RegisterSerializablePacket<T>(Action<T> callback) where T:ISerializablePacket;
        void SendSerializablePacketToServer<T>(T packet) where T:ISerializablePacket;
    }
}
namespace MPAPI
{
    using MPAPI.Interfaces;
    public sealed class MultiplayerAPI
    {
        public static bool IsMultiplayerLoaded=true;
        public static MultiplayerAPI Instance=new();
        public static IServer Server;
        public static IClient Client;
        public static string MultiplayerVersion="fixture",LoadedApiVersion="fixture";
        public bool IsHost;
        public bool IsSinglePlayer;
        public int CompatibilityCalls;
        public bool FailCompatibility;
        public void SetModCompatibility(string id,MPAPI.Types.MultiplayerCompatibility value){CompatibilityCalls++;if(FailCompatibility){FailCompatibility=false;throw new InvalidOperationException("Injected API registration failure");}}
        public static event Action<IServer> ServerStarted;
        public static event Action<IClient> ClientStarted;
        public static event Action ServerStopped,ClientStopped;
        public static int LifecycleSubscribers => (ServerStarted?.GetInvocationList().Length??0)+(ClientStarted?.GetInvocationList().Length??0)+(ServerStopped?.GetInvocationList().Length??0)+(ClientStopped?.GetInvocationList().Length??0);
        public static void StartServer(IServer server){Server=server;Instance.IsHost=true;ServerStarted?.Invoke(server);}
        public static void StopServer(){Server=null;Instance.IsHost=false;ServerStopped?.Invoke();}
        public static void StartClient(IClient client){Client=client;Instance.IsHost=false;ClientStarted?.Invoke(client);}
        public static void StopClient(){Client=null;ClientStopped?.Invoke();}
    }
}
namespace UnityEngine
{
    public class Object { public static int Destroyed;public static void DontDestroyOnLoad(Object value){} public static void Destroy(Object value){Destroyed++;} }
    public class MonoBehaviour:Object { }
    public sealed class GameObject:Object { public static bool FailComponent;public static int Components;public GameObject(string name){} public T AddComponent<T>()where T:new(){if(FailComponent){FailComponent=false;throw new InvalidOperationException("Injected runner creation failure");}Components++;return new();} }
    public static class Time { public static float unscaledTime; }
    public static class GUILayout
    {
        public static readonly Dictionary<string,bool> Values=new();
        public static readonly List<string> Labels=new();
        public static void Label(string value)=>Labels.Add(value);
        public static bool Toggle(bool value,string label)=>Values.TryGetValue(label,out bool next)?next:value;
    }
}
namespace RearrangedS060
{
    public static class Main
    {
        public static readonly ModLogger Logger=new();
        public sealed class ModLogger { public readonly List<string> Rows=new();public void Log(string value)=>Rows.Add(value);public void Error(string value)=>Rows.Add(value);public void LogException(string value,Exception error)=>Rows.Add(value+error); }
    }
    public static class S060MultiplayerSync
    {
        public const int Protocol=2;
        public static long SessionEpoch=100;
        public static string BuildIdentity="core-build-and-assets";
        public static string Status;
        public static Action<string> BroadcastState;
        public static Action<string,int,long> SendRequest;
        public static Action DrawNetworkGUI;
        public static Action<string> ReportFailure;
        public static int Hosts,Clients,Ended,Accepts,Rejects,HostRequests;
        public static bool CanChange,HostRequestResult=true;
        public static readonly List<string> Received=new();
        public static readonly List<string> States=new(){"snapshot-A","snapshot-B"};
        public static readonly List<string> TargetedSnapshots=new();
        public static Action<string> OnReceiveState;
        public static void BeginHost()=>Hosts++;
        public static void BeginClient()=>Clients++;
        public static void EndSession()=>Ended++;
        public static void AcceptClient(bool canChange,string message){CanChange=canChange;Status=message;Accepts++;}
        public static void RejectClient(string message){CanChange=false;Status=message;Rejects++;}
        public static IEnumerable<string> Snapshot()=>States;
        public static IEnumerable<string> SnapshotFor(string guid){TargetedSnapshots.Add(guid);return new[]{"only-"+guid};}
        public static long RevisionFor(string guid)=>guid=="car"?4:0;
        public static void ReceiveState(string json){Received.Add(json);OnReceiveState?.Invoke(json);}
        public static bool TryHostRequest(string guid,int arrangement,long revision,out string reason){HostRequests++;reason="fixture core refusal";return HostRequestResult;}
    }
}
