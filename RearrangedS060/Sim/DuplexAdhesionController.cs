using DV.Simulation.Cars;
using DV.Wheels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RearrangedS060.Sim
{
	internal class DuplexAdhesionController : AdhesionController
	{
		public DuplexAdhesionController(TrainCar car) : base(car)
		{
			wheelslipController = car.transform.Find("[sim duplex]").GetComponent<WheelslipController>();

			//Each AdhesionController sees all the adhesive weight and all the drive axles on the locomotive;
			//i.e., on the 2-4-4-2, each AdhesionController sees ~130t and 4 drive axles, but the real amount
			//of power.
			//
			//So, if we left these values in their default state, the locomotive would have
			//twice the amount of grip that it should. (effectively, twice the power-to-weight ratio.)
			//By changing the wheelslip and wheel slide coefficients, we can correct this.
			//
			//We multiply by 0.55 instead of 0.5 because I want the front engine to slip slightly
			//more than the rear (what would be the point of simulating this stuff separately
			//if they just wheelslip at the exact same time anyway?)
			//
			//It's not enough to just change it for the new engine, it also needs to change
			//for the other engine as well. So the other AdhesionController gets changed
			//in WheelArranger.
			wheelslipFrictionCoef = car.carLivery.parentType.WheelslipFrictionCoef * 0.55f;
			wheelSlideFrictionCoef = car.carLivery.parentType.WheelSlideFrictionCoef * 1.1f;
		}
	}
}
