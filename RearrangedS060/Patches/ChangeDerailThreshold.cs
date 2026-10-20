using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace RearrangedS060.Patches
{
	[HarmonyPatch(typeof(TrainStress), nameof(TrainStress.FixedUpdate))]
	internal class ChangeDerailThreshold
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
		{
			var codes = new List<CodeInstruction>(instructions);
			for (int i = codes.Count - 1; i >= 0; i--)
			{
				if (!codes[i].Calls(typeof(GameParams).GetProperty(nameof(GameParams.DerailBuildUpThreshold)).GetMethod))
				{//if the current opcode doesn't call GameParams.DerailStressThreshold's getter, skip it
					continue;
				}

				//If WheelArranger is not on this object, then we have to clean up our mess on the stack.
				Label ifNoWheelArranger = gen.DefineLabel();
				Label endOfPatch = gen.DefineLabel();

				MethodInfo getComponent = typeof(TrainCar)
					.GetMethod(nameof(TrainCar.GetComponent), new Type[]{})
					.MakeGenericMethod(typeof(WheelRearranger));
				CodeInstruction[] newCodes =
				{
					new(OpCodes.Ldarg_0),
					new(OpCodes.Ldfld,
							typeof(TrainStress)
							.GetField(nameof(TrainStress.car), BindingFlags.NonPublic | BindingFlags.Instance)),
					new(OpCodes.Callvirt, getComponent),
					new(OpCodes.Dup),
					new(OpCodes.Brfalse, ifNoWheelArranger),
					new(OpCodes.Callvirt, typeof(WheelRearranger).GetMethod(nameof(WheelRearranger.GetDerailModifier))),
					new(OpCodes.Add),
					new(OpCodes.Br, endOfPatch),
					new(OpCodes.Pop)
				};
				//endOfPatch gets placed on the first instruction after our mess
				codes[i+1].labels.Add(endOfPatch);
				//ifNoWheelArrangement gets placed on the pop instruction
				newCodes[newCodes.Count() - 1].labels.Add(ifNoWheelArranger);

				//now that we have the patch ready, we add it into the code
				codes.InsertRange(i + 1, newCodes);
				break;
			}

			return codes;
		}
	}
}
