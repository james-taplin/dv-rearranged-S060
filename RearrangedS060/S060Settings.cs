#if B99_S060_FUNCTIONAL
using System;
using UnityEngine;
using UnityModManagerNet;

namespace RearrangedS060
{
    public sealed class S060Settings : UnityModManager.ModSettings
    {
        public float driverScale = 1f; // legacy XML only; release geometry ignores this tuning value
        public bool spawnRandom = false;
        public bool spawn060 = true;
        public bool spawn040 = true;
        public bool spawn080 = false;
        public bool spawn062 = false;
        public bool spawnMeyer = false;
        public bool spawnDockside = false;

        public override void Save(UnityModManager.ModEntry entry) => Save(this, entry);

        internal float ValidScale => S060Selection.IsValidScale(driverScale) ? driverScale : 1f;

        internal S060Selection PickInitial(System.Random random)
            => S060Selection.PickInitial(spawnRandom, spawn060, spawn040, spawn080, 1f, random, spawn062, spawnMeyer, spawnDockside);

        internal void Draw()
        {
            GUILayout.Label("Each arrangement uses its fitted wheel size. RESTORE STOCK returns the original 0-6-0.");
            GUILayout.Label("0-6-0 Short Wheelbase and 0-8-0 share 1.14 m drivers; stock 0-6-0 retains 1.42 m drivers.");
            GUILayout.Label("Gauge and cylinder size stay stock; cylinders, pipe connections and steam outlets follow the gear's movement. Brake meshes are hidden.");
            GUILayout.Label("The 0-8-0 rear axle stays at the stock rear-driver station.");
            GUILayout.Space(8);
            spawnRandom = GUILayout.Toggle(spawnRandom, "Random arrangements for newly spawned S060s");
            GUILayout.Label("Random spawn pool (existing saved locomotives keep their selection)");
            using (new GUILayout.HorizontalScope())
            {
                spawn060 = GUILayout.Toggle(spawn060, "0-6-0");
                spawn040 = GUILayout.Toggle(spawn040, "0-4-0");
                spawn080 = GUILayout.Toggle(spawn080, "0-8-0 (experimental)");
                spawn062 = GUILayout.Toggle(spawn062, "0-6-2 (experimental; stock drivers)");
            }
#if B99_S060_ARTICULATED
            using (new GUILayout.HorizontalScope())
            {
                spawnMeyer = GUILayout.Toggle(spawnMeyer, "Meyer compound");
                spawnDockside = GUILayout.Toggle(spawnDockside, "Franklin Dockside");
            }
            GUILayout.Label("Meyer and Dockside require their installed asset packs; each uses its authored wheel size.");
#endif
            if (!spawn060 && !spawn040 && !spawn080 && !spawn062 && !spawnMeyer && !spawnDockside) GUILayout.Label("Empty pool: new locomotives remain stock.");
        }
    }
}
#endif
