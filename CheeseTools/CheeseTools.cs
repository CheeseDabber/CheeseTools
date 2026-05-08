using CheeseTools.Utils;
using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using OWML.Utils;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// TODO:
// - bramble practice state
// - cloneboosting setup option for instrument hunt
// - custom practice state settings like loop time and put on suit
// - look into ship gravity fix if its frame or fixedupdate dependant
// - stranger decloak
// - https://owml.outerwildsmods.com/guides/rebinding/ uhmm apparently made the keybinds class for nothing cause this exists??

// Bugs:
// - in death animation escape menu and loading scene doesnt work

namespace CheeseTools {
	public class CheeseTools : ModBehaviour {
		public static CheeseTools instance;
		public static IModConsole Console => instance.ModHelper.Console;
		public static Keybinds keybinds = new Keybinds();
		public static Action afterSceneLoad;
		public static bool inPracticeState = false;
		public static Action afterSleepUntil;
		public static double wakeUpTime = 0;

		private static int fixedUpdateCount = 0;
		private static ScreenPrompt loopTimeText = new ScreenPrompt("");
		private static NomaiWarpTransmitter atpWarpTransmitter => GameObject.Find("Prefab_NOM_WarpTransmitter (1)")?.GetComponent<NomaiWarpTransmitter>();
		private static NomaiWarpReceiver atpWarpReceiver => GameObject.Find("Interactibles_TimeLoopRing_Hidden/Prefab_NOM_WarpReceiver").GetComponent<NomaiWarpReceiver>();

		private static ScreenTimer atpEnterTimer = new ScreenTimer("ATP Enter Time: ");
		private static ScreenTimer atpInteriorTimer = new ScreenTimer("ATP Interior Time: ");
		private static ScreenTimer atpExitTimer = new ScreenTimer("ATP Exit Time: ");
		private static ScreenTimer feldsparringTimer = new ScreenTimer("Feldsparring Time: ");
		private static ScreenTimer warpTimer = new ScreenTimer("Warp Time: ");
		private static ScreenTimer observeTimer = new ScreenTimer("Observe Time: ");
		private static ScreenTimer cloneTimer = new ScreenTimer("Clone Time: ");
		public static ScreenTimer instrumentTimer = new ScreenTimer("Instrument Hunt Time: ");

		private static NomaiInterfaceOrb powerOrb;

		public void Awake() {
			instance = this;
		}

		public void Start() {
			Console.WriteLine($"{nameof(CheeseTools)} has been loaded!", MessageType.Success);
			new Harmony("CheeseRunner1.CheeseTools").PatchAll(Assembly.GetExecutingAssembly());

			OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen);
			LoadManager.OnCompleteSceneLoad += (OWScene previousScene, OWScene newScene) => {
				ModHelper.Events.Unity.FireOnNextUpdate(() => { OnCompleteSceneLoad(previousScene, newScene); });
			};

			GlobalMessenger.AddListener("StopSleepingAtCampfire", OnStopSleepingAtCampfire);
			GlobalMessenger.AddListener("StartVesselWarp", OnStartVesselWarp);
			GlobalMessenger<EyeState>.AddListener("EyeStateChanged", OnEyeStateChanged);
			GlobalMessenger<DeathType>.AddListener("PlayerDeath", OnPlayerDeath);

			ScreenTimerController.Start();
		}

