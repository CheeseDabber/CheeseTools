using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
using OWML.Common;
using System.Linq;

namespace CheeseTools.Utils {
    public class Keybinds {
        private Dictionary<string, Keybind> _keybinds = new Dictionary<string, Keybind>();
        private Dictionary<string, string> _defaultKeybinds = new Dictionary<string, string>();

        public void Add(string setting, string defaultKeysString) {
            string keysString = CheeseTools.instance.ModHelper.Config.GetSettingsValue<string>(setting);
            if (keysString == "") return;

            Keybind keybind = new Keybind();
            if (!keybind.Init(keysString)) {
                CheeseTools.Console.WriteLine($"Invalid keybind for {setting}. \"{keysString}\" is not recognized.", MessageType.Warning);
                return;
            }
            _keybinds[setting] = keybind;
            _defaultKeybinds[setting] = defaultKeysString;
        }

        public void Remove(string setting) {
            _keybinds.Remove(setting);
        }

        public Keybind Get(string setting) {
            return _keybinds.TryGetValue(setting, out var value) ? value : null;
        }

        public Dictionary<string, Keybind> GetAll() {
            return _keybinds;
        }

        public void ResetKeybindsToDefaultOnDuplicate() {
            List<string> keyStrings = new List<string>();
            foreach (Keybind keybind in _keybinds.Values) {
                List<string> keys = new List<string>();
                foreach (Key key in keybind.GetKeys()) {
                    keys.Add(key.ToString());
                }
                keys.Sort();
                keyStrings.Add(string.Join("+", keys));
            }

            bool hasDuplicates = keyStrings.Count != new HashSet<string>(keyStrings).Count;
            if (hasDuplicates) {
                foreach (var (setting, keybind) in _keybinds) {
                    string defaultKeysString = _defaultKeybinds[setting];
                    CheeseTools.instance.ModHelper.Config.SetSettingsValue(setting, defaultKeysString);
                    keybind.Init(defaultKeysString);
                }
                CheeseTools.Console.WriteLine("Found duplicate keybinds. Keybinds have been reset back to default.", MessageType.Warning);
            }
        }
            
        public void Clear() {
            _keybinds.Clear();
            _defaultKeybinds.Clear();
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

                string setting = CheeseTools.keybinds.GetAll().First(x => x.Value == this).Key;
                if (setting.Contains("Practice State"))
                    CheeseTools.instance.OnPracticeState(setting);
            }
            if (!isPressed) {
                _wasPressed = false;
            }

            return isPressed && !wasPressed;
        }

        public HashSet<Key> GetKeys() {
            return _keys;
        }
    }
}
