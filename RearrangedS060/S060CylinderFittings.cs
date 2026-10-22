using System;
using System.Collections.Generic;
using System.Linq;
using DV.ModularAudioCar;
using DV.Simulation.Controllers;
using UnityEngine;

namespace RearrangedS060
{
    internal sealed class S060CylinderFittings : IDisposable
    {
        // Rear cylinder/gland station and piston axis from the native S060.
        private readonly List<(Transform part, Vector3 position)> homes = new List<(Transform, Vector3)>();
        private S060BodyPipes pipes;
        private S060NativeAudio.Placement audio;
        internal static S060CylinderFittings Apply(S060Parts parts, float scale, float shift)
        {
            var owner = new S060CylinderFittings();
            var delta = S060PipeGeometry.CylinderOffset(scale, shift);
            try
            {
                var cylinders = parts.Body.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name == "s060_cylinder_left" || t.name == "s060_cylinder_right").ToArray();
                if (cylinders.Length < 2) throw new InvalidOperationException("Split S060 cylinders are not ready.");
                var move = new List<Transform>(cylinders);
                move.AddRange(parts.Car.GetComponentsInChildren<CylinderCockParticlePortReader>(true)
                    .Where(r => r.crankRotationPortId == "steamEngine.CRANK_ROTATION")
                    .SelectMany(r => r.cylinderSetups.SelectMany(s => new[] { s.frontParticlesParent?.transform, s.rearParticlesParent?.transform })));
                move.AddRange(parts.Car.GetComponentsInChildren<ParticlesPortReadersController>(true)
                    .SelectMany(c => c.particlePortReaders).Where(r => r.particleUpdaters.Any(u => u.portId == "cylinderCock.EXT_IN"))
                    .Select(r => r.particlesParent?.transform));
                var crack = parts.Car.transform.Find("LocoS060_Particles/CylinderCrack"); if (crack != null) move.Add(crack);
                var unique = move.Where(t => t != null).Distinct().ToArray();
                foreach (var part in unique.Where(t => !unique.Any(parent => parent != t && t.IsChildOf(parent))))
                {
                    owner.homes.Add((part, part.localPosition));
                    // Translation only: retain complete stock dimensions, mirror,
                    // pipe bore, cock spacing, audio and emitter sizes.
                    part.position += parts.Car.transform.TransformVector(delta);
                }
                owner.pipes = S060BodyPipes.Apply(parts.Body, parts.Body.InverseTransformVector(parts.Car.transform.TransformVector(delta)));
                owner.audio = S060NativeAudio.Translate(parts.Car, delta);
                return owner;
            }
            catch { owner.Dispose(); throw; }
        }
        public void Dispose()
        {
            audio?.Dispose(); audio = null;
            ReleaseResources();
            foreach (var home in homes) if (home.part != null) home.part.localPosition = home.position;
            homes.Clear();
        }
        internal void ReleaseResources() { pipes?.Dispose(); pipes = null; }
    }
}