		public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene) {
			//Console.WriteLine($"previousScene: {previousScene}, newScene: {newScene}");
			fixedUpdateCount = 0;
			afterSleepUntil = null;

			if (newScene == OWScene.SolarSystem) {
				Locator.GetPlayerSectorDetector().OnEnterSector += OnEnterSector;
				Locator.GetPlayerSectorDetector().OnExitSector += OnExitSector;
				bool showSector = ModHelper.Config.GetSettingsValue<bool>("Show Sectors");
				if (showSector) {
					foreach (Sector sector in Locator.GetPlayerSectorDetector()._sectorList) {
						AddScreenText(sector.gameObject.name, PromptPosition.BottomCenter);
					}
				}

				if (LoadManager.GetCurrentScene() == OWScene.SolarSystem) {
					atpWarpTransmitter.OnReceiveWarpedBody += OnReceiveWarpedBodyATPTransmitter;
					atpWarpReceiver.OnReceiveWarpedBody += OnReceiveWarpedBodyATPReceiver;
				}
				powerOrb = GameObject.Find("PowerSwitchInterface/Prefab_NOM_InterfaceOrb").GetComponent<NomaiInterfaceOrb>();
			}
			else {

			}
			if (newScene == OWScene.EyeOfTheUniverse) {
				if (IsTimerEnabled("Observe Timer")) {
					observeTimer.Restart();
				}
			}
			else {

			}
			if (newScene == OWScene.TitleScreen) {
				if (ModHelper.Config.GetSettingsValue<bool>("Create Launch Codes Save") && (previousScene == OWScene.SolarSystem || previousScene == OWScene.EyeOfTheUniverse)) {
					PlayerData.ResetGame();
					PlayerData.LearnLaunchCodes();
					PlayerData.SaveLoopCount(3);
				}
			}
			else {

			}

			if (Locator.GetPlayerBody() == null) return;
			UpdateInvincibility();
		}

		public void Update() {
			CheckInput();

			if (Locator.GetPlayerBody() == null) return;
			UpdateInfiniteResources();
			UpdateLoopTimeText();
			UpdateStrangerMarker();
			ScreenTimerController.Update();
			Locator.GetPauseCommandListener().enabled = true;
		}

		public void FixedUpdate() {
			fixedUpdateCount++;

			if (fixedUpdateCount == 2) {
				if (afterSceneLoad != null) {
					ModHelper.Events.Unity.FireOnNextUpdate(() => {
						Locator.GetPlayerCamera().GetComponent<PlayerCameraEffectController>().OpenEyes(0f);
						var reticle = GameObject.FindObjectOfType<ReticleController>()._image;
						reticle.color = new Color(reticle.color.r, reticle.color.g, reticle.color.b, 1f);

						afterSceneLoad();
						afterSceneLoad = null;
					});
				}
			}
		}

		private void CheckInput() {
			if (ModHelper.Config.GetSettingsValue<bool>("Log Names Of Pressed Keys")) {
				foreach (KeyControl key in Keyboard.current.allKeys) {
					if (key.wasPressedThisFrame) {
						Console.WriteLine($"Pressed Key: \"{Enum.GetName(typeof(Key), key.keyCode)}\"", MessageType.Info);
					}
				}
			}

			if (Locator.GetPlayerBody() == null) return;

			if (keybinds.Get(SettingKeybind.ToggleSuit)?.WasPressedThisFrame() == true) {
				PlayerSpacesuit spacesuit = Locator.GetPlayerSuit();
				if (!spacesuit.IsWearingSuit())
					spacesuit.SuitUp();
				else
					spacesuit.RemoveSuit();
			}
			else if (keybinds.Get(SettingKeybind.ToggleSpeedup)?.WasPressedThisFrame() == true) {
				ToggleSpeedUp();
			}
			else if (keybinds.Get(SettingKeybind.LogPlayerLocation)?.WasPressedThisFrame() == true) {
				OWRigidbody relativeBody = RelativeBody.GetCurrent();
				RelativeBody.PrintRelativeLocation("Player Position:\n", relativeBody, new RelativeLocationData(Locator.GetPlayerBody(), relativeBody));
			}
			else if (keybinds.Get(SettingKeybind.LogShipLocation)?.WasPressedThisFrame() == true) {
				OWRigidbody relativeBody = RelativeBody.GetCurrent();
				RelativeBody.PrintRelativeLocation("Ship Position:\n", relativeBody, new RelativeLocationData(Locator.GetShipBody(), relativeBody));
			}
			else if (keybinds.Get(SettingKeybind.TeleportShipToPlayer)?.WasPressedThisFrame() == true) {
				Teleportation.TeleportShipToPlayer();
			}
			else if (keybinds.Get(SettingKeybind.EnterExitDreamWorld)?.WasPressedThisFrame() == true) {
				if (!Locator.GetDreamWorldController()._insideDream) {
					DreamWorldUtil.EnterDreamWorld();
				}
				else {
					DreamWorldUtil.ExitDreamWorld();
				}
			}
			// Custom Practice States
			else if (keybinds.Get(SettingKeybind.CustomPracticeState1)?.WasPressedThisFrame() == true) {
				CustomPracticeState(1);
			}
			else if (keybinds.Get(SettingKeybind.CustomPracticeState2)?.WasPressedThisFrame() == true) {
				CustomPracticeState(2);
			}
			else if (keybinds.Get(SettingKeybind.CustomPracticeState3)?.WasPressedThisFrame() == true) {
				CustomPracticeState(3);
			}

			if (LoadManager.IsBusy()) return;

			if (keybinds.Get(SettingKeybind.FastLoadNewExpedition)?.WasPressedThisFrame() == true) {
				LoadSolarSystemScene(() => { });
			}
			//Practice States
			else if (keybinds.Get(SettingKeybind.ATPPracticeState)?.WasPressedThisFrame() == true) {
				string timeStr = ModHelper.Config.GetSettingsValue<string>("ATP Loop Time");
				if (!Double.TryParse(timeStr, out Double sleepTime)) {
					Console.WriteLine($"Invalid ATP Loop Time: \"{timeStr}\" is not recognized", MessageType.Warning);
					return;
				}

				LoadSolarSystemScene(() => {
					SleepUntil(sleepTime, () => {
						RelativeLocationData location = new RelativeLocationData(new Vector3(17.74f, -44.73f, 185.74f), Quaternion.Euler(new Vector3(294.14f, 63.13f, 124.75f)), Vector3.zero);
						Teleportation.TeleportPlayerTo(Locator.GetAstroObject(AstroObject.Name.TimberHearth).GetOWRigidbody(), location);
						Locator.GetPlayerSuit().SuitUp(false, true);

						if (IsTimerEnabled("ATP Exit Timer")) {
							atpExitTimer.Restart();
						}
						if (IsTimerEnabled("ATP Enter Timer")) {
							atpEnterTimer.Restart();
						}
					});
				}, true);
			}
			else if (keybinds.Get(SettingKeybind.ATPInteriorPracticeState)?.WasPressedThisFrame() == true) {
				LoadSolarSystemScene(() => {
					Locator.GetPlayerSuit().SuitUp(false, true);
					var sandSphere = GameObject.Find("SandSphere_Draining");
					sandSphere.GetComponent<SandLevelController>().enabled = false;
					sandSphere.transform.localScale = Vector3.zero;
					atpWarpTransmitter._alignmentWindow = 360f;
					Teleportation.TeleportPlayerTo(GameObject.Find("TowerTwin_Body").GetAttachedOWRigidbody(), new RelativeLocationData(new Vector3(-0.17f, 2.17f, -124.05f), Quaternion.Euler(271.01f, 3.51f, 356.50f), Vector3.zero));
					Locator.GetToolModeSwapper().EquipToolMode(ToolMode.Probe);
				}, false);
			}
			else if (keybinds.Get(SettingKeybind.FeldsparringPracticeState)?.WasPressedThisFrame() == true) {
				LoadSolarSystemScene(() => {
					Locator.GetPlayerSuit().SuitUp(false, true);
					RepairShip();
					OWRigidbody ship = Locator.GetShipBody();
					RelativeLocationData shipLocation = new RelativeLocationData(new Vector3(508.07f, 84.54f, -3248.96f), Quaternion.Euler(new Vector3(0.94f, 350.39f, 265.78f)), Vector3.zero);
					Teleportation.TeleportPlayerToShip();
					Teleportation.TeleportBodyTo(ship, Locator.GetAstroObject(AstroObject.Name.DarkBramble).GetOWRigidbody(), shipLocation);
					ship.SetVelocity(Locator.GetAstroObject(AstroObject.Name.DarkBramble).GetOWRigidbody().GetVelocity() + ship.transform.forward * 1150);
					Items.PickUpItem(Items.GetWarpCore());
				}, false);
			}
			else if (keybinds.Get(SettingKeybind.VesselPracticeState)?.WasPressedThisFrame() == true) {
				LoadSolarSystemScene(() => {
					Locator.GetPlayerSuit().SuitUp(false, true);
					RepairShip();
					OWRigidbody ship = Locator.GetShipBody();
					RelativeLocationData shipLocation = new RelativeLocationData(new Vector3(175.26f, -291.37f, -179.26f), Quaternion.Euler(27.46f, 111.93f, 285.54f), Vector3.zero);
					Teleportation.TeleportPlayerToShip();
					Teleportation.TeleportBodyTo(ship, Locator.GetMinorAstroObject("Angler Nest Dimension").GetAttachedOWRigidbody(), shipLocation);
					ship.SetVelocity(Locator.GetMinorAstroObject("Angler Nest Dimension").GetAttachedOWRigidbody().GetVelocity() + ship.transform.forward * 50);
					Items.PickUpItem(Items.GetWarpCore());
				}, false);
			}
			else if (keybinds.Get(SettingKeybind.VesselClipPracticeState)?.WasPressedThisFrame() == true) {
				LoadSolarSystemScene(() => {
					Locator.GetPlayerSuit().SuitUp(false, true);
					Teleportation.TeleportPlayerTo(GameObject.Find("DB_VesselDimension_Body").GetAttachedOWRigidbody(), new RelativeLocationData(new Vector3(175.66f, 13.39f, -19.34f), Quaternion.Euler(353.87f, 95.65f, 12.28f), Vector3.zero));

					VesselWarpController warpController = GameObject.Find("WarpController").GetComponent<VesselWarpController>();
					warpController._coreSocket.PlaceIntoSocket(Items.GetWarpCore());
					warpController._cageAnimator._transform.localPosition = new Vector3(0f, -8.1f, 0f);
					warpController._cageAnimator._transform.localRotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
					warpController._cageTrigger.OnExit -= warpController.OnExitCageTrigger;
					warpController._cageClosed = true;

					OWTriggerVolume gravityTrigger = GameObject.Find("GravityOxygenVolume_VesselBridge").GetComponent<OWTriggerVolume>();
					gravityTrigger.AddObjectToVolume(Locator.GetPlayerDetector());
					gravityTrigger.AddObjectToVolume(Locator.GetPlayerCameraDetector());

					NomaiInterfaceSlot vesselSlot = GameObject.Find("VesselWarpSlot").GetComponent<NomaiInterfaceSlot>();
					powerOrb.SetOrbPosition(vesselSlot.transform.position);

					NomaiCoordinateInterface coordinateInterface = warpController._coordinateInterface;
					coordinateInterface._pillarRoot.localPosition = new Vector3(coordinateInterface._pillarRoot.localPosition.x, 0f, coordinateInterface._pillarRoot.localPosition.z);
					coordinateInterface._pillarRaised = true;
					coordinateInterface._updateHeight = false;

					coordinateInterface._degrees = 240;
					coordinateInterface._basePivot.localEulerAngles = Vector3.up * coordinateInterface._degrees;
					coordinateInterface._activePanelIndex = 2;
					coordinateInterface._rotatingToPanel = false;

					coordinateInterface._upperOrb.RemoveAllLocks();
					coordinateInterface._upperOrb.AddLock();
					coordinateInterface._orb._lockCount = 1;
					coordinateInterface._orb._orbBody.Unsuspend();

					coordinateInterface._orb._isBeingDragged = false;
					coordinateInterface._rotateSlots[1]._occupyingOrb = coordinateInterface._orb;
					coordinateInterface._orb.SetOrbPosition(coordinateInterface._rotateSlots[1].transform.position);

					coordinateInterface._gateAnimators[0]._transform.localPosition = coordinateInterface._gateAnimators[0]._origLocalPosition;
					coordinateInterface._gateAnimators[1]._transform.localPosition = coordinateInterface._gateAnimators[1]._origLocalPosition;
					coordinateInterface._gateAnimators[2]._transform.localPosition = coordinateInterface._gateAnimators[2]._origLocalPosition;
					coordinateInterface._gateAnimators[3]._transform.localPosition = coordinateInterface._gateAnimators[3]._origLocalPosition;
					coordinateInterface._gateAnimators[3]._transform.localPosition = -coordinateInterface._gateAnimators[3]._transform.forward;

					SetCoordinate(coordinateInterface._nodeControllers[0], coordinateInterface._coordinateX);
					SetCoordinate(coordinateInterface._nodeControllers[1], coordinateInterface._coordinateY);
					SetCoordinate(coordinateInterface._nodeControllers[2], coordinateInterface._coordinateZ);

					// if you warp before bundles are loaded the game gets stuck infinitely loading.
					// so I just forcefully clear it. no clue if this breaks anything
					StreamingManager.s_activeBundles.Clear();
				}, true);
			}
			else if (keybinds.Get(SettingKeybind.ClonePracticeState)?.WasPressedThisFrame() == true) {
				LoadEyeScene(() => {
					OWRigidbody eyeBody = GameObject.Find("EyeOfTheUniverse_Body").GetAttachedOWRigidbody();
					Locator.GetPlayerSuit().SuitUp(false, true);
					Teleportation.TeleportPlayerTo(eyeBody, new RelativeLocationData(new Vector3(-80.616f, -3905.84f, 180.686f), Quaternion.identity, Vector3.zero));
				});
			}
			else if (keybinds.Get(SettingKeybind.InstrumentPracticeState)?.WasPressedThisFrame() == true) {
				LoadEyeScene(() => {
					Locator.GetEyeStateManager().SetState(EyeState.ForestIsDark);
					Teleportation.TeleportPlayerTo(GameObject.Find("EyeOfTheUniverse_Body").GetAttachedOWRigidbody(), new RelativeLocationData(new Vector3(-54.48f, 1.00f, 5999.10f), Quaternion.Euler(0.00f, 94.03f, 0.00f), Vector3.zero));
					Locator.GetFlashlight().TurnOn();
					Locator.GetToolModeSwapper().EquipToolMode(ToolMode.SignalScope);
					Locator.GetToolModeSwapper().GetSignalScope()._targetFOV = 60f;
				});
			}
		}

		private int lastFrameConfigureGotCalled = -1;
		public override void Configure(IModConfig config) {
			if (lastFrameConfigureGotCalled == Time.frameCount) return;
			lastFrameConfigureGotCalled = Time.frameCount;

			keybinds.Clear();
			keybinds.Add(SettingKeybind.ToggleSuit, config.GetSettingsValue<string>("Toggle Suit"));
			keybinds.Add(SettingKeybind.ToggleSpeedup, config.GetSettingsValue<string>("Toggle Speedup"));
			keybinds.Add(SettingKeybind.LogPlayerLocation, config.GetSettingsValue<string>("Log Player Location"));
			keybinds.Add(SettingKeybind.LogShipLocation, config.GetSettingsValue<string>("Log Ship Location"));
			keybinds.Add(SettingKeybind.TeleportShipToPlayer, config.GetSettingsValue<string>("Teleport Ship To Player"));
			keybinds.Add(SettingKeybind.FastLoadNewExpedition, config.GetSettingsValue<string>("Fast Load New Expedition"));
			keybinds.Add(SettingKeybind.EnterExitDreamWorld, config.GetSettingsValue<string>("Enter/Exit DreamWorld"));

			keybinds.Add(SettingKeybind.ATPPracticeState, config.GetSettingsValue<string>("ATP Practice State"));
			keybinds.Add(SettingKeybind.ATPInteriorPracticeState, config.GetSettingsValue<string>("ATP Interior Practice State"));
			keybinds.Add(SettingKeybind.FeldsparringPracticeState, config.GetSettingsValue<string>("Ultimate Feldsparring Practice State"));
			keybinds.Add(SettingKeybind.VesselPracticeState, config.GetSettingsValue<string>("Vessel Practice State"));
			keybinds.Add(SettingKeybind.VesselClipPracticeState, config.GetSettingsValue<string>("Vessel Clip Practice State"));
			keybinds.Add(SettingKeybind.ClonePracticeState, config.GetSettingsValue<string>("Clone Practice State"));
			keybinds.Add(SettingKeybind.InstrumentPracticeState, config.GetSettingsValue<string>("Instrument Hunt Practice State"));

			keybinds.Add(SettingKeybind.CustomPracticeState1, config.GetSettingsValue<string>("Custom Practice State 1"));
			keybinds.Add(SettingKeybind.CustomPracticeState2, config.GetSettingsValue<string>("Custom Practice State 2"));
			keybinds.Add(SettingKeybind.CustomPracticeState3, config.GetSettingsValue<string>("Custom Practice State 3"));

			if (Locator.GetPlayerBody() == null) return;
			UpdateInvincibility();
			UpdateSectorText();
		}

		public void OnPracticeState() {
			inPracticeState = true;
		}

		public void OnStartVesselWarp() {
			warpTimer.Stop();
		}

		public void OnEyeStateChanged(EyeState state) {
			//Console.WriteLine($"EyeState changed: {state}");
			if (state == EyeState.InstrumentHunt) {
				if (IsTimerEnabled("Instrument Hunt Timer")) {
					instrumentTimer.Restart();
				}
				if (cloneTimer.IsRunning == true) {
					if (ModHelper.Config.GetSettingsValue<bool>("Clone Trees Locator")) {
						RemoveMarker("Trees Location");
					}
					cloneTimer.Stop();
				}
			}
			if (state == EyeState.ZoomOut) {
				observeTimer.Stop();

				if (IsTimerEnabled("Clone Timer")) {
					cloneTimer.Restart();
					if (ModHelper.Config.GetSettingsValue<bool>("Clone Trees Locator")) {
						CanvasMarker marker = GetOrCreateMarker("Trees Location", GameObject.Find("EyeOfTheUniverse_Body").GetAttachedOWRigidbody(), new Vector3(-54.48f, 1.00f, 5999.10f));
						marker.SetVisibility(true);
					}
				}
			}
		}

		public void OnPlayerDeath(DeathType deathType) {
			if (deathType == DeathType.BigBang) {
				instrumentTimer.Stop();
			}
		}

		public void OnEnterSector(Sector sector) {
			if (ModHelper.Config.GetSettingsValue<bool>("Show Sectors")) {
				AddScreenText(sector.gameObject.name, PromptPosition.BottomCenter);
			}

			bool isFeldsparringTimer = IsTimerEnabled("Ultimate Feldsparring Timer");
			bool isWarpTimer = IsTimerEnabled("Warp Timer");
			if (sector.name == "Sector_AnglerNestDimension") {
				if (isFeldsparringTimer) {
					feldsparringTimer.Restart();
				}
			}
			else if (sector.name == "Sector_VesselDimension") {
				if (feldsparringTimer.IsRunning == true) {
					feldsparringTimer.Stop();
				}
				if (isWarpTimer) {
					warpTimer.Restart();
				}
			}
		}

		public void OnExitSector(Sector sector) {
			if (ModHelper.Config.GetSettingsValue<bool>("Show Sectors")) {
				RemoveScreenText(sector.gameObject.name, PromptPosition.BottomCenter);
			}
		}

		public void OnStopSleepingAtCampfire() {
			if (afterSleepUntil != null) {
				if (TimeLoop.GetSecondsElapsed() >= wakeUpTime) {
					afterSleepUntil();
					afterSleepUntil = null;
				}
				OWTime.Unpause(OWTime.PauseType.Sleeping);
			}
		}

		public void OnReceiveWarpedBodyATPTransmitter(OWRigidbody body, NomaiWarpPlatform startPlatform, NomaiWarpPlatform receivedPlatform) {
			if (body is PlayerBody) {
				atpInteriorTimer.Stop();
				atpExitTimer.Stop();
			}
		}

		public void OnReceiveWarpedBodyATPReceiver(OWRigidbody body, NomaiWarpPlatform startPlatform, NomaiWarpPlatform receivedPlatform) {
			if (body is PlayerBody) {
				if (atpWarpTransmitter._alignmentWindow == 360f)
					atpWarpTransmitter._alignmentWindow = 0f;

				atpEnterTimer.Stop();
				if (IsTimerEnabled("ATP Interior Timer")) {
					atpInteriorTimer.Restart();
				}
			}
		}

		public static void RepairShip() {
			ShipDamageController damageController = Locator.GetShipTransform()?.GetComponent<ShipDamageController>();
			if (damageController == null) return;

			foreach (ShipHull hull in damageController._shipHulls) {
				hull._integrity = 1f;
				hull.RepairTick();
			}

			foreach(ShipComponent component in damageController._shipComponents) {
				component.SetDamaged(false);
			}
		}

		public static void LoadSector(Sector sector) {
			if (sector != null && !sector.GetOccupants().Contains(Locator.GetPlayerSectorDetector())) {
				sector.AddOccupant(Locator.GetPlayerSectorDetector());
			}
		}

		public static void UnloadSector(Sector sector) {
			if (sector != null && sector.GetOccupants().Contains(Locator.GetPlayerSectorDetector())) {
				sector.RemoveOccupant(Locator.GetPlayerSectorDetector());
			}
		}

		public static void SetCoordinate(NomaiNodeController nodeController, int[] coordinate) {
			nodeController.ResetNodes();
			foreach (int i in coordinate) {
				if (nodeController._nodes[i].slot)
				nodeController.OnSlotActivated(nodeController._nodes[i].slot);
			}
		}

		public static void LoadSolarSystemScene(Action afterSceneLoad) {
			LoadSolarSystemScene(afterSceneLoad, instance.ModHelper.Config.GetSettingsValue<bool>("Create Launch Codes Save"));
		}

		public static void LoadSolarSystemScene(Action afterSceneLoad, bool launchCodes) {
			PlayerData.ResetGame();
			if (launchCodes) {
				PlayerData.LearnLaunchCodes();
				PlayerData.SaveLoopCount(3);
			}

			LoadManager.LoadScene(OWScene.SolarSystem);
			CheeseTools.afterSceneLoad = afterSceneLoad;
		}

		public static void LoadEyeScene(Action afterSceneLoad) {
			PlayerData.SaveWarpedToTheEye(TimeLoop.GetSecondsRemaining());
			LoadManager.LoadScene(OWScene.EyeOfTheUniverse);
			CheeseTools.afterSceneLoad = afterSceneLoad;
		}

		public static void SleepUntil(double seconds, Action afterSleepUntil) {
			Campfire campfire = GetClosestCampfire();
			campfire.StartSleeping();
			campfire.StartFastForwarding();

			wakeUpTime = seconds;
			CheeseTools.afterSleepUntil = afterSleepUntil;
		}

		public static Campfire GetClosestCampfire() {
			Campfire closest = null;
			float closestDistance = Mathf.Infinity;
			foreach (Campfire campfire in GameObject.FindObjectsOfType<Campfire>()) {
				float distance = Vector3.Distance(Locator.GetPlayerBody().GetPosition(), campfire.GetAttachedOWRigidbody().GetPosition());
				if (distance < closestDistance) {
					closest = campfire;
					closestDistance = distance;
				}
			}
			return closest;
		}

		public static void ToggleSpeedUp() {
			Time.timeScale = Time.timeScale == 1f ? 50f : 1f;
		}

		public static ScreenPrompt GetScreenPrompt(string text, PromptPosition position) {
			foreach (ScreenPrompt prompt in Locator.GetPromptManager().GetScreenPromptList(position)._listPrompts) {
				if (prompt._text == text) {
					return prompt;
				}
			}
			return null;
		}

		public static void AddScreenText(string text, PromptPosition position) {
			if (GetScreenPrompt(text, position) == null) {
				Locator.GetPromptManager().AddScreenPrompt(new ScreenPrompt(text), position, true);
			}
		}

		public static void RemoveScreenText(string text, PromptPosition position) {
			ScreenPrompt prompt = GetScreenPrompt(text, position);
			if (prompt != null) {
				Locator.GetPromptManager().RemoveScreenPrompt(prompt);
			}
		}

		public static CanvasMarker GetMarker(string label) {
			foreach (CanvasMarker marker in Locator.GetMarkerManager()._activeMarkers) {
				if (marker._label == label) {
					return marker;
				}
			}
			return null;
		}

		public static CanvasMarker GetOrCreateMarker(string label, OWRigidbody targetBody) {
			var markerManager = Locator.GetMarkerManager();
			CanvasMarker marker = GetMarker(label);

			if (marker == null) {
				marker = markerManager.InstantiateNewMarker();
				markerManager.RegisterMarker(marker, targetBody, label);
			}

			return marker;
		}

		public static CanvasMarker GetOrCreateMarker(string label, OWRigidbody parent, Vector3 localPosition) {
			var markerManager = Locator.GetMarkerManager();
			CanvasMarker marker = GetMarker(label);

			if (marker == null) {
				Transform transform = new GameObject($"CanvasMarker_{label}").transform;
				transform.SetParent(parent.transform);
				transform.localPosition = localPosition;

				marker = markerManager.InstantiateNewMarker();
				markerManager.RegisterMarker(marker, transform, label);
			}

			return marker;
		}

		public static void RemoveMarker(string label) {
			GetMarker(label)?.DestroyMarker();
		}

		public static Vector3 ConvertStringToVector3(string str) {
			string[] split = str.Split(',');
			return split.Length == 3 && float.TryParse(split[0], out float x) && float.TryParse(split[1], out float y) && float.TryParse(split[2], out float z) ? new Vector3(x, y, z) : Vector3.zero;
		}

		private bool IsTimerEnabled(string str) {
			return inPracticeState && ModHelper.Config.GetSettingsValue<bool>(str);
		}

		private void CustomPracticeState(int num) {
			OWRigidbody relativeBody = RelativeBody.GetFromString(ModHelper.Config.GetSettingsValue<string>($"Custom Practice State {num} Planet"));
			RelativeLocationData relativeLocation = new RelativeLocationData(ConvertStringToVector3(ModHelper.Config.GetSettingsValue<string>($"Custom Practice State {num} Position")),
				Quaternion.Euler(ConvertStringToVector3(ModHelper.Config.GetSettingsValue<string>($"Custom Practice State {num} Rotation"))),
				Vector3.zero
			);
			Teleportation.TeleportPlayerTo(relativeBody, relativeLocation);
		}

		private void UpdateInfiniteResources() {
			bool infFuel = ModHelper.Config.GetSettingsValue<bool>("Infinite Fuel");
			bool infOxygen = ModHelper.Config.GetSettingsValue<bool>("Infinite Oxygen");
			if ((infFuel || infOxygen) && Locator.GetPlayerTransform()?.TryGetComponent(out PlayerResources resources) == true) {
				if (infFuel)
					resources.SetValue("_currentFuel", resources.GetValue<float>("_maxFuel"));
				if (infOxygen)
					resources.SetValue("_currentOxygen", resources.GetValue<float>("_maxOxygen"));
			}
		}

		private void UpdateInvincibility() {
			ShipDamageController damageController = Locator.GetShipTransform()?.GetComponent<ShipDamageController>();
			if (damageController) {
				bool shipInvincible = ModHelper.Config.GetSettingsValue<bool>("Ship Invincibility");
				damageController._invincible = shipInvincible;
				if (shipInvincible) RepairShip();
			}

			bool playerInvincible = ModHelper.Config.GetSettingsValue<bool>("Player Invincibility");
			PlayerResources resources = Locator.GetPlayerTransform()?.GetComponent<PlayerResources>();
			resources._invincible = playerInvincible;
			if (playerInvincible) {
				resources._currentHealth = PlayerResources._maxHealth;
				resources.PatchAllPunctures();
			}
		}

		private void UpdateSectorText() {
			bool showSector = ModHelper.Config.GetSettingsValue<bool>("Show Sectors");
			if (showSector) {
				foreach (Sector sector in Locator.GetPlayerSectorDetector()._sectorList) {
					AddScreenText(sector.gameObject.name, PromptPosition.BottomCenter);
				}
			}
			else {
				foreach (Sector sector in Locator.GetPlayerSectorDetector()._sectorList) {
					RemoveScreenText(sector.gameObject.name, PromptPosition.BottomCenter);
				}
			}
		}

		private void UpdateLoopTimeText() {
			bool showLoopTime = ModHelper.Config.GetSettingsValue<bool>("Show Loop Time");
			if (showLoopTime && TimeLoop.IsTimeLoopEnabled() && TimeLoop.IsTimeFlowing()) {
				loopTimeText.SetText($"Loop Time: {TimeSpan.FromSeconds(TimeLoop.GetSecondsElapsed()).ToString(@"mm\:ss")} [{(int)TimeLoop.GetSecondsElapsed()}]");
				if (!Locator.GetPromptManager().GetScreenPromptList(PromptPosition.LowerLeft).Contains(loopTimeText)) {
					Locator.GetPromptManager().AddScreenPrompt(loopTimeText, PromptPosition.LowerLeft, true);
				}
			}
			else {
				Locator.GetPromptManager().RemoveScreenPrompt(loopTimeText);
			}
		}

		private void UpdateStrangerMarker() {
			var stranger = Locator.GetAstroObject(AstroObject.Name.RingWorld)?.GetOWRigidbody();
			if (stranger == null) return;

			var strangerMarker = GetOrCreateMarker("THE STRANGER", stranger);
			bool visible = ModHelper.Config.GetSettingsValue<bool>("Mark Stranger") && !Locator.GetDreamWorldController()._insideDream && strangerMarker.GetMarkerDistance() > 1000;
			if (strangerMarker.IsVisible() != visible) {
				strangerMarker.SetVisibility(visible);
			}
		}
	}
}
