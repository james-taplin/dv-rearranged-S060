using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DV;
using DV.Localization;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using DV.Utils;
using HarmonyLib;
using UnityEngine;
using VLB;

namespace RearrangedS282
{
	internal class CommsRadioS282Rearranger : MonoBehaviour, ICommsRadioMode
	{
		protected enum State
		{
			SelectS282,
			SelectWheelArrangement,
		}

		public static CommsRadioController Controller;

		public ButtonBehaviourType ButtonBehaviour { get; private set; }

		public CommsRadioDisplay display;
		public Transform signalOrigin;
		public Material selectionMaterial;
		public Material skinningMaterial;
		public GameObject trainHighlighter;

		// Sounds
		public AudioClip HoverCarSound;
		public AudioClip SelectedCarSound;
		public AudioClip ConfirmSound;
		public AudioClip CancelSound;

		private State CurrentState;
		private LayerMask TrainCarMask;
		private RaycastHit Hit;
		private TrainCar SelectedCar = null;
		private TrainCar PointedCar = null;
		private MeshRenderer HighlighterRender;

		private int selectedWheelArrangement = (int)WheelArrangementType.s282;

		private const float SIGNAL_RANGE = 100f;
		private static readonly Vector3 HIGHLIGHT_BOUNDS_EXTENSION = new Vector3(0.25f, 0.8f, 0f);
		private static readonly Color LASER_COLOR = new Color(0f, 0.4f, 0.07f);

		public Color GetLaserBeamColor()
		{
			return LASER_COLOR;
		}
		public void OverrideSignalOrigin(Transform signalOrigin) => this.signalOrigin = signalOrigin;

		#region Initialization

		public void Awake()
		{
			// steal components from other radio modes
			CommsRadioCarDeleter deleter = Controller.deleteControl;

			if (deleter != null)
			{
				signalOrigin = deleter.signalOrigin;
				display = deleter.display;
				selectionMaterial = new Material(deleter.selectionMaterial);
				skinningMaterial = new Material(deleter.deleteMaterial);
				// Each mode owns its highlight object; do not reparent the vanilla one.
				trainHighlighter = Instantiate(deleter.trainHighlighter);

				// sounds
				HoverCarSound = deleter.hoverOverCar;
				SelectedCarSound = deleter.warningSound;
				ConfirmSound = deleter.confirmSound;
				CancelSound = deleter.cancelSound;
			}
			else
			{
				Main.Logger.Error("CommsRadioS282Rearranger: couldn't get properties from siblings");
			}

			if (!signalOrigin)
			{
				Main.Logger.Error("CommsRadioS282Rearranger: signalOrigin on isn't set, using this.transform!");
				signalOrigin = transform;
			}

			if (display == null)
			{
				Main.Logger.Error("CommsRadioS282Rearranger: display not set, can't function properly!");
			}

			if ((selectionMaterial == null) || (skinningMaterial == null))
			{
				Main.Logger.Error("CommsRadioS282Rearranger: Selection material(s) not set. Visuals won't be correct.");
			}

			if (trainHighlighter == null)
			{
				Main.Logger.Error("CommsRadioS282Rearranger: trainHighlighter not set, can't function properly!!");
			}

			if ((HoverCarSound == null) || (SelectedCarSound == null) || (ConfirmSound == null) || (CancelSound == null))
			{
				Main.Logger.Error("Not all audio clips set, some sounds won't be played!");
			}

			TrainCarMask = LayerMask.GetMask(new string[]
			{
			"Train_Big_Collider"
			});

			HighlighterRender = trainHighlighter.GetComponentInChildren<MeshRenderer>(true);
			trainHighlighter.SetActive(false);
			trainHighlighter.transform.SetParent(null);
		}

		public void Enable() { }

		public void Disable()
		{
			ResetState();
		}

		public void SetStartingDisplay()
		{
			string content = "Select an S282 to rearrange its wheels.";
			display.SetDisplay("WHEEL REARRANGER", content, "");
		}

