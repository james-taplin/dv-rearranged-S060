using System;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace RearrangedS060
{
	public static class Main
	{
		public static UnityModManager.ModEntry.ModLogger Logger { get; private set; }
		public static string ModPath { get; private set; }
		public static Settings settings { get; private set; }

		public static bool IsGaugeModInstalled { get; private set; }
#if B99_ALL_STANDARD
		private static bool showArrangementSettings;
#endif

		// Unity Mod Manage Wiki: https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager
		private static bool Load(UnityModManager.ModEntry modEntry)
		{
			Logger = modEntry.Logger;
			ModPath = modEntry.Path;
#if B99_STAGE1
			// Stage 1 is the S060 spike build: manual conversions driven from this
			// window, with no comms radio, no persistence and no spawn hooks. The
			// inherited S282 geometry is compiled but never reached.
			try
			{
				var p1Harmony = new Harmony(modEntry.Info.Id);
				// The only patch the spike needs: Bogie.Axles must stop handing back
				// axles we have deactivated.
				p1Harmony.CreateClassProcessor(typeof(Patches.BogieAxlesPatch)).Patch();
			}
			catch (Exception ex)
			{
				Logger.LogException("S060 P1 patch failed", ex);
				return false;
			}
			modEntry.OnGUI = entry => S060SpikeWindow.Draw();
			Logger.Log("S060 spike build loaded: manual 0-4-0 / 0-8-0 / driver-resize conversions. Stock S282s are untouched.");
			return true;
#elif B99_STAGE2 || B99_ALL_STANDARD
			// Register B99 integrations explicitly. Legacy B98 global patches stay disabled.
			try
			{
				var stage2Harmony = new Harmony(modEntry.Info.Id);
#if B99_ALL_STANDARD
				settings = Settings.Load<Settings>(modEntry);
				if (settings.MigrateSpawnPool()) settings.Save(modEntry);
				settings.OnChange();
				modEntry.OnSaveGUI = OnSaveGUI;
				stage2Harmony.CreateClassProcessor(typeof(PowerDiagnosticsTick)).Patch();
				ArticulatedSimulation.Register(stage2Harmony);
				ArticulatedEffects.Register(stage2Harmony);
				DeltaTrailingTruckCompatibility.Register(stage2Harmony);
				ConventionalSpawn.RegisterPatches(stage2Harmony);
				stage2Harmony.CreateClassProcessor(typeof(WheelRearranger.CurveSquealPatch)).Patch();
				PowerDiagnostics.CurveSampler = CurveDiagnostics.Sample;
				PowerDiagnostics.CurveSessionInfo = CurveDiagnostics.SessionInfo;
				modEntry.OnUpdate = (entry, delta) => PowerDiagnostics.Update();
				Application.quitting += () => PowerDiagnostics.Stop("game exiting");
				// Live geometry and save owners cannot safely outlive their Harmony hooks.
				modEntry.OnUnload = entry => { PowerDiagnostics.Stop("unload requested"); Logger.Log("Restart the game to unload Rearranged S060."); return false; };
#endif
				stage2Harmony.CreateClassProcessor(typeof(CommsRadio_Awake_Patch)).Patch();
				modEntry.OnGUI = entry => {
#if B99_ALL_STANDARD
					GUILayout.Label("44 ARRANGEMENTS — ERIE TRIPLEX TEST BUILD\n" +
						"Use Wheel Rearranger on the comms radio. Stop and close the regulator first.\n" +
						"Selections are saved with each locomotive. Loaded arrangements wait until switching is safe.\n" +
						"First conversion may take a few seconds to prepare rods.");
					if (DeltaTrailingTruckCompatibility.Status != null) GUILayout.Label(DeltaTrailingTruckCompatibility.Status);
					GUILayout.Space(8);
					GUILayout.Label(PowerDiagnostics.Status);
					if (PowerDiagnostics.Latest != null) GUILayout.Label(PowerDiagnostics.Latest);
					if (PowerDiagnostics.LastPath != null) GUILayout.Label(PowerDiagnostics.LastPath);
					if (PowerDiagnostics.Recording)
					{
						if (GUILayout.Button("STOP AND SAVE POWER DIAGNOSTICS")) PowerDiagnostics.Stop();
					}
					else if (!Stage2RuntimeChecks.Running && GUILayout.Button("START POWER DIAGNOSTICS — all loaded S282s")) PowerDiagnostics.Begin();
#else
					GUILayout.Label(
					"B99 stage 2: select a stationary S282 with the Wheel Rearranger radio.\n" +
					"Stock 2-8-2 and 2-8-4 only. Close the regulator before switching.\n" +
					"Selections are temporary and are not written to saves.");
#endif
					GUILayout.Label(Stage2RuntimeChecks.Status);
					if (!Stage2RuntimeChecks.Running
#if B99_ALL_STANDARD
						&& !PowerDiagnostics.Recording
#endif
						&& GUILayout.Button("RUN STRUCTURAL TEST — two stationary S282s"))
						Stage2RuntimeChecks.Begin();
#if B99_ALL_STANDARD
					GUILayout.Space(8);
					showArrangementSettings = GUILayout.Toggle(showArrangementSettings, "Show arrangement and random-spawn settings");
					if (showArrangementSettings)
					{
						GUILayout.Label("Random spawning affects new S282s only. Saved choices are kept. An empty pool uses stock 2-8-2.");
						settings.Draw(entry);
					}
#endif
				};
#if B99_ALL_STANDARD
				Logger.Log("B99 expanded trial: 40 choices including the 4-14-4, articulated 2-6-6-2 Mallet and 2-2-2-2-2 triplex, polished Pacific/Hudson Walschaerts, 2-6-0/2-4-0, giant-driver 2-2-2 clearance prototype and the retained original catalogue.");
#else
				Logger.Log("B99 stage 2: stock / 2-8-4 runtime trial. Persistence and other arrangements disabled.");
#endif
				return true;
			}
			catch (Exception ex)
			{
				new Harmony(modEntry.Info.Id).UnpatchAll(modEntry.Info.Id);
				Logger.LogException("Stage 2 radio initialization failed", ex);
				return false;
			}
#else
			Harmony harmony = null;

			settings = Settings.Load<Settings>(modEntry);
			settings.OnChange();
			modEntry.OnGUI = OnGUI;
			modEntry.OnSaveGUI = OnSaveGUI;

			IsGaugeModInstalled = UnityModManager.FindMod("Gauge") is not null;

#if DEBUG
			Harmony.DEBUG = true;
#endif

			try
			{
				harmony = new Harmony(modEntry.Info.Id);
				harmony.PatchAll();

			}
			catch (Exception ex)
			{
				modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
				//harmony?.UnpatchAll();
				return false;
			}

			return true;
#endif
		}

		static void OnGUI(UnityModManager.ModEntry modEntry)
		{
			settings.Draw(modEntry);
			//GUILayout.BeginVertical();
			//settings.spawnRandomWA = GUILayout.Toggle(settings.spawnRandomWA, "Spawn S282 with random wheel arrangement");

		}

		static void OnSaveGUI(UnityModManager.ModEntry modEntry)
		{
			settings.Save(modEntry);
		}
	}
}
