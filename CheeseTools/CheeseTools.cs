using CheeseTools.Utils;
using HarmonyLib;
using System.Globalization;
using OWML.Common;
using OWML.ModHelper;
using OWML.Utils;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace CheeseTools {
    public class CheeseTools : ModBehaviour {
        public static CheeseTools instance;
        public static IModConsole Console => instance.ModHelper.Console;
        public static Keybinds keybinds = new Keybinds();
        public static Action afterSceneLoad;
        public static bool skipWakeUpAnim = false;
        public static string currentPracticeState = "";
        public static Action afterSleepUntil;
        public static double wakeUpTime = 0;
        public static float speedupTimeScale = 51f;

        private static string version = "1.1.1";
        private static ScreenPrompt watermark = new ScreenPrompt($"CheeseTools v{version}: Enabled");
        private static ScreenPrompt loopTimeText = new ScreenPrompt("");
        private static EyeState afterSceneLoadEyeState;
        private static NomaiWarpTransmitter atpWarpTransmitter => GameObject.Find("Prefab_NOM_WarpTransmitter (1)")?.GetComponent<NomaiWarpTransmitter>();
        private static NomaiWarpReceiver atpWarpReceiver => GameObject.Find("Interactibles_TimeLoopRing_Hidden/Prefab_NOM_WarpReceiver").GetComponent<NomaiWarpReceiver>();
        private static NomaiInterfaceOrb powerOrb;

        private static ScreenPrompt shipBonkPrompt = new ScreenPrompt("Ship Bonk: Enabled");
        private static ScreenPrompt shuttleBonkPrompt = new ScreenPrompt("Shuttle Bonk: Enabled");
        private static ScreenPrompt helBonkPrompt = new ScreenPrompt("HEL Bonk: Enabled");
        private static ScreenPrompt strangerBonkPrompt = new ScreenPrompt("Stranger Bonk: Enabled");
        private static ScreenPrompt caveBonkPrompt = new ScreenPrompt("0g Cave Bonk: Enabled");

        private static ScreenTimer villageTimer = new ScreenTimer("Village Time: ");
        private static ScreenTimer atpEnterTimer = new ScreenTimer("ATP Enter Time: ");
        private static ScreenTimer atpInteriorTimer = new ScreenTimer("ATP Interior Time: ");
        private static ScreenTimer atpExitTimer = new ScreenTimer("ATP Exit Time: ");
        private static ScreenTimer brambleTimer = new ScreenTimer("Bramble Timer: ");
        private static ScreenTimer feldsparringTimer = new ScreenTimer("Feldsparring Time: ");
        private static ScreenTimer warpTimer = new ScreenTimer("Warp Time: ");
        public static ScreenTimer coordinatesTimer = new ScreenTimer("Coordinates Time: ");
        private static ScreenTimer museumTimer = new ScreenTimer("Museum Time: ");
        private static ScreenTimer observeTimer = new ScreenTimer("Observe Time: ");
        private static ScreenTimer museumObserveTimer = new ScreenTimer("Museum+Observe Time: ");
        private static ScreenTimer cloneTimer = new ScreenTimer("Clone Time: ");
        public static ScreenTimer instrumentTimer = new ScreenTimer("Instrument Hunt Time: ");

        public void Awake() {
            instance = this;
        }

        public void Start() {
            new Harmony("CheeseRunner1.CheeseTools").PatchAll(Assembly.GetExecutingAssembly());

            OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen);
            LoadManager.OnCompleteSceneLoad += (OWScene previousScene, OWScene newScene) => {
                if (afterSceneLoad != null && newScene == OWScene.EyeOfTheUniverse) {
                    Locator.GetEyeStateManager()._initialState = afterSceneLoadEyeState;
                }
                ModHelper.Events.Unity.FireOnNextUpdate(() => { OnCompleteSceneLoad(previousScene, newScene); });
            };

            GlobalMessenger<bool>.AddListener("StartSleepingAtCampfire", OnStartSleepingAtCampfire);
            GlobalMessenger.AddListener("StopSleepingAtCampfire", OnStopSleepingAtCampfire);
            GlobalMessenger.AddListener("StartVesselWarp", OnStartVesselWarp);
            GlobalMessenger<EyeState>.AddListener("EyeStateChanged", OnEyeStateChanged);
            GlobalMessenger<DeathType>.AddListener("PlayerDeath", OnPlayerDeath);
            GlobalMessenger<Signalscope>.AddListener("EnterSignalscopeZoom", OnEnterSignalScopeZoom);

            ScreenTimerController.Start();
            Console.WriteLine($"{nameof(CheeseTools)} v{version} initialized.", MessageType.Success);
        }

        public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene) {
            //Console.WriteLine($"previousScene: {previousScene}, newScene: {newScene}");
            afterSleepUntil = null;

            if (newScene == OWScene.SolarSystem) {
                Locator.GetPlayerSectorDetector().OnEnterSector += OnEnterSector;
                Locator.GetPlayerSectorDetector().OnExitSector += OnExitSector;
                atpWarpTransmitter.OnReceiveWarpedBody += OnReceiveWarpedBodyATPTransmitter;
                atpWarpReceiver.OnReceiveWarpedBody += OnReceiveWarpedBodyATPReceiver;
                powerOrb = GameObject.Find("PowerSwitchInterface/Prefab_NOM_InterfaceOrb").GetComponent<NomaiInterfaceOrb>();

                if (currentPracticeState != "") {
                    var coordinateInterface = GameObject.Find("WarpController").GetComponent<VesselWarpController>()._coordinateInterface;
                    coordinateInterface._lowerPillarSlot.OnSlotActivated += slot => {
                        if (coordinateInterface.CheckEyeCoordinates()) {
                            coordinatesTimer.Stop();
                        }
                    };
                }

                if (ModHelper.Config.GetSettingsValue<bool>("Learn Quantum Frequency")) {
                    PlayerData.LearnFrequency(SignalFrequency.Quantum);
                }

                if (ModHelper.Config.GetSettingsValue<bool>("Show Sectors")) {
                    foreach (Sector sector in Locator.GetPlayerSectorDetector()._sectorList) {
                        AddScreenText(sector.gameObject.name, PromptPosition.BottomCenter);
                    }
                }
            }
            else {

            }
            if (newScene == OWScene.EyeOfTheUniverse) {
                if (Locator.GetEyeStateManager().GetState() == EyeState.AboardVessel && currentPracticeState != "Clone Practice State") {
                    if (IsTimerEnabled("Museum Timer"))
                        museumTimer.Start();
                    if (IsTimerEnabled("Museum+Observe Timer"))
                        museumObserveTimer.Start();
                }
            }
            else {

            }
            if (newScene == OWScene.TitleScreen) {
                currentPracticeState = "";
                if (ModHelper.Config.GetSettingsValue<bool>("Create Launch Codes Save !OVERWRITES SAVEFILE!") && (previousScene == OWScene.SolarSystem || previousScene == OWScene.EyeOfTheUniverse)) {
                    PlayerData.ResetGame();
                    PlayerData.LearnLaunchCodes();
                    PlayerData.SaveLoopCount(3);
                }
            }
            else {

            }

            if (Locator.GetPlayerBody() == null) return;

            UpdateInvincibility();
            if (afterSceneLoad != null) {
                FixedUpdateDispatcher.FireAfterNFixedUpdates(() => {
                    if (skipWakeUpAnim) {
                        Locator.GetPlayerCamera().GetComponent<PlayerCameraEffectController>().OpenEyes(0f);
                        var reticle = GameObject.FindObjectOfType<ReticleController>()._image;
                        reticle.color = new Color(reticle.color.r, reticle.color.g, reticle.color.b, 1f);
                    }

                    afterSceneLoad();
                    afterSceneLoad = null;

                    if (currentPracticeState != "" && ModHelper.Config.GetSettingsValue<bool>("Suitless Practice States")) {
                        RemoveSpacesuit(true);
                    }
                }, 2);
            }
        }

        public void Update() {
            CheckInput();
            UpdateWatermark();

            if (Locator.GetPlayerBody() == null) return;

            UpdateInfiniteResources();
            UpdateLoopTimeText();
            ScreenTimerController.Update();
            Locator.GetPauseCommandListener().enabled = true;

            if (EntitlementsManager.IsDlcOwned() != EntitlementsManager.AsyncOwnershipStatus.Owned) return;

            UpdateStrangerMarker();
        }

        public void FixedUpdate() {
            FixedUpdateDispatcher.FixedUpdate();
        }

        private void CheckInput() {
            if (ModHelper.Config.GetSettingsValue<bool>("Log Names Of Pressed Keys")) {
                foreach (KeyControl key in Keyboard.current.allKeys) {
                    if (key.wasPressedThisFrame) {
                        Console.WriteLine($"Pressed Key: \"{Enum.GetName(typeof(Key), key.keyCode)}\"", MessageType.Info);
                    }
                }
            }

            if (!PlayerData.IsLoaded() || LoadManager.IsBusy()) return;

            if (keybinds.Get("Fast Load New Expedition")?.WasPressedThisFrame() == true) {
                currentPracticeState = "";
                if (ModHelper.Config.GetSettingsValue<bool>("Create Launch Codes Save !OVERWRITES SAVEFILE!")) {
                    PlayerData.ResetGame();
                    PlayerData.LearnLaunchCodes();
                    PlayerData.SaveLoopCount(3);
                }
                LoadSolarSystemScene(() => { }, false);
            }
            //Practice States
            else if (keybinds.Get("Village Practice State !RESETS SAVEFILE!")?.WasPressedThisFrame() == true) {
                PlayerData.ResetGame();
                LoadSolarSystemScene(() => {
                    if (IsTimerEnabled("Village Timer")) {
                        villageTimer.Start();
                    }
                }, false);
            }
            else if (keybinds.Get("ATP Practice State")?.WasPressedThisFrame() == true) {
                if (!PlayerData.KnowsLaunchCodes()) {
                    PlayerData.LearnLaunchCodes();
                    PlayerData.SaveLoopCount(3);
                }
                LoadSolarSystemScene(() => {
                    SleepUntil(ModHelper.Config.GetSettingsValue<double>("ATP Loop Time"), () => {
                        EquipSpacesuit(true);
                        RelativeLocationData location = new RelativeLocationData(new Vector3(17.74f, -44.73f, 185.74f), Quaternion.Euler(new Vector3(294.14f, 63.13f, 124.75f)), Vector3.zero);
                        Teleportation.TeleportPlayerTo(Locator.GetAstroObject(AstroObject.Name.TimberHearth).GetOWRigidbody(), location);

                        if (IsTimerEnabled("ATP Exit Timer")) {
                            atpExitTimer.Start();
                        }
                        if (IsTimerEnabled("ATP Enter Timer")) {
                            atpEnterTimer.Start();
                        }
                    });
                });
            }
            else if (keybinds.Get("ATP Interior Practice State")?.WasPressedThisFrame() == true) {
                LoadSolarSystemScene(() => {
                    EquipSpacesuit(true);
                    var sandSphere = GameObject.Find("SandSphere_Draining");
                    sandSphere.GetComponent<SandLevelController>().enabled = false;
                    sandSphere.transform.localScale = Vector3.zero;
                    atpWarpTransmitter._alignmentWindow = 360f;
                    Teleportation.TeleportPlayerTo(GameObject.Find("TowerTwin_Body").GetAttachedOWRigidbody(), new RelativeLocationData(new Vector3(-0.17f, 2.17f, -124.05f), Quaternion.Euler(271.01f, 3.51f, 356.50f), Vector3.zero));
                    Locator.GetToolModeSwapper().EquipToolMode(ToolMode.Probe);
                });
            }
            else if (keybinds.Get("Bramble Practice State")?.WasPressedThisFrame() == true) {
                if (!PlayerData.KnowsLaunchCodes()) {
                    PlayerData.LearnLaunchCodes();
                    PlayerData.SaveLoopCount(3);
                }
                LoadSolarSystemScene(() => {
                    SleepUntil(490, () => {
                        EquipSpacesuit(true);
                        Items.PickUpItem(Items.GetWarpCore());
                        Locator.GetToolModeSwapper().EquipToolMode(ToolMode.Probe);

                        var sandSphere = GameObject.Find("SandSphere_Draining");
                        sandSphere.GetComponent<SandLevelController>().enabled = false;
                        sandSphere.transform.localScale = Vector3.zero;

                        var timeLoopRingController = GameObject.FindObjectOfType<TimeLoopRingController>();
                        timeLoopRingController._ringBody.SetAngularVelocity(Vector3.zero);
                        timeLoopRingController.SetRunning(false);
                        var receiver = atpWarpReceiver;
                        receiver._returnPlatform = atpWarpTransmitter;
                        receiver._returnOnEntry = true;
                        receiver._returnGlowFadeController.SetFade(1f);

                        Teleportation.TeleportBodyTo(Locator.GetShipBody(), GameObject.Find("TowerTwin_Body").GetAttachedOWRigidbody(), new RelativeLocationData(new Vector3(-3.68f, 1.20f, -128.05f), Quaternion.Euler(326.12f, 117.71f, 254.52f), Vector3.zero));
                        Teleportation.TeleportPlayerTo(GameObject.Find("TimeLoopRing_Body").GetAttachedOWRigidbody(), new RelativeLocationData(new Vector3(0.0f, 10.0f, 0.0f), Quaternion.Euler(272.28f, 84.02f, 5.81f), Vector3.zero));
                        var playerBody = Locator.GetPlayerBody();
                        playerBody.SetVelocity(GameObject.Find("TimeLoopRing_Body").GetAttachedOWRigidbody().GetVelocity() + playerBody.transform.forward * 5);
                    });
                });
            }
            else if (keybinds.Get("Ultimate Feldsparring Practice State")?.WasPressedThisFrame() == true) {
                LoadSolarSystemScene(() => {
                    EquipSpacesuit(true);
                    RepairShip();
                    OWRigidbody ship = Locator.GetShipBody();
                    RelativeLocationData shipLocation = new RelativeLocationData(new Vector3(508.07f, 84.54f, -3248.96f), Quaternion.Euler(new Vector3(0.94f, 350.39f, 265.78f)), Vector3.zero);
                    Teleportation.TeleportPlayerToShip();
                    Teleportation.TeleportBodyTo(ship, Locator.GetAstroObject(AstroObject.Name.DarkBramble).GetOWRigidbody(), shipLocation);
                    ship.SetVelocity(Locator.GetAstroObject(AstroObject.Name.DarkBramble).GetOWRigidbody().GetVelocity() + ship.transform.forward * ModHelper.Config.GetSettingsValue<int>("Ultimate Feldsparring Ship Speed"));
                    Items.PickUpItem(Items.GetWarpCore());
                });
            }
            else if (keybinds.Get("Vessel Practice State")?.WasPressedThisFrame() == true) {
                LoadSolarSystemScene(() => {
                    EquipSpacesuit(true);
                    RepairShip();
                    OWRigidbody ship = Locator.GetShipBody();
                    RelativeLocationData shipLocation = new RelativeLocationData(new Vector3(-11.94f, -284.85f, -138.69f), Quaternion.Euler(9.78f, 104.03f, 296.32f), Vector3.zero);
                    Teleportation.TeleportPlayerToShip();
                    Teleportation.TeleportBodyTo(ship, Locator.GetMinorAstroObject("Angler Nest Dimension").GetAttachedOWRigidbody(), shipLocation);
                    ship.SetVelocity(Locator.GetMinorAstroObject("Angler Nest Dimension").GetAttachedOWRigidbody().GetVelocity() + ship.transform.forward * 100);
                    Items.PickUpItem(Items.GetWarpCore());
                });
            }
            else if (keybinds.Get("Vessel Clip Practice State")?.WasPressedThisFrame() == true) {
                LoadSolarSystemScene(() => {
                    EquipSpacesuit(true);
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

                    coordinateInterface._upperOrb.RemoveAllLocks();
                    coordinateInterface._upperOrb.AddLock();
                    coordinateInterface._orb._lockCount = 1;
                    coordinateInterface._orb._orbBody.Unsuspend();

                    if (ModHelper.Config.GetSettingsValue<bool>("Fill In Eye Coordinates")) {
                        coordinateInterface._degrees = 240;
                        coordinateInterface._basePivot.localEulerAngles = Vector3.up * coordinateInterface._degrees;
                        coordinateInterface._activePanelIndex = 2;
                        coordinateInterface._rotatingToPanel = false;

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
                    }

                    // if you warp before bundles are loaded the game gets stuck infinitely loading.
                    // so I just forcefully clear it. no clue if this breaks anything
                    StreamingManager.s_activeBundles.Clear();
                });
            }
            else if (keybinds.Get("Clone Practice State")?.WasPressedThisFrame() == true) {
                LoadEyeScene(EyeState.AboardVessel, () => {
                    EquipSpacesuit(true);
                    OWRigidbody eyeBody = GameObject.Find("EyeOfTheUniverse_Body").GetAttachedOWRigidbody();
                    Teleportation.TeleportPlayerTo(eyeBody, new RelativeLocationData(new Vector3(-80.616f, -3905.84f, 180.686f), Quaternion.identity, Vector3.zero));
                });
            }
            else if (keybinds.Get("Instrument Hunt Practice State")?.WasPressedThisFrame() == true) {
                currentPracticeState = "Instrument Hunt Practice State";

                if (ModHelper.Config.GetSettingsValue<bool>("Menu Storage !RESETS SAVEFILE!")) {
                    PlayerData.ResetGame();

                    PlayerData.SetPersistentCondition("MET_SOLANUM", ModHelper.Config.GetSettingsValue<bool>("Solanum"));
                    if (EntitlementsManager.IsDlcOwned() == EntitlementsManager.AsyncOwnershipStatus.Owned)
                        PlayerData.SetPersistentCondition("MET_PRISONER", ModHelper.Config.GetSettingsValue<bool>("Prisoner"));

                    LoadManager.LoadScene(OWScene.EyeOfTheUniverse);
                    return;
                }

                PlayerData.SetPersistentCondition("MET_SOLANUM", ModHelper.Config.GetSettingsValue<bool>("Solanum"));
                if (EntitlementsManager.IsDlcOwned() == EntitlementsManager.AsyncOwnershipStatus.Owned)
                    PlayerData.SetPersistentCondition("MET_PRISONER", ModHelper.Config.GetSettingsValue<bool>("Prisoner"));

                LoadEyeScene(EyeState.ForestIsDark, () => {
                    EquipSpacesuit(true);
                    Locator.GetFlashlight().TurnOn();
                    Locator.GetToolModeSwapper().GetSignalScope()._targetFOV = 60f;
                    NotificationManager.SharedInstance.ClearAllNotifications();

                    Quaternion playerOrientation;
                    if (ModHelper.Config.GetSettingsValue<bool>("Cloneboosting Setup")) {
                        playerOrientation = Quaternion.Euler(0f, 268f, 0f);
                        Locator.GetToolModeSwapper().EquipToolMode(ToolMode.Probe);

                        ModHelper.Events.Unity.FireOnNextUpdate(() => {
                            var probe = Locator.GetProbe();
                            var probeLauncher = GameObject.FindObjectOfType<ProbeLauncher>();
                            probeLauncher._activeProbe = probe;
                            probeLauncher._allowRetrieval = false;
                            probeLauncher._preLaunchProbeProxy.SetActive(false);
                            probe.Launch(probeLauncher._launcherTransform, Vector3.zero);
                            Teleportation.TeleportBodyTo(probe.GetOWRigidbody(), GameObject.Find("EyeOfTheUniverse_Body").GetAttachedOWRigidbody(), new RelativeLocationData(new Vector3(-65f, 1f, 5999f), Quaternion.Euler(90f, 0f, 0f), Vector3.zero));
                        });
                    }
                    else {
                        playerOrientation = Quaternion.Euler(0f, 95f, 0f);
                        Locator.GetToolModeSwapper().EquipToolMode(ToolMode.SignalScope);
                    }
                    Teleportation.TeleportPlayerTo(GameObject.Find("EyeOfTheUniverse_Body").GetAttachedOWRigidbody(), new RelativeLocationData(new Vector3(-54.48f, 1f, 5999.10f), playerOrientation, Vector3.zero));
                });
            }
            // Custom Practice States
            else if (keybinds.Get("Custom Practice State 1")?.WasPressedThisFrame() == true) {
                CustomPracticeState(1);
            }
            else if (keybinds.Get("Custom Practice State 2")?.WasPressedThisFrame() == true) {
                CustomPracticeState(2);
            }
            else if (keybinds.Get("Custom Practice State 3")?.WasPressedThisFrame() == true) {
                CustomPracticeState(3);
            }
            else if (keybinds.Get("Custom Practice State 4")?.WasPressedThisFrame() == true) {
                CustomPracticeState(4);
            }
            else if (keybinds.Get("Custom Practice State 5")?.WasPressedThisFrame() == true) {
                CustomPracticeState(5);
            }
            else if (keybinds.Get("Custom Practice State 6")?.WasPressedThisFrame() == true) {
                CustomPracticeState(6);
            }
            else if (keybinds.Get("Custom Practice State 7")?.WasPressedThisFrame() == true) {
                CustomPracticeState(7);
            }
            else if (keybinds.Get("Custom Practice State 8")?.WasPressedThisFrame() == true) {
                CustomPracticeState(8);
            }
            else if (keybinds.Get("Custom Practice State 9")?.WasPressedThisFrame() == true) {
                CustomPracticeState(9);
            }
            else if (keybinds.Get("Custom Practice State 10")?.WasPressedThisFrame() == true) {
                CustomPracticeState(10);
            }
            // dev keybinds for testing
            //else if (Keyboard.current[Key.Slash].IsPressed() && Keyboard.current[Key.F1].wasPressedThisFrame) {
            //}

            if (Locator.GetPlayerBody() == null) return;

            if (keybinds.Get("Toggle Spacesuit")?.WasPressedThisFrame() == true) {
                if (!Locator.GetPlayerSuit().IsWearingSuit())
                    EquipSpacesuit(false);
                else
                    RemoveSpacesuit(false);
            }
            else if (keybinds.Get("Toggle Speedup")?.WasPressedThisFrame() == true) {
                ToggleSpeedup();
            }
            else if (keybinds.Get("Log Player Location")?.WasPressedThisFrame() == true) {
                OWRigidbody relativeBody = RelativeBody.GetCurrent();
                RelativeBody.PrintRelativeLocation("Player Location:\n", relativeBody, new RelativeLocationData(Locator.GetPlayerBody(), relativeBody));
            }
            else if (keybinds.Get("Teleport Ship To Player")?.WasPressedThisFrame() == true) {
                Teleportation.TeleportShipToPlayer();
            }
            else if (keybinds.Get("Give Warp Core")?.WasPressedThisFrame() == true) {
                Items.PickUpItem(Items.GetWarpCore());
            }

            if (LoadManager.GetCurrentScene() == OWScene.SolarSystem) {
                if (keybinds.Get("Toggle Bonk")?.WasPressedThisFrame() == true) {
                    string type = ModHelper.Config.GetSettingsValue<string>("Bonk Type");
                    if (type == "Ship") {
                        if (ToggleBonk(shipBonkPrompt, GameObject.Find("ShipGravityEntryTrigger").GetComponent<EntrywayTrigger>(), GameObject.Find("ShipGeneralEntryTrigger").GetComponent<EntrywayTrigger>())) {
                            PlayerState._isAttached = true;
                            HatchController hatchController = GameObject.FindObjectOfType<HatchController>();
                            hatchController.OpenHatch();
                            ShipTractorBeamSwitch tractorBeam = GameObject.FindObjectOfType<ShipTractorBeamSwitch>();
                            tractorBeam.DeactivateTractorBeam();
                        } else {
                            PlayerState._isAttached = false;
                            HatchController hatchController = GameObject.FindObjectOfType<HatchController>();
                            hatchController.OpenHatch();
                            ShipTractorBeamSwitch tractorBeam = GameObject.FindObjectOfType<ShipTractorBeamSwitch>();
                            tractorBeam.ActivateTractorBeam();
                        }
                    }
                    else if (type == "Shuttle") {
                        ToggleBonk(shuttleBonkPrompt, GameObject.Find("ShuttleVolume/EntrywayTrigger").GetComponent<EntrywayTrigger>());
                    }
                    else if (type == "HEL") {
                        ToggleBonk(helBonkPrompt, GameObject.Find("EntrywayTrigger_TLE_1").GetComponent<EntrywayTrigger>());
                    }
                    else if (type == "Stranger" && EntitlementsManager.IsDlcOwned() == EntitlementsManager.AsyncOwnershipStatus.Owned) {
                        OWTriggerVolume volume = GameObject.Find("RingInteriorSectorTriggerVolume").GetComponent<OWTriggerVolume>();
                        if (!Locator.GetPromptManager().GetScreenPromptList(PromptPosition.LowerLeft).Contains(strangerBonkPrompt)) {
                            Locator.GetPromptManager().AddScreenPrompt(strangerBonkPrompt, PromptPosition.LowerLeft, true);
                            volume.AddObjectToVolume(Locator.GetPlayerDetector());
                            volume.AddObjectToVolume(Locator.GetPlayerCameraDetector());
                        }
                        else {
                            Locator.GetPromptManager().RemoveScreenPrompt(strangerBonkPrompt);
                            volume.RemoveObjectFromVolume(Locator.GetPlayerDetector());
                            volume.RemoveObjectFromVolume(Locator.GetPlayerCameraDetector());
                        }
                    }
                    else if (type == "0g Cave") {
                        ToggleBonk(caveBonkPrompt, GameObject.Find("EntryWayTrigger_ZeroGCave").GetComponent<EntrywayTrigger>());
                    }
                }
            }

            if (EntitlementsManager.IsDlcOwned() != EntitlementsManager.AsyncOwnershipStatus.Owned) return;

            if (keybinds.Get("Enter/Exit DreamWorld")?.WasPressedThisFrame() == true) {
                if (!Locator.GetDreamWorldController()._insideDream) {
                    DreamWorldUtil.EnterDreamWorld();
                }
                else {
                    DreamWorldUtil.ExitDreamWorld();
                }
            }
            else if (keybinds.Get("Give Dream Lantern")?.WasPressedThisFrame() == true) {
                Items.PickUpItem(Items.GetDreamLantern());
            }
        }

        private int lastFrameConfigureGotCalled = -1;
        public override void Configure(IModConfig config) {
            if (lastFrameConfigureGotCalled == Time.frameCount) return;
            lastFrameConfigureGotCalled = Time.frameCount;

            keybinds.Clear();
            keybinds.Add("Toggle Spacesuit", "Slash+R");
            keybinds.Add("Fast Load New Expedition", "Slash+T");
            keybinds.Add("Teleport Ship To Player", "Slash+Y");
            keybinds.Add("Toggle Speedup", "Slash+U");
            keybinds.Add("Enter/Exit DreamWorld", "Slash+I");
            keybinds.Add("Log Player Location", "Slash+O");
            keybinds.Add("Toggle Bonk", "Slash+P");

            keybinds.Add("Give Warp Core", "I+Digit1");
            keybinds.Add("Give Dream Lantern", "I+Digit2");

            keybinds.Add("Village Practice State !RESETS SAVEFILE!", "P+Digit1");
            keybinds.Add("ATP Practice State", "P+Digit2");
            keybinds.Add("ATP Interior Practice State", "P+Digit3");
            keybinds.Add("Bramble Practice State", "P+Digit4");
            keybinds.Add("Ultimate Feldsparring Practice State", "P+Digit5");
            keybinds.Add("Vessel Practice State", "P+Digit6");
            keybinds.Add("Vessel Clip Practice State", "P+Digit7");
            keybinds.Add("Clone Practice State", "P+Digit8");
            keybinds.Add("Instrument Hunt Practice State", "P+Digit9");

            keybinds.Add("Custom Practice State 1", "Slash+Digit1");
            keybinds.Add("Custom Practice State 2", "Slash+Digit2");
            keybinds.Add("Custom Practice State 3", "Slash+Digit3");
            keybinds.Add("Custom Practice State 4", "Slash+Digit4");
            keybinds.Add("Custom Practice State 5", "Slash+Digit5");
            keybinds.Add("Custom Practice State 6", "Slash+Digit6");
            keybinds.Add("Custom Practice State 7", "Slash+Digit7");
            keybinds.Add("Custom Practice State 8", "Slash+Digit8");
            keybinds.Add("Custom Practice State 9", "Slash+Digit9");
            keybinds.Add("Custom Practice State 10", "Slash+Digit0");
            keybinds.ResetKeybindsToDefaultOnDuplicate();

            if (Locator.GetPlayerBody() == null) return;
            UpdateInvincibility();
            UpdateSectorText();

            if (ModHelper.Config.GetSettingsValue<bool>("Learn Quantum Frequency")) {
                PlayerData.LearnFrequency(SignalFrequency.Quantum);
            }
        }

        public void OnPracticeState(string practiceState) {
            currentPracticeState = practiceState;
        }

        public void OnStartVesselWarp() {
            warpTimer.Stop();
        }

        public void OnEyeStateChanged(EyeState state) {
            //Console.WriteLine($"EyeState changed: {state}");
            if (state == EyeState.InstrumentHunt) {
                if (IsTimerEnabled("Instrument Hunt Timer")) {
                    instrumentTimer.Start();
                }
                cloneTimer.Stop();
                RemoveMarker("Trees Location");
            }
            if (state == EyeState.Observatory) {
                museumTimer.Stop();
                if (IsTimerEnabled("Observe Timer")) {
                    observeTimer.Start();
                }
            }
            if (state == EyeState.ZoomOut) {
                observeTimer.Stop();
                museumObserveTimer.Stop();
                if (IsTimerEnabled("Clone Timer")) {
                    cloneTimer.Start();
                }
                if (currentPracticeState != "" && ModHelper.Config.GetSettingsValue<bool>("Clone Trees Locator")) {
                    CanvasMarker marker = GetOrCreateMarker("Trees Location", GameObject.Find("EyeOfTheUniverse_Body").GetAttachedOWRigidbody(), new Vector3(-54.48f, 1.00f, 5999.10f));
                    marker.SetVisibility(true);
                }
            }
        }

        public void OnPlayerDeath(DeathType deathType) {
            if (deathType == DeathType.BigBang) {
                instrumentTimer.Stop();
            }
        }

        public void OnEnterSignalScopeZoom(Signalscope signalscope) {
            // hacky fix for signalscope UI not loading in on first zoom
            ModHelper.Events.Unity.FireInNUpdates(() => {
                signalscope._signalscopeUI.OnExitSignalscopeZoom();
                signalscope._signalscopeUI.OnEnterSignalscopeZoom(signalscope);
            }, 2);
        }

        public void OnEnterSector(Sector sector) {
            if (ModHelper.Config.GetSettingsValue<bool>("Show Sectors")) {
                AddScreenText(sector.gameObject.name, PromptPosition.BottomCenter);
            }
            if (sector.name == "Sector_AnglerNestDimension") {
                if (IsTimerEnabled("Ultimate Feldsparring Timer")) {
                    feldsparringTimer.Start();
                }
            }
            else if (sector.name == "Sector_VesselDimension") {
                feldsparringTimer.Stop();
                brambleTimer.Stop();
                if (IsTimerEnabled("Warp Timer")) {
                    warpTimer.Start();
                }
            }
        }

        public void OnExitSector(Sector sector) {
            if (ModHelper.Config.GetSettingsValue<bool>("Show Sectors")) {
                RemoveScreenText(sector.gameObject.name, PromptPosition.BottomCenter);
            }
        }

        public void OnStartSleepingAtCampfire(bool isDreamCampfire) {
            villageTimer.Stop();
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

                if (IsTimerEnabled("Bramble Timer")) {
                    brambleTimer.Start();
                }
            }
        }

        public void OnReceiveWarpedBodyATPReceiver(OWRigidbody body, NomaiWarpPlatform startPlatform, NomaiWarpPlatform receivedPlatform) {
            if (body is PlayerBody) {
                if (atpWarpTransmitter._alignmentWindow == 360f)
                    atpWarpTransmitter._alignmentWindow = 0f;

                atpEnterTimer.Stop();
                if (IsTimerEnabled("ATP Interior Timer")) {
                    atpInteriorTimer.Start();
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

        public static void EquipSpacesuit(bool instant) {
            Locator.GetPlayerSuit().SuitUp(false, instant);
            Locator.GetPlayerTransform().GetComponent<PlayerResources>()._jetpackThruster.DebugResetBoostCharge();
        }

        public static void RemoveSpacesuit(bool instant) {
            Locator.GetPlayerSuit().RemoveSuit(instant);
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

        public static void LoadSceneIfNotInScene(OWScene scene, Action afterSceneLoad) {
            if (scene == OWScene.SolarSystem && LoadManager.GetCurrentScene() != OWScene.SolarSystem) {
                LoadSolarSystemScene(afterSceneLoad);
                return;
            }
            else if (scene == OWScene.EyeOfTheUniverse && LoadManager.GetCurrentScene() != OWScene.EyeOfTheUniverse) {
                LoadEyeScene(EyeState.AboardVessel, afterSceneLoad);
                return;
            }
            afterSceneLoad();
        }

        public static void LoadSolarSystemScene(Action afterSceneLoad, bool skipWakeUpAnim = true) {
            PlayerData._currentGameSave.warpedToTheEye = false;
            PlayerData.SaveCurrentGame();

            LoadManager.LoadScene(OWScene.SolarSystem);
            CheeseTools.afterSceneLoad = afterSceneLoad;
            CheeseTools.skipWakeUpAnim = skipWakeUpAnim;
        }

        public static void LoadEyeScene(EyeState eyeState, Action afterSceneLoad) {
            PlayerData.SaveWarpedToTheEye(TimeLoop.GetSecondsRemaining());
            LoadManager.LoadScene(OWScene.EyeOfTheUniverse);
            CheeseTools.afterSceneLoad = afterSceneLoad;
            skipWakeUpAnim = true;
            afterSceneLoadEyeState = eyeState;
        }

        public static void SleepUntil(double seconds, Action afterSleepUntil) {
            if (seconds == 0) {
                afterSleepUntil();
                return;
            }

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

        public static void ToggleSpeedup() {
            OWTime.SetTimeScale(OWTime.GetTimeScale() != speedupTimeScale ? speedupTimeScale : 1f);
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

        public static bool TryParseVector3(string str, out Vector3 result) {
            result = Vector3.zero;
            string[] split = str.Split(',');
            if (split.Length != 3)
                return false;

            if (!float.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
                return false;
            if (!float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                return false;
            if (!float.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                return false;

            result = new Vector3(x, y, z);
            return true;
        }

        public static bool IsTimerEnabled(string str) {
            return currentPracticeState != "" && instance.ModHelper.Config.GetSettingsValue<bool>(str);
        }

        private bool ToggleBonk(ScreenPrompt prompt, params EntrywayTrigger[] triggers) {
            if (!Locator.GetPromptManager().GetScreenPromptList(PromptPosition.LowerLeft).Contains(prompt)) {
                Locator.GetPromptManager().AddScreenPrompt(prompt, PromptPosition.LowerLeft, true);
                foreach (EntrywayTrigger trigger in triggers) {
                    trigger.ForceEntry(Locator.GetPlayerDetector());
                    trigger.ForceEntry(Locator.GetPlayerCameraDetector());
                }
                return true;
            }
            Locator.GetPromptManager().RemoveScreenPrompt(prompt);
            foreach (EntrywayTrigger trigger in triggers) {
                trigger.ForceExit(Locator.GetPlayerDetector());
                trigger.ForceExit(Locator.GetPlayerCameraDetector());
            }
            return false;
        }

        private void CustomPracticeState(int num) {
            if (!TryParseVector3(ModHelper.Config.GetSettingsValue<string>($"Custom Practice State {num} Position"), out Vector3 position)) {
                Console.WriteLine($"Failed to start Custom Practice State {num}: Position is invalid.", MessageType.Error);
                return;
            }
            if (!TryParseVector3(ModHelper.Config.GetSettingsValue<string>($"Custom Practice State {num} Rotation"), out Vector3 rotation)) {
                Console.WriteLine($"Failed to start Custom Practice State {num}: Rotation is invalid.", MessageType.Error);
                return;
            }

            Action action = () => {
                OWRigidbody relativeBody = RelativeBody.GetFromString(ModHelper.Config.GetSettingsValue<string>($"Custom Practice State {num} Body"));
                RelativeLocationData relativeLocation = new RelativeLocationData(position, Quaternion.Euler(rotation), Vector3.zero);
                if (ModHelper.Config.GetSettingsValue<bool>($"Custom Practice State {num} Ship")) {
                    Vector3 relativePlayerPosToShipWhenSeated = new Vector3(0f, 0.34f, 4.22f);
                    relativeLocation.localPosition = relativeLocation.localPosition - relativePlayerPosToShipWhenSeated;
                    Teleportation.TeleportBodyTo(Locator.GetShipBody(), relativeBody, relativeLocation);
                    Teleportation.TeleportPlayerToShip();
                }
                else {
                    Teleportation.TeleportPlayerTo(relativeBody, relativeLocation);
                }

                if (ModHelper.Config.GetSettingsValue<bool>($"Custom Practice State {num} Spacesuit"))
                    EquipSpacesuit(true);
                else
                    RemoveSpacesuit(true);
            };

            var loopTime = ModHelper.Config.GetSettingsValue<double>($"Custom Practice State {num} Loop Time");
            if (LoadManager.GetCurrentScene() != OWScene.SolarSystem || loopTime > 0) {
                if (loopTime > 0 && !PlayerData.KnowsLaunchCodes()) {
                    PlayerData.LearnLaunchCodes();
                    PlayerData.SaveLoopCount(3);
                }
                LoadSolarSystemScene(() => {
                    SleepUntil(loopTime, action);
                });
            } else {
                action();
            }
        }

        private void UpdateInfiniteResources() {
            if (Locator.GetPlayerTransform()?.TryGetComponent(out PlayerResources resources) == true) {
                if (ModHelper.Config.GetSettingsValue<bool>("Infinite Fuel"))
                    resources.SetValue("_currentFuel", resources.GetValue<float>("_maxFuel"));
                if (ModHelper.Config.GetSettingsValue<bool>("Infinite Oxygen"))
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
            if (ModHelper.Config.GetSettingsValue<bool>("Show Loop Time") && TimeLoop.GetSecondsElapsed() > 0f) {
                loopTimeText.SetText($"Loop Time: {TimeSpan.FromSeconds(TimeLoop.GetSecondsElapsed()).ToString(@"mm\:ss")} [{(int)TimeLoop.GetSecondsElapsed()}]");
                if (!Locator.GetPromptManager().GetScreenPromptList(PromptPosition.LowerLeft).Contains(loopTimeText)) {
                    Locator.GetPromptManager().AddScreenPrompt(loopTimeText, PromptPosition.LowerLeft, true);
                }
            }
            else {
                Locator.GetPromptManager().RemoveScreenPrompt(loopTimeText);
            }
        }

        private void UpdateWatermark() {
            if (ModHelper.Config.GetSettingsValue<bool>("Watermark")) {
                if (Locator.GetPromptManager()?.GetScreenPromptList(PromptPosition.LowerLeft)?.Contains(watermark) == false) {
                    Locator.GetPromptManager().AddScreenPrompt(watermark, true);
                }
            } else {
                if (Locator.GetPromptManager()?.GetScreenPromptList(PromptPosition.LowerLeft)?.Contains(watermark) == true) {
                    Locator.GetPromptManager().RemoveScreenPrompt(watermark, PromptPosition.LowerLeft);
                }
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
