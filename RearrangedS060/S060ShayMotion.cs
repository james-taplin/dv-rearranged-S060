using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RearrangedS060
{
    [DefaultExecutionOrder(1000)]
    internal sealed class S060ShayMotion : MonoBehaviour
    {
        private S060ShayEngine engine;
        private float cylinderPitch, valveAxisX;
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
            S060ShayAssets.ValidateMotion(data);
            // Revision 2 is exported at its final dimensions, without an R2 refit.
            var specification = JObject.Parse(data.text);
            cylinderPitch = (float?)specification["cylinderPitch"] ?? throw new InvalidOperationException("Shay cylinder pitch missing.");
            valveAxisX = (float?)specification["valveAxisX"] ?? throw new InvalidOperationException("Shay valve axis missing.");
            if (float.IsNaN(cylinderPitch) || float.IsInfinity(cylinderPitch) || cylinderPitch <= 0 ||
                float.IsNaN(valveAxisX) || float.IsInfinity(valveAxisX)) throw new InvalidOperationException("Invalid authored Shay engine datums.");
            levels = model.Cast<Transform>().Where(t => t.name.StartsWith("LOD", StringComparison.Ordinal)).ToArray();
            cranks = levels.Select(l=>l.Find("CRANK")).ToArray();
            reversed = levels.Select(l=>l.Find("FRONT_REVERSED")).ToArray();
            rods = levels.Select(l => Enumerable.Range(1,3).Select(i=>l.Find("ROD_"+i)).ToArray()).ToArray();
            crossheads = levels.Select(l => Enumerable.Range(1,3).Select(i=>l.Find("CROSSHEAD_"+i)).ToArray()).ToArray();
            valves = levels.Select(l => Enumerable.Range(1,3).Select(i=>l.Find("VALVE_VISUAL_ONLY_"+i)).ToArray()).ToArray();
            transmission = root.Find("Transmission").gameObject.AddComponent<S060ShayTransmission>();
            transmission.Configure(specification);
            // JsonUtility returned an empty list in game for this mod-assembly type
            // (0.20.2 trace: every part null), so nothing but the crank and axles turned.
            transmission.parts = Parts(data.text);
            if (transmission.parts.Length != 43 || transmission.parts.Any(p => string.IsNullOrEmpty(p.role) || string.IsNullOrEmpty(p.path)))
                throw new InvalidOperationException("Shay transmission motion data incomplete: " + transmission.parts.Length + " of 43 parts.");
            foreach (var p in transmission.parts)
            { p.transform = all.Single(t=>t.name==p.path.Substring(p.path.LastIndexOf('/')+1)); }
            transmission.rearTruck = all.Single(t=>t.name=="TRUCK_REAR_PIVOT");
            transmission.frontTruck = all.Single(t=>t.name=="TRUCK_FRONT_PIVOT");
            axles = new[] { "REAR_AXLE_1", "REAR_AXLE_2", "FRONT_AXLE_3", "FRONT_AXLE_4" }
                .Select(n=>all.Single(t=>t.name==n)).ToArray();
            LateUpdate();
        }
        internal static S060ShayTransmission.Part[] Parts(string json)
        {
            Vector3 V(JToken t) => new Vector3((float)t["x"], (float)t["y"], (float)t["z"]);
            Quaternion Q(JToken t) => new Quaternion((float)t["x"], (float)t["y"], (float)t["z"], (float)t["w"]);
            return ((JArray)JObject.Parse(json)["parts"] ?? new JArray()).Select(p => new S060ShayTransmission.Part {
                path = (string)p["path"], role = (string)p["role"], index = (int)p["index"],
                restPosition = V(p["restPosition"]), restRotation = Q(p["restRotation"]) }).ToArray();
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
                    double r=S060SteamCalibration.Shay.CrankRadius;
                    double axis=S060SteamCalibration.Shay.PistonAxisX,length=S060SteamCalibration.Shay.RodLength;
                    double angle=2*Math.PI*(fraction+i/3.0),x=r*Math.Sin(angle),y=r*Math.Cos(angle),dx=axis-x,dy=Math.Sqrt(length*length-dx*dx);
                    float z=(i-1)*cylinderPitch;
                    rods[lod][i].localPosition=new Vector3((float)x,(float)y,z-.035f);
                    rods[lod][i].localRotation=Quaternion.AngleAxis((float)(Math.Atan2(-dx,dy)*180/Math.PI),Vector3.forward);
                    crossheads[lod][i].localPosition=new Vector3((float)axis,(float)(y+dy),z);
                    valves[lod][i].localPosition=new Vector3(valveAxisX,(float)(1.22+.015*Math.Sin(angle+Math.PI/2)),z);
                }
            }
            transmission.engineTurns=-turns; transmission.Evaluate();
            for(int i=0;i<axles.Length;i++) axles[i].localRotation=Quaternion.AngleAxis((float)((i<2?transmission.RearWheelTurns:transmission.FrontWheelTurns)%1*360),Vector3.right);
        }
    }
}
