using HarmonyLib;
using Kingmaker.Blueprints;
using ModKit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ToyBox.PatchTool;
public static class Patcher {
    public static Dictionary<string, SimpleBlueprint> OriginalBps = new();
    public static Dictionary<string, Patch> AppliedPatches = new();
    public static Dictionary<string, Patch> KnownPatches = new();
    public static bool IsInitialized = false;
    public static string PatchDirectoryPath => Path.Combine(Main.ModEntry.Path, "Patches");
    public static string PatchFilePath(Patch patch) => Path.Combine(PatchDirectoryPath, $"{patch.BlueprintGuid}_{patch.PatchId}.json");
    public static void PatchAll() {
        if (!IsInitialized) {
            Directory.CreateDirectory(PatchDirectoryPath);
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new PatchToolJsonConverter());
            foreach (var file in Directory.GetFiles(PatchDirectoryPath)) {
                try {
                    var patch = JsonConvert.DeserializeObject<Patch>(File.ReadAllText(file), settings);
                    KnownPatches[patch.BlueprintGuid] = patch;
                } catch (Exception ex) {
                    Mod.Log($"Error trying to load patch file {file}:\n{ex.ToString()}");
                }
            }
            IsInitialized = true;
        }
        foreach (var patch in KnownPatches.Values) {
            try {
                if (!Main.Settings.disabledPatches.Contains(patch.PatchId)) {
                    patch.ApplyPatch();
                }
            } catch (Exception ex) {
                Mod.Log($"Error trying to patch blueprint {patch.BlueprintGuid} with patch {patch.PatchId}:\n{ex.ToString()}");
            }
        }
    }
    public static SimpleBlueprint ApplyPatch(this SimpleBlueprint blueprint, Patch patch) {
        AppliedPatches[blueprint.AssetGuid] = patch;
        foreach (var operation in patch.Operations) {
            operation.Apply(blueprint);
        }
        return blueprint;
    }
    // As previous troubles with the BlueprintLoader indicate,
    // replacing an existing instance of a Blueprint after its been loaded can be problematic.
    // Suggest restarting the game after finishing the patches...
    public static SimpleBlueprint ApplyPatch(this Patch patch) {
        if (patch == null) return null;
        var current = ResourcesLibrary.TryGetBlueprint(patch.BlueprintGuid);
        // TODO: Instead of creating a copy, a proper unpatch would work by creating inverse operations
        // based on the stored original blueprint and the applied patch
        if (OriginalBps.TryGetValue(current.AssetGuid, out var pair)) {
            current = DeepBlueprintCopy(pair);
        } else {
            OriginalBps[current.AssetGuid] = DeepBlueprintCopy(current);
        }
        var patched = current.ApplyPatch(patch);
        var entry = ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints[current.AssetGuid];
        entry.Blueprint = patched;
        ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints[current.AssetGuid] = entry;
        return patched;
    }
    public static void RestoreOriginal(string blueprintGuid) {
        if (OriginalBps.TryGetValue(blueprintGuid, out var pair)) {
            var entry = ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints[blueprintGuid];
            entry.Blueprint = pair;
            ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints[blueprintGuid] = entry;
            AppliedPatches.Remove(blueprintGuid);
        }
    }
    public static void RegisterPatch(this Patch patch) {
        try {
            var userPatchesFolder = 
            Directory.CreateDirectory(PatchDirectoryPath);
            File.WriteAllText(PatchFilePath(patch), JsonConvert.SerializeObject(patch, Formatting.Indented));
            KnownPatches[patch.BlueprintGuid] = patch;
            patch.ApplyPatch();
        } catch (Exception ex) {
            Mod.Log($"Error registering patch for blueprint {patch.BlueprintGuid} with patch {patch.PatchId}:\n{ex.ToString()}");
        }
    }
    public static SimpleBlueprint DeepBlueprintCopy(SimpleBlueprint blueprint) {
        return blueprint.Copy();
    }
}
