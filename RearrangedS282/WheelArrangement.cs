using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RearrangedS282
{
	public enum WheelArrangementType
	{
		s440 = 0,
		s442,
		s444,
		s460,
		s462,
		s464,
		s080,
		s082,
		s084,
		s280,
		s282,
		s284,
		s280Big,
		s282Big,
		s284Big,
		s480,
		s482,
		s484,
		s0100,
		s0102,
		s0104,
		s2100,
		s2102,
		s2104,
		s4100,
		s4102,
		s4104,
		s0120,
		s2120,
		s2122,
		s4122,
		s2442,
		s4444,
		s404,
		s260 = 34,
		s240 = 35,
		s222 = 36,
		s2662 = 37,
		s22222 = 38,
		s4144 = 39,
		s02220 = 40,
		/*s2680,
		s4664*/
	}

	public static class WheelArrangement
	{
		// Legacy numeric IDs and name-table positions are save compatibility data.
		// IDs 31/32/37/38/40 identify articulated conversions; ID 33 is the unpowered novelty.
		// New conventional IDs are appended; never insert into the legacy range.
		public static bool IsStandardId(int id) => id >= 0 && id <= (int)WheelArrangementType.s4122;
		public static bool IsConventionalId(int id) => IsStandardId(id) ||
			(id >= (int)WheelArrangementType.s260 && id <= (int)WheelArrangementType.s222) || id == (int)WheelArrangementType.s4144;
		public static bool IsArticulatedId(int id) => id == (int)WheelArrangementType.s2442 || id == (int)WheelArrangementType.s4444 ||
			id == (int)WheelArrangementType.s2662 || id == (int)WheelArrangementType.s22222 || id == (int)WheelArrangementType.s02220;
		public static bool IsSupportedId(int id) => IsConventionalId(id) || IsArticulatedId(id) || id == (int)WheelArrangementType.s404;
		public const int SupportedCount = 41;

		public static int NormalizeSavedId(int id) => IsStandardId(id) ? id : (int)WheelArrangementType.s282;

		public static int NextStandardId(int id, int direction)
		{
			int count = (int)WheelArrangementType.s4122 + 1;
			return (NormalizeSavedId(id) + (direction < 0 ? count - 1 : 1)) % count;
		}

		public static bool IsRuntimeId(int id)
		{
#if B99_STAGE2
			return id == (int)WheelArrangementType.s282 || id == (int)WheelArrangementType.s284;
#else
			return IsSupportedId(id);
#endif
		}

		public static int NextRuntimeId(int id, int direction)
		{
#if B99_STAGE2
			return id == (int)WheelArrangementType.s282 ? (int)WheelArrangementType.s284 : (int)WheelArrangementType.s282;
#else
			int current = IsSupportedId(id) ? id : (int)WheelArrangementType.s282;
			return (current + (direction < 0 ? SupportedCount - 1 : 1)) % SupportedCount;
#endif
		}

		//The idea here is that the values of the enum WheelArrangementType can be
		//used as indices for the array WheelArrangementNames.
		//Ideally I would have used an enum with a string value (like in Rust), or
		//a union (like in C or C++), but C# doesn't support either.

		public static readonly string[] WheelArrangementNames =
		{
			"4-4-0 American",
			"4-4-2 Atlantic",
			"4-4-4 Reading",
			"4-6-0 Ten-wheeler",
			"4-6-2 Pacific",
			"4-6-4 Hudson",
			"0-8-0 Eight–wheel switcher",
			"0-8-2",
			"0-8-4",
			"2-8-0 Consolidation",
			"2-8-2 Mikado",
			"2-8-4 Berkshire",
			"2-8-0 High-speed Consolidation",
			"2-8-2 High-speed Mikado",
			"2-8-4 High-speed Berkshire",
			"4-8-0 Twelve wheeler",
			"4-8-2 Mountain",
			"4-8-4 Northern",
			"0-10-0 Ten-wheel switcher",
			"0-10-2 Union",
			"0-10-4",
			"2-10-0 Decapod",
			"2-10-2 Santa Fe",
			"2-10-4 Texas",
			"4-10-0 Mastodon",
			"4-10-2 Southern Pacific",
			"4-10-4",
			"0-12-0 Twelve-wheel switcher",
			"2-12-0",
			"2-12-2",
			"4-12-2 Union Pacific",
			"2-4-4-2 Articulated",
			"4-4-4-4 Articulated",
			"4-0-4 Drivers not found",
			"2-6-0 Mogul",
			"2-4-0",
			"2-2-2 Giant-driver clearance prototype",
			"2-6-6-2 Articulated Mallet",
			"2-2-2-2-2 Articulated Triplex",
			"4-14-4",
			"0-2-2-2-0 Articulated Triplex",
			/*"2-6-8-0",
			"4-6-6-4 Challenger"*/
		};

		public static readonly float[] NumOfNondrivenWheels =
		{
			4,//"4-4-0 American",
			6,//"4-4-2 Atlantic",
			8,//"4-4-4 Reading",
			4,//"4-6-0 Ten-wheeler",
			6,//"4-6-2 Pacific",
			8,//"4-6-4 Hudson",
			0,//"0-8-0 Eight–wheel switcher",
			2,//"0-8-2",
			4,//"0-8-4",
			2,//"2-8-0 Consolidation",
			4,//"2-8-2 Mikado",
			6,//"2-8-4 Berkshire",
			2,//"2-8-0 High-speed Consolidation",
			4,//"2-8-2 High-speed Mikado",
			6,//"2-8-4 High-speed Berkshire",
			4,//"4-8-0 Twelve wheeler",
			6,//"4-8-2 Mountain",
			8,//"4-8-4 Northern",
			0,//"0-10-0 Ten-wheel switcher",
			2,//"0-10-2 Union",
			4,//"0-10-4",
			2,//"2-10-0 Decapod",
			4,//"2-10-2 Santa Fe",
			6,//"2-10-4 Texas",
			4,//"4-10-0 Mastodon",
			6,//"4-10-2 Southern Pacific",
			8,//"4-10-4",
			0,//"0-12-0 Twelve-wheel switcher",
			2,//"2-12-0",
			4,//"2-12-2",
			6,//"4-12-2 Union Pacific",
			4,//"2-4-4-2",
			8,//"4-4-4-4",
			8,//"4-0-4",
			2,//"2-6-0 Mogul",
			2,//"2-4-0",
			4,//"2-2-2 Giant-driver clearance prototype",
			4,//"2-6-6-2 Articulated Mallet",
			4,//"2-2-2-2-2 Articulated Triplex",
			8,//"4-14-4",
			0,//"0-2-2-2-0 Articulated Triplex",
		};
	}
}
