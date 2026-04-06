using UnityEngine;
using OWML.Common;

namespace CheeseTools.Utils {
	public static class Items {
		public static ItemTool GetItemTool() {
			return Locator.GetToolModeSwapper().GetItemCarryTool();
		}

		public static void PickUpItem(OWItem item) {
			if (item == null || item.gameObject == null) {
				CheeseTools.Console.WriteLine("Item is null", MessageType.Error);
				return;
			}

			GetItemTool().PickUpItemInstantly(item);
		}

		public static DreamLanternItem GetDreamLantern() {
			foreach (DreamLanternItem lantern in Resources.FindObjectsOfTypeAll<DreamLanternItem>()) {
				if (lantern.IsInteractable() && lantern.GetLanternType() == DreamLanternType.Functioning) {
					return lantern;
				}
			}
			return null;
		}

		public static WarpCoreItem GetWarpCore() {
			foreach (WarpCoreItem warpCore in Resources.FindObjectsOfTypeAll<WarpCoreItem>()) {
				if (warpCore.GetWarpCoreType() == WarpCoreType.Vessel) {
					return warpCore;
				}
			}
			return null;
		}
	}
}