		#endregion

		#region Car Highlighting

		private void HighlightCar(TrainCar car, Material highlightMaterial)
		{
			if (car == null)
			{
				Debug.LogError("Highlight car is null. Ignoring request.");
				return;
			}

			if (car.carType != TrainCarType.LocoSteamHeavy)
			{
				Debug.LogError("Tried to rearrange something that wasn't an S282.");
				return;
			}

			HighlighterRender.material = highlightMaterial;

			trainHighlighter.transform.localScale = car.Bounds.size + HIGHLIGHT_BOUNDS_EXTENSION;
			Vector3 b = car.transform.up * (trainHighlighter.transform.localScale.y / 2f);
			Vector3 b2 = car.transform.forward * car.Bounds.center.z;
			Vector3 position = car.transform.position + b + b2;

			trainHighlighter.transform.SetPositionAndRotation(position, car.transform.rotation);
			trainHighlighter.SetActive(true);
			trainHighlighter.transform.SetParent(car.transform, true);
		}

		private void ClearHighlightedCar()
		{
			trainHighlighter.SetActive(false);
			trainHighlighter.transform.SetParent(null);
		}

		private void PointToCar(TrainCar car)
		{
			if (PointedCar != car)
			{
				if (car != null)
				{
					PointedCar = car;
					HighlightCar(PointedCar, selectionMaterial);
					CommsRadioController.PlayAudioFromRadio(HoverCarSound, transform);
				}
				else
				{
					PointedCar = null;
					ClearHighlightedCar();
				}
			}
		}

		#endregion

		#region State Machine Actions

		private void SetState(State newState)
		{
			if (newState == CurrentState) return;

			CurrentState = newState;
			switch (CurrentState)
			{
				case State.SelectS282:
					SetStartingDisplay();
					ButtonBehaviour = ButtonBehaviourType.Regular;
					break;

				case State.SelectWheelArrangement:
					SetSelectedWheelArrangement(selectedWheelArrangement);
					ButtonBehaviour = ButtonBehaviourType.Override;
					break;
			}
		}

		private void ResetState()
		{
			PointedCar = null;

			SelectedCar = null;
			ClearHighlightedCar();

			SetState(State.SelectS282);
		}

		public void OnUpdate()
		{
			TrainCar trainCar;

			switch (CurrentState)
			{
				case State.SelectS282:
					if (!(SelectedCar == null))
					{
						Main.Logger.Error("Invalid setup for current state, reseting flags!");
						ResetState();
						return;
					}

					//Check if not pointing at anything
					if (!Physics.Raycast(signalOrigin.position, signalOrigin.forward, out Hit, SIGNAL_RANGE, TrainCarMask))
					{
						PointToCar(null);
					}
					else
					{
						//Try to get the traincar we're pointing at
						trainCar = TrainCar.Resolve(Hit.transform.root);

						//Only highlight this traincar if it's an unexploded S282
						if (trainCar != null
							&& trainCar.carType == TrainCarType.LocoSteamHeavy
							&& !trainCar.transform.GetComponent<ExplosionModelHandler>().usingExplodedModel)
						{
							PointToCar(trainCar);
						}
					}

					break;

				case State.SelectWheelArrangement:
					//if we turn away from the locomotive
					if (!Physics.Raycast(signalOrigin.position, signalOrigin.forward, out Hit, SIGNAL_RANGE, TrainCarMask))
					{
						PointToCar(null);
						display.SetAction("cancel");
					}
					else
					{
						//if we're facing the locomotive
						trainCar = TrainCar.Resolve(Hit.transform.root);
						PointToCar(trainCar);
						display.SetAction("confirm");
					}

					break;

				default:
					ResetState();
					break;
			}
		}

