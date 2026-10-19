using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using DV.RemoteControls;
using DV.Wheels;
using DV.Simulation.Cars;
using DV.ThingTypes;
using LocoSim.Implementations;
using UnityEngine;
using static Oculus.Avatar.CAPI;
using DV.ModularAudioCar;
using JetBrains.Annotations;
using VLB;
using DV.Damage;
using LocoSim.Definitions;


namespace RearrangedS282
{
	public class Spawner
	{

		//This patch runs after a train car spawns in.
		//It adds the WheelRearranger code to each S282A.
		[HarmonyPatch(typeof(TrainCar), nameof(TrainCar.Start))]
		class RandomizeWA
		{
			[HarmonyAfter(new string[] {"Gauge", "LocoMeshSplitter"})]
			static void Postfix(ref TrainCar __instance)
			{
				if (__instance is not null
					&& __instance.carType == TrainCarType.LocoSteamHeavy)
				{
					__instance.gameObject.AddComponent<WheelRearranger>();
				}
			}
		}

		[HarmonyPatch(typeof(TrainCar), "Start")]
		class YourPatchName
		{
			[HarmonyAfter(new string[] { "LocoMeshSplitter" })]
			static void Postfix(TrainCar __instance)
			{
				//Your code here
			}
		}

		/*
		 * If a ten-coupled locomotive explodes, add the fifth drive axle and the extra
		 * side rod to the model.
		 * We can't do this beforehand because the exploded locomotive prefab hasn't
		 * been spawned in yet, so we need to alter it as the exploded locomotive body
		 * prefab is spawned in.
		 */
		//[HarmonyPatch(typeof(ExplosionModelHandler), nameof(ExplosionModelHandler.HandleExplosionModelChange))]
		class ExplodeDriveAxles
		{
			static void Postfix(ref ExplosionModelHandler __instance)
			{
				Transform loco = __instance.transform;
				TrainCar locoTrainCar = loco.GetComponent<TrainCar>();
				if (locoTrainCar is null || locoTrainCar.carType != TrainCarType.LocoSteamHeavy)
				{//only continue if we're working with an S282
					//Main.Logger.Error("Tried to explode something that wasn't an S282");
					return;
				}
				Main.Logger.Log("Exploding an S282");

				//copy unexplodedMovingParts over
				Transform unexplodedMovingParts = loco.Find("LocoS282A_Body/MovingParts_LOD0");
				Transform oldExplodedMovingParts = loco.Find("LocoS282AExploded_Body(Clone)/MovingParts_LOD0");
				Transform newMovingParts = loco.Find("LocoS282AExploded_Body(Clone)/MovingParts_LOD0(Clone)");

				//get exploded material. Doesn't matter from what part, they all have the same texture
				var explodedMaterial = oldExplodedMovingParts
					.Find("DriveMechanism L/s282_wheels_driving_1")
					.GetComponent<MeshRenderer>()
					.material;

				//replace all textures in newMovingParts with exploded texture
				//This code was heavily inspired by code from Skin Manager (https://github.com/derail-valley-modding/skin-manager)
				foreach (var renderer in newMovingParts.GetComponentsInChildren<MeshRenderer>(true))
				{
					if (renderer.material is null)
						continue;

					//apply exploded material to renderer
					renderer.material = explodedMaterial;
				}

				//hide siderods if they were already hidden
				Transform oldLeftSideRod = unexplodedMovingParts.Find("DriveMechanism L/s282_mech_wheels_connect");
				Transform oldRightSideRod = unexplodedMovingParts.Find("DriveMechanism R/s282_mech_wheels_connect");
				Transform newLeftSideRod = newMovingParts.Find("DriveMechanism L/s282_mech_wheels_connect");
				Transform newRightSideRod = newMovingParts.Find("DriveMechanism R/s282_mech_wheels_connect");
				newLeftSideRod.GetComponent<MeshRenderer>().enabled = oldLeftSideRod.GetComponent<MeshRenderer>().enabled;
				newRightSideRod.GetComponent<MeshRenderer>().enabled = oldRightSideRod.GetComponent<MeshRenderer>().enabled;
				newLeftSideRod.Find("s282_mech_wheels_connect_LOD1").gameObject.SetActive(false);
				newRightSideRod.Find("s282_mech_wheels_connect_LOD1").gameObject.SetActive(false);

				//hide some LODs to get rid of z-fighting on the drivers
				loco.Find("Axle_F/bogie_car/[axle] 1/axleF_modelLOD1").gameObject.SetActive(false);
				loco.Find("Axle_R/bogie_car/[axle] 1/axleR_modelLOD1").gameObject.SetActive(false);

				Transform firstLeftDriveWheel = newMovingParts.Find("DriveMechanism L/s282_wheels_driving_1");
				Transform secondLeftDriveWheel = newMovingParts.Find("DriveMechanism L/s282_wheels_driving_2");
				Transform thirdLeftDriveWheel = newMovingParts.Find("DriveMechanism L/s282_wheels_driving_3");
				Transform fourthLeftDriveWheel = newMovingParts.Find("DriveMechanism L/s282_wheels_driving_4");
				Transform fifthLeftDriveWheel = newMovingParts.Find("DriveMechanism L/s282_wheels_driving_5");
				Transform sixthLeftDriveWheel = newMovingParts.Find("DriveMechanism L/s282_wheels_driving_6");
				Transform firstRightDriveWheel = newMovingParts.Find("DriveMechanism R/s282_wheels_driving_1");
				Transform secondRightDriveWheel = newMovingParts.Find("DriveMechanism R/s282_wheels_driving_2");
				Transform thirdRightDriveWheel = newMovingParts.Find("DriveMechanism R/s282_wheels_driving_3");
				Transform fourthRightDriveWheel = newMovingParts.Find("DriveMechanism R/s282_wheels_driving_4");
				Transform fifthRightDriveWheel = newMovingParts.Find("DriveMechanism R/s282_wheels_driving_5");
				Transform sixthRightDriveWheel = newMovingParts.Find("DriveMechanism R/s282_wheels_driving_6");

				//Hide more LODs
				firstLeftDriveWheel.GetChild(0).gameObject.SetActive(false);
				secondLeftDriveWheel.GetChild(0).gameObject.SetActive(false);
				thirdLeftDriveWheel.GetChild(0).gameObject.SetActive(false);
				fourthLeftDriveWheel.GetChild(0).gameObject.SetActive(false);
				fifthLeftDriveWheel.GetChild(0).gameObject.SetActive(false);
				sixthLeftDriveWheel.GetChild(0).gameObject.SetActive(false);
				firstRightDriveWheel.GetChild(0).gameObject.SetActive(false);
				secondRightDriveWheel.GetChild(0).gameObject.SetActive(false);
				thirdRightDriveWheel.GetChild(0).gameObject.SetActive(false);
				fourthRightDriveWheel.GetChild(0).gameObject.SetActive(false);
				fifthRightDriveWheel.GetChild(0).gameObject.SetActive(false);
				sixthRightDriveWheel.GetChild(0).gameObject.SetActive(false);

				//rotate 5th and 6th drive wheels to be in the right position
				fifthLeftDriveWheel.gameObject.GetComponent<PoweredWheelRotater>().Init(fourthLeftDriveWheel.gameObject);
				fifthRightDriveWheel.gameObject.GetComponent<PoweredWheelRotater>().Init(fourthRightDriveWheel.gameObject);
				sixthLeftDriveWheel.gameObject.GetComponent<PoweredWheelRotater>().Init(fourthLeftDriveWheel.gameObject);
				sixthRightDriveWheel.gameObject.GetComponent<PoweredWheelRotater>().Init(fourthRightDriveWheel.gameObject);

				//offset the right side drivetrain
				loco.Find("[wheel rotation]").GetComponent<PoweredWheelRotationViaAnimation>().animatorSetups[0].startTimeOffset = 0.75f;

				//copy axle supports over
				UnityEngine.Object.Instantiate(loco.Find("LocoS282A_Body/Static_LOD0/s282_wheels_front_support_2").gameObject, loco.Find("LocoS282AExploded_Body(Clone)/Static_LOD0/"));
				UnityEngine.Object.Instantiate(loco.Find("LocoS282A_Body/Static_LOD0/s282_wheels_rear_support_1").gameObject, loco.Find("LocoS282AExploded_Body(Clone)/Static_LOD0/"));
				
				Main.Logger.Log("Loaded new exploded S282 mesh");

				//make exploded drivetrain rotate
				/*if (Main.settings.explodedDrivetrainRotate)
				{
					var drivetrainRotater = loco.Find("[wheel rotation]")
					.GetComponent<PoweredWheelRotationViaAnimation>();
					Animator leftAnimation = newMovingParts.Find("DriveMechanism L")
						.GetComponent<Animator>();
					Animator rightAnimation = newMovingParts.Find("DriveMechanism R")
						.GetComponent<Animator>();
					PoweredWheelRotationViaAnimation.AnimatorStartTimeOffsetPair leftAnimationOffset = new()
					{
						animator = leftAnimation,
						startTimeOffset = 0
					};
					PoweredWheelRotationViaAnimation.AnimatorStartTimeOffsetPair rightAnimationOffset = new()
					{
						animator = rightAnimation,
						startTimeOffset = 0.75f
					};
					drivetrainRotater.animatorSetups.AddItem(leftAnimationOffset);
					drivetrainRotater.animatorSetups.AddItem(rightAnimationOffset);
				}*/
			}
		}

		[HarmonyPatch(typeof(ExplosionModelHandler), nameof(ExplosionModelHandler.RevertToUnexplodedModel))]
		class DeexplodeLocomotive
		{
			static void Postfix(ref ExplosionModelHandler __instance)
			{
				Transform loco = __instance.transform;
				var wheelRearranger = __instance.gameObject.GetOrAddComponent<WheelRearranger>();
				wheelRearranger.SwitchWheelArrangement((int) wheelRearranger.currentWA);
				//We hid these LODs earlier, but now they're useful again
				loco.Find("Axle_F/bogie_car/[axle] 1/axleF_modelLOD1").gameObject.SetActive(true);
				loco.Find("Axle_R/bogie_car/[axle] 1/axleR_modelLOD1").gameObject.SetActive(true);
				UnityEngine.Object.Destroy(loco.Find("LocoS282AExploded_Body(Clone)/MovingParts_LOD0(Clone)").gameObject);
			}
		}
	}
}
