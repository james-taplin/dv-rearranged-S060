#if B99_S060_FUNCTIONAL
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using DV.Utils;
using LocoMeshSplitter.MeshLoaders;
using Newtonsoft.Json;
using UnityEngine;
using UnityModManagerNet;

[assembly: InternalsVisibleTo("RearrangedS060.Multiplayer")]

namespace RearrangedS060
{
    // Multiplayer owns transport, port replication, movement and simulation authority.
    // This class owns only the extra arrangement state absent from native car packets.
    internal static class S060MultiplayerSync
    {
        internal const int Protocol = 2;
        internal const int MaxStateLength = 32768;
        internal const int MaxRetainedTombstones = 256;
        internal const int MaxRetainedStates = 8192;
        internal const float ReplicaReadyTimeout = 120f;
        private enum Role { Offline, Host, Client, Blocked }
        private static Role role;
        private static bool accepted, canChange, bridgeLoaded, bridgeStartupBlocked;
        private static int bridgeAttempts;
        private static float nextTick, nextBridgeAttempt;
        private static string buildIdentity;
        private static long sessionEpoch, lastHostEpoch;
        private static int retainedTombstones;
        private static TrainCar applyingCar;
        private static CarSpawner observedSpawner;
        private static readonly Dictionary<string, NetworkArrangement> states = new Dictionary<string, NetworkArrangement>(StringComparer.Ordinal);
        private static readonly HashSet<TrainCar> tracked = new HashSet<TrainCar>();
        private static readonly Dictionary<string, AppliedState> applied = new Dictionary<string, AppliedState>(StringComparer.Ordinal);
        private static readonly HashSet<string> failed = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, float> waitingSince = new Dictionary<string, float>(StringComparer.Ordinal);
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.None, MaxDepth = 8,
            MissingMemberHandling = MissingMemberHandling.Error
        };

        internal static string Status { get; set; }
        internal static long SessionEpoch => sessionEpoch;
        internal static bool IsHost => role == Role.Host;
        internal static bool IsClient => role == Role.Client || role == Role.Blocked;
        internal static bool IsSimulationAuthority => role == Role.Offline || role == Role.Host;
        internal static bool ApplyingRemote => applyingCar != null;
        internal static bool InSession => role != Role.Offline;
        internal static Action<string> BroadcastState;
        internal static Action<string, int, long> SendRequest;
        internal static Action<string> ReportFailure;
        internal static Action DrawNetworkGUI;

