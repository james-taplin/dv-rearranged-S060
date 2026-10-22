#if B99_ALL_STANDARD
using System.Collections.Generic;
using DV.ModularAudioCar;
using UnityEngine;

namespace RearrangedS060
{
    internal partial class WheelRearranger
    {
        // Native rolling audio caches one joint-step entry per physical axle.
        // Rebuild that cache only after the new bogie arrays have been committed.
        private void RefreshConventionalRollingAudio()
        {
            if (loco == null) return;
            var seen = new HashSet<CarRollingAudioModule>();
            void Refresh(Component root)
            {
                if (root == null) return;
                foreach (var module in root.GetComponentsInChildren<CarRollingAudioModule>(true))
                    if (module != null && seen.Add(module) && ReferenceEquals(module.car, loco))
                        module.ResetJointSoundValues();
            }
            Refresh(loco);
            // Native SetupInterior detaches this from the TrainCar hierarchy.
            // An uninitialized/pooled module is left to its native Initialize.
            Refresh(loco.interior);
        }
    }
}
#endif
