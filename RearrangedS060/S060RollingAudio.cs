using System.Collections.Generic;
using DV.ModularAudioCar;
using UnityEngine;

namespace RearrangedS060
{
    internal static class S060RollingAudio
    {
        // Native audio stores one joint-step entry per axle. Rebuild it after
        // committing the bogie arrays, as the conventional S282 changer does.
        internal static void Refresh(TrainCar car)
        {
            if (car == null) return;
            var seen = new HashSet<CarRollingAudioModule>();
            void RefreshRoot(Component root)
            {
                if (root == null) return;
                foreach (var module in root.GetComponentsInChildren<CarRollingAudioModule>(true))
                    if (module != null && seen.Add(module) && ReferenceEquals(module.car, car))
                        module.ResetJointSoundValues();
            }
            RefreshRoot(car);
            // SetupInterior detaches this hierarchy from the car.
            RefreshRoot(car.interior);
        }
    }
}
