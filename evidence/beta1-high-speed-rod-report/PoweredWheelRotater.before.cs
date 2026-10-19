using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RearrangedS282
{
	//This class matches syncs the rotation of the paired wheel with another wheel.
	//Well, it could work on any GameObject, but I only need to do this for the fifth
	//drive axle on 10-coupled locomotives.
	internal class PoweredWheelRotater : MonoBehaviour
	{
		//Wheel whose position we'll match
		private GameObject coupledWheel;

		public void Init(GameObject coupledWheel)
		{
			this.coupledWheel = coupledWheel;
		}

		public void Update()
		{
			if (coupledWheel is not null)
			{
				this.transform.localEulerAngles = coupledWheel.transform.localEulerAngles;
			}
		}

	}
}
