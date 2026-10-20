using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using LocoSim.Implementations;
using static LocoSim.Implementations.ReciprocatingSteamEngine;
using LocoSim.Definitions;
using System.Linq;
using System.Reflection.Emit;

namespace RearrangedS060.Patches
{
	/*[HarmonyPatch(typeof(ReciprocatingSteamEngine),
				  MethodType.Constructor,
				  new Type[] { typeof(ReciprocatingSteamEngineDefinition) })]*/
	internal class AddTwoCylinders
	{
		static void Prefix(ReciprocatingSteamEngine __instance, ReciprocatingSteamEngineDefinition seDef)
		{
			seDef.numCylinders = 4;
		}

		//Without this, the code automatically sets the # of exhaust beats per cycle to
		//numCylinders * 2
		//Since we always want 4 beats, even on the 4 cylinder locomotives, we set this
		//to numCylinders * 1, i.e. 4
		/*static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var codes = new List<CodeInstruction>(instructions);

			for (int i = codes.Count - 1; i >= 0; i--)
			{
				if (codes[i].opcode == OpCodes.Ldc_I4_2)
				{
					codes[i].opcode = OpCodes.Ldc_I4_1;
					break;
				}
			}

			return codes.AsEnumerable();
		}*/

		private static void Postfix(ReciprocatingSteamEngine __instance, ReciprocatingSteamEngineDefinition seDef)
		{
			__instance.numCylinders = 2;
			seDef.numCylinders = 2;
		}
	}
}
