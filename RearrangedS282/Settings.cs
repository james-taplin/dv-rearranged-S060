using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityModManagerNet;

namespace RearrangedS282
{
	[Serializable]
	public class Settings : UnityModManager.ModSettings, IDrawable
	{
		public enum X82Options
		{
			[Description("Use vanilla-style big trailing wheels")]
			vanilla,

			[Description("Use smaller trailing wheels")]
			small_wheels,

			[Description("Use smaller wheels, but slightly further forward")]
			small_wheels_alternate
		}

		public enum X102Options
		{
			[Description("Use vanilla-style big trailing wheels")]
			vanilla,

			[Description("Use smaller trailing wheels")]
			small_wheels,
		}

		private const int HEIGHT = 12;

		//[Header("Derails")]
		//[Draw("Wheel arrangement sensitivity to derailing:",Max)]

		[Header("Reapply wheel arrangement to update")]
		[Space(5)]
		[Header("Trailing truck style")]
		[Draw("4-4-2 trailing truck style:", DrawType.ToggleGroup)] public X82Options x42Options = X82Options.vanilla;
		[Draw("4-6-2 trailing truck style:", DrawType.ToggleGroup)] public X102Options x62Options = X102Options.vanilla;
		[Draw("x-8-2 trailing truck style:", DrawType.ToggleGroup)] public X82Options x82Options = X82Options.small_wheels;
		[Draw("x-10-2 trailing truck style:", DrawType.ToggleGroup)] public X102Options x102Options = X102Options.small_wheels;

		[Header("Valve gear")]
		[Draw("Show x-4-x valve gear:", Height = HEIGHT)] public bool show4CoupledValveGear = false;
		[Draw("Show x-6-x valve gear:", Height = HEIGHT)] public bool show6CoupledValveGear = false;
		[Draw("Polished Walschaerts on 4-6-2 / 4-6-4 (reapply):", Height = HEIGHT)] public bool polishedPacificHudsonValveGear = true;
		[Draw("Show x-8-x valve gear:", Height = HEIGHT)] public bool show8CoupledValveGear = true;
		[Draw("Show high-speed 2-8-2 valve gear:", Height = HEIGHT)] public bool show282BigValveGear = false;
		[Draw("Show x-10-x valve gear:", Height = HEIGHT)] public bool show10CoupledValveGear = true;
		[Draw("Show x-12-x valve gear:", Height = HEIGHT)] public bool show12CoupledValveGear = true;

		[Header("Curving (wheelbase): long rigid driver groups drag and squeal more on tight curves")]
		[Draw("Extra curve drag for long rigid wheelbases:", Height = HEIGHT)] public bool curveDrag = true;
		[Draw("Extra flange squeal for long rigid wheelbases:", Height = HEIGHT)] public bool curveSqueal = true;

		[Header("Experimental 2-2-2: non-colliding clearance prototype")]
		[Header("Reapply the arrangement after changing its driver diameter")]
		[Draw("2-2-2 driver diameter (metres):", DrawType.Slider, Min = 2.1, Max = 3.6, Precision = 2)]
		public float giant222DriverDiameterMetres = 2.8f;

		[Header("Random spawns")]
		[Draw("Spawn S282 with random wheel arrangement:", DrawType.Toggle, Height = HEIGHT)] public bool spawnRandomWA = false;
		
