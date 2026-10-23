#if B99_S060_ARTICULATED || UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RearrangedS060
{
    // The attached rear assembly belongs to the existing native rear-bogie
    // Rigidbody. Keep its colliders in that frame instead of continuously
    // moving a second copy inside the car's automatically computed compound.
    [DefaultExecutionOrder(10000)]
    internal sealed class S060MeyerCollision : MonoBehaviour, IDisposable
    {
        private Transform frame;
        private Transform nativeCollisionRoot;
        private Collider nativeCollider;
        private Rigidbody carBody;
#if B99_S060_ARTICULATED
        private Bogie nativeBogie;
        internal void BindNativeBogie(Bogie bogie) { nativeBogie = bogie; SyncPose(); }
        internal void BindNativeContacts(CollisionInfoDispenser target)
        {
            if (nativeBogie == null) throw new InvalidOperationException("Native rear bogie required before binding Meyer contacts.");
            ReleaseContactBindings();
            contactBindings.Add(S060CollisionRelay.Bind(nativeBogie.gameObject, target, boxes.ToArray()));
        }
#endif
        private readonly List<BoxCollider> boxes = new List<BoxCollider>();
        private readonly List<IDisposable> contactBindings = new List<IDisposable>();
        private bool disposed;

        internal static S060MeyerCollision Create(Transform collisionRoot, Transform rearFrame, Collider materialSource)
        {
            if (collisionRoot == null || rearFrame == null)
                throw new ArgumentNullException("Meyer rear collision requires native collision and rear-frame roots.");
            var group = new GameObject("[Meyer attached rear collision]");
            group.transform.SetParent(rearFrame, false);
            group.layer = materialSource != null ? materialSource.gameObject.layer : collisionRoot.gameObject.layer;
            S060MeyerCollision owner = null;
            try
            {
                owner = group.AddComponent<S060MeyerCollision>();
                if (owner == null) throw new InvalidOperationException("Unity could not attach the Meyer rear collision owner.");
                owner.nativeCollisionRoot = collisionRoot;
                owner.nativeCollider = materialSource;
                owner.carBody = collisionRoot.GetComponentInParent<Rigidbody>();
                owner.Box("Rear deck", new Vector3(0, 1.55f, MeyerPlan.DeckCentreZ),
                    new Vector3(2.35f, .10f, MeyerPlan.DeckLength), materialSource);
                owner.Box("Rear buffer beam", new Vector3(0, 1.05f, MeyerPlan.RearBeamCentreZ),
                    new Vector3(2.35f, .25f, MeyerPlan.RearBeamDepth), materialSource);
                foreach (int side in new[] { -1, 1 })
                    owner.Box("Rear pannier " + side, new Vector3(side * (float)MeyerPlan.TankCentreX, (float)MeyerPlan.TankCentreY, 0),
                        new Vector3((float)MeyerPlan.TankWidth, (float)MeyerPlan.TankHeight, (float)MeyerPlan.TankLength), materialSource);
                owner.BindFrame(rearFrame);
                return owner;
            }
            catch
            {
                if (owner != null) owner.Dispose();
                else if (Application.isPlaying) Destroy(group); else DestroyImmediate(group);
                throw;
            }
        }

        private void Box(string name, Vector3 centre, Vector3 size, Collider source)
        {
            var part = new GameObject(name); part.transform.SetParent(transform, false); part.layer = gameObject.layer;
            var box = part.AddComponent<BoxCollider>(); box.center = centre; box.size = size;
            box.enabled = false; boxes.Add(box);
            if (source != null) box.sharedMaterial = source.sharedMaterial;
        }

        internal void BindFrame(Transform rearFrame)
        {
            if (disposed) throw new ObjectDisposedException(nameof(S060MeyerCollision));
            if (rearFrame == null) throw new ArgumentNullException(nameof(rearFrame));
            frame = rearFrame;
            transform.SetParent(frame, false);
            transform.localPosition = Vector3.zero; transform.localRotation = Quaternion.identity; transform.localScale = Vector3.one;
            SyncPose();
        }

        // No pose writes: native physics/interpolation/world moves already own
        // this hierarchy. Mirror only the native collision/rerail policy.
        private void FixedUpdate() => SyncPose();
        private void LateUpdate() => SyncPose();
        internal void SyncPose()
        {
            if (disposed) return;
            if (frame == null) { gameObject.SetActive(false); return; }
            if (carBody == null && nativeCollisionRoot != null) carBody = nativeCollisionRoot.GetComponentInParent<Rigidbody>();
            var body = frame.GetComponentInParent<Rigidbody>();
            // Save preparation can precede native Bogie.SetupPhysics. Do not
            // briefly add the shell to the car while that rear body is absent.
            // DV's existing [bogies] colliders take over on derailment. Wait
            // for its NEW rear Rigidbody on rerail instead of falling back
            // into the car's compound during that native transition.
            bool ready = body != null && body != carBody;
#if B99_S060_ARTICULATED
            if (nativeBogie != null) ready = !nativeBogie.HasDerailed && nativeBogie.rb != null && body == nativeBogie.rb;
#endif
            bool enabled = ready && nativeCollisionRoot != null &&
                nativeCollisionRoot.gameObject.activeInHierarchy && (nativeCollider == null || nativeCollider.enabled);
            bool trigger = nativeCollider != null && nativeCollider.isTrigger;
            foreach (var box in boxes)
            {
                if (box.enabled != enabled) box.enabled = enabled;
                if (box.isTrigger != trigger) box.isTrigger = trigger;
            }
        }

        internal Bounds Encapsulate(Bounds bounds, Transform car)
        {
            foreach (var box in GetComponentsInChildren<BoxCollider>())
                for (int corner = 0; corner < 8; corner++)
                {
                    var offset = new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                    bounds.Encapsulate(car.InverseTransformPoint(box.transform.TransformPoint(box.center + Vector3.Scale(offset, box.size) / 2)));
                }
            return bounds;
        }

        public void Dispose()
        {
            if (disposed) return;
            // Disable before unsubscribing; a failed binding disposal can be
            // retried through the retained conversion owner without contacts.
            gameObject.SetActive(false);
            ReleaseContactBindings();
            disposed = true; ReleaseReferences();
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }
        private void OnDestroy()
        {
            if (disposed) return;
            disposed = true;
            // Native pooling/parent deletion may bypass normal conversion
            // cleanup. Release subscriptions without recursively destroying GO.
            try { ReleaseContactBindings(); }
            finally { ReleaseReferences(); }
        }
        private void ReleaseContactBindings()
        {
            var errors = new List<Exception>();
            for (int i = contactBindings.Count - 1; i >= 0; i--)
                try { contactBindings[i].Dispose(); contactBindings.RemoveAt(i); }
                catch (Exception ex) { errors.Add(ex); }
            if (errors.Count != 0) throw new AggregateException("Meyer native contact unsubscription failed", errors);
        }
        private void ReleaseReferences()
        {
            frame = null; nativeCollider = null; nativeCollisionRoot = null; carBody = null;
#if B99_S060_ARTICULATED
            nativeBogie = null;
#endif
            boxes.Clear();
        }
    }
}
#endif
