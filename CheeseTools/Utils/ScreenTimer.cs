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
			ScreenTimerController.Register(this);
			Restart();
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
		private static Dictionary<string, ScreenTimer> _screenTimers = new Dictionary<string, ScreenTimer>();

		public static void Update() {
			foreach (ScreenTimer screenTimer in _screenTimers.Values) {
				if (screenTimer.IsRunning) {
					TimeSpan time = TimeSpan.FromSeconds(screenTimer.Elapsed.TotalSeconds);
					string formattedTime = time.Minutes >= 1 ? time.ToString(@"m\:ss\.ff") : time.ToString(@"ss\.ff");
					screenTimer.SetText($"{screenTimer.prefix}[{formattedTime}]");
					if (!Locator.GetPromptManager().GetScreenPromptList(PromptPosition.LowerLeft).Contains(screenTimer.GetScreenPrompt())) {
						Locator.GetPromptManager().AddScreenPrompt(screenTimer.GetScreenPrompt(), PromptPosition.LowerLeft, true);
					}
				}
			}
		}

		public static void Register(ScreenTimer screenTimer) {
			if (_screenTimers.TryGetValue(screenTimer.prefix, out ScreenTimer value)) {
				if (screenTimer == value) return;
				else {
					value.Stop();
					value.Remove();
				}
			}
			_screenTimers.Add(screenTimer.prefix, screenTimer);
		}

		public static void Unregister(ScreenTimer screenTimer) {
			_screenTimers.Remove(screenTimer.prefix);
		}

		public static bool IsRegistered(ScreenTimer screenTimer) {
			return _screenTimers.TryGetValue(screenTimer.prefix, out ScreenTimer value) && value != null;
		}
	}
}
