using LocoSim.Definitions;
using LocoSim.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RearrangedS060.Sim
{
	internal class DuplexTractionDefinition : TractionDefinition
	{
		public override SimComponent InstantiateImplementation()
		{
			ID = "duplexTraction";
			return new Traction(this);
		}
	}
}
