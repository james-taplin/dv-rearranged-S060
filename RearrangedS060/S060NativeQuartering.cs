using System;
using System.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // The native wheel controller seeds phases only in Awake. Retain both
    // original Animators while their mechanisms are hidden for an articulated
    // layout, then return their original flags after the mechanisms are restored.
    // Native playback and the complete linkage pose remain authoritative.
    internal sealed class S060NativeQuartering : IDisposable
    {
        private readonly Animator[] animators;
        private readonly bool[] originalRetention;
        private bool disposed;

        internal S060NativeQuartering(Transform left, Transform right)
        {
            animators = new[] { left?.GetComponent<Animator>(), right?.GetComponent<Animator>() };
            if (animators.Any(a => a == null)) throw new InvalidOperationException("Both native S060 drivetrain Animators are required.");
            originalRetention = animators.Select(a => a.keepAnimatorControllerStateOnDisable).ToArray();
        }

        internal void Retain()
        {
            if (disposed) throw new ObjectDisposedException(nameof(S060NativeQuartering));
            foreach (var animator in animators) if (animator != null) animator.keepAnimatorControllerStateOnDisable = true;
        }

        public void Dispose()
        {
            if (disposed) return;
            for (int i = 0; i < animators.Length; i++)
                if (animators[i] != null) animators[i].keepAnimatorControllerStateOnDisable = originalRetention[i];
            disposed = true;
        }
    }
}
