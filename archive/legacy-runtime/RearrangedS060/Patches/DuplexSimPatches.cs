using DV.Damage;
using DV.ModularAudioCar;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using DV.ThingTypes;
using DV.Wheels;
using HarmonyLib;
using LocoSim.Definitions;
using LocoSim.Implementations;
using RearrangedS060.Sim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RearrangedS060.Patches
{
	internal class DuplexSimPatches
	{

		//This patch runs after an S282A spawns in, but before it is initialized.
		//It changes the S282's simulation to work with duplexes.
		//The idea is that we need a place to modify the SimController before it's initialized.
		[HarmonyPatch(typeof(TrainCar), nameof(TrainCar.Awake))]
		class DuplexSimControllerPatch
		{
			static void Prefix(ref TrainCar __instance)
			{
				//This is why we're patching TrainCar and not SimController.
				//We need to be able to detect if we're an S282A or not.
				if (__instance is null || __instance.carType != TrainCarType.LocoSteamHeavy)
				{
					return;
				}

				//Simulation changes for duplex
				var oldSimCtrlr = __instance.transform.GetComponent<SimController>();
				var oldSim = __instance.transform.Find("[sim]");
				var duplexSim = new GameObject("[sim duplex]").transform;
				duplexSim.parent = __instance.transform;

				//make second steam engine
				var duplexSteamEngine = new GameObject("duplexSteamEngine").transform;
				duplexSteamEngine.parent = oldSim;
				var duplexEngineDef
					= duplexSteamEngine.gameObject.AddComponent<DuplexReciprocatingSteamEngineDefinition>();

				//Get VolumetricEfficiency lookup table from old ReciprocatingSteamEngine
				var oldSteamEngine = oldSim.Find("steamEngine").GetComponent<ReciprocatingSteamEngineDefinition>();
				duplexEngineDef.volumetricEfficiency = oldSteamEngine.volumetricEfficiency;

				//gotta add a MultiplePortSum to collect the two exhausts
				var exhaustCollectorT = new GameObject("duplexExhaustCollector").transform;
				exhaustCollectorT.parent = oldSim;
				var exhaustCollectorDef = exhaustCollectorT.gameObject.AddComponent<MultiplePortSumDefinition>();
				exhaustCollectorDef.inputs = new PortReferenceDefinition[]
				{
				new(PortValueType.MASS_RATE, "EXHAUST_FLOW_1"),
				new(PortValueType.MASS_RATE, "EXHAUST_FLOW_2"),
				};
				exhaustCollectorDef.output = new PortDefinition(PortType.READONLY_OUT, PortValueType.MASS_RATE, "STEAM_FLOW");
				exhaustCollectorDef.ID = "duplexExhaustCollector";

				//gotta add a MultiplePortSum to collect the exhaust chuff events
				var chuffCollectorGO = new GameObject("duplexChuffCollector");
				chuffCollectorGO.transform.parent = oldSim;
				var chuffCollectorDef = chuffCollectorGO.gameObject.AddComponent<MultiplePortSumDefinition>();
				chuffCollectorDef.inputs = new PortReferenceDefinition[]
				{
					new(PortValueType.STATE, "CHUFF_EVENT_1"),
					new(PortValueType.STATE, "CHUFF_EVENT_2"),
				};
				chuffCollectorDef.output = new PortDefinition(PortType.READONLY_OUT, PortValueType.STATE, "CHUFF_EVENT");
				chuffCollectorDef.ID = "duplexChuffCollector";

				//TODO: Should technically add a MultiplePortSum to average the exhaust pressures
				//of the two engines. Although tbh, I'm not sure if this is necessary or not

				//make second set of traction simulation
				var duplexTraction = new GameObject("duplexTraction").transform;
				duplexTraction.parent = oldSim;
				var duplexTractionDef = duplexTraction.gameObject.AddComponent<DuplexTractionDefinition>();

				var duplexTractionPortsFeeder = duplexSim.gameObject.AddComponent<DuplexTractionPortsFeeder>();
				var duplexDrivingForce = duplexSim.gameObject.AddComponent<DrivingForce>();
				var duplexWheelslipCtrlr = duplexSim.gameObject.AddComponent<WheelslipController>();

				duplexTractionPortsFeeder.forwardSpeedPortId = "duplexTraction.FORWARD_SPEED_EXT_IN";
				duplexTractionPortsFeeder.wheelRpmPortId = "duplexTraction.WHEEL_RPM_EXT_IN";
				duplexTractionPortsFeeder.wheelSpeedKmhPortId = "duplexTraction.WHEEL_SPEED_KMH_EXT_IN";
				duplexDrivingForce.torqueGeneratedPortId = "duplexTraction.TORQUE_IN";
				duplexWheelslipCtrlr.numberOfPoweredAxlesPortId = "poweredAxles.NUM";
				duplexWheelslipCtrlr.sandCoefPortId = "sander.SAND_COEF";

				duplexWheelslipCtrlr.wheelslipToAdhesionDrop = oldSimCtrlr.wheelslipController.wheelslipToAdhesionDrop;

				//add SimComponentDefinitions in the right place in the executionOrder array
				var executionOrderList = new List<SimComponentDefinition>(oldSimCtrlr.connectionsDefinition.executionOrder);
				for (int i = 0; i < executionOrderList.Count; i++)
				{
					if (executionOrderList[i].GetType() == typeof(ReciprocatingSteamEngineDefinition))
					{
						executionOrderList.Insert(i + 1, duplexEngineDef);
						executionOrderList.Insert(i + 2, exhaustCollectorDef);
						executionOrderList.Insert(i + 3, chuffCollectorDef);
					}
					if (executionOrderList[i].GetType() == typeof(TractionDefinition))
					{
						executionOrderList.Insert(i + 1, duplexTractionDef);
					}
				}
				oldSimCtrlr.connectionsDefinition.executionOrder = executionOrderList.ToArray();
				/*duplexSimCtrlr.connectionsDefinition.executionOrder
					= new SimComponentDefinition[] { duplexEngineDef, duplexTractionDef};*/

				//add connection between duplexSteamEngine and duplexTraction
				Connection connection = new Connection("duplexSteamEngine.TORQUE_OUT", "duplexTraction.TORQUE_IN");
				oldSimCtrlr.connectionsDefinition.connections
					= oldSimCtrlr.connectionsDefinition.connections
					.Append(connection)
					.ToArray();

				//Make the exhaust connect to the exhaust header instead of just the front cylinders
				//Lubricator should also run off the front engine, since that's where it is
				for (int i = 0; i < oldSimCtrlr.connectionsDefinition.portReferenceConnections.Length; i++)
				{
					PortReferenceConnection pfc = oldSimCtrlr.connectionsDefinition.portReferenceConnections[i];
					if (pfc.portReferenceId == "steamConsumptionCalculator.ENGINE")
					{
						pfc.portId = "duplexExhaustCollector.STEAM_FLOW";
					}
					else if (pfc.portReferenceId == "exhaust.EXHAUST_FLOW")
					{
						pfc.portId = "duplexExhaustCollector.STEAM_FLOW";
					}
					else if (pfc.portReferenceId == "lubricator.WHEEL_RPM")
					{
						pfc.portId = "duplexTraction.WHEEL_RPM_EXT_IN";
					}
				}

				//expand steamConsumptionCalculator to include the second set of cylinder cocks
				var steamConsumptionCalc =
					oldSim.Find("steamConsumptionCalculator").GetComponent<MultiplePortSumDefinition>();
				steamConsumptionCalc.inputs = steamConsumptionCalc.inputs
					.Append(new PortReferenceDefinition(PortValueType.MASS_RATE, "CYLINDER_DUMP_2")).ToArray();

				//add port reference connections
				PortReferenceConnection[] newPortReferenceConnections = {
					//new("firebox.FORWARD_SPEED", "duplexTraction.FORWARD_SPEED_EXT_IN"), //FIX?
					new("steamConsumptionCalculator.CYLINDER_DUMP_2", "duplexSteamEngine.DUMPED_FLOW"),
					new("duplexSteamEngine.REVERSER_CONTROL", "reverser.REVERSER"),
					new("duplexSteamEngine.CYLINDER_COCK_CONTROL", "cylinderCock.EXT_IN"),
					new("duplexSteamEngine.STEAM_CHEST_PRESSURE", "throttleCalculator.STEAM_CHEST_PRESSURE"),
					new("duplexSteamEngine.STEAM_CHEST_TEMPERATURE", "firebox.TEMPERATURE"),
					new("duplexSteamEngine.STEAM_QUALITY", "boiler.OUTLET_STEAM_QUALITY"),
					new("duplexSteamEngine.CRANK_RPM", "duplexTraction.WHEEL_RPM_EXT_IN"),
					new("duplexSteamEngine.LUBRICATION_NORMALIZED", "lubricator.LUBRICATION_NORMALIZED"),
					new("duplexExhaustCollector.EXHAUST_FLOW_1", "steamEngine.STEAM_FLOW"),
					new("duplexExhaustCollector.EXHAUST_FLOW_2", "duplexSteamEngine.STEAM_FLOW"),
					new("duplexChuffCollector.CHUFF_EVENT_1", "steamEngine.CHUFF_EVENT"),
					new("duplexChuffCollector.CHUFF_EVENT_2", "duplexSteamEngine.CHUFF_EVENT"),
				};
				oldSimCtrlr.connectionsDefinition.portReferenceConnections
					= oldSimCtrlr.connectionsDefinition.portReferenceConnections
					.Concat(newPortReferenceConnections)
					.ToArray();
			}
		}

		[HarmonyPatch(typeof(AudioClipSimReadersController), nameof(AudioClipSimReadersController.Init))]
		class DuplexChuffSoundPatch
		{
			static void Prefix(ref AudioClipSimReadersController __instance,
				TrainCar car, SimulationFlow simFlow)
			{
				if (car == null || car.carType != TrainCarType.LocoSteamHeavy)
				{
					return;
				}
				Main.Logger.Log("Adding duplex chuff sounds to an S282A");
				GameObject oldSteamChuff = __instance.transform.Find("SteamChuff").gameObject;
				GameObject duplexSteamChuff = UnityEngine.Object.Instantiate(oldSteamChuff, __instance.transform);
				duplexSteamChuff.name = "DuplexSteamChuff";
				duplexSteamChuff.transform.localPosition = oldSteamChuff.transform.localPosition;
				duplexSteamChuff.transform.localEulerAngles = oldSteamChuff.transform.localEulerAngles;
				var duplexChuffReader = duplexSteamChuff.GetComponent<ChuffClipsSimReader>();
				duplexChuffReader.chuffEventPortId = "duplexSteamEngine.CHUFF_EVENT";
				duplexChuffReader.exhaustPressurePortId = "duplexSteamEngine.EXHAUST_PRESSURE";
				duplexChuffReader.chuffFrequencyPortId = "duplexSteamEngine.CHUFF_FREQUENCY";
				duplexChuffReader.cylinderWaterNormalizedPortId = "duplexSteamEngine.WATER_IN_CYLINDERS_NORMALIZED";

				__instance.entries = __instance.entries.Append(duplexChuffReader).ToArray();
			}
		}

		[HarmonyPatch(typeof(LayeredAudioSimReadersController), nameof(LayeredAudioSimReadersController.Init))]
		class DuplexCockSoundPatch
		{
			static void Prefix(ref LayeredAudioSimReadersController __instance,
				TrainCar car, SimulationFlow simFlow)
			{
				if (car == null || car.carType != TrainCarType.LocoSteamHeavy)
				{
					return;
				}
				Main.Logger.Log("Adding duplex cock sounds to an S282A");
				GameObject oldCock = __instance.transform.Find("CylinderCock").gameObject;
				GameObject duplexCock = UnityEngine.Object.Instantiate(oldCock, __instance.transform);
				duplexCock.name = "DuplexCylinderCock";
				duplexCock.transform.localPosition = oldCock.transform.localPosition + new Vector3(0, 0, -4.65f);
				duplexCock.transform.localEulerAngles = oldCock.transform.localEulerAngles;
				var duplexCockReader = duplexCock.GetComponent<CylinderCockLayeredPortReader>();
				duplexCockReader.crankRotationPortId = "duplexSteamEngine.CRANK_ROTATION";
				duplexCockReader.cylindersSteamInjectionPortId = "duplexSteamEngine.CYLINDERS_STEAM_INJECTION";
				duplexCockReader.cylinderCockFlowNormalizedPortId = "duplexSteamEngine.CYLINDER_COCK_FLOW_NORMALIZED";

				__instance.entries = __instance.entries.Append(duplexCockReader).ToArray();
			}
		}

		[HarmonyPatch(typeof(SimController), nameof(SimController.Initialize))]
		class DuplexCockParticlePatch
		{
			static void Prefix(ref SimController __instance,
				TrainCar trainCar, DamageController damageController)
			{
				if (trainCar == null
					|| trainCar.carType != TrainCarType.LocoSteamHeavy)
				{
					return;
				}

				if (__instance.particlesController == null)
				{
					return;
				}
				ParticlesPortReadersController particlesCtrlr = __instance.particlesController;
				GameObject particlesGO = particlesCtrlr.gameObject;
				GameObject cylCockWaterDripGO = particlesGO.transform.Find("CylCockWaterDrip").gameObject;
				GameObject cylCockSteamGO = particlesGO.transform.Find("CylCockSteam").gameObject;
				GameObject steamSmokeGO = particlesGO.transform.Find("SteamSmoke").gameObject;
				GameObject wheelSparksGO = particlesGO.transform.Find("[wheel sparks]").gameObject;

				GameObject duplexCylCockWaterDripGO = UnityEngine.Object.Instantiate(cylCockWaterDripGO, particlesGO.transform);
				GameObject duplexCylCockSteamGO = UnityEngine.Object.Instantiate(cylCockSteamGO, particlesGO.transform);
				GameObject duplexWheelSparksGO = UnityEngine.Object.Instantiate(wheelSparksGO, particlesGO.transform);

				duplexCylCockWaterDripGO.name = "DuplexCylCockWaterDrip";
				duplexCylCockWaterDripGO.transform.localPosition = new Vector3(0, 0.09f, -4.65f);
				duplexCylCockSteamGO.name = "DuplexCylCockSteam";
				duplexCylCockSteamGO.transform.localPosition = new Vector3(0, 0.09f, -4.65f);
				duplexWheelSparksGO.name = "[duplex wheel sparks]";
				duplexWheelSparksGO.transform.localPosition += new Vector3(0, 0.05f, -4.65f);

				//redirect the rear cylider cocks to look at the correct ports
				var duplexCylCockSteamPortReader = duplexCylCockSteamGO.GetComponent<CylinderCockParticlePortReader>();
				duplexCylCockSteamPortReader.crankRotationPortId = "duplexSteamEngine.CRANK_ROTATION";
				duplexCylCockSteamPortReader.cylindersSteamInjectionPortId = "duplexSteamEngine.CYLINDERS_STEAM_INJECTION";
				duplexCylCockSteamPortReader.cylinderCockFlowNormalizedPortId = "duplexSteamEngine.CYLINDER_COCK_FLOW_NORMALIZED";
				duplexCylCockSteamPortReader.forwardSpeedPortId = "duplexTraction.FORWARD_SPEED_EXT_IN";

				var cylCockSteamFR = duplexCylCockSteamGO.transform.Find("CylCockSteam FR").gameObject;
				var cylCockSteamRR = duplexCylCockSteamGO.transform.Find("CylCockSteam RR").gameObject;
				var cylCockSteamFL = duplexCylCockSteamGO.transform.Find("CylCockSteam FL").gameObject;
				var cylCockSteamRL = duplexCylCockSteamGO.transform.Find("CylCockSteam RL").gameObject;

				duplexCylCockSteamPortReader.cylinderSetups[0].frontParticlesParent = cylCockSteamFR;
				duplexCylCockSteamPortReader.cylinderSetups[0].rearParticlesParent = cylCockSteamRR;
				duplexCylCockSteamPortReader.cylinderSetups[1].frontParticlesParent = cylCockSteamFL;
				duplexCylCockSteamPortReader.cylinderSetups[1].rearParticlesParent = cylCockSteamRL;

				particlesCtrlr.entries = particlesCtrlr.entries.Append(duplexCylCockSteamPortReader).ToArray();

				//while we're here, add CylCockWaterDrip for the rear engine as well
				var waterDripRRPortReader = new ParticlesPortReadersController.ParticlePortReader();
				var waterDripRLPortReader = new ParticlesPortReadersController.ParticlePortReader();
				waterDripRRPortReader.particlesParent
					= duplexCylCockWaterDripGO.transform.Find("CylCockWaterDrip R").GetChild(0).gameObject;
				waterDripRLPortReader.particlesParent
					= duplexCylCockWaterDripGO.transform.Find("CylCockWaterDrip L").GetChild(0).gameObject;

				//This mess grabs the old PortReader and gets all necessary info from that into the new PortReaders.
                ParticlesPortReadersController.ParticlePortReader oldCylCockParticlePortReader;
				foreach ( var portReader in particlesCtrlr.particlePortReaders)
				{
					if (!GameObject.ReferenceEquals(
							portReader.particlesParent.transform.parent.parent.gameObject,
							cylCockWaterDripGO))
					{
						continue;
					}

					Main.Logger.Log("Adding duplex cylinder cock water drips to an S282A");

					oldCylCockParticlePortReader = portReader;

					var ppudL1 = new ParticlesPortReadersController.ParticlePortReader.PortParticleUpdateDefinition();
					var ppudL2 = new ParticlesPortReadersController.ParticlePortReader.PortParticleUpdateDefinition();
					var ppudR1 = new ParticlesPortReadersController.ParticlePortReader.PortParticleUpdateDefinition();
					var ppudR2 = new ParticlesPortReadersController.ParticlePortReader.PortParticleUpdateDefinition();
					ppudL1.portId = "duplexSteamEngine.WATER_IN_CYLINDERS_NORMALIZED";
					ppudL1.inputModifier = oldCylCockParticlePortReader.particleUpdaters[0].inputModifier;
					ppudL1.propertiesToUpdate = oldCylCockParticlePortReader.particleUpdaters[0].propertiesToUpdate;
					ppudL2.portId = "cylinderCock.EXT_IN";
					ppudL2.inputModifier = oldCylCockParticlePortReader.particleUpdaters[1].inputModifier;
					ppudL2.propertiesToUpdate = oldCylCockParticlePortReader.particleUpdaters[1].propertiesToUpdate;
					ppudR1.portId = "duplexSteamEngine.WATER_IN_CYLINDERS_NORMALIZED";
					ppudR1.inputModifier = oldCylCockParticlePortReader.particleUpdaters[0].inputModifier;
					ppudR1.propertiesToUpdate = oldCylCockParticlePortReader.particleUpdaters[0].propertiesToUpdate;
					ppudR2.portId = "cylinderCock.EXT_IN";
					ppudR2.inputModifier = oldCylCockParticlePortReader.particleUpdaters[1].inputModifier;
					ppudR2.propertiesToUpdate = oldCylCockParticlePortReader.particleUpdaters[1].propertiesToUpdate;
					waterDripRLPortReader.particleUpdaters
						= new List<ParticlesPortReadersController.ParticlePortReader.PortParticleUpdateDefinition>();
					waterDripRRPortReader.particleUpdaters
						= new List<ParticlesPortReadersController.ParticlePortReader.PortParticleUpdateDefinition>();
					waterDripRLPortReader.particleUpdaters.Add(ppudL1);
					waterDripRLPortReader.particleUpdaters.Add(ppudL2);
					waterDripRRPortReader.particleUpdaters.Add(ppudR1);
					waterDripRRPortReader.particleUpdaters.Add(ppudR2);
					particlesCtrlr.particlePortReaders.Add(waterDripRLPortReader);
					particlesCtrlr.particlePortReaders.Add(waterDripRRPortReader);
					break;

				}

				//while we're here, fix the port names in the exhaust port reader
				var exhaustParticlePortReader = steamSmokeGO.GetComponent<SteamSmokeParticlePortReader>();
				exhaustParticlePortReader.chuffEventPortId = "duplexChuffCollector.CHUFF_EVENT";
				//exhaustParticlePortReader.exhaustPressurePortId = "";

				//while we're here, also set up the rear wheel sparks.
				//Altfuture's naming for wheel sparks sucks. I refuse to use their names for these.
				var spark1L = duplexWheelSparksGO.transform.Find("spark7");
				var spark2L = duplexWheelSparksGO.transform.Find("spark5");
				var spark3L = duplexWheelSparksGO.transform.Find("spark3");
				var spark4L = duplexWheelSparksGO.transform.Find("spark1");
				var spark1R = duplexWheelSparksGO.transform.Find("spark8");
				var spark2R = duplexWheelSparksGO.transform.Find("spark6");
				var spark3R = duplexWheelSparksGO.transform.Find("spark4");
				var spark4R = duplexWheelSparksGO.transform.Find("spark2");

				var duplexWheelslipCtrlr = duplexWheelSparksGO.GetComponent<WheelslipSparksController>();
				duplexWheelslipCtrlr.wheelSparks[0].sparksLeftAnchor = spark1L;
				duplexWheelslipCtrlr.wheelSparks[0].sparksRightAnchor = spark1R;
				duplexWheelslipCtrlr.wheelSparks[1].sparksLeftAnchor = spark2L;
				duplexWheelslipCtrlr.wheelSparks[1].sparksRightAnchor = spark2R;
				duplexWheelslipCtrlr.wheelSparks[2].sparksLeftAnchor = spark3L;
				duplexWheelslipCtrlr.wheelSparks[2].sparksRightAnchor = spark3R;
				duplexWheelslipCtrlr.wheelSparks[3].sparksLeftAnchor = spark4L;
				duplexWheelslipCtrlr.wheelSparks[3].sparksRightAnchor = spark4R;
				duplexWheelslipCtrlr.wheelSparks[0].Init();
				duplexWheelslipCtrlr.wheelSparks[1].Init();
				duplexWheelslipCtrlr.wheelSparks[2].Init();
				duplexWheelslipCtrlr.wheelSparks[3].Init();
			}
		}
	}
}