        internal static string BuildIdentity
        {
            get
            {
                if (buildIdentity == null)
                {
                    using (var sha = SHA256.Create())
                    {
                        string Digest(string path) => File.Exists(path)
                            ? BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "") : "missing";
                        var parts = new List<string> { "RearrangedS060.dll:" + Digest(typeof(Main).Assembly.Location) };
                        foreach (string relative in new[] { "RearrangedS060.Multiplayer.dll", "assets/Meyer/ccl_bundle", "assets/Dockside/ccl_bundle", "assets/Shay/shay_running_gear" })
                            parts.Add(relative + ":" + Digest(Path.Combine(Main.ModPath, relative)));
                        // Identical DLLs with stale or replaced meshes can still
                        // produce different running gear. Hash the runtime assets
                        // once too; paths/settings/docs never enter the identity.
                        string fingerprint = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Join("|", parts)))).Replace("-", "");
                        buildIdentity = Main.RunningVersion + ":" + fingerprint;
                    }
                }
                return buildIdentity;
            }
        }

        internal static void Initialize()
        {
            // Retry after UMM has finished loading its mods. No MPAPI type is resolved
            // at all in single player; the optional assembly is loaded explicitly.
            TryLoadBridge();
        }

        private static void ObserveCarDeletion()
        {
            var current = CarSpawner.Instance;
            if (ReferenceEquals(current, observedSpawner)) return;
            // This native event runs before both pool return and unpooled
            // destruction, including world unload. A destroyed old singleton
            // still has its managed event, so detach by reference identity.
            if (!ReferenceEquals(observedSpawner, null)) observedSpawner.CarAboutToBeDeleted -= Forget;
            observedSpawner = current;
            if (current != null) current.CarAboutToBeDeleted += Forget;
        }

        private static void TryLoadBridge()
        {
            if (bridgeLoaded) return;
            var mod = UnityModManager.FindMod("Multiplayer");
            if (mod == null || !mod.Active)
            {
                // Failed optional startup must not leave ordinary single player
                // locked after Multiplayer is disabled. A loaded adapter handles
                // its real native server/client stop events itself.
                if (!bridgeLoaded)
                {
                    bridgeAttempts = 0;
                    nextBridgeAttempt = 0;
                    if (bridgeStartupBlocked) EndSession();
                }
                return;
            }
            if (bridgeLoaded || bridgeAttempts >= 15 || Time.unscaledTime < nextBridgeAttempt) return;
            bridgeAttempts++;
            nextBridgeAttempt = Time.unscaledTime + 2f;
            role = Role.Blocked;
            bridgeStartupBlocked = true;
            Status = "Waiting for the Multiplayer API.";
            try
            {
                var assembly = Assembly.LoadFrom(Path.Combine(Main.ModPath, "RearrangedS060.Multiplayer.dll"));
                assembly.GetType("RearrangedS060.Multiplayer.Bridge", true).GetMethod("Init", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
                bridgeLoaded = true;
                bridgeStartupBlocked = false;
            }
            catch (Exception ex)
            {
                // A failed adapter may already have entered and cleaned up a
                // native session. Restore the uncertain-startup lock afterward.
                role = Role.Blocked;
                bridgeStartupBlocked = true;
                string cause = BridgeFailureReason(ex);
                Status = bridgeAttempts < 15 ? "Waiting for Multiplayer integration: " + cause + " Retrying; arrangement changes are locked."
                    : "Multiplayer integration could not start: " + cause + " Arrangement changes are locked. See Player.log and restart after correcting the load error.";
                if (bridgeAttempts == 1 || bridgeAttempts == 15) Main.Logger.LogException(Status, ex);
            }
        }

        private static string BridgeFailureReason(Exception error)
        {
            while ((error is TargetInvocationException || error is TypeInitializationException) && error.InnerException != null)
                error = error.InnerException;
            if (error is ReflectionTypeLoadException types && types.LoaderExceptions?.FirstOrDefault(e => e != null) is Exception loader)
                return BridgeFailureReason(loader);
            if (error is FileNotFoundException missing)
            {
                string name = missing.FileName ?? "";
                if (name.IndexOf("RearrangedS060.Multiplayer", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Missing RearrangedS060.Multiplayer.dll; reinstall the complete " + Main.RunningVersion + " ZIP.";
                if (name.IndexOf("MultiplayerAPI", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "MultiplayerAPI.dll is unavailable; check DV Multiplayer's installation and UMM load errors.";
            }
            if (error is InvalidOperationException && error.Message == "Multiplayer API is not ready.")
                return "DV Multiplayer's API is not ready; check its UMM load status and Player.log.";
            string detail = error.GetType().Name + ": " + error.Message.Replace('\r', ' ').Replace('\n', ' ');
            return detail.Length > 240 ? detail.Substring(0, 240) + "..." : detail;
        }

        internal static void BeginHost()
        {
            ClearSession();
            role = Role.Host;
            sessionEpoch = lastHostEpoch = Math.Max(DateTime.UtcNow.Ticks, checked(lastHostEpoch + 1));
            Status = "Hosting; all matching players can use their wheel-arranger radios.";
        }

        internal static void BeginClient()
        {
            ClearSession();
            role = Role.Client;
            Status = "Connecting; arrangement changes are locked until the host accepts this build.";
        }

        internal static void EndSession()
        {
            ClearSession();
            role = Role.Offline;
            Status = null;
        }

        private static void ClearSession()
        {
            states.Clear(); applied.Clear(); failed.Clear();
            waitingSince.Clear();
            sessionEpoch = 0;
            retainedTombstones = 0;
            applyingCar = null;
            accepted = canChange = false;
            BroadcastState = null; SendRequest = null; ReportFailure = null;
            DrawNetworkGUI = null;
            nextTick = 0;
            bridgeStartupBlocked = false;
        }

        internal static void AcceptClient(bool allowed, string message)
        {
            if (role != Role.Client) return;
            accepted = true;
            canChange = allowed;
            Status = message;
        }

        internal static void RejectClient(string message)
        {
            if (role != Role.Client && role != Role.Blocked) return;
            accepted = canChange = false;
            role = Role.Blocked;
            Status = message;
        }

        internal static bool DeferAutomatic(TrainCar car) => IsClient;

        internal static bool BeforeSwitch(TrainCar car) => !IsClient || accepted && ReferenceEquals(car, applyingCar);

        internal static void OnApplied(TrainCar car, S060Selection selection)
        {
            if (!ValidCar(car)) return;
            tracked.Add(car);
            ObserveCarDeletion();
            if (!IsHost) return;
            var state = new NetworkArrangement { CarGuid = car.CarGUID, Arrangement = S060NetworkSelection.Encode(selection),
                Epoch = sessionEpoch, Revision = checked(RevisionFor(car.CarGUID) + 1) };
            if (states.TryGetValue(car.CarGUID, out var previous) && previous.Removed) retainedTombstones--;
            states[car.CarGUID] = state;
            Publish(state);
        }

        internal static void OnSwitchFailed(TrainCar car)
        {
            // Conversion can recover the prior selection or fall back to stock.
            // Publish that actual result; never publish the failed request.
            if (!IsHost || car == null || S060Conversion.Current(car)?.CleanupPending == true) return;
            var selection = S060Conversion.Current(car)?.Selection ?? S060Selection.Stock;
            if (!states.TryGetValue(car.CarGUID, out var prior) || prior.Removed || prior.Arrangement != S060NetworkSelection.Encode(selection))
                OnApplied(car, selection);
        }

        internal static void Forget(TrainCar car)
        {
            if (car == null) return;
            tracked.Remove(car);
            if (string.IsNullOrEmpty(car.CarGUID)) return;
            string guid = car.CarGUID;
            applied.Remove(guid); failed.Remove(guid); waitingSince.Remove(guid);
            if (IsHost && states.TryGetValue(guid, out var previous) && !previous.Removed)
            {
                var removed = new NetworkArrangement { CarGuid = guid, Epoch = sessionEpoch, Revision = checked(previous.Revision + 1), Removed = true };
                states[guid] = removed;
                Publish(removed);
                if (++retainedTombstones >= MaxRetainedTombstones) CompactHostState();
            }
        }

        private static NetworkArrangement ResetMarker() => new NetworkArrangement { Epoch = sessionEpoch, Reset = true };

        private static void CompactHostState()
        {
            // Custom packets are reliably delivered but unordered. Advancing the
            // epoch makes delayed old updates harmless without retaining every
            // deleted GUID for the lifetime of a long-running session.
            var live = states.Values.Where(s => !s.Removed).ToArray();
            sessionEpoch = lastHostEpoch = checked(Math.Max(sessionEpoch, lastHostEpoch) + 1);
            states.Clear();
            retainedTombstones = 0;
            foreach (var state in live) { state.Epoch = sessionEpoch; states[state.CarGuid] = state; }
            Publish(ResetMarker());
            foreach (var state in live) Publish(state);
        }

        private static void Publish(NetworkArrangement state)
        {
            try { BroadcastState?.Invoke(JsonConvert.SerializeObject(state, JsonSettings)); }
            catch (Exception ex) { Main.Logger.LogException("Arrangement applied, but its multiplayer update could not be sent", ex); }
        }

        internal static long RevisionFor(string guid) => guid != null && states.TryGetValue(guid, out var state) ? state.Revision : 0;
        internal static IEnumerable<string> Snapshot() => sessionEpoch > 0
            ? new[] { ResetMarker() }.Concat(states.Values).Select(s => JsonConvert.SerializeObject(s, JsonSettings)).ToArray()
            : Array.Empty<string>();
        internal static IEnumerable<string> SnapshotFor(string guid) => guid != null && states.TryGetValue(guid, out var state)
            ? new[] { JsonConvert.SerializeObject(state, JsonSettings) } : Array.Empty<string>();

        internal static bool RequestChange(TrainCar car, S060Selection selection, out string reason)
        {
            reason = null;
            if (!ValidCar(car)) { reason = "Select an initialized S060."; return false; }
            int id = S060NetworkSelection.Encode(selection);
            if (IsClient)
            {
                if (!accepted || !canChange || SendRequest == null) { reason = "The host has locked arrangement changes, or this build has not been accepted."; return false; }
                try { SendRequest(car.CarGUID, id, RevisionFor(car.CarGUID)); }
                catch (Exception ex) { Main.Logger.LogException("Could not request arrangement from host", ex); reason = "The request could not be sent to the host."; return false; }
                Status = reason = "Arrangement requested from host; wait for synchronization.";
                return true;
            }
            if (IsHost) return TryHostRequest(car.CarGUID, id, RevisionFor(car.CarGUID), out reason);
            return S060Lifecycle.ApplyLocal(car, selection, out reason);
        }

        internal static bool TryHostRequest(string guid, int id, long expectedRevision, out string reason)
        {
            reason = null;
            if (!IsHost || !S060NetworkSelection.IsSupported(id) || !ValidGuid(guid)) { reason = "Invalid arrangement request."; return false; }
            if (expectedRevision != RevisionFor(guid)) { reason = "This locomotive changed since your selection. Select it again."; return false; }
            var car = Cars().FirstOrDefault(c => ValidCar(c) && string.Equals(c.CarGUID, guid, StringComparison.Ordinal));
            if (car == null) { reason = "The host cannot find that S060."; return false; }
            bool result = S060Lifecycle.ApplyLocal(car, S060NetworkSelection.Decode(id), out reason);
            Status = reason;
            return result;
        }

        internal static void ReceiveState(string json)
        {
            if (role != Role.Client || !accepted) return;
            try
            {
                if (string.IsNullOrEmpty(json) || json.Length > MaxStateLength) throw new InvalidDataException("Arrangement message is too large or empty.");
                var state = JsonConvert.DeserializeObject<NetworkArrangement>(json, JsonSettings);
                if (state == null || state.Epoch <= 0 || (state.Reset && (state.Removed || state.CarGuid != null || state.Revision != 0)) ||
                    (!state.Reset && (!ValidGuid(state.CarGuid) || state.Revision <= 0 ||
                    (!state.Removed && !S060NetworkSelection.IsSupported(state.Arrangement)))
                    ))
                    throw new InvalidDataException("Invalid host arrangement state.");
                if (state.Epoch < sessionEpoch) return;
                if (state.Epoch > sessionEpoch)
                {
                    sessionEpoch = state.Epoch;
                    states.Clear(); applied.Clear(); failed.Clear(); waitingSince.Clear();
                    retainedTombstones = 0;
                }
                // A state may overtake its reset marker. Repeating a reset for
                // the same epoch must never erase already-received newer data.
                if (state.Reset) return;
                if (state.Revision <= RevisionFor(state.CarGuid)) return;
                if (!states.ContainsKey(state.CarGuid) && states.Count >= MaxRetainedStates)
                    throw new InvalidDataException("The host exceeded the supported arrangement snapshot size; reconnect after reducing loaded S060s.");
                if (state.Removed && (!states.TryGetValue(state.CarGuid, out var prior) || !prior.Removed)) retainedTombstones++;
                else if (!state.Removed && states.TryGetValue(state.CarGuid, out var priorLive) && priorLive.Removed) retainedTombstones--;
                states[state.CarGuid] = state;
                failed.Remove(state.CarGuid);
                waitingSince.Remove(state.CarGuid);
                if (state.Removed) { applied.Remove(state.CarGuid); }
            }
            catch (Exception ex)
            {
                FailSession("Rejected invalid host arrangement data: " + ex.Message);
            }
        }

        internal static void Tick()
        {
            TryLoadBridge();
            // Do not query a native world singleton while UMM is loading in
            // the main menu. An installed profile implies an actual live car.
            if (IsHost || role == Role.Client && accepted || tracked.Count != 0) ObserveCarDeletion();
            if (Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + .5f;
            if (IsHost)
            {
                foreach (var car in Cars().Where(ValidCar).ToArray())
                    if ((!states.TryGetValue(car.CarGUID, out var known) || known.Removed) && car.GetComponent<S060Lifecycle>()?.HasPending != true)
                        OnApplied(car, S060Conversion.Current(car)?.Selection ?? S060Selection.Stock);
                return;
            }
            if (role != Role.Client || !accepted) return;
            foreach (var car in Cars().Where(ValidCar).ToArray())
            {
                if (!states.TryGetValue(car.CarGUID, out var state) || state.Removed || failed.Contains(car.CarGUID)) continue;
                if (applied.TryGetValue(car.CarGUID, out var previous) && ReferenceEquals(previous.Car, car) && previous.Revision == state.Revision) continue;
                // An exploded car legitimately retains its host arrangement,
                // but the native model/material swap must finish repair before
                // rebuilding its running gear. Leave the newest revision queued
                // and do not count wreck time toward initialization failure.
                if (car.isExploded || car.GetComponent<ExplosionModelHandler>()?.usingExplodedModel == true)
                {
                    waitingSince.Remove(car.CarGUID);
                    continue;
                }
                try
                {
                    if (!ReadyForRemote(car, state.Arrangement))
                    {
                        if (!waitingSince.TryGetValue(car.CarGUID, out var since)) waitingSince[car.CarGUID] = Time.unscaledTime;
                        else if (Time.unscaledTime - since >= ReplicaReadyTimeout)
                            FailReplica(car, "Native simulation, bogies or split running gear did not become ready within two minutes.");
                        if (role != Role.Client || !accepted) break;
                        continue;
                    }
                    waitingSince.Remove(car.CarGUID);
                    applyingCar = car;
                    if (S060Lifecycle.ApplyLocal(car, S060NetworkSelection.Decode(state.Arrangement), out var reason, S060Conversion.Current(car) == null))
                    {
                        applied[car.CarGUID] = new AppliedState { Car = car, Revision = state.Revision };
                        Status = "Synchronized " + car.ID + ": " + S060NetworkSelection.Decode(state.Arrangement).Name + (canChange ? ". Radio changes allowed." : ". Radio changes locked by host.");
                    }
                    else
                    {
                        FailReplica(car, reason ?? "The native running gear could not be initialized.");
                    }
                }
                catch (Exception ex) { FailReplica(car, ex.Message); }
                finally { applyingCar = null; }
                if (role != Role.Client || !accepted) break;
            }
        }

        private static void FailReplica(TrainCar car, string reason)
        {
            failed.Add(car.CarGUID);
            FailSession("Could not synchronize " + car.ID + ": " + reason + ". Send the host your Player.log; reload before driving this locomotive.");
        }

        private static void FailSession(string message)
        {
            if (role == Role.Blocked) return;
            RejectClient(message);
            Main.Logger.Error(message);
            try { ReportFailure?.Invoke(message.Length > 1000 ? message.Substring(0, 1000) : message); }
            catch (Exception ex) { Main.Logger.LogException("Could not report arrangement synchronization failure to the host", ex); }
        }

        private static IEnumerable<TrainCar> Cars() => CarSpawner.Instance != null ? CarSpawner.Instance.AllCars : Enumerable.Empty<TrainCar>();
        private static bool ValidGuid(string guid) => Guid.TryParse(guid, out _);
        private static bool ValidCar(TrainCar car) => S060Parts.IsS060(car) && car.logicCar != null && ValidGuid(car.CarGUID);
        private static bool ReadyForRemote(TrainCar car, int id) => car.entity != null && car.entity.IsConverted && car.AreBogiesFullyInitialized() &&
            car.SimController?.simFlow != null && car.SimController.drivingForce != null && car.SimController.wheelslipController != null &&
            car.SimController.tractionPortsFeeder != null && car.SimController.poweredWheels?.poweredWheels != null &&
            car.adhesionController != null && car.GetComponent<S060MeshLoader>() != null &&
            car.transform.Find("LocoS060_Body/DrivingMechanism_R") != null;

        internal static void Draw()
        {
            if (!InSession) return;
            GUILayout.Label("Multiplayer: " + Status);
            if (InSession) GUILayout.Label("The host checks arrangement changes. All players need the identical S060 package.");
            DrawNetworkGUI?.Invoke();
        }

        private sealed class AppliedState { internal TrainCar Car; internal long Revision; }
    }

    internal sealed class NetworkArrangement
    {
        public long Epoch;
        public bool Reset;
        public string CarGuid;
        public int Arrangement;
        public long Revision;
        public bool Removed;
    }
}
#endif
