using UnityEngine;

namespace CheeseTools.Utils {
	public static class Teleportation {
		public static void TeleportShipToPlayer() {
			OWRigidbody playerBody = Locator.GetPlayerBody();

			TeleportBodyTo(
				Locator.GetShipBody(),
				playerBody.transform.position + playerBody.transform.up * 10,
				playerBody.GetRotation(),
				playerBody.GetVelocity(),
				playerBody.GetAngularVelocity()
			);
		}

		public static void TeleportPlayerToShip() {
			OWRigidbody ship = Locator.GetShipBody();
			TeleportBodyTo(Locator.GetPlayerBody(), ship.GetPosition(), ship.GetRotation(), ship.GetVelocity(), ship.GetAngularVelocity());

			HatchController hatchController = GameObject.FindObjectOfType<HatchController>();
			hatchController.OnEntry(Locator.GetPlayerDetector());

			OWTriggerVolume oxygenVolume = GameObject.Find("ShipAtmosphereVolume").GetComponent<OWTriggerVolume>();
			oxygenVolume.AddObjectToVolume(Locator.GetPlayerDetector());
			oxygenVolume.AddObjectToVolume(Locator.GetPlayerCameraDetector());

			OWTriggerVolume gravityVolume = GameObject.Find("ShipGravityVolume").GetComponent<OWTriggerVolume>();
			gravityVolume.AddObjectToVolume(Locator.GetPlayerDetector());
			gravityVolume.AddObjectToVolume(Locator.GetPlayerCameraDetector());

			// hacky fix for bug where gravity doesn't apply when seated
			ShipCockpitController cockpitController = GameObject.FindObjectOfType<ShipCockpitController>();
			cockpitController.ExitFlightConsole();
			cockpitController.CompleteExitFlightConsole();
			FixedUpdateDispatcher.FireAfterFixedUpdate(() => {
				cockpitController.OnPressInteract();
			});
		}

		public static void TeleportPlayerTo(OWRigidbody relativeBody, RelativeLocationData relativeLocation) {
			if (PlayerState.IsInsideShip()) {
				ShipCockpitController cockpitController = GameObject.FindObjectOfType<ShipCockpitController>();
				if (cockpitController._playerAtFlightConsole) {
					cockpitController.ExitFlightConsole();
					cockpitController.CompleteExitFlightConsole();
				}
				GameObject.FindObjectOfType<HatchController>().OpenHatch();
				GameObject.FindObjectOfType<ShipTractorBeamSwitch>().ActivateTractorBeam();
			}
			TeleportBodyTo(Locator.GetPlayerBody(), relativeBody, relativeLocation);
			Locator.GetPlayerCameraController().SetDegreesY(0f);
		}

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
	}
}
