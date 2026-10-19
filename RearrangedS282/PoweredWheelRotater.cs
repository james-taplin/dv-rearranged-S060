using UnityEngine;

namespace RearrangedS282
{
	// The native Animator drives the rods and original wheels. Added fifth/sixth
	// wheels must copy its completed pose, not the previous frame's Update pose.
	internal class PoweredWheelRotater : MonoBehaviour
	{
		//Wheel whose position we'll match
		private GameObject coupledWheel;

		public void Init(GameObject coupledWheel)
		{
			this.coupledWheel = coupledWheel;
		}

		public void LateUpdate()
		{
			if (coupledWheel != null)
			{
				transform.localRotation = coupledWheel.transform.localRotation;
			}
		}

	}
}
