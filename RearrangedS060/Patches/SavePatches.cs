using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityModManagerNet.UnityModManager;

namespace RearrangedS060.Patches
{

	internal class SavePatches
	{
		/*public const string SAVE_DATA_KEY = "Mod_RearrangedS060";

		// Save Configuration:

		//  SaveData
		//      Mod_NumManager
		//          carNumbers[]
		//              entry
		//                  guid
		//                  wheelArrangement
		//              entry
		//                  guid
		//                  wheelArrangement

		public struct CarSaveEntry
		{
			public string guid;
			public int? wheelArrangement;

			public CarSaveEntry(string guid, int? wheelArrangement)
			{
				this.guid = guid;
				this.wheelArrangement = wheelArrangement;
			}
		}

		public class NumberData
		{
			public CarSaveEntry[]? wheelArrangements;
		}

		public static void SetWheelArrangement(TrainCar car, int guid)

		public static void LoadSaveData()
		{
			var wheelArrangementData = SaveGameManager.Instance.data.GetObject<NumberData>(SAVE_DATA_KEY);

			if ((wheelArrangementData != null) && (wheelArrangementData.wheelArrangements != null))
			{
				Main.Logger.Log($"Loaded data, {wheelArrangementData.wheelArrangements.Length} entries");
				foreach (var entry in wheelArrangementData.wheelArrangements)
				{
					if (!string.IsNullOrEmpty(entry.guid) && entry.wheelArrangement.HasValue)
					{
						SetCarNumber(entry.guid, entry.wheelArrangement.Value);
					}
				}
			}
		}*/
	}
}
