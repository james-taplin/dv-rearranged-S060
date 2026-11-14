#if B99_S060_ARTICULATED
using System;
using System.Linq;
using DV.DopplerEffects;
using DV.ModularAudioCar;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RearrangedS060
{
    // Sound data/configuration only. Native ChuffClipsSimReader still owns the
    // engine events, pressure selection, one-shot pool, loops and wet/ash effects.
    internal sealed class S060ShayExhaust : IDisposable
    {
        internal static readonly int[] Rates = { 4, 8, 16, 24, 32, 48, 64, 96 };
        private static readonly string[] Families = { "low", "medium", "high" };
        private readonly ChuffClipsSimReader reader;
        private readonly ChuffClipsSimReader.OrderedChuffClips[][] banks;
        private readonly ChuffClipsSimReader.ChuffLoop[] loops;
        private readonly ChuffClipsSimReader.IndividualChuffAudioSourceConfig regular;
        private readonly AnimationCurve pressure;
        private GameObject root;
        private bool assigned;

        private S060ShayExhaust(ChuffClipsSimReader reader)
        {
            this.reader = reader;
            banks = new[] { reader.lowPressureClips, reader.mediumPressureClips, reader.highPressureClips };
            loops = reader.chuffLoops; regular = reader.regularChuffConfig; pressure = reader.pressureToVolumeCurve;
        }

        internal static S060ShayExhaust Apply(ChuffClipsSimReader reader) => Apply(reader, S060ShayAssets.Audio);

        // The same loader boundary lets the offline native fixture use the exact
        // built bundle without registering or spawning a game vehicle.
        internal static S060ShayExhaust Apply(ChuffClipsSimReader reader, Func<string, AudioClip> load)
        {
            var owner = new S060ShayExhaust(reader);
            try { owner.Build(load); return owner; }
            catch { owner.Dispose(); throw; }
        }

        private void Build(Func<string, AudioClip> load)
        {
            // Resolve all data before changing any donor reference.
            AudioClip Clip(string name)
            {
                var clip = load("RS060_Shay_" + name);
                if (clip == null || clip.channels != 1 || clip.frequency != 24000 || clip.length <= 0)
                    throw new InvalidOperationException("Missing or invalid original Shay exhaust: " + name);
                return clip;
            }
            var replacement = Families.Select(family => Enumerable.Range(0, 6).Select(phase =>
                new ChuffClipsSimReader.OrderedChuffClips {
                    chuffVariations = Enumerable.Range(0, 2).Select(take => Clip(family + "_" + phase + "_" + take)).ToArray()
                }).ToArray()).ToArray();
            var running = Rates.Select(rate => Families.Select(family => Clip(family + "_loop_" + rate)).ToArray()).ToArray();
            if (regular == null || regular.parent == null || pressure == null)
                throw new InvalidOperationException("Native Shay exhaust source configuration is incomplete.");
            root = new GameObject("[Shay original geared exhaust]");
            root.SetActive(false); root.transform.SetParent(reader.transform, false);
            root.transform.position = regular.parent.position;
            var replacementLoops = new ChuffClipsSimReader.ChuffLoop[Rates.Length];
            for (int i = 0; i < Rates.Length; i++)
            {
                int rate = Rates[i];
                var node = new GameObject("Shay " + rate + " exhausts per second");
                node.transform.SetParent(root.transform, false);
                var layers = new LayeredAudio.Layer[Families.Length];
                for (int family = 0; family < layers.Length; family++)
                {
                    var emitter = new GameObject(Families[family]); emitter.transform.SetParent(node.transform, false);
                    var source = emitter.AddComponent<AudioSource>();
                    source.clip = running[i][family]; source.loop = true; source.playOnAwake = false;
                    source.volume = 0; source.pitch = 1; source.spatialBlend = 1; source.dopplerLevel = 0;
                    source.spread = regular.spread; source.minDistance = regular.minDistance; source.maxDistance = regular.maxDistance;
                    if (regular.spatialCurve != null && regular.spatialCurve.length > 0)
                    { source.rolloffMode = AudioRolloffMode.Custom; source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, regular.spatialCurve); }
                    else source.rolloffMode = AudioRolloffMode.Logarithmic;
                    source.outputAudioMixerGroup = regular.mixerGroup;
                    emitter.AddComponent<Doppler>();
                    layers[family] = new LayeredAudio.Layer {
                        name = Families[family], source = source, volumeCurve = PressureGain(family),
                        usePitchCurve = true, pitchCurve = AnimationCurve.Linear(0, 0, 192, 192f / rate),
                        inertia = 0, inertialPitch = false
                    };
                }
                var audio = node.AddComponent<LayeredAudio>();
                audio.type = LayeredAudio.Type.Continuous; audio.randomizeStartTime = false;
                audio.audioMixerGroup = regular.mixerGroup; audio.layers = layers;
                replacementLoops[i] = new ChuffClipsSimReader.ChuffLoop {
                    chuffLoop = audio, chuffFrequencyToMasterVolume = FrequencyGain(i)
                };
            }
            var config = new ChuffClipsSimReader.IndividualChuffAudioSourceConfig {
                parent = regular.parent, pitch = regular.pitch, spatialCurve = regular.spatialCurve,
                spread = regular.spread, minDistance = regular.minDistance, maxDistance = regular.maxDistance,
                mixerGroup = regular.mixerGroup,
                chuffFrequencyToMasterVolume = Linear(0, 0, .1f, 1, 2, 1, 4, 0)
            };
            var keys = pressure.keys;
            for (int i = 0; i < keys.Length; i++)
            { keys[i].value *= .72f; keys[i].inTangent *= .72f; keys[i].outTangent *= .72f; }
            var gain = new AnimationCurve(keys) { preWrapMode = pressure.preWrapMode, postWrapMode = pressure.postWrapMode };
            // Keep the old loops intact but silent for exact restoration. Do not
            // delete their clips, objects or shared configurations.
            foreach (var loop in loops) if (loop?.chuffLoop != null) { loop.chuffLoop.Stop(); loop.chuffLoop.Reset(); }
            reader.lowPressureClips = replacement[0]; reader.mediumPressureClips = replacement[1]; reader.highPressureClips = replacement[2];
            reader.regularChuffConfig = config; reader.pressureToVolumeCurve = gain; reader.chuffLoops = replacementLoops;
            assigned = true;
            // All nested sources exist before any Doppler/LayeredAudio Awake.
            root.SetActive(true);
        }

        private AnimationCurve PressureGain(int family)
        {
            float middle = reader.mediumPressureThreshold - 1, high = reader.highPressureThreshold - 1;
            var keys = Enumerable.Range(0, 121).Select(i =>
            {
                float gauge = i * .25f;
                float low = 1 - Mathf.InverseLerp(middle - .75f, middle + .75f, gauge);
                float top = Mathf.InverseLerp(high - .75f, high + .75f, gauge);
                float weight = family == 0 ? low : family == 2 ? top : 1 - low - top;
                return new Vector2(gauge, .72f * Mathf.Max(0, pressure.Evaluate(gauge + 1)) * Mathf.Max(0, weight));
            }).ToArray();
            return Linear(keys.SelectMany(p => new[] { p.x, p.y }).ToArray());
        }

        private static AnimationCurve FrequencyGain(int index)
        {
            float previous = index == 0 ? 2 : Rates[index - 1];
            return index == Rates.Length - 1 ? Linear(previous, 0, Rates[index], 1)
                : Linear(previous, 0, Rates[index], 1, Rates[index + 1], 0);
        }

        private static AnimationCurve Linear(params float[] xy)
        {
            var keys = new Keyframe[xy.Length / 2];
            for (int i = 0; i < keys.Length; i++)
            {
                float before = i == 0 ? 0 : (xy[2 * i + 1] - xy[2 * i - 1]) / (xy[2 * i] - xy[2 * i - 2]);
                float after = i == keys.Length - 1 ? 0 : (xy[2 * i + 3] - xy[2 * i + 1]) / (xy[2 * i + 2] - xy[2 * i]);
                keys[i] = new Keyframe(xy[2 * i], xy[2 * i + 1], before, after);
            }
            return new AnimationCurve(keys) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };
        }

        public void Dispose()
        {
            if (root != null)
            {
                foreach (var audio in root.GetComponentsInChildren<LayeredAudio>(true)) audio.Stop();
                root.SetActive(false);
                if (Application.isPlaying) Object.Destroy(root); else Object.DestroyImmediate(root);
                root = null;
            }
            if (!assigned || reader == null) return;
            reader.lowPressureClips = banks[0]; reader.mediumPressureClips = banks[1]; reader.highPressureClips = banks[2];
            reader.chuffLoops = loops; reader.regularChuffConfig = regular; reader.pressureToVolumeCurve = pressure;
            assigned = false;
        }
    }
}
#endif
