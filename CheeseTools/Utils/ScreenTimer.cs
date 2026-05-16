using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheeseTools.Utils {
    public class ScreenTimer {
        public string prefix = "";
        public bool isRunning { get; private set; }

        private float _elapsed = 0f;
        private ScreenPrompt _screenPrompt = new ScreenPrompt("");

        public ScreenTimer(string prefix) {
            this.prefix = prefix;
        }

        public ScreenPrompt GetScreenPrompt() {
            return _screenPrompt;
        }

        public void SetText(string text) => _screenPrompt.SetText(text);
        public string GetText() => _screenPrompt.GetText();

        public void Update() {
            if (!isRunning || OWTime.IsPaused()) return;
            _elapsed += Time.deltaTime;

            if (!Locator.GetPromptManager().GetScreenPromptList(PromptPosition.LowerLeft).Contains(_screenPrompt)) {
                Locator.GetPromptManager().AddScreenPrompt(_screenPrompt, PromptPosition.LowerLeft, true);
            }

            TimeSpan time = TimeSpan.FromSeconds(_elapsed);
            string formattedTime = time.Minutes >= 1 ? time.ToString(@"m\:ss\.ff") : time.ToString(@"ss\.ff");
            _screenPrompt.SetText($"{prefix}[{formattedTime}]");
        }

        public void Start() {
            _elapsed = 0f;
            isRunning = true;
            ScreenTimerController.Register(this);
        }

        public void Stop() {
            isRunning = false;
            ScreenTimerController.Unregister(this);
        }

        public float GetElapsed() {
            return _elapsed;
        }

        public void SetVisibility(bool isVisible) {
            _screenPrompt.SetVisibility(isVisible);
        }

        public void Remove() {
            Locator.GetPromptManager().RemoveScreenPrompt(_screenPrompt);
        }
    }

    public static class ScreenTimerController {
        private static List<ScreenTimer> _screenTimers = new List<ScreenTimer>();

        public static void Start() {
            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
        }

        public static void Update() {
            foreach (ScreenTimer screenTimer in _screenTimers) {
                screenTimer.Update();
            }
        }

        public static void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene) {
            for (int i = _screenTimers.Count - 1; i >= 0; i--) {
                ScreenTimer screenTimer = _screenTimers[i];
                if (screenTimer.prefix == "Village Time: " && newScene == OWScene.SolarSystem && CheeseTools.afterSceneLoad == null) {
                    Console.Write("Continued village timer");
                    continue;
                }

                screenTimer.Stop();
            }
        }

        public static void Register(ScreenTimer screenTimer) {
            _screenTimers.Add(screenTimer);
        }

        public static void Unregister(ScreenTimer screenTimer) {
            _screenTimers.Remove(screenTimer);
        }

        public static bool IsRegistered(ScreenTimer screenTimer) {
            return _screenTimers.Contains(screenTimer);
        }
    }
}
