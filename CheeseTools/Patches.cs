using HarmonyLib;
using System;
using UnityEngine;

namespace CheeseTools {
    [HarmonyPatch]
    public static class Patches {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PromptManager), nameof(PromptManager.SetPromptsVisible))]
        public static bool PromptManager_SetPromptsVisible() {
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ScreenPromptList), nameof(ScreenPromptList.OnPlayerDeath))]
        public static bool ScreenPromptList_OnPlayerDeath(DeathType deathType) {
            return deathType != DeathType.BigBang;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(InputManager), nameof(InputManager.OnStartOfTimeLoop))]
        public static bool InputManager_OnStartOfTimeLoop(InputManager __instance) {
            if (CheeseTools.afterSceneLoad != null && CheeseTools.skipWakeUpAnim) {
                __instance.ChangeInputMode(InputMode.Character);
                __instance._inputFadeFraction = 1f;
                if (LoadManager.GetCurrentScene() == OWScene.SolarSystem)
                    GlobalMessenger.FireEvent("TakeFirstFlashbackSnapshot");
                return false;
            }
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipEjectionSystem), nameof(ShipEjectionSystem.OnPressInteract))]
        public static bool ShipEjectionSystem_OnPressInteract() {
            ShipDamageController damageController = Locator.GetShipTransform()?.GetComponent<ShipDamageController>();
            if (damageController) 
                damageController._invincible = false;
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ReticleController), nameof(ReticleController.LateUpdate))]
        public static bool ReticleController_LateUpdate(ReticleController __instance) {
            if (ReticleController.s_hideReticle || Locator.GetPromptManager().IsCenterPromptDisplayed() || PlayerState.IsDead() || PlayerState.InConversation() || PlayerState.UsingShipComputer() || PlayerState.InLandingView() || OWTime.IsPaused(OWTime.PauseType.Menu) || !GUIMode.IsReticleVisible() || PlayerState.IsPlayerCameraLockingOn() || PlayerState.IsViewingProjector()) {
                if (__instance._canvas.enabled) {
                    __instance._canvas.enabled = false;
                }
                return false;
            }
            if (!__instance._canvas.enabled) {
                __instance._canvas.enabled = true;
            }
            Vector3 localScale = Vector3.one;
            if (PlayerState.InMapView()) {
                __instance._image.sprite = __instance._zeroGReticle;
                __instance._image.rectTransform.localScale = localScale;
                return false;
            }
            bool flag;
            switch (Locator.GetToolModeSwapper().GetToolMode()) {
                case ToolMode.Probe:
                    flag = true;
                    __instance._image.sprite = __instance._probeLauncherReticle;
                    goto IL_15A;
                case ToolMode.SignalScope:
                    flag = false;
                    goto IL_15A;
                case ToolMode.Translator:
                    flag = true;
                    __instance._image.sprite = __instance._translatorReticle;
                    localScale = Vector3.one * Mathf.Lerp(1f, 3f, Mathf.Clamp01(NomaiTranslator.distToClosestTextCenter));
                    goto IL_15A;
            }
            flag = true;
            if (PlayerState.InZeroG()) {
                __instance._image.sprite = __instance._zeroGReticle;
            }
            else {
                __instance._image.sprite = __instance._defaultReticle;
            }
            IL_15A:
            if (__instance._image.enabled != flag) {
                __instance._image.enabled = flag;
            }
            __instance._image.rectTransform.localScale = localScale;
            Color color = __instance._image.color;
            if (color.a == 1f) return false; // added this line so I can set reticle alpha to 1
            float t = Mathf.InverseLerp(1f, 5f, Time.timeSinceLevelLoad);
            color.a = Mathf.Lerp(0f, 1f, t);
            __instance._image.color = color;
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerBreathingAudio), nameof(PlayerBreathingAudio.OnWakeUp))]
        public static bool PlayerBreathingAudio_OnWakeUp() {
            return CheeseTools.afterSceneLoad == null || !CheeseTools.skipWakeUpAnim;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Campfire), nameof(Campfire.Update))]
        public static bool Campfire_Update(Campfire __instance) {
            float num = 0f;
            switch (__instance._state) {
                case Campfire.State.UNLIT:
                    num = 0f;
                    break;
                case Campfire.State.LIT:
                    num = 1f;
                    break;
                case Campfire.State.SMOLDERING:
                    num = 0.4f;
                    break;
            }
            if (__instance._litFraction != num) {
                __instance.SetLitFraction(Mathf.MoveTowards(__instance._litFraction, num, Time.deltaTime));
            }
            if (__instance._canSleepHere) {
                __instance._sleepPrompt.SetVisibility(false);
                if (__instance._interactVolumeFocus && !__instance._isPlayerSleeping && !__instance._isPlayerRoasting && OWInput.IsInputMode(InputMode.Character)) {
                    __instance._sleepPrompt.SetVisibility(true);
                    __instance._sleepPrompt.SetDisplayState(__instance.CanSleepHereNow() ? ScreenPrompt.DisplayState.Normal : ScreenPrompt.DisplayState.GrayedOut);
                    if (OWInput.IsNewlyPressed(InputLibrary.interactSecondary, InputMode.All) && __instance.CanSleepHereNow()) {
                        __instance.StartSleeping();
                    }
                }
            }
            if (__instance._isPlayerSleeping && !__instance._isTimeFastForwarding && Time.timeSinceLevelLoad > __instance._fastForwardStartTime) {
                __instance.StartFastForwarding();
            }
            if (__instance._isTimeFastForwarding) {
                __instance._wakePrompt.SetVisibility(OWInput.IsInputMode(InputMode.None) && Time.timeSinceLevelLoad - __instance._fastForwardStartTime > __instance.GetWakePromptDelay());
                if (__instance.ShouldWakeUp()) {
                    __instance.StopSleeping(false);
                    return false;
                }
                if (!OWTime.IsPaused()) {
                    // cheesetools logic
                    if (CheeseTools.afterSleepUntil != null) {
                        if (TimeLoop.GetSecondsElapsed() < CheeseTools.wakeUpTime) {
                            __instance._fastForwardMultiplier = Mathf.Clamp((float)CheeseTools.wakeUpTime - TimeLoop.GetSecondsElapsed(), 2f, 50f);
                            OWTime.SetTimeScale(__instance._fastForwardMultiplier);
                        }
                        else if (!OWTime.IsPaused()) {
                            OWTime.Pause(OWTime.PauseType.Sleeping);
                        }
                    } else if (OWTime.GetTimeScale() != CheeseTools.speedupTimeScale) {
                        // outer wilds logic
                        __instance._fastForwardMultiplier = Mathf.MoveTowards(__instance._fastForwardMultiplier, 10f, 2f * Time.unscaledDeltaTime);
                        OWTime.SetTimeScale(__instance._fastForwardMultiplier);
                    }
                }
            }
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SleepTimerUI), nameof(SleepTimerUI.OnWillRenderCanvases))]
        public static bool SleepTimerUI_OnWillRenderCanvases(SleepTimerUI __instance) {
            if (CheeseTools.afterSleepUntil != null) {
                // cheesetools logic
                if (!OWTime.IsPaused()) {
                    __instance._text.text = $"Sleeping until {TimeSpan.FromSeconds(CheeseTools.wakeUpTime).ToString(@"mm\:ss")}\n" + TimeSpan.FromSeconds(TimeLoop.GetSecondsElapsed()).ToString(@"mm\:ss");
                } else {
                    __instance._text.text = $"Ready. Wake up to start\n" + TimeSpan.FromSeconds(TimeLoop.GetSecondsElapsed()).ToString(@"mm\:ss");
                }
            } else {
                // outer wilds logic
                float num = Mathf.Max(Time.timeSinceLevelLoad - __instance._sleepStartTime, 0f);
                int num2 = Mathf.FloorToInt(num / 60f);
                int num3 = Mathf.FloorToInt(num) % 60;
                __instance._stringBuilder.Length = 0;
                __instance._stringBuilder.Append(num2.ToString("D2"));
                __instance._stringBuilder.Append(":");
                __instance._stringBuilder.Append(num3.ToString("D2"));

                if (OWTime.GetTimeScale() == CheeseTools.speedupTimeScale) {
                    __instance._stringBuilder.Append("\nSpeedup Enabled");
                }

                __instance._text.text = __instance._stringBuilder.ToString();
            }
            float a = Mathf.Clamp01((Time.unscaledTime - __instance._sleepStartTimeUnscaled) / 3f);
            __instance._text.color = new Color(__instance._textColor.r, __instance._textColor.g, __instance._textColor.b, a);
            if (__instance._emberInstances != null) {
                for (int i = 0; i < __instance._emberInstances.Length; i++) {
                    if (__instance._emberInstances[i].alive) {
                        __instance._emberInstances[i].image.color = __instance._emberInstances[i].tint * __instance.GetHeatTint(__instance._emberInstances[i].heat);
                        __instance._emberInstances[i].rectTransform.localPosition = __instance._emberInstances[i].position;
                        __instance._emberInstances[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, __instance._emberInstances[i].rotation);
                        __instance._emberInstances[i].rectTransform.localScale = new Vector3(__instance._emberInstances[i].scale, __instance._emberInstances[i].scale, __instance._emberInstances[i].scale);
                    }
                }
            }
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerCameraEffectController), nameof(PlayerCameraEffectController.OnStartOfTimeLoop))]
        public static bool PlayerCameraEffectController_OnStartOfTimeLoop(PlayerCameraEffectController __instance) {
            if (__instance.gameObject.CompareTag("MainCamera") && LoadManager.GetCurrentScene() != OWScene.EyeOfTheUniverse) {
                // added afterSceneLoad check to disable wakeup prompt when starting practice state from titlescreen
                if (LoadManager.GetPreviousScene() == OWScene.TitleScreen && CheeseTools.afterSceneLoad == null) {
                    __instance._owCamera.postProcessingSettings.eyeMask.openness = 0f;
                    __instance._owCamera.postProcessingSettings.bloom.threshold = 0f;
                    __instance._owCamera.postProcessingSettings.eyeMaskEnabled = true;
                    __instance._waitForWakeInput = true;
                    __instance._wakePrompt = new ScreenPrompt(InputLibrary.interact, UITextLibrary.GetString(UITextType.WakeUpPrompt), 0, ScreenPrompt.DisplayState.Normal, false);
                    __instance._wakePrompt.SetVisibility(false);
                    Locator.GetPromptManager().AddScreenPrompt(__instance._wakePrompt, PromptPosition.Center, false);
                    OWTime.Pause(OWTime.PauseType.Sleeping);
                    Locator.GetPauseCommandListener().AddPauseCommandLock();
                    return false;
                }
                __instance.WakeUp();
            }
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(JetpackThrusterModel), nameof(JetpackThrusterModel.OnBreakAlignment))]
        public static bool JetpackThrusterModel_OnBreakAlignment(JetpackThrusterModel __instance) {
            __instance._manualAngularVelocity = Vector3.zero;
            __instance._boostActivated = false;
            //__instance._boostChargeFraction = 0f; commented out this line cause its useless and it was messing up my insta boost fill on equipspacesuit
            RumbleManager.StopJetpackBoost();
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CosmicInflationController), nameof(CosmicInflationController.UpdateFormation))]
        public static void CosmicInflationController_UpdateFormation(CosmicInflationController __instance) {
            if (!CheeseTools.instrumentTimer.isRunning || !CheeseTools.instance.ModHelper.Config.GetSettingsValue<bool>("Predict Instrument Hunt Time")) return;

            if (__instance._finishFormationTime >= 0f && __instance._startFormationTime == Time.time) {
                float bigBangTime = 37f; // scout boosting to big bang is considered but times can vary. this is just an estimation.
                string predictedTime = TimeSpan.FromSeconds(CheeseTools.instrumentTimer.GetElapsed() + (__instance._finishFormationTime - __instance._startFormationTime) + bigBangTime).ToString(@"m\:ss\.ff");
                CheeseTools.AddScreenText($"Predicted Instrument Hunt Time: [{predictedTime}]", PromptPosition.LowerLeft);
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(NomaiInterfaceOrb), nameof(NomaiInterfaceOrb.StartDragFromPosition))]
        public static void NomaiInterfaceOrb_StartDragFromPosition(NomaiInterfaceOrb __instance) {
            if (__instance._orbBody.GetOrigParent()?.name == "PillarRoot" && !__instance._orbBody.IsSuspended() && CheeseTools.IsTimerEnabled("Coordinates Timer") && !CheeseTools.coordinatesTimer.isRunning) {
                var coordinateInterface = GameObject.Find("WarpController").GetComponent<VesselWarpController>()._coordinateInterface;
                if (coordinateInterface.CheckEyeCoordinates()) return;
                CheeseTools.coordinatesTimer.Start();
            }
        }
    }
}
