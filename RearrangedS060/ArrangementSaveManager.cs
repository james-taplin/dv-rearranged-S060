using DV.JObjectExtstensions;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RearrangedS060
{

	//Thanks to Katycat for figuring out how to do this
	//https://github.com/derail-valley-modding/skin-manager/blob/master/SkinManagerMod/SaveLoadPatches.cs
	//https://github.com/derail-valley-modding/skin-manager/blob/master/SkinManagerMod/SkinManager.cs

	class ArrangementSaveManager
	{
		private const string WHEEL_ARRANGEMENT_SAVE_KEY = "RearrangedS060_wheelArrangment";

		private static readonly Random rand = new();

		[HarmonyPatch(typeof(SaveGameManager), nameof(SaveGameManager.Save))]
		class SaveGameManagerPatch
		{
			static void Prefix(SaveGameManager __instance)
			{
				JObject wheelArrangementSaveData = GetWheelArrangementData();
				SaveGameManager.Instance.data.SetJObject(WHEEL_ARRANGEMENT_SAVE_KEY, wheelArrangementSaveData);
			}
		}

		[HarmonyPatch(typeof(CarsSaveManager), nameof(CarsSaveManager.Load))]
		class CarsSaveManagerPatch
		{
			static void Prefix(JObject savedData)
			{
				if (savedData == null)
				{
					Main.Logger.Error("Save data is null: saved wheel arrangements will not be loaded");
					return;
				}
				JObject wheelArrangementSaveData = SaveGameManager.Instance.data.GetJObject(WHEEL_ARRANGEMENT_SAVE_KEY);
				if (wheelArrangementSaveData != null)
				{
					LoadWheelArrangementData(wheelArrangementSaveData);
				}
			}
		}

		private static readonly Dictionary<string, int> carGuidToWheelArrangementMap = new Dictionary<string, int>();

		public static JObject GetWheelArrangementData()
		{
			JObject wheelArrangmentSaveData = new JObject();
			JObject[] array = new JObject[carGuidToWheelArrangementMap.Count];
			int i = 0;
			foreach (var kvp in carGuidToWheelArrangementMap)
			{
				JObject dataObject = new JObject();
				dataObject.SetString("guid", kvp.Key);
				dataObject.SetInt("wheelArrangment", kvp.Value);
				array[i] = dataObject;
				i++;
			}
			wheelArrangmentSaveData.SetJObjectArray(CarsSaveManager.CARS_DATA_SAVE_KEY, array);
			return wheelArrangmentSaveData;
		}

		public static void LoadWheelArrangementData(JObject wheelArrangementSaveData)
		{
			JObject[] jobjectArray = wheelArrangementSaveData.GetJObjectArray(CarsSaveManager.CARS_DATA_SAVE_KEY);
			if (jobjectArray == null)
			{
				return;
			}
			foreach (JObject jobject in jobjectArray)
			{
				var guid = jobject.GetString("guid");
				var wheelArrangement = jobject.GetInt("wheelArrangment");
				if (!carGuidToWheelArrangementMap.ContainsKey(guid))
				{
					carGuidToWheelArrangementMap.Add(guid, (int)wheelArrangement);
				}
			}
		}

		public static int GetWheelArrangement(TrainCar car)
		{
			if (carGuidToWheelArrangementMap.ContainsKey(car.CarGUID))
			{
				return carGuidToWheelArrangementMap[car.CarGUID];
			}
			return -1;
		}

		public static void SetWheelArrangement(TrainCar car, WheelArrangementType type)
		{
			carGuidToWheelArrangementMap[car.CarGUID] = (int) type;
		}
	}
}
