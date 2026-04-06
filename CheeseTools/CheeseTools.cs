using CheeseTools.Utils;
using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using OWML.Utils;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

// planned features:
// - stranger decloak
// - ship invincibility

// bugs:
// - teleporting from stranger does some weird shit to velocity
// - sector text dissapearing upon entering dreamworld
// - some null errors when quitting to menu

//TODO:
// - Add timers category in settings and make then toggleable there instead of depending on practice state activation
// - ATP interior pratice state
// - List out all relative body names and put them in config

namespace CheeseTools {
	public class CheeseTools : ModBehaviour {
		public static CheeseTools instance;
		public static IModConsole Console => instance.ModHelper.Console;
		public static bool isInDreamWorld = false;

		private static Action afterEyeWarp;
		private static bool hasInfResources = false;
		private static ScreenPrompt loopTimeText = new ScreenPrompt("");
		private static CanvasMarker strangerMarker;
		private static NomaiWarpTransmitter atpWarpTransmitter => GameObject.Find("Prefab_NOM_WarpTransmitter (1)")?.GetComponent<NomaiWarpTransmitter>();
		private static NomaiWarpReceiver atpWarpReceiver => GameObject.Find("Interactibles_TimeLoopRing_Hidden/Prefab_NOM_WarpReceiver").GetComponent<NomaiWarpReceiver>();

		private static bool isSleeping = false;
		private static Campfire campfire;
		private static double wakeUpTime = 0;
		private static Action onWakeUp;

		private static ScreenTimer atpEnterTimer = new ScreenTimer("ATP Enter Time: ");
		private static ScreenTimer atpInteriorTimer = new ScreenTimer("ATP Interior Time: ");
		private static ScreenTimer atpExitTimer = new ScreenTimer("ATP Exit Time: ");
		private static ScreenTimer feldsparringTimer = new ScreenTimer("Feldsparring Time: ");
		private static ScreenTimer eyeTimer = new ScreenTimer("Eye Time: ");
		private static ScreenTimer observeTimer = new ScreenTimer("Observe Time: ");
		private static ScreenTimer cloneTimer = new ScreenTimer("Clone Time: ");
		private static ScreenTimer instrumentTimer = new ScreenTimer("Instrument Hunt Time: ");

		public void Awake() {
			instance = this;
		}

		public void Start() {
			Console.WriteLine($"{nameof(CheeseTools)} has been loaded!", MessageType.Success);
			new Harmony("CheeseRunner1.CheeseTools").PatchAll(Assembly.GetExecutingAssembly());

			OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen);
			LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;

			GlobalMessenger.AddListener("WakeUp", OnWakeUp);
			GlobalMessenger.AddListener("EnterDreamWorld", OnEnterDreamWorld);
			GlobalMessenger.AddListener("ExitDreamWorld", OnExitDreamWorld);
			GlobalMessenger.AddListener("StopSleepingAtCampfire", OnStopSleepingAtCampfire);
			GlobalMessenger.AddListener("StartVesselWarp", OnStartVesselWarp);
			GlobalMessenger<EyeState>.AddListener("EyeStateChanged", OnEyeStateChanged);
		}

		public void OnWakeUp() {
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
		}

