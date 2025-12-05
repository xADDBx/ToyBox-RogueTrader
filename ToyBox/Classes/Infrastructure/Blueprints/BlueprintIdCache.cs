using Kingmaker.AI.Blueprints;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Quests;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.GameInfo;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Globalmap.Blueprints.Colonization;
using Kingmaker.Globalmap.Blueprints.SectorMap;
using Kingmaker.Globalmap.Blueprints.SystemMap;
using Kingmaker.Modding;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Blueprints;
using Kingmaker.UnitLogic.Progression.Features;
using Kingmaker.UnitLogic.Progression.Paths;
using Kingmaker.Visual.Sound;
using System.Runtime.Serialization;
using System.Text;

namespace ToyBox.Infrastructure.Blueprints;

public class BlueprintIdCache {
    public static BlueprintIdCache Instance {
        get {
            if (field == null) {
                field = Load();
                if (field == null) {
                    field = new();
                    field.Save();
                }
            }
            return field;
        }
        private set;
    }

    public string CachedGameVersion = "";
    public HashSet<(string, string)> UmmList = [];
    public HashSet<(string, string)> OmmList = [];
    public Dictionary<Type, HashSet<string>> IdsByType = [];
    public static HashSet<Type> CachedIdTypes = [
                typeof(BlueprintItem), typeof(BlueprintItemWeapon), typeof(BlueprintItemArmor), typeof(BlueprintAbility),
                typeof(BlueprintEtude), typeof(BlueprintArea), typeof(BlueprintItemEnchantment), typeof(BlueprintAnswer),
                typeof(BlueprintBuff), typeof(BlueprintPortrait), typeof(BlueprintAbility), typeof(BlueprintAreaEnterPoint),
                typeof(BlueprintUnit), typeof(BlueprintBrain), typeof(BlueprintUnitFact), typeof(BlueprintFeature),
                typeof(BlueprintAreaPreset), typeof(Cutscene), typeof(BlueprintUnitAsksList), typeof(BlueprintCareerPath),
                typeof(BlueprintColony), typeof(BlueprintPlanet), typeof(BlueprintSectorMapPointStarSystem), typeof(BlueprintStarSystemMap),
                typeof(BlueprintUnlockableFlag), typeof(BlueprintQuest), typeof(BlueprintQuestObjective), typeof(BlueprintAbilityResource),
                typeof(BlueprintMechanicEntityFact), typeof(BlueprintItemMechadendrite), typeof(BlueprintAbilityFXSettings), typeof(BlueprintResource),
                typeof(BlueprintColonyTrait), typeof (BlueprintColonyEventResult)
        ];

    private static bool? m_NeedsCacheRebuilt = null;
    public static bool NeedsCacheRebuilt {
        get {
            if (m_NeedsCacheRebuilt.HasValue) {
                return m_NeedsCacheRebuilt.Value && !IsRebuilding;
            }

            foreach (var type in CachedIdTypes) {
                if (!Instance.IdsByType.TryGetValue(type, out var ids)) {
                    Error($"BPId Cache does not contain type {type}!");
                    return true;
                }
                if (ids.Count == 0) {
                    Error($"BPId Cache has no blueprints for type {type}!");
                    return true;
                }
            }

            var gameVersionChanged = GameVersion.GetVersion() != Instance.CachedGameVersion;

            var ummSet = Instance.UmmList.ToHashSet();
            var ummModsChanged = !(ummSet.Count == UnityModManagerNet.UnityModManager.ModEntries.Count);
            if (!ummModsChanged) {
                foreach (var modEntry in UnityModManagerNet.UnityModManager.ModEntries) {
                    if (!ummSet.Contains(new(modEntry.Info.Id, modEntry.Info.Version))) {
                        ummModsChanged = true;
                        break;
                    }
                }
            }

            var ommSet = Instance.OmmList.ToHashSet();
            var ommModsChanged = !(ommSet.Count == OwlcatModificationsManager.s_Instance.AppliedModifications.Length);
            if (!ommModsChanged) {
                foreach (var modEntry in OwlcatModificationsManager.s_Instance.AppliedModifications) {
                    if (!ommSet.Contains(new(modEntry.Manifest.UniqueName, modEntry.Manifest.Version))) {
                        ommModsChanged = true;
                        break;
                    }
                }
            }
            Log($"Test for BPId Cache constincy: Game Version Changed: {gameVersionChanged}; UMM Mod Changed: {ummModsChanged}; OMM Mod Changed: {ommModsChanged}");
            m_NeedsCacheRebuilt = gameVersionChanged || ummModsChanged || ommModsChanged;
            return m_NeedsCacheRebuilt.Value && !IsRebuilding;
        }
    }

