#if B99_S060_ARTICULATED || UNITY_EDITOR
using System;
using UnityEngine;

namespace RearrangedS060
{
    // The visual rear frame belongs to the bogie's Rigidbody. Its collision
    // shell must follow that pose but remain part of the CAR compound body.
    // Reparenting colliders to the frame would silently change body ownership.
    [DefaultExecutionOrder(10000)]
    internal sealed class S060MeyerCollision : MonoBehaviour, IDisposable
    {
        private Transform frame;
        private bool disposed;

        internal static S060MeyerCollision Create(Transform collisionRoot, Transform rearFrame, Collider materialSource)
        {
            if (collisionRoot == null || rearFrame == null)
                throw new ArgumentNullException("Meyer rear collision requires native collision and rear-frame roots.");
            var group = new GameObject("[Meyer attached rear collision]");
            group.transform.SetParent(collisionRoot, false);
            group.layer = materialSource != null ? materialSource.gameObject.layer : collisionRoot.gameObject.layer;
            S060MeyerCollision owner = null;
            try
            {
                owner = group.AddComponent<S060MeyerCollision>();
                if (owner == null) throw new InvalidOperationException("Unity could not attach the Meyer rear collision owner.");
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
            if (source != null) box.sharedMaterial = source.sharedMaterial;
        }

        internal void BindFrame(Transform rearFrame)
        {
            if (disposed) throw new ObjectDisposedException(nameof(S060MeyerCollision));
            if (rearFrame == null) throw new ArgumentNullException(nameof(rearFrame));
            frame = rearFrame;
            SyncPose();
        }

        // FixedUpdate supplies the current pose to the next physics step;
        // LateUpdate follows interpolated rendering, rerail and world moves.
        private void FixedUpdate() => SyncPose();
        private void LateUpdate() => SyncPose();
        internal void SyncPose()
        {
            if (disposed) return;
            if (frame == null) { gameObject.SetActive(false); return; }
            var parentScale = transform.parent.lossyScale;
            var scale = frame.lossyScale;
            if (!transform.position.Equals(frame.position) || !transform.rotation.Equals(frame.rotation))
                transform.SetPositionAndRotation(frame.position, frame.rotation);
            var localScale = new Vector3(scale.x / parentScale.x, scale.y / parentScale.y, scale.z / parentScale.z);
            if (!transform.localScale.Equals(localScale)) transform.localScale = localScale;
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
            disposed = true; frame = null;
            // Destroy is deferred. Disable immediately so returning to stock
            // cannot retain an extra collision shell for another physics tick.
            gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }
    }
}
#endif
