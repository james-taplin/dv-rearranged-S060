using System;
using UnityEngine;

namespace RearrangedS060
{
    // Datums measured from the existing S282 branch_pipe custom prefab. Only
    // its three author-owned meshes are reused; no S282 DLL/runtime dependency.
    internal static class S060PipeGeometry
    {
        internal const float Radial = .60f, SourceLowerY = 1.59f;
        internal const float PipeLength = 3.0988f, PipeMinZ = -3.0226f;
        internal const float CylinderAnchorZ = 2.817f;
        internal const float NativeCylinderCentreZ = 3.16791177f;
        internal const float NativeCylinderLowerY = 1.49f;
        internal const float BoilerCutY = 2.10f;
        internal static Vector3 BoilerSocket(int side) => new Vector3(side * .70521827f, BoilerCutY, 3.16791177f);
        internal static Vector3 CylinderOffset(float scale, float shift) => new Vector3(0, .712128f * (scale - 1), CylinderAnchorZ * (scale - 1) + shift);
        internal static Vector3 Manifold(Vector3 source, bool front, int side, float lowerY, float cylinderZ) =>
            new Vector3(side * (1.03f + (source.x - 1.09f) * Radial),
                lowerY + (source.y - SourceLowerY) * Radial,
                cylinderZ + (source.z - (front ? 9.49f : 4.88f)) * Radial);
        internal static Vector3 Socket(bool front, int side, float lowerY, float cylinderZ) =>
            Manifold(front ? new Vector3(1.08821906f, 1.945054f, 8.695943f) : new Vector3(1.08762906f, 1.943393f, 5.634934f), front, side, lowerY, cylinderZ);
        internal static float FrontWeight(float vertexY, float lowerY)
        {
            // Preserve both complete flange bands. The actual inclined boiler
            // cap begins at lower+.55, not the old guessed marker height.
            // The transfer flange itself reaches lower+.30. Keep its entire
            // face rigid as well; a centre-only test missed its deformed rim.
            float t = Math.Max(0, Math.Min(1, (vertexY - lowerY - .33f) / .19f));
            return t * t * (3 - 2 * t);
        }
        internal static float SpanZ(float z, float length)
        {
            // Keep the original end collars; lengthen the plain centre only.
            const float collar = .18f;
            float t = Math.Max(0, Math.Min(1, (z - collar) / (PipeLength - 2 * collar)));
            return z + t * (length - PipeLength);
        }
    }
}
