using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using DV.Simulation.Ports;
using DV.Wheels;
using LocoSim.Implementations;
using UnityEngine;

namespace RearrangedS060.Sim
{
	internal class DuplexTractionPortsFeeder: TractionPortsFeeder
    {
		internal AdhesionController adhesionController;

		public new void Tick(float deltaTime)
		{
			float speedMs = car.GetForwardSpeed();
			if (Mathf.Abs(speedMs) < 0.001f)
			{
				speedMs = 0f;
			}
			float wheelRpm = speedMs * speedMsToWheelRpmConst;
			if (adhesionController != null)
			{
				if (adhesionController.wheelSlide > 0f)
				{
					wheelRpm = Mathf.Lerp(wheelRpm, 0f, adhesionController.wheelSlide);
				}
				else if (adhesionController.wheelslipController != null && adhesionController.wheelslipController.wheelslip > 0f)
				{
					wheelRpm = Mathf.Lerp(wheelRpm, adhesionController.wheelslipController.orientedMaxWheelslipRpm, adhesionController.wheelslipController.wheelslip);
				}
			}
			base.wheelRpm = Mathf.SmoothDamp(base.wheelRpm, wheelRpm, ref wheelRpmRefVel, 0.2f, float.PositiveInfinity, deltaTime);
			float num3 = Mathf.Abs(base.wheelRpm);
			if (num3 < 0.01f && num3 > 0f && wheelRpm == 0f)
			{
				base.wheelRpm = 0f;
				wheelRpmRefVel = 0f;
			}
			forwardSpeedPort?.ExternalValueUpdate(speedMs);
			wheelRpmPort?.ExternalValueUpdate(base.wheelRpm);
			wheelSpeedKmhPort?.ExternalValueUpdate(base.wheelRpm * wheelRpmToSpeedKmhConst);
		}
	}
}
