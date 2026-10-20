using DV.ThingTypes;
using DV.VRTK_Extensions;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RearrangedS060.Patches
{

	//TODO: figure out how to make fifth wheel, axles always spawn right when gauge mod is installed

	[HarmonyPatch(typeof(TrainCar), nameof(TrainCar.Start))]
	internal class FixS282FifthWheelClipping
	{
		[HarmonyAfter("Gauge")]
		static void Postfix(TrainCar __instance)
		{
			/*if (Main.IsGaugeModInstalled)
			{
				Main.Logger.Log("Gauge mod installed, skipping alteration of S282 mesh");
				return;
			}*/
			/*if (__instance.carType != TrainCarType.LocoSteamHeavy)
				return;

			MeshFilter filter = __instance.transform
				.Find("LocoS282A_Body/Static_LOD0/s282_locomotive_body")
				.GetComponent<MeshFilter>();
			if (filter == null)
			{
				Main.Logger.Warning("MeshFilter was null on an S282");
				return;
			}
			Mesh mesh = filter.sharedMesh;
			if (mesh == null)
			{
				Main.Logger.Warning("Mesh was null on an S282");
				return;
			}
			Main.Logger.Log("Loading new S282 mesh...");
			Mesh newS282Mesh = UnityEngine.Object.Instantiate(MeshFinder.Instance.S282Mesh);
			//for some reason the OBJloader flips the mesh left to right, so we have to flip it back
			__instance.transform.Find("LocoS282A_Body/Static_LOD0/s282_locomotive_body").localScale = new Vector3(-1, 1, 1);
			//newS282Mesh.UploadMeshData(true);
			filter.sharedMesh = newS282Mesh;
			Main.Logger.Log("Loaded new S282 mesh");*/
		}
	}
}
