using UnityEngine;

namespace RearrangedS060
{
    internal static class S060AxleGeometry
    {
        internal static Vector3 FromSupport(Vector3 originalPosition, Quaternion originalRotation, Vector3 axleLocal) =>
            originalPosition + originalRotation * axleLocal;
    }
}
