#if B99_S060_FUNCTIONAL
using System;
using System.Collections.Generic;
using DV;
using HarmonyLib;
using UnityEngine;

namespace RearrangedS060
{
    // Uses the same ICommsRadioMode and vanilla presentation assets as the S282
    // fork, with independent state, materials, highlight and registration.
    internal sealed class S060Radio : MonoBehaviour, ICommsRadioMode
    {
        private CommsRadioDisplay display;
        private Transform origin;
        private Material hoverMaterial, selectedMaterial;
        private GameObject highlight;
        private MeshRenderer highlightRenderer;
        private AudioClip hoverSound, selectSound, confirmSound, cancelSound;
        private TrainCar pointed, selected;
        private int choice;
        private int trainMask;
        public ButtonBehaviourType ButtonBehaviour { get; private set; }

        internal bool Initialize(CommsRadioController controller)
        {
            var source = controller.deleteControl;
            if (source == null || source.display == null || source.trainHighlighter == null ||
                source.selectionMaterial == null || source.deleteMaterial == null)
            {
                Main.Logger.Warning("S060 radio presentation is not ready; UMM conversions remain available.");
                return false;
            }
            display = source.display;
            origin = source.signalOrigin != null ? source.signalOrigin : transform;
            hoverMaterial = new Material(source.selectionMaterial);
            selectedMaterial = new Material(source.deleteMaterial);
            highlight = Instantiate(source.trainHighlighter);
            highlight.transform.SetParent(null, true);
            highlight.SetActive(false);
            highlightRenderer = highlight.GetComponentInChildren<MeshRenderer>(true);
            hoverSound = source.hoverOverCar;
            selectSound = source.warningSound;
            confirmSound = source.confirmSound;
            cancelSound = source.cancelSound;
            trainMask = LayerMask.GetMask("Train_Big_Collider");
            return highlightRenderer != null;
        }

        public Color GetLaserBeamColor() => new Color(.08f, .45f, .65f);
        public void OverrideSignalOrigin(Transform signalOrigin) => origin = signalOrigin != null ? signalOrigin : transform;
        public void Enable() => Reset();
        public void Disable() => Reset(false);
        public void SetStartingDisplay() => display.SetDisplay("WHEEL REARRANGER", "Select an S060 to rearrange its wheels.", "");

        private void Reset(bool updateDisplay = true)
        {
            pointed = selected = null;
            ButtonBehaviour = ButtonBehaviourType.Regular;
            if (highlight != null) highlight.SetActive(false);
            if (updateDisplay && display != null) SetStartingDisplay();
        }

        private void ShowHighlight(TrainCar car, bool chosen)
        {
            if (highlight == null || highlightRenderer == null) return;
            if (car == null) { highlight.SetActive(false); return; }
            highlightRenderer.sharedMaterial = chosen ? selectedMaterial : hoverMaterial;
            var size = car.Bounds.size + new Vector3(.25f, .8f, 0f);
            highlight.transform.localScale = size;
            highlight.transform.SetPositionAndRotation(car.transform.position + car.transform.up * size.y / 2f +
                car.transform.forward * car.Bounds.center.z, car.transform.rotation);
            highlight.SetActive(true);
        }

        public void OnUpdate()
        {
            TrainCar candidate = null;
            if (origin != null && Physics.Raycast(origin.position, origin.forward, out var hit, 100f, trainMask))
                candidate = TrainCar.Resolve(hit.transform.root);
            if (!S060Parts.IsS060(candidate) || candidate.isExploded) candidate = null;
            if (pointed != candidate && candidate != null && selected == null) Play(hoverSound);
            pointed = candidate;
            if (selected != null)
            {
                ShowHighlight(selected, true);
                display.SetAction(pointed == selected ? "confirm" : "cancel");
            }
            else
            {
                // A destroyed selected car also clears Override button mode.
                if (ButtonBehaviour == ButtonBehaviourType.Override) Reset();
                ShowHighlight(pointed, false);
                display.SetAction(pointed != null ? "select" : "");
            }
        }

