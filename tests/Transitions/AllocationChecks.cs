using System;
using System.Reflection;
using RearrangedS060.Patches;

internal static class AllocationChecks
{
    private delegate void GetterPostfix(Bogie bogie, ref Bogie.AxleInfo[] result);

    internal static void Run()
    {
        var method = typeof(BogieAxlesPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);
        var postfix = (GetterPostfix)method.CreateDelegate(typeof(GetterPostfix));

        CheckUnchangedResults(postfix);
        CheckFiltering(postfix);
        CheckRetirementAfterReads(postfix);
        CheckRepeatedReadAllocations(postfix);
    }

    private static Bogie.AxleInfo Make(bool active) => new Bogie.AxleInfo
    {
        transform = new Bogie.Transform
        {
            gameObject = new Bogie.GameObject { activeInHierarchy = active }
        }
    };

    private static void CheckUnchangedResults(GetterPostfix postfix)
    {
        for (int axleCount = 0; axleCount <= 5; axleCount++)
        {
            // A distinct empty array also verifies that the unchanged path
            // preserves a caller's reference rather than substituting Empty().
            var source = new Bogie.AxleInfo[axleCount];
            for (int i = 0; i < source.Length; i++)
                source[i] = Make(true);
            var bogie = new Bogie { axles = source };
            var result = bogie.Axles;
            postfix(bogie, ref result);
            Require(ReferenceEquals(source, result) && ReferenceEquals(result, bogie.axles),
                "Unchanged live/empty getter replaced its cached array.");
        }

        // Preserve cache/return agreement even if an earlier Harmony postfix
        // has supplied a different all-live result.
        var replacement = new[] { Make(true) };
        var replacedBogie = new Bogie { axles = new[] { Make(true) } };
        var replacedResult = replacement;
        postfix(replacedBogie, ref replacedResult);
        Require(ReferenceEquals(replacement, replacedResult) && ReferenceEquals(replacement, replacedBogie.axles),
            "Unchanged result did not update the native cache reference.");
    }

    private static void CheckFiltering(GetterPostfix postfix)
    {
        // Place an invalid entry at every position, using both inactive
        // transforms and missing transforms as in the native retirement path.
        for (int invalidPosition = 0; invalidPosition < 5; invalidPosition++)
        {
            foreach (bool missingTransform in new[] { false, true })
            {
                var source = new Bogie.AxleInfo[5];
                var expected = new Bogie.AxleInfo[4];
                int nextExpected = 0;
                for (int i = 0; i < source.Length; i++)
                {
                    source[i] = i == invalidPosition
                        ? missingTransform ? new Bogie.AxleInfo() : Make(false)
                        : Make(true);
                    if (i != invalidPosition)
                        expected[nextExpected++] = source[i];
                }
                CheckFilteredResult(postfix, source, expected);
            }
        }

        var first = Make(true);
        var second = Make(true);
        var third = Make(true);
        CheckFilteredResult(postfix,
            new[] { Make(false), first, new Bogie.AxleInfo(), second, Make(false), third, Make(false) },
            new[] { first, second, third });
        CheckFilteredResult(postfix,
            new[] { Make(false), new Bogie.AxleInfo(), Make(false) },
            Array.Empty<Bogie.AxleInfo>());
        Console.WriteLine("19 live/empty/filtering cache and ordering cases passed using the production postfix.");
    }

    private static void CheckFilteredResult(GetterPostfix postfix, Bogie.AxleInfo[] source, Bogie.AxleInfo[] expected)
    {
        var originalEntries = (Bogie.AxleInfo[])source.Clone();
        var bogie = new Bogie { axles = source };
        var result = bogie.Axles;
        postfix(bogie, ref result);
        Require(result.Length == expected.Length && ReferenceEquals(result, bogie.axles),
            "First filtered getter and native cache disagree.");
        Require(!ReferenceEquals(source, result), "Filtering reused the unfiltered array.");
        for (int i = 0; i < expected.Length; i++)
            Require(ReferenceEquals(expected[i], result[i]), "Filtering changed the surviving axle order.");
        for (int i = 0; i < source.Length; i++)
            Require(ReferenceEquals(originalEntries[i], source[i]), "Filtering mutated a previous caller's array.");

        var cached = result;
        result = bogie.Axles;
        postfix(bogie, ref result);
        Require(ReferenceEquals(cached, result) && ReferenceEquals(cached, bogie.axles),
            "Second filtered getter allocated another array.");
    }

    private static void CheckRetirementAfterReads(GetterPostfix postfix)
    {
        var first = Make(true);
        var retired = Make(true);
        var last = Make(true);
        var source = new[] { first, retired, last };
        var bogie = new Bogie { axles = source };
        for (int i = 0; i < 10; i++)
        {
            var current = bogie.Axles;
            postfix(bogie, ref current);
            Require(ReferenceEquals(source, current), "Live cache changed before retirement.");
        }

        retired.transform.gameObject.activeInHierarchy = false;
        var result = bogie.Axles;
        postfix(bogie, ref result);
        Require(result.Length == 2 && ReferenceEquals(result[0], first) && ReferenceEquals(result[1], last)
            && ReferenceEquals(result, bogie.axles), "Newly retired axle escaped the very next getter.");

        // A missing/destroyed transform after caching also needs immediate
        // removal, rather than a one-time validity check per cached array.
        last.transform = null;
        result = bogie.Axles;
        postfix(bogie, ref result);
        Require(result.Length == 1 && ReferenceEquals(result[0], first) && ReferenceEquals(result, bogie.axles),
            "Newly missing transform escaped the very next getter.");
        Console.WriteLine("Dynamic inactive and missing-transform retirement passed on the first subsequent getter.");
    }

    private static void CheckRepeatedReadAllocations(GetterPostfix postfix)
    {
        var bogies = new Bogie[7];
        var expected = new Bogie.AxleInfo[bogies.Length][];
        for (int b = 0; b < 6; b++)
        {
            var axles = new Bogie.AxleInfo[b];
            for (int i = 0; i < axles.Length; i++)
                axles[i] = Make(true);
            bogies[b] = new Bogie { axles = axles };
            expected[b] = axles;
        }

        // Include a cache that has already had one real retirement/filter.
        bogies[6] = new Bogie { axles = new[] { Make(false), Make(true), new Bogie.AxleInfo(), Make(true) } };
        var filtered = bogies[6].Axles;
        postfix(bogies[6], ref filtered);
        expected[6] = filtered;

        const int iterations = 100000;
        for (int i = 0; i < 10000; i++)
        {
            var bogie = bogies[i % bogies.Length];
            var result = bogie.Axles;
            postfix(bogie, ref result);
        }

        // Bind reflection to a delegate before timing so only repeated native
        // getter/postfix work is measured, without Invoke's object[]/boxing.
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++)
        {
            int index = i % bogies.Length;
            var bogie = bogies[index];
            var result = bogie.Axles;
            postfix(bogie, ref result);
            if (!ReferenceEquals(expected[index], result) || !ReferenceEquals(result, bogie.axles))
                throw new Exception("Repeated getter replaced its unchanged cache.");
        }
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(allocatedBytes == 0, "Repeated unchanged getter allocated " + allocatedBytes + " managed bytes.");
        Console.WriteLine(iterations + " repeated getter/postfix calls allocated " + allocatedBytes
            + " managed bytes across 0-5 axles and an already-filtered cache; Unity activity is a fixture double.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
