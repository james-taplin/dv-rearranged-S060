#if B99_S060_ARTICULATED
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace RearrangedS060
{
    // Keep the existing car CID's collision policy when this owned shell is
    // carried by its native bogie Rigidbody. No damage/audio policy is copied.
    internal sealed class S060CollisionRelay : IDisposable
    {
        private CollisionInfoDispenser source;
        private Action<Collision> enter, stay, exit;
        private readonly HashSet<Collider> owned = new HashSet<Collider>();
        // Exit has no contact points; retain only external colliders previously
        // observed touching our shell so unrelated bogie contacts stay native.
        private readonly HashSet<Collider> contacts = new HashSet<Collider>();
        private bool ownsSource, disposed;
        internal static S060CollisionRelay Bind(GameObject sourceObject, CollisionInfoDispenser target, Collider[] colliders)
        {
            if (sourceObject == null || target == null || colliders == null || colliders.Length == 0)
                throw new ArgumentException("Native collision relay needs its bogie, car dispenser and owned shell.");
            var owner = new S060CollisionRelay();
            try
            {
                foreach (var collider in colliders) if (collider != null) owner.owned.Add(collider);
                // A previous owned dispenser may await deferred destruction
                // during same-frame rollback. Never subscribe to that instance.
                owner.source = sourceObject.GetComponents<CollisionInfoDispenser>().FirstOrDefault(c => c.enabled);
                if (owner.source == null) { owner.source = sourceObject.AddComponent<CollisionInfoDispenser>(); owner.ownsSource = true; }
                owner.enter = AccessTools.MethodDelegate<Action<Collision>>(AccessTools.Method(typeof(CollisionInfoDispenser), "OnCollisionEnter"), target);
                owner.stay = AccessTools.MethodDelegate<Action<Collision>>(AccessTools.Method(typeof(CollisionInfoDispenser), "OnCollisionStay"), target);
                owner.exit = AccessTools.MethodDelegate<Action<Collision>>(AccessTools.Method(typeof(CollisionInfoDispenser), "OnCollisionExit"), target);
                owner.source.CollisionEnterInfo += owner.Enter; owner.source.CollisionStayInfo += owner.Stay; owner.source.CollisionExitInfo += owner.Exit;
                return owner;
            }
            catch { owner.Dispose(); throw; }
        }
        private bool IsOwned(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++) if (owned.Contains(collision.GetContact(i).thisCollider)) return true;
            return false;
        }
        private void Enter(Collision collision, bool paused)
        { if (!disposed && IsOwned(collision)) { contacts.Add(collision.collider); enter(collision); } }
        private void Stay(Collision collision, bool paused)
        { if (!disposed && IsOwned(collision)) { contacts.Add(collision.collider); stay(collision); } }
        private void Exit(Collision collision, bool paused)
        { if (!disposed && contacts.Remove(collision.collider)) exit(collision); }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (source != null)
            {
                source.CollisionEnterInfo -= Enter; source.CollisionStayInfo -= Stay; source.CollisionExitInfo -= Exit;
                if (ownsSource)
                {
                    source.enabled = false;
                    if (Application.isPlaying) UnityEngine.Object.Destroy(source); else UnityEngine.Object.DestroyImmediate(source);
                }
            }
            source = null; enter = stay = exit = null; owned.Clear(); contacts.Clear();
        }
    }
}
#endif