		public void Update() {
			if (!InWorld()) return;

			if (Keyboard.current[Key.Slash].IsPressed() && Keyboard.current[Key.R].wasPressedThisFrame) {
				hasInfResources = !hasInfResources;
				if (hasInfResources) {
					AddScreenText("Infinite Resources Enabled", PromptPosition.UpperLeft);
					Console.WriteLine("Infinite resources enabled");
				}
				else {
					RemoveScreenText("Infinite Resources Enabled", PromptPosition.UpperLeft);
					Console.WriteLine("Infinite resources disabled");
				}
			}
			else if (Keyboard.current[Key.Slash].IsPressed() && Keyboard.current[Key.T].wasPressedThisFrame) {
				PlayerSpacesuit spacesuit = Locator.GetPlayerSuit();
				if (!spacesuit.IsWearingSuit()) {
					spacesuit.SuitUp();
					Console.WriteLine("Suited up");
				}
				else {
					spacesuit.RemoveSuit();
					Console.WriteLine("Suit removed");
				}
			}
			else if (Keyboard.current[Key.Slash].IsPressed() && Keyboard.current[Key.RightBracket].wasPressedThisFrame) {
				OWRigidbody relativeBody = RelativeBody.GetCurrent();
				RelativeBody.PrintRelativeLocation("Player Position:\n", relativeBody, new RelativeLocationData(Locator.GetPlayerBody(), relativeBody));
			}
			else if (Keyboard.current[Key.Slash].IsPressed() && Keyboard.current[Key.LeftBracket].wasPressedThisFrame) {
				OWRigidbody relativeBody = RelativeBody.GetCurrent();
				RelativeBody.PrintRelativeLocation("Ship Position:\n", relativeBody, new RelativeLocationData(Locator.GetShipBody(), relativeBody));
			}
			else if (Keyboard.current[Key.Slash].IsPressed() && Keyboard.current[Key.Backquote].wasPressedThisFrame) {
				Teleportation.TeleportShipToPlayer();
			}
			else if (Keyboard.current[Key.Slash].IsPressed() && Keyboard.current[Key.Digit1].wasPressedThisFrame) {
				OWRigidbody relativeBody = RelativeBody.GetFromString(ModHelper.Config.GetSettingsValue<string>("Player Teleport 1 Planet"));
				RelativeLocationData relativeLocation = new RelativeLocationData(ConvertStringToVector3(ModHelper.Config.GetSettingsValue<string>("Player Teleport 1 Position")),
					Quaternion.Euler(ConvertStringToVector3(ModHelper.Config.GetSettingsValue<string>("Player Teleport 1 Rotation"))),
					Vector3.zero
				);
				Teleportation.TeleportPlayerTo(relativeBody, relativeLocation);
			}
			else if (Keyboard.current[Key.Slash].IsPressed() && Keyboard.current[Key.Digit2].wasPressedThisFrame) {
				OWRigidbody relativeBody = RelativeBody.GetFromString(ModHelper.Config.GetSettingsValue<string>("Player Teleport 2 Planet"));
				RelativeLocationData relativeLocation = new RelativeLocationData(ConvertStringToVector3(ModHelper.Config.GetSettingsValue<string>("Player Teleport 2 Position")),
					Quaternion.Euler(ConvertStringToVector3(ModHelper.Config.GetSettingsValue<string>("Player Teleport 2 Rotation"))),
					Vector3.zero
				);
				Teleportation.TeleportPlayerTo(relativeBody, relativeLocation);
			}
			else if (Keyboard.current[Key.Slash].IsPressed() && Keyboard.current[Key.Digit3].wasPressedThisFrame) {
				OWRigidbody relativeBody = RelativeBody.GetFromString(ModHelper.Config.GetSettingsValue<string>("Player Teleport 3 Planet"));
				RelativeLocationData relativeLocation = new RelativeLocationData(ConvertStringToVector3(ModHelper.Config.GetSettingsValue<string>("Player Teleport 3 Position")),
					Quaternion.Euler(ConvertStringToVector3(ModHelper.Config.GetSettingsValue<string>("Player Teleport 3 Rotation"))),
					Vector3.zero
				);
				Teleportation.TeleportPlayerTo(relativeBody, relativeLocation);
			}
			else if (Keyboard.current[Key.Slash].IsPressed() && Keyboard.current[Key.L].wasPressedThisFrame) {
				if (!isInDreamWorld) {
					DreamWorldUtil.EnterDreamWorld();
				}
				else {
					DreamWorldUtil.ExitDreamWorld();
				}
			}
			else if (Keyboard.current[Key.Slash].IsPressed() && Keyboard.current[Key.Y].wasPressedThisFrame) {
				OWRigidbody player = Locator.GetPlayerBody();
				player.SetPosition(player.GetPosition() + player.transform.up * -10);
			}
			else if (Keyboard.current[Key.Escape].wasPressedThisFrame) {
				Locator.GetMenuInputModule().ProcessMouseEvent();
			}
			else if (Keyboard.current[Key.Slash].IsPressed() && Keyboard.current[Key.Backslash].wasPressedThisFrame) {
				ToggleSpeedUp();
			}
			else if (Keyboard.current[Key.P].IsPressed() && Keyboard.current[Key.Digit1].wasPressedThisFrame) {
				// ATP practice state
				SleepUntil(441, () => {
					RelativeLocationData location = new RelativeLocationData(new Vector3(17.74f, -44.73f, 185.74f), Quaternion.Euler(new Vector3(294.14f, 63.13f, 124.75f)), Vector3.zero);
					Teleportation.TeleportPlayerTo(Locator.GetAstroObject(AstroObject.Name.TimberHearth).GetOWRigidbody(), location);
					Locator.GetPlayerSuit().SuitUp(false, true);

					if (ModHelper.Config.GetSettingsValue<bool>("ATP Exit Timer")) {
						atpExitTimer.Start();
					}
					if (ModHelper.Config.GetSettingsValue<bool>("ATP Enter Timer")) {
						atpEnterTimer.Start();
					}
				});
			}
			else if (Keyboard.current[Key.P].IsPressed() && Keyboard.current[Key.Digit2].wasPressedThisFrame) {
				// Ultimate Feldsparring practice state
				Locator.GetPlayerSuit().SuitUp(false, true);
				OWRigidbody ship = Locator.GetShipBody();
				RelativeLocationData shipLocation = new RelativeLocationData(new Vector3(508.07f, 84.54f, -3248.96f), Quaternion.Euler(new Vector3(0.94f, 350.39f, 265.78f)), Vector3.zero);
				Teleportation.TeleportPlayerToShip(true);
				Teleportation.TeleportBodyTo(ship, Locator.GetAstroObject(AstroObject.Name.DarkBramble).GetOWRigidbody(), shipLocation);
				ship.SetVelocity(Locator.GetAstroObject(AstroObject.Name.DarkBramble).GetOWRigidbody().GetVelocity() + ship.transform.forward * 1150);
				Items.PickUpItem(Items.GetWarpCore());
			}
			else if (Keyboard.current[Key.P].IsPressed() && Keyboard.current[Key.Digit3].wasPressedThisFrame) {
				// Observe practice state
				Locator.GetPlayerSuit().SuitUp(false, true);
				RelativeLocationData location = new RelativeLocationData(new Vector3(117.16f, 6.99f, -13.36f), Quaternion.Euler(351.76f, 95.56f, 11.61f), Vector3.zero);
				Teleportation.TeleportPlayerTo(Locator.GetMinorAstroObject("Vessel Dimension").GetOWRigidbody(), location);
				LoadSector(GameObject.Find("Sector_VesselDimension").GetComponent<Sector>());
				Items.PickUpItem(Items.GetWarpCore());
			}
			else if (Keyboard.current[Key.P].IsPressed() && Keyboard.current[Key.Digit4].wasPressedThisFrame) {
				// Clone practice state
				WarpToEye(() => {
					Teleportation.TeleportPlayerTo(GameObject.Find("EyeOfTheUniverse_Body").GetAttachedOWRigidbody(), new RelativeLocationData(new Vector3(-1050.61f, -3927.77f, 2104.22f), Quaternion.identity, Vector3.zero));
				});
			}
			else if (Keyboard.current[Key.P].IsPressed() && Keyboard.current[Key.Digit5].wasPressedThisFrame) {
				// Instrument hunt practice state
				WarpToEye(() => {
					Locator.GetEyeStateManager().SetState(EyeState.ForestIsDark);
					Teleportation.TeleportPlayerTo(GameObject.Find("EyeOfTheUniverse_Body").GetAttachedOWRigidbody(), new RelativeLocationData(new Vector3(-54.48f, 1.00f, 5999.10f), Quaternion.Euler(0.00f, 94.03f, 0.00f), Vector3.zero));
					Locator.GetFlashlight().TurnOn();
					Locator.GetToolModeSwapper().EquipToolMode(ToolMode.SignalScope);
					Locator.GetToolModeSwapper().GetSignalScope()._targetFOV = 60f;
				});
			}
			else if (Keyboard.current[Key.Slash].IsPressed() && Keyboard.current[Key.H].wasPressedThisFrame) {
				LoadManager.LoadScene(OWScene.EyeOfTheUniverse);
				Teleportation.TeleportPlayerTo(GameObject.Find("EyeOfTheUniverse_Body").GetAttachedOWRigidbody(), new RelativeLocationData(new Vector3(-54.48f, 1.00f, 5999.10f), Quaternion.Euler(0.00f, 94.03f, 0.00f), Vector3.zero));
				//Locator.GetEyeStateManager().SetState(EyeState.ForestOfGalaxies);
			}

			ScreenTimerController.Update();
			UpdateInfiniteResources();
			UpdateStrangerMarker();
			UpdateLoopTimeText();

			if (isSleeping) {
				if (TimeLoop.GetSecondsElapsed() < wakeUpTime) {
					campfire._fastForwardMultiplier = Mathf.Clamp((float)wakeUpTime - TimeLoop.GetSecondsElapsed(), 2f, 50f);
					GameObject.FindObjectOfType<SleepTimerUI>()._text.text = $"Sleeping until {TimeSpan.FromSeconds(wakeUpTime).ToString(@"m\:ss")}\n" + TimeSpan.FromSeconds(TimeLoop.GetSecondsElapsed()).ToString(@"m\:ss");
				}
				else {
					if (!OWTime.IsPaused()) {
						OWTime.Pause(OWTime.PauseType.Sleeping);
					}
					GameObject.FindObjectOfType<SleepTimerUI>()._text.text = $"Ready. Wake up to start\n" + TimeSpan.FromSeconds(TimeLoop.GetSecondsElapsed()).ToString(@"m\:ss");
				}
			}

			if (instrumentTimer?.IsRunning == true && GameObject.FindObjectOfType<CosmicInflationController>()?._state == CosmicInflationController.State.Collapsing) {
				Locator.GetPromptManager().SetPromptsVisible(true);
				instrumentTimer.Stop();
			}
		}

