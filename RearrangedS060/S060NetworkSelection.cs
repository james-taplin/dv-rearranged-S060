using System;
namespace RearrangedS060
{
    // Wire-only IDs. Existing saved arrangement/scale pairs remain unchanged.
    internal static class S060NetworkSelection
    {
        internal const int ShortWheelbase = 8;
        internal static bool IsSupported(int id) => S060Selection.IsSupported(id) || id == ShortWheelbase;
        internal static int Encode(S060Selection selection) => selection.Arrangement == S060Arrangement.SixCoupled && !selection.IsStock
            ? ShortWheelbase : (int)selection.Arrangement;
        internal static S060Selection Decode(int id)
        {
            if (!IsSupported(id)) throw new ArgumentOutOfRangeException(nameof(id));
            return id == ShortWheelbase ? S060Selection.ForConversion(S060Arrangement.SixCoupled, 1f)
                : new S060Selection((S060Arrangement)id, 1f);
        }
    }
}
