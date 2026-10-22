using System;
using System.Reflection;
using DV.Logic.Job;

namespace RearrangedS060
{
    internal static class S060LogicLength
    {
        private static readonly FieldInfo Length = typeof(Car).GetField("length", BindingFlags.Instance | BindingFlags.Public);
        internal static void Set(Car car, float length)
        {
            if (!(length > 0) || float.IsInfinity(length)) throw new ArgumentOutOfRangeException(nameof(length));
            if (car != null) (Length ?? throw new MissingFieldException("Native car length field missing")).SetValue(car, length);
        }
    }
}