		//Gets called when we click with the comms radio
		public void OnUse()
		{
			switch (CurrentState)
			{
				case State.SelectS282:
					if (PointedCar != null && PointedCar.carType == TrainCarType.LocoSteamHeavy)
					{
						SelectedCar = PointedCar;
						PointedCar = null;

						HighlightCar(SelectedCar, skinningMaterial);
						CommsRadioController.PlayAudioFromRadio(SelectedCarSound, transform);
						SetState(State.SelectWheelArrangement);
					}
					break;

				case State.SelectWheelArrangement:
					if ((PointedCar != null) && (PointedCar == SelectedCar))
					{
						//clicked on the selected car again, this means confirm
						bool applied = ApplySelectedWheelArrangement();
						CommsRadioController.PlayAudioFromRadio(applied ? ConfirmSound : CancelSound, transform);
					}
					else
					{
						//clicked off the selected car, this means cancel
						CommsRadioController.PlayAudioFromRadio(CancelSound, transform);
					}
					ResetState();
					break;
			}
		}

		public bool ButtonACustomAction()
		{
			if (CurrentState == State.SelectWheelArrangement)
			{
				selectedWheelArrangement = WheelArrangement.NextRuntimeId(selectedWheelArrangement, -1);
				SetSelectedWheelArrangement(selectedWheelArrangement);
				return true;
			}
			else
			{
				Main.Logger.Error($"Unexpected state {CurrentState}!");
				return false;
			}
		}

		public bool ButtonBCustomAction()
		{
			if (CurrentState == State.SelectWheelArrangement)
			{
				selectedWheelArrangement = WheelArrangement.NextRuntimeId(selectedWheelArrangement, 1);
				SetSelectedWheelArrangement(selectedWheelArrangement);
				return true;
			}
			else
			{
				Main.Logger.Error($"Unexpected state {CurrentState}!");
				return false;
			}
		}

		#endregion

		#region wheel arrangment stuff

		private bool ApplySelectedWheelArrangement()
		{
			if (!WheelArrangement.IsRuntimeId(selectedWheelArrangement))
			{
				Main.Logger.Error("Tried to select out-of-bounds wheel arrangement");
				return false;
			}
			if (SelectedCar == null)
			{
				Main.Logger.Error("Tried to select null locomotive");
				return false;
			}
			if (SelectedCar.carType != TrainCarType.LocoSteamHeavy)
			{
				Main.Logger.Error("Tried to rearrange something that wasn't an S282");
				return false;
			}

#if B99_STAGE2 || B99_ALL_STANDARD
			bool applied = SelectedCar.GetOrAddComponent<WheelRearranger>().TrySwitchStage2(selectedWheelArrangement);
#if B99_ALL_STANDARD
			if (applied) ConventionalSpawn.RecordApplied(SelectedCar, selectedWheelArrangement);
#endif
			return applied;
#else
			SelectedCar.GetOrAddComponent<WheelRearranger>().SwitchWheelArrangement(selectedWheelArrangement);
			return true;
#endif
		}

		private void SetSelectedWheelArrangement(int wa)
		{
			string selectedWheelArrangement = WheelArrangement.WheelArrangementNames[wa];
			display.SetContent($"Select Wheel Arrangement:\n{selectedWheelArrangement}");
		}

		private void OnDestroy()
		{
			if (trainHighlighter != null) Destroy(trainHighlighter);
			if (selectionMaterial != null) Destroy(selectionMaterial);
			if (skinningMaterial != null) Destroy(skinningMaterial);
		}

		#endregion
	}

	[HarmonyPatch(typeof(CommsRadioController), "Awake")]
	static class CommsRadio_Awake_Patch
	{
		public static CommsRadioS282Rearranger rearranger = null;

		static void Postfix(CommsRadioController __instance, List<ICommsRadioMode> ___allModes)
		{
			CommsRadioS282Rearranger.Controller = __instance;
			rearranger = __instance.gameObject.AddComponent<CommsRadioS282Rearranger>();
			___allModes.Add(rearranger);
		}
	}
}