		[Draw("Randomly spawn 4-4-0:", Height = HEIGHT)] public bool spawn440 = true;
		[Draw("Randomly spawn 4-4-2:", Height = HEIGHT)] public bool spawn442 = true;
		[Draw("Randomly spawn 4-4-4:", Height = HEIGHT)] public bool spawn444 = true;
		[Draw("Randomly spawn 4-6-0:", Height = HEIGHT)] public bool spawn460 = true;
		[Draw("Randomly spawn 4-6-2:", Height = HEIGHT)] public bool spawn462 = true;
		[Draw("Randomly spawn 4-6-4:", Height = HEIGHT)] public bool spawn464 = true;
		[Draw("Randomly spawn 0-8-0:", Height = HEIGHT)] public bool spawn080 = true;
		[Draw("Randomly spawn 0-8-2:", Height = HEIGHT)] public bool spawn082 = true;
		[Draw("Randomly spawn 0-8-4:", Height = HEIGHT)] public bool spawn084 = true;
		[Draw("Randomly spawn 2-8-0:", Height = HEIGHT)] public bool spawn280 = true;
		[Draw("Randomly spawn 2-8-2:", Height = HEIGHT)] public bool spawn282 = true;
		[Draw("Randomly spawn high-speed 2-8-0:", Height = HEIGHT)] public bool spawn280Big = true;
		[Draw("Randomly spawn high-speed 2-8-2:", Height = HEIGHT)] public bool spawn282Big = true;
		[Draw("Randomly spawn high-speed 2-8-4:", Height = HEIGHT)] public bool spawn284Big = true;
		[Draw("Randomly spawn 2-8-4:", Height = HEIGHT)] public bool spawn284 = true;
		[Draw("Randomly spawn 4-8-0:", Height = HEIGHT)] public bool spawn480 = true;
		[Draw("Randomly spawn 4-8-2:", Height = HEIGHT)] public bool spawn482 = true;
		[Draw("Randomly spawn 4-8-4:", Height = HEIGHT)] public bool spawn484 = true;
		[Draw("Randomly spawn 0-10-0:", Height = HEIGHT)] public bool spawn0100 = true;
		[Draw("Randomly spawn 0-10-2:", Height = HEIGHT)] public bool spawn0102 = true;
		[Draw("Randomly spawn 0-10-4:", Height = HEIGHT)] public bool spawn0104 = true;
		[Draw("Randomly spawn 2-10-0:", Height = HEIGHT)] public bool spawn2100 = true;
		[Draw("Randomly spawn 2-10-2:", Height = HEIGHT)] public bool spawn2102 = true;
		[Draw("Randomly spawn 2-10-4:", Height = HEIGHT)] public bool spawn2104 = true;
		[Draw("Randomly spawn 4-10-0:", Height = HEIGHT)] public bool spawn4100 = true;
		[Draw("Randomly spawn 4-10-2:", Height = HEIGHT)] public bool spawn4102 = true;
		[Draw("Randomly spawn 4-10-4:", Height = HEIGHT)] public bool spawn4104 = true;
		[Draw("Randomly spawn 0-12-0:", Height = HEIGHT)] public bool spawn0120 = true;
		[Draw("Randomly spawn 2-12-0:", Height = HEIGHT)] public bool spawn2120 = true;
		[Draw("Randomly spawn 2-12-2:", Height = HEIGHT)] public bool spawn2122 = true;
		[Draw("Randomly spawn 4-12-2:", Height = HEIGHT)] public bool spawn4122 = true;
		[Draw("Randomly spawn articulated 2-4-4-2:", Height = HEIGHT)] public bool spawn2442 = false;
		[Draw("Randomly spawn articulated 4-4-4-4:", Height = HEIGHT)] public bool spawn4444 = false;
		[Draw("Randomly spawn unpowered 4-0-4 Drivers not found:", Height = HEIGHT)] public bool spawn404 = false;
		[Draw("Randomly spawn 2-6-0 Mogul:", Height = HEIGHT)] public bool spawn260 = true;
		[Draw("Randomly spawn 2-4-0:", Height = HEIGHT)] public bool spawn240 = true;
		[Draw("Randomly spawn experimental 2-2-2 clearance prototype:", Height = HEIGHT)] public bool spawn222 = false;
		[Draw("Randomly spawn articulated 2-6-6-2 Mallet:", Height = HEIGHT)] public bool spawn2662 = false;
		[Draw("Randomly spawn articulated 2-2-2-2-2 Triplex:", Height = HEIGHT)] public bool spawn22222 = false;
		[Draw("Randomly spawn articulated 0-2-2-2-0 Triplex:", Height = HEIGHT)] public bool spawn02220 = false;
		[Draw("Randomly spawn articulated 0-8-8-0 Angus:", Height = HEIGHT)] public bool spawn08880 = false;
		[Draw("Randomly spawn articulated 2-6-8-0 Gt. Northern:", Height = HEIGHT)] public bool spawn2680 = false;
		[Draw("Randomly spawn 4-14-4:", Height = HEIGHT)] public bool spawn4144 = false;

		// Derived from the toggles, not a second authoritative saved pool.
		[System.Xml.Serialization.XmlIgnore]
		public List<WheelArrangementType> RandomWAs = new();

		public override void Save(UnityModManager.ModEntry modEntry)
		{
			Save(this, modEntry);
		}

