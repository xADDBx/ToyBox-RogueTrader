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
    private static SimpleBlueprint ApplyPatch(this SimpleBlueprint blueprint, Patch patch) {
        AppliedPatches[blueprint.AssetGuid] = patch;
        foreach (var operation in patch.Operations) {
            operation.Apply(blueprint);
        }
        return blueprint;
    }
    public static SimpleBlueprint ApplyPatch(this Patch patch) {
        if (patch == null) return null;
        var current = ResourcesLibrary.TryGetBlueprint(patch.BlueprintGuid);

        if (!OriginalBps.ContainsKey(current.AssetGuid)) {
            OriginalBps[current.AssetGuid] = DeepBlueprintCopy(current);
        }
        var copy = DeepBlueprintCopy(OriginalBps[current.AssetGuid]);
        var patched = copy.ApplyPatch(patch);
        var patchedCurrent = DeepBlueprintCopy(patched, current);
        Mod.Debug($"Assert: current == patchedCurrent: {current == patchedCurrent}");
        Mod.Debug($"Assert: patched == copy: {patched == copy}");
        Mod.Debug($"Assert: current != patched: {current != patched}");
        Mod.Debug($"Assert: current != copy: {current != copy}");
        return patchedCurrent;
    }
    public static void RestoreOriginal(string blueprintGuid) {
        if (OriginalBps.TryGetValue(blueprintGuid, out var copy)) {
            var bp = ResourcesLibrary.TryGetBlueprint(blueprintGuid);
            DeepBlueprintCopy(copy, bp);
            AppliedPatches.Remove(blueprintGuid);
        }
    }
    public static void RegisterPatch(this Patch patch) {
        if (patch == null) return;
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
    public static SimpleBlueprint DeepBlueprintCopy(SimpleBlueprint blueprint, SimpleBlueprint target = null) {
        return blueprint.Copy(target);
    }
}
