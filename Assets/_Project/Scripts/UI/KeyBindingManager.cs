using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DonGeonMaster.UI
{
    public class KeyBindingManager : MonoBehaviour
    {
        public static KeyBindingManager Instance { get; private set; }

        private Dictionary<string, KeyCode> bindings = new Dictionary<string, KeyCode>();
        private Dictionary<string, KeyControl> cachedControls = new Dictionary<string, KeyControl>();
        private bool isRebinding;
        private string rebindAction;
        private Action<KeyCode> rebindCallback;

        public static readonly string[] Actions =
        {
            "MoveForward", "MoveBack", "MoveLeft", "MoveRight",
            "Run", "Jump", "Interact", "Inventory"
        };

        // Display names for the Settings UI (French)
        public static readonly Dictionary<string, string> ActionDisplayNames = new Dictionary<string, string>
        {
            { "MoveForward", "Avancer" },
            { "MoveBack", "Reculer" },
            { "MoveLeft", "Gauche" },
            { "MoveRight", "Droite" },
            { "Run", "Courir" },
            { "Jump", "Sauter" },
            { "Interact", "Interagir" },
            { "Inventory", "Inventaire" }
        };

        private static readonly Dictionary<string, KeyCode> DefaultBindings = new Dictionary<string, KeyCode>
        {
            { "MoveForward", KeyCode.W },
            { "MoveBack", KeyCode.S },
            { "MoveLeft", KeyCode.A },
            { "MoveRight", KeyCode.D },
            { "Run", KeyCode.LeftShift },
            { "Jump", KeyCode.Space },
            { "Interact", KeyCode.E },
            { "Inventory", KeyCode.I }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadBindings();
        }

        private void Start()
        {
            RebuildControlCache();
        }

        public KeyCode GetBinding(string action)
        {
            return bindings.TryGetValue(action, out var key) ? key : KeyCode.None;
        }

        public bool GetKey(string action)
        {
            if (cachedControls.TryGetValue(action, out var control) && control != null)
                return control.isPressed;

            if (Keyboard.current != null && cachedControls.Count == 0)
                RebuildControlCache();

            return false;
        }

        public bool GetKeyDown(string action)
        {
            if (cachedControls.TryGetValue(action, out var control) && control != null)
                return control.wasPressedThisFrame;

            if (Keyboard.current != null && cachedControls.Count == 0)
                RebuildControlCache();

            return false;
        }

        public void StartRebind(string action, Action<KeyCode> callback)
        {
            isRebinding = true;
            rebindAction = action;
            rebindCallback = callback;
        }

        public bool IsRebinding => isRebinding;

        private void OnGUI()
        {
            if (!isRebinding) return;

            Event e = Event.current;
            if (e == null || !e.isKey || e.keyCode == KeyCode.None) return;

            KeyCode newKey = e.keyCode;
            bindings[rebindAction] = newKey;
            SaveBindings();
            RebuildControlCache();

            isRebinding = false;
            rebindCallback?.Invoke(newKey);
            rebindCallback = null;
            rebindAction = null;
        }

        private void RebuildControlCache()
        {
            cachedControls.Clear();
            var kb = Keyboard.current;
            if (kb == null) return;

            foreach (var kvp in bindings)
            {
                string targetName = kvp.Value.ToString();
                KeyControl found = null;

                foreach (var keyControl in kb.allKeys)
                {
                    if (string.Equals(keyControl.displayName, targetName, StringComparison.OrdinalIgnoreCase))
                    {
                        found = keyControl;
                        break;
                    }
                }

                if (found == null)
                {
                    string lowerName = targetName.ToLower();
                    foreach (var keyControl in kb.allKeys)
                    {
                        if (keyControl.name.Equals(lowerName, StringComparison.OrdinalIgnoreCase))
                        {
                            found = keyControl;
                            break;
                        }
                    }
                }

                if (found != null)
                    cachedControls[kvp.Key] = found;
            }
        }

        private void LoadBindings()
        {
            bindings.Clear();
            foreach (var action in Actions)
            {
                string saved = PlayerPrefs.GetString("KeyBind_" + action, "");
                if (!string.IsNullOrEmpty(saved) && Enum.TryParse(saved, out KeyCode key))
                    bindings[action] = key;
                else
                    bindings[action] = DefaultBindings[action];
            }
        }

        private void SaveBindings()
        {
            foreach (var kvp in bindings)
                PlayerPrefs.SetString("KeyBind_" + kvp.Key, kvp.Value.ToString());
            PlayerPrefs.Save();
        }

        public void ResetToDefaults()
        {
            foreach (var kvp in DefaultBindings)
                bindings[kvp.Key] = kvp.Value;
            SaveBindings();
            RebuildControlCache();
        }

        public string GetBindingDisplayName(string action)
        {
            return GetBinding(action).ToString();
        }

        public static string GetActionLabel(string action)
        {
            return ActionDisplayNames.TryGetValue(action, out var label) ? label : action;
        }
    }
}
