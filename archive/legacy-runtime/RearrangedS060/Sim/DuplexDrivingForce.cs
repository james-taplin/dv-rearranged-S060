using DV.Wheels;
using LocoSim.Attributes;
using LocoSim.Definitions;
using LocoSim.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RearrangedS060.Sim
{
	internal class DuplexDrivingForce : MonoBehaviour
	{
		[PortId(PortValueType.TORQUE, true)]
		public string torqueGeneratedPortId;

		[NonSerialized]
		public float generatedForce;

		private Port torqueGeneratedPort;

		private TrainCar train;

		private float wheelRadius;

		public void Init(TrainCar train, SimulationFlow simFlow)
		{
			this.train = train;
			wheelRadius = train.carLivery.parentType.wheelRadius;
			if (!simFlow.TryGetPort(torqueGeneratedPortId, out torqueGeneratedPort))
			{
				Debug.LogError("[" + base.gameObject.GetPath() + "]: DrivingForce Sim won't apply any force.", this);
				base.enabled = false;
			}
		}

		private void FixedUpdate()
		{
			generatedForce = torqueGeneratedPort.Value / wheelRadius;
			float forceGenerated = generatedForce;
			DuplexAdhesionController adhesionController = train.GetComponent<DuplexSimController>().adhesionController;
			bool flag = false;
			if (adhesionController != null)
			{
				if (adhesionController.wheelSlide > 0f)
				{
					forceGenerated = 0f;
				}
				else if (adhesionController.wheelslipController != null
					&& adhesionController.wheelslipController.wheelslip > 0f)
				{
					flag = true;
					float totalBrakingForce = (
						(!train.derailed)
						? (train.Bogies[0].brakingForce * (float)train.Bogies.Length)
						: (train.brakeSystem.brakingFactor * train.Bogies[0].maxBrakingForcePerKg * train.massController.TotalBogiesMass));
					forceGenerated = Mathf.Clamp(
						forceGenerated,
						0f - adhesionController.wheelslipController.TotalForceLimit,
						adhesionController.wheelslipController.TotalForceLimit);
				}
			}
			if (forceGenerated == 0f)
			{
				return;
			}
			if (!train.derailed)
			{
				if (train.isEligibleForSleep)
				{
					train.ForceOptimizationState(sleep: false);
				}
				float generatedForcePerBogie = forceGenerated / (float)train.Bogies.Length;
				Bogie[] bogies = train.Bogies;
				foreach (Bogie bogie in bogies)
				{
					if (bogie.enabled)
					{
						bogie.rb.AddForce(bogie.transform.forward * generatedForcePerBogie);
					}
				}
			}
			else if (train.groundFriction.IsGrounded)
			{
				if (train.isEligibleForSleep)
				{
					train.ForceOptimizationState(sleep: false);
				}
				float num4 = 0f;
				if (!flag)
				{
					num4 = train.brakeSystem.brakingFactor * train.Bogies[0].maxBrakingForcePerKg * train.massController.TotalBogiesMass;
				}
				forceGenerated = Mathf.Sign(forceGenerated) * Mathf.Clamp(Mathf.Abs(forceGenerated) - num4, 0f, float.PositiveInfinity);
				train.rb.AddForce(train.transform.forward * forceGenerated);
			}
		}
	}
}
