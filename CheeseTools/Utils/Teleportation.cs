using OWML.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace CheeseTools.Utils {
	public static class Teleportation {
		public static void TeleportShipToPlayer() {
			CheeseTools.Console.WriteLine("Teleporting ship to player");
			OWRigidbody playerBody = Locator.GetPlayerBody();

			TeleportBodyTo(
				Locator.GetShipBody(),
				playerBody.transform.position + playerBody.transform.up * 10,
				playerBody.GetRotation(),
				playerBody.GetVelocity(),
				playerBody.GetAngularVelocity()
			);
		}

		public static void TeleportPlayerToShip(bool seated) {
			OWRigidbody ship = Locator.GetShipBody();
			TeleportBodyTo(Locator.GetPlayerBody(), ship.GetPosition(), ship.GetRotation(), ship.GetVelocity(), ship.GetAngularVelocity());

			HatchController hatchController = GameObject.FindObjectOfType<HatchController>();
			hatchController.OnEntry(Locator.GetPlayerDetector());

			var oxygenVolume = GameObject.Find("ShipAtmosphereVolume").GetComponent<OWTriggerVolume>();
			oxygenVolume.AddObjectToVolume(Locator.GetPlayerDetector());
			oxygenVolume.AddObjectToVolume(Locator.GetPlayerCameraDetector());

			var gravityVolume = GameObject.Find("ShipGravityVolume").GetComponent<OWTriggerVolume>();
			gravityVolume.AddObjectToVolume(Locator.GetPlayerDetector());
			gravityVolume.AddObjectToVolume(Locator.GetPlayerCameraDetector());

			if (seated) {
				CheeseTools.instance.ModHelper.Events.Unity.FireInNUpdates(() => {
					ShipCockpitController cockpitController = GameObject.FindObjectOfType<ShipCockpitController>();
					cockpitController.OnPressInteract();
				}, 2);
			}
		}

		public static void TeleportPlayerTo(OWRigidbody relativeBody, RelativeLocationData relativeLocation) => TeleportBodyTo(Locator.GetPlayerBody(), relativeBody, relativeLocation);

		public static void TeleportBodyTo(OWRigidbody body, OWRigidbody relativeBody, RelativeLocationData relativeLocation) {
			Vector3 worldPosition = relativeBody.transform.TransformPoint(relativeLocation.localPosition);
			body.WarpToPositionRotation(worldPosition, relativeBody.transform.rotation * relativeLocation.localRotation);
			body.SetVelocity(relativeBody.GetPointVelocity(worldPosition));
			body.SetAngularVelocity(relativeBody.GetAngularVelocity());
		}

		public static void TeleportBodyTo(OWRigidbody body, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity) {
			body.WarpToPositionRotation(position, rotation);
			body.SetVelocity(velocity);
			body.SetAngularVelocity(angularVelocity);
		}

		//public static void TeleportPlayerTo(OWRigidbody relativeBody, RelativeLocationData relativeLocation) {
		//	if (relativeLocation.localPosition == Vector3.zero) {
		//		CheeseTools.Console.WriteLine("Unable to teleport: Position is invalid", MessageType.Error);
		//		return;
		//	}

		//	//if (relativeBody.GetComponent<AstroObject>()?.GetAstroObjectName() == AstroObject.Name.RingWorld) LoadStrangerInterior(); else UnloadStrangerInterior();
		//	//if (relativeBody.GetComponent<AstroObject>()?.GetAstroObjectName() == AstroObject.Name.DreamWorld && !CheeseTools.isInDreamWorld) EnterDreamWorld();

		//	CheeseTools.instance.ModHelper.Events.Unity.FireInNUpdates(() => {
		//		TeleportBodyTo(Locator.GetPlayerBody(), relativeBody, relativeLocation);
		//	}, 2);
		//}

		//private static void LoadStrangerInterior() {
		//	Sector sector = GameObject.Find("Sector_RingInterior")?.GetComponent<Sector>();
		//	if (sector != null && !sector.GetOccupants().Contains(Locator.GetPlayerSectorDetector())) {
		//		sector.AddOccupant(Locator.GetPlayerSectorDetector());
		//	}
		//}

		//private static void UnloadStrangerInterior() {
		//	Sector sector = GameObject.Find("Sector_RingInterior")?.GetComponent<Sector>();
		//	if (sector != null && sector.GetOccupants().Contains(Locator.GetPlayerSectorDetector())) {
		//		sector.RemoveOccupant(Locator.GetPlayerSectorDetector());
		//	}
		//}
	}
}
