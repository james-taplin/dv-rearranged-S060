using HarmonyLib;
using System;

namespace RearrangedS060.Patches
{
	//By default, the Bogie.Axles getter gets all [axles] that it can find.
	//We don't want it to get the inactive ones, so we tell it to remove the
	//ones that aren't active from the list.
	[HarmonyPatch(typeof(Bogie), nameof(Bogie.Axles), MethodType.Getter)]
	internal class BogieAxlesPatch
	{
		static void Postfix(Bogie __instance, ref Bogie.AxleInfo[] __result)
		{
			var source = __result;
			int firstInactive = 0;
			while (firstInactive < source.Length && IsActive(source[firstInactive]))
				firstInactive++;

			// Native callers read this getter repeatedly (including rolling audio).
			// Keep its cached array when nothing has retired since the previous read.
			if (firstInactive == source.Length)
			{
				__instance.axles = source;
				return;
			}

			int activeCount = firstInactive;
			for (int i = firstInactive + 1; i < source.Length; i++)
				if (IsActive(source[i]))
					activeCount++;

			var activeAxles = activeCount == 0 ? Array.Empty<Bogie.AxleInfo>() : new Bogie.AxleInfo[activeCount];
			Array.Copy(source, activeAxles, firstInactive);
			int destination = firstInactive;
			for (int i = firstInactive + 1; i < source.Length; i++)
				if (IsActive(source[i]))
					activeAxles[destination++] = source[i];

			// The getter has already copied its cache into the return value.
			// Updating only the cache leaves the FIRST caller holding retired
			// pony/driver axles until Destroy runs at the end of this frame.
			__result = __instance.axles = activeAxles;
		}

		private static bool IsActive(Bogie.AxleInfo axle) =>
			axle.transform != null && axle.transform.gameObject.activeInHierarchy;
	}
}
