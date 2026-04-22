using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
using OWML.Common;

namespace CheeseTools.Utils {
	public enum SettingKeybind {
		ToggleSuit,
		ToggleSpeedup,
		LogPlayerLocation,
		LogShipLocation,
		TeleportShipToPlayer,
		EnterExitDreamWorld,
		ATPPracticeState,
		FeldsparringPracticeState,
		VesselClipPracticeState,
		InstrumentPracticeState,
		CustomPracticeState1,
		CustomPracticeState2,
		CustomPracticeState3,
	}

	public class Keybinds {
		private Dictionary<SettingKeybind, Keybind> _keybinds = new Dictionary<SettingKeybind, Keybind>();

		public void Add(SettingKeybind option, string keysString) {
			Keybind keybind = new Keybind();
			if (!keybind.Init(keysString)) {
				CheeseTools.Console.WriteLine($"Invalid keybind for {Enum.GetName(option.GetType(), option)}. \"{keysString}\" is not recognized.", MessageType.Warning);
				return;
			}
			_keybinds[option] = keybind;
		}

		public void Remove(SettingKeybind option) {
			_keybinds.Remove(option);
		}

		public Keybind Get(SettingKeybind option) {
			return _keybinds.TryGetValue(option, out var value) ? value : null;
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
					Key key = (Key) Enum.Parse(Key.A.GetType(), keyString, true);
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
			}	
			if (!isPressed) {
				_wasPressed = false;
			}

			return isPressed && !wasPressed;
		}
	}
}