		public override void Configure(IModConfig config) {
			if (Locator.GetPlayerSectorDetector() != null) {
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
		}

		public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene) {
			//if (newScene == OWScene.EyeOfTheUniverse) {
			//	Locator.GetEyeStateManager()._initialState = EyeState.ForestOfGalaxies;
			//}


			//Console.WriteLine($"previousScene: {previousScene}, newScene: {newScene}");
			hasInfResources = false;

			atpEnterTimer.Stop();
			atpInteriorTimer.Stop();
			atpExitTimer.Stop();
			feldsparringTimer.Stop();
			eyeTimer.Stop();
			observeTimer.Stop();
			cloneTimer.Stop();
			instrumentTimer.Stop();

			if (ModHelper.Config.GetSettingsValue<bool>("Create Launch Codes Save")) {
				if ((previousScene == OWScene.SolarSystem || previousScene == OWScene.EyeOfTheUniverse) && newScene == OWScene.TitleScreen) {
					PlayerData.ResetGame();
					PlayerData.LearnLaunchCodes();
					PlayerData.SaveLoopCount(3);
				}
			}

			if (afterEyeWarp != null) {
				if (newScene == OWScene.EyeOfTheUniverse) {
					ModHelper.Events.Unity.FireOnNextUpdate(() => {
						afterEyeWarp();
					});
				}
				else {
					afterEyeWarp = null;
				}
			}

			if (ModHelper.Config.GetSettingsValue<bool>("Observe Timer") && afterEyeWarp == null && newScene == OWScene.EyeOfTheUniverse) {
				observeTimer.Start();
			}
		}

