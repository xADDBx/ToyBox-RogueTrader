using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Enums;
using Newtonsoft.Json;

namespace ToyBox.Infrastructure;

public class SaveSpecificSettings {
    #region Infrastructure
    private static bool m_IsInitialized = false;
    internal static void Initialize() {
        if (m_IsInitialized) {
            return;
        }
        _ = Main.HarmonyInstance.Patch(AccessTools.Method(typeof(SaveManager), nameof(SaveManager.SaveRoutine)), new(AccessTools.Method(typeof(SaveSpecificSettings), nameof(SaveManager_SaveRoutine_Patch))));
        _ = Main.HarmonyInstance.Patch(AccessTools.Method(typeof(SaveManager), nameof(SaveManager.LoadFolderSave)), new(AccessTools.Method(typeof(SaveSpecificSettings), nameof(SaveManager_LoadRoutine_Patch))));
        _ = Main.HarmonyInstance.Patch(AccessTools.Method(typeof(ThreadedGameLoader), nameof(ThreadedGameLoader.DeserializeInGameSettings)), null, new(AccessTools.Method(typeof(SaveSpecificSettings), nameof(ThreadedGameLoader_DeserializeInGameSettings_Patch))));
        m_IsInitialized = true;
    }
    private static void TryLoadSaveSpecificSettings(InGameSettings? maybeSettings) {
        var settingsList = maybeSettings?.List ?? Game.Instance?.State?.InGameSettings?.List;
        if (settingsList == null) {
            return;
        }
        Debug($"Reloading SaveSpecificSettings.");
        SaveSpecificSettings? loaded = null;
        if (settingsList.TryGetValue(Constants.SaveFileKey, out var obj) && obj is string json) {
            try {
                loaded = JsonConvert.DeserializeObject<SaveSpecificSettings>(json);
                Debug($"Successfully deserialized SaveSpecificSettings.");
            } catch (Exception ex) {
                Error($"Deserialization of SaveSpecificSettings failed:\n{ex}");
            }
        }
        if (loaded == null) {
            Warn("SaveSpecificSettings not found, creating new...");
            loaded = new();
            loaded.Save();
        }
        Instance = loaded;
    }
    public static SaveSpecificSettings? Instance {
        get {
            if (field == null) {
                TryLoadSaveSpecificSettings(null);
            }
            return field;
        }
        private set;
    }
    public void Save() {
        var list = Game.Instance?.State?.InGameSettings?.List;
        if (list == null) {
            Debug("Warning: Tried to save SaveSpecificSettings while InGameSettingsList was null");
            return;
        }
        var json = JsonConvert.SerializeObject(this);
        list[Constants.SaveFileKey] = json;
    }
    private static void SaveManager_SaveRoutine_Patch() {
        Instance?.Save();
    }
    private static void SaveManager_LoadRoutine_Patch() {
        Instance = null;
    }
    private static void ThreadedGameLoader_DeserializeInGameSettings_Patch(ref Task<InGameSettings> __result) {
        __result = __result.ContinueWith(t => {
            TryLoadSaveSpecificSettings(t.Result);
            return t.Result;
        });
    }
    #endregion Infrastructure
    public Dictionary<string, int> LastRespecLevelForUnit = [];
    public Dictionary<string, Size> MechanicalSizeOverrides = [];
}