		public void OnChange() {
			RandomWAs = new();
			if (spawn440)
				RandomWAs.Add(WheelArrangementType.s440);
			if (spawn442)
				RandomWAs.Add(WheelArrangementType.s442);
			if (spawn444)
				RandomWAs.Add(WheelArrangementType.s444);

			if (spawn460)
				RandomWAs.Add(WheelArrangementType.s460);
			if (spawn462)
				RandomWAs.Add(WheelArrangementType.s462);
			if (spawn464)
				RandomWAs.Add(WheelArrangementType.s464);

			//EIGHT COUPLED

			if (spawn080)
				RandomWAs.Add(WheelArrangementType.s080);
			if (spawn082)
				RandomWAs.Add(WheelArrangementType.s082);
			if (spawn084)
				RandomWAs.Add(WheelArrangementType.s084);

			if (spawn280)
				RandomWAs.Add(WheelArrangementType.s280);
			if (spawn282)
				RandomWAs.Add(WheelArrangementType.s282);
			if (spawn280Big)
				RandomWAs.Add(WheelArrangementType.s280Big);
			if (spawn282Big)
				RandomWAs.Add(WheelArrangementType.s282Big);
			if (spawn284Big)
				RandomWAs.Add(WheelArrangementType.s284Big);
			if (spawn284)
				RandomWAs.Add(WheelArrangementType.s284);

			if (spawn480)
				RandomWAs.Add(WheelArrangementType.s480);
			if (spawn482)
				RandomWAs.Add(WheelArrangementType.s482);
			if (spawn484)
				RandomWAs.Add(WheelArrangementType.s484);

			//TEN COUPLED

			if (spawn0100)
				RandomWAs.Add(WheelArrangementType.s0100);
			if (spawn0102)
				RandomWAs.Add(WheelArrangementType.s0102);
			if (spawn0104)
				RandomWAs.Add(WheelArrangementType.s0104);

			if (spawn2100)
				RandomWAs.Add(WheelArrangementType.s2100);
			if (spawn2102)
				RandomWAs.Add(WheelArrangementType.s2102);
			if (spawn2104)
				RandomWAs.Add(WheelArrangementType.s2104);

			if (spawn4100)
				RandomWAs.Add(WheelArrangementType.s4100);
			if (spawn4102)
				RandomWAs.Add(WheelArrangementType.s4102);
			if (spawn4104)
				RandomWAs.Add(WheelArrangementType.s4104);

			//TWELVE COUPLED

			if (spawn0120)
				RandomWAs.Add(WheelArrangementType.s0120);
			if (spawn2120)
				RandomWAs.Add(WheelArrangementType.s2120);
			if (spawn2122)
				RandomWAs.Add(WheelArrangementType.s2122);
			if (spawn4122)
				RandomWAs.Add(WheelArrangementType.s4122);
			if (spawn2442)
				RandomWAs.Add(WheelArrangementType.s2442);
			if (spawn4444)
				RandomWAs.Add(WheelArrangementType.s4444);
			if (spawn404)
				RandomWAs.Add(WheelArrangementType.s404);
			if (spawn260)
				RandomWAs.Add(WheelArrangementType.s260);
			if (spawn240)
				RandomWAs.Add(WheelArrangementType.s240);
			if (spawn222)
				RandomWAs.Add(WheelArrangementType.s222);
			if (spawn2662)
				RandomWAs.Add(WheelArrangementType.s2662);
			if (spawn22222)
				RandomWAs.Add(WheelArrangementType.s22222);
			if (spawn02220)
				RandomWAs.Add(WheelArrangementType.s02220);
			if (spawn08880)
				RandomWAs.Add(WheelArrangementType.s08880);
			if (spawn2680)
				RandomWAs.Add(WheelArrangementType.s2680);
			if (spawn4144)
				RandomWAs.Add(WheelArrangementType.s4144);

		}

		internal int PickRandomArrangement(System.Random random)
		{
			if (random == null) throw new ArgumentNullException(nameof(random));
			// Refresh here as well as OnChange, so UI and programmatic edits cannot
			// leave a stale pool. Articulated/novelty layouts and the experimental
			// giant-driver clearance prototype remain opt-in.
			OnChange();
			return RandomWAs.Count == 0 ? (int)WheelArrangementType.s282
				: (int)RandomWAs[random.Next(RandomWAs.Count)];
		}
	}
}