        public void OnUse()
        {
            if (selected == null)
            {
                if (pointed == null) return;
                selected = pointed;
                var current = S060Conversion.Current(selected)?.Selection ?? S060Selection.Stock;
                choice = current.IsStock ? 0 : current.Arrangement == S060Arrangement.FourCoupled ? 1 :
                    current.Arrangement == S060Arrangement.SixCoupled ? 2 : current.Arrangement == S060Arrangement.SixTwo ? 4 :
                    current.Arrangement == S060Arrangement.Meyer ? 5 : current.Arrangement == S060Arrangement.Dockside ? 6 : current.Arrangement == S060Arrangement.TenCoupled ? 7 : 3;
                ButtonBehaviour = ButtonBehaviourType.Override;
                Play(selectSound);
                ShowChoice();
                return;
            }
            if (pointed != selected) { Play(cancelSound); Reset(); return; }
            if (S060Lifecycle.TryApply(selected, Selection(), out string message))
            {
                Play(confirmSound);
                Reset();
            }
            else
            {
                Play(cancelSound);
                display.SetContent(message + "\nStop, close regulator and retry. Aim away to cancel.");
            }
        }

        private S060Selection Selection() => choice == 7 ? new S060Selection(S060Arrangement.TenCoupled, 1f) : choice == 0 ? S060Selection.Stock : choice == 5 ? new S060Selection(S060Arrangement.Meyer, 1f) :
            choice == 6 ? new S060Selection(S060Arrangement.Dockside, 1f) : choice == 4 ?
            new S060Selection(S060Arrangement.SixTwo, 1f) :
            S060Selection.ForConversion(choice == 1 ? S060Arrangement.FourCoupled : choice == 2 ?
                S060Arrangement.SixCoupled : S060Arrangement.EightCoupled, 1f);

        private void ShowChoice()
        {
            // Match the S282's short, two-line arrangement chooser. Keep the
            // S060-specific prompt on the initial screen to identify the mode.
            display.SetContent("Select Wheel Arrangement:\n" + (choice == 0 ? "0-6-0 (Stock)" : Selection().Name) +
                (choice != 0 && (Selection().Scale != 1f || (Selection().Arrangement == S060Arrangement.Dockside || Selection().Arrangement == S060Arrangement.TenCoupled)) ? "\n" + Selection().DriverDiameter.ToString("0.00") + " m drivers" : ""));
        }
        public bool ButtonACustomAction() => Cycle(-1);
        public bool ButtonBCustomAction() => Cycle(1);
        private bool Cycle(int direction)
        {
            if (selected == null) return false;
            int count = S060Selection.IsSupported(6) ? 8 : S060Selection.IsSupported(5) ? 7 : 5;
            choice = (choice + direction + count) % count;
            ShowChoice();
            return true;
        }
        private void Play(AudioClip clip) { if (clip != null) CommsRadioController.PlayAudioFromRadio(clip, transform); }
        private void OnDestroy()
        {
            if (highlight != null) Destroy(highlight);
            if (hoverMaterial != null) Destroy(hoverMaterial);
            if (selectedMaterial != null) Destroy(selectedMaterial);
        }
    }

    [HarmonyPatch(typeof(CommsRadioController), "Awake")]
    internal static class S060RadioRegistration
    {
        private static void Postfix(CommsRadioController __instance, List<ICommsRadioMode> ___allModes) => Attach(__instance, ___allModes);

        private static void Attach(CommsRadioController controller, List<ICommsRadioMode> modes)
        {
            if (controller.GetComponent<S060Radio>() != null) return;
            var mode = controller.gameObject.AddComponent<S060Radio>();
            if (mode.Initialize(controller)) modes.Add(mode);
            else UnityEngine.Object.Destroy(mode);
        }

        internal static void AttachExisting()
        {
            foreach (var controller in UnityEngine.Object.FindObjectsOfType<CommsRadioController>())
                Attach(controller, AccessTools.FieldRefAccess<CommsRadioController, List<ICommsRadioMode>>("allModes")(controller));
        }
    }
}
#endif
