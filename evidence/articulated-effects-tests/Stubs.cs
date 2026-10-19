using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    public class Component
    {
        internal readonly List<Component> children=new List<Component>();
        internal bool active=true;
        internal readonly List<bool> includeInactiveRequests=new List<bool>();
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T:Component
        {
            includeInactiveRequests.Add(includeInactive);
            IEnumerable<Component> Walk(Component component)
            {
                if(component==null||(!includeInactive&&!component.active))yield break;
                yield return component;
                foreach(var child in component.children)
                    foreach(var result in Walk(child))yield return result;
            }
            return Walk(this).OfType<T>().ToArray();
        }
    }
    public sealed class ClipController:Component { }
    public sealed class LayerController:Component { }
}

public sealed class TrainCar:UnityEngine.Component
{
    public UnityEngine.Component interior;
    internal string id;
    public override bool Equals(object other)=>other is TrainCar car&&car.id==id;
    public override int GetHashCode()=>id?.GetHashCode()??0;
}
namespace LocoSim.Implementations
{
    public sealed class SimulationFlow
    {
        internal string id;
        public override bool Equals(object other)=>other is SimulationFlow flow&&flow.id==id;
        public override int GetHashCode()=>id?.GetHashCode()??0;
    }
}

namespace RearrangedS282
{
    internal static partial class ArticulatedEffects
    {
        // Data stand-ins only. Production selection logic is extracted verbatim.
        internal sealed class AudioBinding
        {
            internal TrainCar Car;
            internal LocoSim.Implementations.SimulationFlow Flow;
            internal bool Ready;
        }
        internal static T[] Discover<T>(TrainCar car) where T:UnityEngine.Component=>AudioControllers<T>(car).ToArray();
        internal static bool Matches(AudioBinding binding,TrainCar car,LocoSim.Implementations.SimulationFlow flow)=>AudioBindingMatches(binding,car,flow);
    }
}
