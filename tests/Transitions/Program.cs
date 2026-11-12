using System;
using System.Reflection;
using RearrangedS060.Patches;

// Small getter-contract fixture: a getter's returned reference is separate from
// its field after a Harmony postfix replaces that field. No Unity execution.
public sealed class Bogie
{
    public sealed class GameObject { public bool activeInHierarchy; }
    public sealed class Transform { public GameObject gameObject = new GameObject(); }
    public sealed class AxleInfo { public Transform transform; }
    public AxleInfo[] axles;
    public AxleInfo[] Axles => axles;
}
internal static class Program
{
    private static int Main()
    {
        AllocationChecks.Run();
        var postfix = typeof(BogieAxlesPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);
        Bogie.AxleInfo Make(bool active) => new Bogie.AxleInfo { transform = new Bogie.Transform { gameObject = new Bogie.GameObject { activeInHierarchy = active } } };
        var stock = new[] { Make(true), Make(true), Make(true) };
        for (int i = 0; i < 100; i++)
        {
            var retired = Make(false);
            var bogie = new Bogie { axles = new[] { stock[0], stock[1], stock[2], retired, new Bogie.AxleInfo() } };
            var firstGetterResult = bogie.Axles;
            var args = new object[] { bogie, firstGetterResult };
            postfix.Invoke(null, args);
            var result = (Bogie.AxleInfo[])args[1];
            if (result.Length != 3 || !ReferenceEquals(result, bogie.axles) || Array.IndexOf(result, retired) >= 0)
                throw new Exception("First same-frame getter returned a retired axle.");
        }
        Console.WriteLine("100 same-frame retired-axle getter checks passed using the production postfix; no Unity/game execution.");
        return 0;
    }
}