		public void OnStartVesselWarp() {
			eyeTimer.Stop();
		}

		public void OnEyeStateChanged(EyeState state) {
			Console.WriteLine($"EyeState changed: {state}");
			if (state == EyeState.InstrumentHunt) {
				if (ModHelper.Config.GetSettingsValue<bool>("Instrument Hunt Timer")) {
					instrumentTimer = new ScreenTimer("Instrument Hunt Time: ");
					instrumentTimer.Start();
				}
				if (cloneTimer.IsRunning == true) {
					cloneTimer.Stop();
				}
			}
			if (state == EyeState.ZoomOut) {
				observeTimer.Stop();

				if (ModHelper.Config.GetSettingsValue<bool>("Clone Timer")) {
					cloneTimer.Start();
				}
			}
		}

		public void OnEnterDreamWorld() {
			isInDreamWorld = true;
		}

		public void OnExitDreamWorld() {
			isInDreamWorld = false;
		}

		public void OnEnterSector(Sector sector) {
			bool showSector = ModHelper.Config.GetSettingsValue<bool>("Show Sectors");
			if (showSector) {
				AddScreenText(sector.gameObject.name, PromptPosition.BottomCenter);
			}

			bool isFeldsparringTimer = ModHelper.Config.GetSettingsValue<bool>("Ultimate Feldsparring Timer");
			bool isEyeTimer = ModHelper.Config.GetSettingsValue<bool>("The Eye Timer");
			if (sector.name == "Sector_AnglerNestDimension") {
				if (isFeldsparringTimer) {
					feldsparringTimer.Start();
				}
			}
			else if (sector.name == "Sector_VesselDimension") {
				if (feldsparringTimer.IsRunning == true) {
					feldsparringTimer.Stop();
				}
				if (isEyeTimer) {
					eyeTimer.Start();
				}
			}
		}

		public void OnExitSector(Sector sector) {
			bool showSector = ModHelper.Config.GetSettingsValue<bool>("Show Sectors");
			if (showSector) {
				RemoveScreenText(sector.gameObject.name, PromptPosition.BottomCenter);
			}
		}

