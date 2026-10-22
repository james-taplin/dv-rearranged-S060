using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
			// The getter has already copied its cache into the return value.
			// Updating only the cache leaves the FIRST caller holding retired
			// pony/driver axles until Destroy runs at the end of this frame.
			__result = __instance.axles = __result
				.Where(source => source.transform != null && source.transform.gameObject.activeInHierarchy)
				.ToArray();
		}
	}
}