    public static bool IsRebuilding = false;
    internal static void RebuildCache(List<SimpleBlueprint> blueprints) {
        if (!Settings.UseBPIdCache) {
            return;
        }
        Log("Starting to build BPId Cache");
        IsRebuilding = true;
        try {
            // Header
            Instance.CachedGameVersion = GameVersion.GetVersion();

            Instance.UmmList.Clear();
            foreach (var modEntry in UnityModManagerNet.UnityModManager.ModEntries) {
                _ = Instance.UmmList.Add(new(modEntry.Info.Id, modEntry.Info.Version));
            }

            Instance.OmmList.Clear();
            foreach (var modEntry in OwlcatModificationsManager.s_Instance.AppliedModifications) {
                _ = Instance.OmmList.Add(new(modEntry.Manifest.UniqueName, modEntry.Manifest.Version));
            }

            //Ids
            Instance.IdsByType.Clear();
            foreach (var type in CachedIdTypes) {
                HashSet<string> idsForType = [];
                foreach (var bp in blueprints) {
                    if (type.IsInstanceOfType(bp)) {
                        _ = idsForType.Add(bp.AssetGuid);
                    }
                }
                if (idsForType.Count > 0) {
                    Instance.IdsByType[type] = idsForType;
                } else {
                    Warn($"BPId Cache Rebuild found no ids for type {type}");
                }
            }

            Instance.Save();
        } catch (Exception ex) {
            Error(ex.ToString());
        }
        m_NeedsCacheRebuilt = false;
        IsRebuilding = false;
        Log("Finished rebuilding BPId Cache");
    }
    internal void Save() {
        using var stream = new FileStream(EnsureFilePath(), FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream, Encoding.UTF8);

        writer.Write(CachedGameVersion);

        writer.Write(UmmList.Count);
        foreach (var (item1, item2) in UmmList) {
            writer.Write(item1);
            writer.Write(item2);
        }

        writer.Write(OmmList.Count);
        foreach (var (item1, item2) in OmmList) {
            writer.Write(item1);
            writer.Write(item2);
        }

        writer.Write(IdsByType.Count);
        foreach (var kvp in IdsByType) {
            writer.Write(kvp.Key.AssemblyQualifiedName);
            writer.Write(kvp.Value.Count);
            foreach (var guid in kvp.Value) {
                writer.Write(guid);
            }
        }
    }
    internal static void Delete() {
        var path = EnsureFilePath();
        if (File.Exists(path)) {
            File.Delete(path);
            Debug($"Manually deleted BPId Cache at: {path}");
        }
        Instance = null!;
    }
    private static BlueprintIdCache? Load() {
        try {
            Trace("Started loading BPId Cache");
            using var stream = new FileStream(EnsureFilePath(), FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            BlueprintIdCache result = new() {
                CachedGameVersion = reader.ReadString()
            };

            var ummCount = reader.ReadInt32();
            for (var i = 0; i < ummCount; i++) {
                _ = result.UmmList.Add((reader.ReadString(), reader.ReadString()));
            }

            var ommCount = reader.ReadInt32();
            for (var i = 0; i < ommCount; i++) {
                _ = result.OmmList.Add((reader.ReadString(), reader.ReadString()));
            }

            var dictCount = reader.ReadInt32();
            for (var i = 0; i < dictCount; i++) {

                var typeName = reader.ReadString();
                var type = Type.GetType(typeName) ?? throw new SerializationException($"BPId Cache references {typeName}, but the type couldn't be found not found.");
                var listCount = reader.ReadInt32();
                var guidList = new HashSet<string>();
                for (var j = 0; j < listCount; j++) {
                    _ = guidList.Add(reader.ReadString());
                }
                if (guidList.Count > 0) {
                    result.IdsByType[type] = guidList;
                } else {
                    Warn($"BPId Cache Load found no ids for type {type}");
                }
            }
            Trace("Finished loading BPId Cache");
            return result;
        } catch (FileNotFoundException) {
            Debug("No BPId Cache found, creating new.");
            return null;
        } catch (SerializationException ex) {
            Error(ex);
            return null;
        }
    }

    private static string EnsureFilePath() {
        var userConfigFolder = Path.Combine(Main.ModEntry.Path, "Settings");
        _ = Directory.CreateDirectory(userConfigFolder);
        return Path.Combine(userConfigFolder, "BlueprintIdCache.bin");
    }
}
