using System;
using System.Linq;
using UnityEngine;

namespace RearrangedS060
{
    [DefaultExecutionOrder(1000)]
    internal sealed class S060ShayMotion : MonoBehaviour
    {
        [Serializable] private class MotionData { public S060ShayTransmission.Part[] parts=Array.Empty<S060ShayTransmission.Part>(); }
        private S060ShayEngine engine;
        private S060ShayTransmission transmission;
        private Transform[] levels, axles, cranks, reversed;
        // Read-only diagnostics for the articulated trace (gear clock reports).
        internal S060ShayEngine Engine => engine;
        internal S060ShayTransmission Transmission => transmission;
        internal Transform Crank => cranks?.FirstOrDefault();
        internal Transform RearAxle => axles?.FirstOrDefault();
        internal int Updates { get; private set; }
        internal int LastFrame { get; private set; } = -1;
        internal double LastTurns { get; private set; } = double.NaN;
        private Transform[][] rods, crossheads, valves;
        internal void Bind(S060EngineBank bank, Transform root, TextAsset data, Transform[] all)
        {
            engine = bank.Main.Engines[3] as S060ShayEngine ?? throw new InvalidOperationException("Native Shay engine unavailable.");
            var model = root.Find("Engine");
            levels = model.Cast<Transform>().Where(t => t.name.StartsWith("LOD", StringComparison.Ordinal)).ToArray();
            cranks = levels.Select(l=>l.Find("CRANK")).ToArray();
            reversed = levels.Select(l=>l.Find("FRONT_REVERSED")).ToArray();
            rods = levels.Select(l => Enumerable.Range(1,3).Select(i=>l.Find("ROD_"+i)).ToArray()).ToArray();
            crossheads = levels.Select(l => Enumerable.Range(1,3).Select(i=>l.Find("CROSSHEAD_"+i)).ToArray()).ToArray();
            valves = levels.Select(l => Enumerable.Range(1,3).Select(i=>l.Find("VALVE_VISUAL_ONLY_"+i)).ToArray()).ToArray();
            transmission = root.Find("Transmission").gameObject.AddComponent<S060ShayTransmission>();
            transmission.parts = JsonUtility.FromJson<MotionData>(data.text).parts;
            foreach (var p in transmission.parts)
            { p.transform = all.Single(t=>t.name==p.path.Substring(p.path.LastIndexOf('/')+1)); }
            transmission.rearTruck = all.Single(t=>t.name=="TRUCK_REAR_PIVOT");
            transmission.frontTruck = all.Single(t=>t.name=="TRUCK_FRONT_PIVOT");
            axles = new[] { "REAR_AXLE_1", "REAR_AXLE_2", "FRONT_AXLE_3", "FRONT_AXLE_4" }
                .Select(n=>all.Single(t=>t.name==n)).ToArray();
            LateUpdate();
        }
        private void LateUpdate()
        {
            if (engine == null || transmission == null) return;
            double turns=engine.GearTurns, fraction=turns-Math.Floor(turns);
            Updates++; LastFrame=Time.frameCount; LastTurns=turns;
            for(int lod=0;lod<levels.Length;lod++)
            {
                cranks[lod].localRotation=Quaternion.AngleAxis((float)(-fraction*360),Vector3.forward);
                reversed[lod].localRotation=Quaternion.AngleAxis((float)(fraction*360+9),Vector3.forward);
                for(int i=0;i<3;i++)
                {
                    double angle=2*Math.PI*(fraction+i/3.0),x=.075*Math.Sin(angle),y=.075*Math.Cos(angle),dx=-.065-x,dy=Math.Sqrt(.9*.9-dx*dx);
                    float z=(i-1)*.24f;
                    rods[lod][i].localPosition=new Vector3((float)x,(float)y,z-.035f);
                    rods[lod][i].localRotation=Quaternion.AngleAxis((float)(Math.Atan2(-dx,dy)*180/Math.PI),Vector3.forward);
                    crossheads[lod][i].localPosition=new Vector3(-.065f,(float)(y+dy),z);
                    valves[lod][i].localPosition=new Vector3(-.194f,(float)(1.22+.015*Math.Sin(angle+Math.PI/2)),z);
                }
            }
            transmission.engineTurns=-turns; transmission.Evaluate();
            for(int i=0;i<axles.Length;i++) axles[i].localRotation=Quaternion.AngleAxis((float)((i<2?transmission.RearWheelTurns:transmission.FrontWheelTurns)%1*360),Vector3.right);
        }
    }
}
