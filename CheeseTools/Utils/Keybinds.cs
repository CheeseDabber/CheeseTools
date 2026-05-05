using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
using OWML.Common;
using System.Linq;

namespace CheeseTools.Utils {
	public enum SettingKeybind {
		ToggleSuit,
		ToggleSpeedup,
		LogPlayerLocation,
		LogShipLocation,
		TeleportShipToPlayer,
		FastLoadNewExpedition,
		EnterExitDreamWorld,
		ATPPracticeState,
		FeldsparringPracticeState,
		VesselPracticeState,
		VesselClipPracticeState,
		ClonePracticeState,
		InstrumentPracticeState,
		CustomPracticeState1,
		CustomPracticeState2,
		CustomPracticeState3,
	}

	public class Keybinds {
		private Dictionary<SettingKeybind, Keybind> _keybinds = new Dictionary<SettingKeybind, Keybind>();

		public void Add(SettingKeybind setting, string keysString) {
			Keybind keybind = new Keybind();
			if (!keybind.Init(keysString)) {
				CheeseTools.Console.WriteLine($"Invalid keybind for {Enum.GetName(setting.GetType(), setting)}. \"{keysString}\" is not recognized.", MessageType.Warning);
				return;
			}
			_keybinds[setting] = keybind;
		}

		public void Remove(SettingKeybind setting) {
			_keybinds.Remove(setting);
		}

		public Keybind Get(SettingKeybind setting) {
			return _keybinds.TryGetValue(setting, out var value) ? value : null;
		}

		public Dictionary<SettingKeybind, Keybind> GetAll() {
			return _keybinds;
		}

		public void Clear() {
			_keybinds.Clear();
		}
	}

	public class Keybind {
		private HashSet<Key> _keys;
		private bool _wasPressed = false;

		public bool Init(string keysString) {
			_keys = new HashSet<Key>();
			try {
				foreach (string keyString in keysString.Split('+')) {
					Key key = (Key) Enum.Parse(typeof(Key), keyString, true);
					_keys.Add(key);
				}
			} catch(Exception) {
				_keys = null;
				return false;
			}
			return true;
		}

		// this function needs to be called every frame to work properly.
		public bool WasPressedThisFrame() {
			bool isPressed = true;
			bool wasPressed = _wasPressed;
			foreach (Key key in _keys) {
				if (!Keyboard.current[key].IsPressed()) {
					isPressed = false;
					break;
				}
			}

			if (isPressed && !wasPressed) {
				_wasPressed = true;

				SettingKeybind setting = CheeseTools.keybinds.GetAll().First(x => x.Value == this).Key;
				if (setting.ToString().Contains("PracticeState"))
					CheeseTools.instance.OnPracticeState();
			}
			if (!isPressed) {
				_wasPressed = false;
			}

			return isPressed && !wasPressed;
		}
	}
}
