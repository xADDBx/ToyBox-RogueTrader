using Newtonsoft.Json;
using UnityEngine;

namespace ToyBox.Infrastructure.Keybinds;

internal class HotkeySettings : AbstractSettings {
    protected override string Name {
        get {
            return "Hotkeys.json";
        }
    }
    private static readonly Lazy<HotkeySettings> m_Instance = new(() => {
        var instance = new HotkeySettings();
        instance.Load();

        foreach (var item in instance.m_BoundKeys) {
            // Pseudo Hotkeys are such that there should be keybinds which can be configured in ui and are saved; but which shouldn't run via update loop.
            item.Key.Precompute();
            if (!item.Key.IsPseudo) {
                instance.m_HotkeysByMask[item.Key.GetMask()].Add(item.Key);
            }
            instance.m_HotkeyByAction[item.Value] = item.Key;
        }


        return instance;
    });
    public static HotkeySettings Hotkeys {
        get {
            return m_Instance.Value;
        }
    }
    [JsonProperty]
    private readonly Dictionary<Hotkey, Type> m_BoundKeys = [];
    private readonly Dictionary<Type, Hotkey> m_HotkeyByAction = [];
    private readonly List<Hotkey>[] m_HotkeysByMask = [[], [], [], [], [], [], [], []];
    private static readonly uint[][] m_ConflictingMasks = [
        // 000           , // 001     , // 010     , // 011     , // 100     , // 101     , // 110     , // 111
        [0,1,2,3,4,5,6,7], [0,1,3,5,7], [0,2,3,6,7], [0,1,2,3,7], [0,4,5,6,7], [0,1,4,5,7], [0,2,4,6,7], [0,1,2,3,4,5,6,7]
    ];
    public Hotkey? MaybeGetHotkey(Type type) {
        _ = m_HotkeyByAction.TryGetValue(type, out var hotkey);
        return hotkey;
    }
    public void UpdateLoop() {
        var currentMask = GetCurrentMask();
        foreach (var mask in m_ConflictingMasks[currentMask]) {
            foreach (var hotkey in m_HotkeysByMask[mask]) {
                if (hotkey.IsActive(currentMask)) {
                    var feature = ModFeature.GetInstance<ModFeature>(m_BoundKeys[hotkey]);
                    if (feature is IBindableFeature action) {
                        action.ExecuteAction();
                    } else {
                        throw new NotSupportedException($"Trying to execute feature via hotkey; but feature does not implement Execute: {feature.Name} (type: {feature.GetType()}");
                    }
                }
            }
        }
    }
    public bool AddHotkey(Hotkey hotkey, IBindableFeature feature, bool skipConflictCheck = false) {
        if (skipConflictCheck || !HasConflict(hotkey)) {
            var type = feature.GetType();
            m_BoundKeys.Add(hotkey, type);
            if (!hotkey.IsPseudo) {
                m_HotkeysByMask[hotkey.GetMask()].Add(hotkey);
            }
            if (m_HotkeyByAction.TryGetValue(type, out var maybeOld)) {
                _ = RemoveHotkey(maybeOld);
            }
            m_HotkeyByAction[type] = hotkey;
            hotkey.Precompute();
            Hotkeys.Save();
            return true;
        }
        return false;
    }
    public bool RemoveHotkey(Hotkey hotkey) {
        if (m_BoundKeys.ContainsKey(hotkey)) {
            _ = m_BoundKeys.Remove(hotkey);
            _ = m_HotkeysByMask[hotkey.GetMask()].Remove(hotkey);
            Hotkeys.Save();
            return true;
        }
        return false;
    }
    public bool HasConflict(Hotkey hotkey) {
        if (hotkey.IsPseudo) {
            return false;
        }
        var mask = hotkey.GetMask();
        foreach (var conflictingMask in m_ConflictingMasks[mask]) {
            foreach (var otherHotkey in m_HotkeysByMask[conflictingMask]) {
                if (otherHotkey.Key == KeyCode.None || hotkey.Key == KeyCode.None || otherHotkey.Key == hotkey.Key) {
                    return true;
                }
            }
        }
        return false;
    }
    public static uint GetCurrentMask() {
        return (IsControlHeld() ? 4u : 0u) | (IsShiftHeld() ? 2u : 0u) | (IsAltHeld() ? 1u : 0u);
    }
    private static bool IsControlHeld() {
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
            || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
    }
    private static bool IsShiftHeld() {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }
    private static bool IsAltHeld() {
        return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
    }
}
