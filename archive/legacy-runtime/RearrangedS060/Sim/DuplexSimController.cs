using DV.Damage;
using DV.PitStops;
using DV.Rain;
using DV.RemoteControls;
using DV.ServicePenalty;
using DV.Simulation.Brake;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using DV.Simulation.Ports;
using DV.Utils;
using DV.Wheels;
using DV;
using LocoSim.Definitions;
using LocoSim.Implementations.Wheels;
using LocoSim.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RearrangedS060.Sim
{
	//heavily inspired by SimController class in game files
	internal class DuplexSimController : MonoBehaviour, ISimulationFlowProvider
	{
		[Header("required")]
		public SimConnectionDefinition connectionsDefinition;

		[Header("optional")]
		public PoweredWheelsManager poweredWheels;

		public ParticlesPortReadersController particlesController;

		public DuplexTractionPortsFeeder tractionPortsFeeder;

		public DrivingForce drivingForce;

		public WheelslipController wheelslipController;

		public DuplexAdhesionController adhesionController;

		public ASimInitializedController[] otherSimControllers;

		[Space]
		public float simTimeMultiplier = 1f;

		[Header("Must be infinite when forwarding resources between sims")]
		public float maxTickTime = float.PositiveInfinity;

		//This is a reference to the vanilla simFlow. This class uses it for reference,
		//but does not call simFlow.Tick()
		[NonSerialized]
		public SimulationFlow simFlow;

		[NonSerialized]
		public ResourceContainerController resourceContainerController;

		private SimulatedCarDebtTracker debt;

		private TrainCar train;

		private List<ASimInitializedController> otherSimControllersWithTick = new List<ASimInitializedController>();

		public SimulationFlow SimulationFlow => simFlow;

		internal void OnValidate()
		{
			if (connectionsDefinition == null)
			{
				connectionsDefinition = transform.GetComponentInChildren<SimConnectionDefinition>();
			}
			/*if (poweredWheels == null)
			{
				poweredWheels = GetComponentInChildren<PoweredWheelsManager>();
			}*/
			
			/*if (particlesController == null)
			{
				particlesController = GetComponentInChildren<ParticlesPortReadersController>();
			}*/
			if (tractionPortsFeeder == null)
			{
				tractionPortsFeeder = transform.Find("[sim duplex]").GetComponent<DuplexTractionPortsFeeder>();
			}
			if (drivingForce == null)
			{
				drivingForce = transform.Find("[sim duplex]").GetComponent<DrivingForce>();
			}
			if (wheelslipController == null)
			{
				wheelslipController = transform.Find("[sim duplex]").GetComponent<WheelslipController>();
			}
			if (adhesionController == null && train != null)
			{
				adhesionController = new DuplexAdhesionController(train);
			}

			/*ASimInitializedController[] componentsInChildren = GetComponentsInChildren<ASimInitializedController>();
			if (otherSimControllers == null || otherSimControllers.Any((ASimInitializedController c) => c == null) || otherSimControllers.Length != componentsInChildren.Length)
			{
				otherSimControllers = componentsInChildren;
			}*/
			if (connectionsDefinition == null)
			{
				Debug.LogError("Missing SimConnectionDefinition, SimController can't extract it.");
			}
		}

		public void Initialize(TrainCar trainCar, DamageController damageController)
		{
			train = trainCar;
			if (train == null)
			{
				base.enabled = false;
				Debug.LogError("Unexpected state: SimController has no train! Can't function properly", this);
				return;
			}
			if (simFlow == null)
			{
				simFlow = new SimulationFlow(connectionsDefinition, Globals.G.GameParams.SimParams);
			}
			base.gameObject.AddComponent<SimCarStateSave>().Initialize(simFlow, damageController, train.muModule);
			resourceContainerController = new ResourceContainerController(simFlow);
			/*
			if (particlesController != null)
			{
				particlesController.Init(simFlow);
			}*/
			if (tractionPortsFeeder != null)
			{
				tractionPortsFeeder.Init(train, simFlow);
			}
			if (drivingForce != null)
			{
				drivingForce.Init(train, simFlow);
			}
			if (wheelslipController != null)
			{
				wheelslipController.Init(train, simFlow, drivingForce);
			}
			if (adhesionController == null)
			{
				adhesionController = new DuplexAdhesionController(train);
			}
			/*
			if (otherSimControllers != null)
			{
				ASimInitializedController[] array = otherSimControllers;
				foreach (ASimInitializedController aSimInitializedController in array)
				{
					aSimInitializedController.Init(train, simFlow);
					if (aSimInitializedController.ExternalTick)
					{
						otherSimControllersWithTick.Add(aSimInitializedController);
					}
				}
			}
			train.LogicCarInitialized += OnLogicCarInitialized;*/
		}

		/*private void OnLogicCarInitialized()
		{
			train.LogicCarInitialized -= OnLogicCarInitialized;
			DamageController component = GetComponent<DamageController>();
			base.gameObject.AddComponent<SimulatedCarPitStopParameters>().Initialize(resourceContainerController.resourceContainers, component);
			if (!train.playerSpawnedCar)
			{
				debt = new SimulatedCarDebtTracker(component, resourceContainerController, environmentDamageController, simFlow, train.ID, train.carType);
				SingletonBehaviour<LocoDebtController>.Instance.RegisterLocoDebtTracker(debt);
			}
			train.OnDestroyCar += OnCarDestroyed;
		}*/

		private void OnCarDestroyed(TrainCar train)
		{
			train.OnDestroyCar -= OnCarDestroyed;
			if (!train.playerSpawnedCar)
			{
				SingletonBehaviour<LocoDebtController>.Instance.StageLocoDebtOnLocoDestroy(debt);
			}
		}

		private void Update()
		{
			if (Time.deltaTime <= float.Epsilon || Time.timeScale <= float.Epsilon || SingletonBehaviour<PausePhysicsHandler>.Instance.PhysicsHandlingInProcess)
			{
				return;
			}
			resourceContainerController.UpdateTimer();
			float deltaTime = Time.deltaTime * simTimeMultiplier;
			if (tractionPortsFeeder != null)
			{
				tractionPortsFeeder.Tick(deltaTime);
			}
			if (adhesionController != null
				&& (!train.isStationary || !adhesionController.RegularState))
			{
				adhesionController.UpdateAdhesion(deltaTime);
			}

			foreach (ASimInitializedController item in otherSimControllersWithTick)
			{
				item.Tick(deltaTime);
			}
			//Because this simFlow is a copy of the one in the vanilla SimController, it's already updated there:
			//We don't need to do it again.
			/*if (Time.deltaTime <= maxTickTime)
			{
				simFlow.Tick(deltaTime);
				return;
			}
			int numberOfExtraTicks = Mathf.CeilToInt(Time.deltaTime / maxTickTime);
			float delta = deltaTime / (float)numberOfExtraTicks;
			for (int i = 0; i < numberOfExtraTicks; i++)
			{
				simFlow.Tick(delta);
			}*/
		}
	}
}