		public void OnStopSleepingAtCampfire() {
			if (isSleeping) {
				isSleeping = false;
				if (onWakeUp != null && TimeLoop.GetSecondsElapsed() >= wakeUpTime) {
					onWakeUp();
					onWakeUp = null;
				}
				OWTime.Unpause(OWTime.PauseType.Sleeping);
			}
		}

		public void OnReceiveWarpedBodyATPTransmitter(OWRigidbody body, NomaiWarpPlatform startPlatform, NomaiWarpPlatform receivedPlatform) {
			if (body is PlayerBody && Items.GetItemTool().GetHeldItemType() == ItemType.WarpCore) {
				atpInteriorTimer.Stop();
				atpExitTimer.Stop();
			}
		}

		public void OnReceiveWarpedBodyATPReceiver(OWRigidbody body, NomaiWarpPlatform startPlatform, NomaiWarpPlatform receivedPlatform) {
			if (body is PlayerBody) {
				atpEnterTimer.Stop();
				if (ModHelper.Config.GetSettingsValue<bool>("ATP Interior Timer")) {
					atpInteriorTimer.Start();
				}
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

		public static bool InWorld() {
			return LoadManager.GetCurrentScene() == OWScene.SolarSystem || LoadManager.GetCurrentScene() == OWScene.EyeOfTheUniverse;
		}

		public static void WarpToEye(Action afterEyeWarp) {
			PlayerData.SaveWarpedToTheEye(TimeLoop.GetSecondsRemaining());
			LoadManager.LoadScene(OWScene.EyeOfTheUniverse);
			CheeseTools.afterEyeWarp = afterEyeWarp;
		}

		public static void SleepUntil(double seconds, Action onWakeUp) {
			campfire = GetClosestCampfire();
			campfire.StartSleeping();
			campfire._fastForwardStartTime = Time.timeSinceLevelLoad;
			campfire.StartFastForwarding();

			wakeUpTime = seconds;
			CheeseTools.onWakeUp = onWakeUp;
			isSleeping = true;
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

		public static Vector3 ConvertStringToVector3(string str) {
			string[] split = str.Split(',');
			return split.Length == 3 && float.TryParse(split[0], out float x) && float.TryParse(split[1], out float y) && float.TryParse(split[2], out float z) ? new Vector3(x, y, z) : Vector3.zero;
		}

		private bool InitStrangerMarker() {
			if (strangerMarker != null) return true;

			CanvasMarkerManager markerManager = Locator.GetMarkerManager();
			OWRigidbody stranger = Locator.GetAstroObject(AstroObject.Name.RingWorld)?.GetOWRigidbody();
			if (markerManager == null || stranger == null) return false;

			strangerMarker = markerManager.InstantiateNewMarker();
			markerManager.RegisterMarker(strangerMarker, stranger, "THE STRANGER");
			return true;
		}

		private void UpdateLoopTimeText() {
			bool showLoopTime = ModHelper.Config.GetSettingsValue<bool>("Show Loop Time");
			if (showLoopTime && TimeLoop.IsTimeLoopEnabled() && TimeLoop.IsTimeFlowing()) {
				loopTimeText.SetText($"Loop Time: [{TimeSpan.FromSeconds(TimeLoop.GetSecondsElapsed()).ToString(@"m\:ss")}]");
				if (!Locator.GetPromptManager().GetScreenPromptList(PromptPosition.LowerLeft).Contains(loopTimeText)) {
					Locator.GetPromptManager().AddScreenPrompt(loopTimeText, PromptPosition.LowerLeft, true);
				}
			}
			else {
				Locator.GetPromptManager().RemoveScreenPrompt(loopTimeText);
			}
		}

		private void UpdateStrangerMarker() {
			if (!InitStrangerMarker()) return;

			bool visible = ModHelper.Config.GetSettingsValue<bool>("Mark Stranger Location") ? !isInDreamWorld && strangerMarker.GetMarkerDistance() > 1000 : false;
			if (strangerMarker.IsVisible() != visible) {
				strangerMarker.SetVisibility(visible);
				Console.WriteLine($"Stranger marker visibility set to {visible}");
			}
		}

		private void UpdateInfiniteResources() {
			if (!hasInfResources) return;
			PlayerResources resources = null;
			Locator.GetPlayerTransform()?.TryGetComponent(out resources);
			if (resources == null) return;

			resources.SetValue("_currentFuel", resources.GetValue<float>("_maxFuel"));
			resources.SetValue("_currentOxygen", resources.GetValue<float>("_maxOxygen"));
		}
	}
}
