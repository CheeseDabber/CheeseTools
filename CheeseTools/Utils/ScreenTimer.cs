using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CheeseTools.Utils {
	public class ScreenTimer : Stopwatch {
		private ScreenPrompt _screenPrompt = new ScreenPrompt("");
		public string prefix = "";

		public ScreenTimer(string prefix) {
			this.prefix = prefix;
		}

		public ScreenPrompt GetScreenPrompt() {
			return _screenPrompt;
		}

		public void SetText(string text) => _screenPrompt.SetText(text);
		public string GetText() => _screenPrompt.GetText();

		public new void Start() {
			if (!Locator.GetPromptManager().GetScreenPromptList(PromptPosition.LowerLeft).Contains(_screenPrompt)) {
				Locator.GetPromptManager().AddScreenPrompt(_screenPrompt, PromptPosition.LowerLeft, true);
			}
			ScreenTimerController.Register(this);
			Reset();
			base.Start();
		}

		public new void Stop() {
			ScreenTimerController.Unregister(this);
			base.Stop();
		}

		public void SetVisibility(bool isVisible) {
			_screenPrompt.SetVisibility(isVisible);
		}

		public void Remove() {
			Locator.GetPromptManager().RemoveScreenPrompt(_screenPrompt);
		}
	}

	public static class ScreenTimerController {
		private static HashSet<ScreenTimer> _screenTimers = new HashSet<ScreenTimer>();

		public static void Start() {
			LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
		}

		public static void Update() {
			foreach (ScreenTimer screenTimer in _screenTimers) {
				if (screenTimer.IsRunning) {
					TimeSpan time = TimeSpan.FromSeconds(screenTimer.Elapsed.TotalSeconds);
					string formattedTime = time.Minutes >= 1 ? time.ToString(@"m\:ss\.ff") : time.ToString(@"ss\.ff");
					screenTimer.SetText($"{screenTimer.prefix}[{formattedTime}]");
				}
			}
		}

		public static void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene) {
			foreach (Stopwatch stopwatch in _screenTimers) {
				stopwatch.Stop();
			}
			_screenTimers.Clear();
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
