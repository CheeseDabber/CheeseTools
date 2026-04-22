using OWML;
using UnityEngine;

namespace CheeseTools.Utils {
	public static class DreamWorldUtil {
		public enum DreamCampfireType {
			Undefined = 0,
			RiverLowlands = 1,
			CinderIsles = 2,
			HiddenGorge = 3,
			Reservoir = 4,
		}

		public static DreamCampfireType GetDreamCampfireTypeFromString(string name) {
			switch (name) {
				case "River Lowlands":
					return DreamCampfireType.RiverLowlands;
				case "Cinder Isles":
					return DreamCampfireType.CinderIsles;
				case "Hidden Gorge":
					return DreamCampfireType.HiddenGorge;
				case "Reservoir":
					return DreamCampfireType.Reservoir;
			}
			return DreamCampfireType.Undefined;
		}

		public static DreamCampfire GetDreamCampfire(DreamCampfireType dreamCampfire) {
			switch (dreamCampfire) {
				case DreamCampfireType.RiverLowlands:
					return Locator.GetDreamCampfire(DreamArrivalPoint.Location.Zone1);
				case DreamCampfireType.CinderIsles:
					return Locator.GetDreamCampfire(DreamArrivalPoint.Location.Zone2);
				case DreamCampfireType.HiddenGorge:
					return Locator.GetDreamCampfire(DreamArrivalPoint.Location.Zone3);
				case DreamCampfireType.Reservoir:
					return Locator.GetDreamCampfire(DreamArrivalPoint.Location.Zone4);
			}
			return null;
		}

		public static DreamCampfireType GetSettingsCampfireType() {
			return GetDreamCampfireTypeFromString(CheeseTools.instance.ModHelper.Config.GetSettingsValue<string>("DreamWorld Enter Campfire"));
		}

		public static void EnterDreamWorld() => EnterDreamWorld(GetSettingsCampfireType());

		public static void EnterDreamWorld(DreamCampfireType dreamCampfireType) {
			DreamCampfire dreamCampfire = GetDreamCampfire(dreamCampfireType);
			if (dreamCampfire == null) return;

			OpenSlidingDoor(dreamCampfire);
			RelativeLocationData relativeLocation = new RelativeLocationData(new Vector3(1.7f, 1.5f, -1), new Quaternion(0, 0, 0, 0), Vector3.zero);

			DreamArrivalPoint arrivalPoint = Locator.GetDreamArrivalPoint(dreamCampfire.GetLocation());
			if (Items.GetItemTool().GetHeldItemType() != ItemType.Lantern) {
				Items.PickUpItem(Items.GetDreamLantern());
			}
			Locator.GetDreamWorldController().EnterDreamWorld(dreamCampfire, arrivalPoint, relativeLocation);
		}

		public static void ExitDreamWorld() {
			Locator.GetDreamWorldController().ExitDreamWorld();
		}

		private static void OpenSlidingDoor(DreamCampfire dreamCampfire) {
			SlidingDoor door = null;
			foreach (SlidingDoor currentDoor in Resources.FindObjectsOfTypeAll<SlidingDoor>()) {
				float distance = Vector3.Distance(dreamCampfire.transform.position, currentDoor.transform.position);
				if (distance < 15f) {
					door = currentDoor;
				}
			}

			if (door == null) return;
			door.Open();
		}
	}
}
