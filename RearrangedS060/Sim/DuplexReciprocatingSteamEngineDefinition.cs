using LocoSim.Definitions;
using LocoSim.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RearrangedS060.Sim
{
	internal class DuplexReciprocatingSteamEngineDefinition: ReciprocatingSteamEngineDefinition
	{
		public override SimComponent InstantiateImplementation()
		{
			ID = "duplexSteamEngine";
			cylinderBore = 0.55f * 0.97f;
			pistonStroke = 0.711f * 0.97f;
			minCutoff = 0.01f;
			maxCutoff = 0.75f;
			return new ReciprocatingSteamEngine(this);
		}

	}
}
