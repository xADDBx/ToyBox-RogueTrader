using HarmonyLib;
using Kingmaker.Blueprints;
using ModKit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ToyBox.PatchTool;
public static class Patcher {
    public static readonly Version CurrentPatchVersion = new(1, 1, 1, 0);
    public static Dictionary<string, SimpleBlueprint> OriginalBps = new();
    public static Dictionary<string, Patch> AppliedPatches = new();
    public static Dictionary<string, Patch> KnownPatches = new();
    public static HashSet<Patch> FailedPatches = new();
    public static SimpleBlueprint CurrentlyPatching = null;
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

                    // Update old patches; 1.0 => 1.1: Serialize enums as strings
                    if ((patch.PatchVersion ?? new(1, 0)) < CurrentPatchVersion) {
                        patch.RegisterPatch(true);
                    }

                    KnownPatches[patch.BlueprintGuid] = patch;
                } catch (Exception ex) {
                    Mod.Log($"Error trying to load patch file {file}:\n{ex.ToString()}");
                }
            }
            IsInitialized = true;
        }
        Stopwatch watch = new();
        watch.Start();
        int applied = 0;
        foreach (var patch in KnownPatches.Values) {
            if (!Main.Settings.disabledPatches.Contains(patch.PatchId)) {
                if (patch.ApplyPatch()) {
                    applied++;
                }
            }
        }
        watch.Stop();
        Mod.Log($"Successfully applied {applied} of {KnownPatches.Values.Count} patches in {watch.ElapsedMilliseconds}ms");
    }
    private static SimpleBlueprint ApplyPatch(this SimpleBlueprint blueprint, Patch patch) {
        CurrentlyPatching = blueprint;
        foreach (var operation in patch.Operations) {
            try {
                operation.Apply(blueprint);
                blueprint.OnEnable();
            } catch (Exception ex) {
                Mod.Warn($"Error trying to patch blueprint {patch.BlueprintGuid} with patch {patch.PatchId}:\n{ex.ToString()}, Operation {patch.Operations.IndexOf(operation) + 1}/{patch.Operations.Count}");
                throw;
            }
        }
        CurrentlyPatching = null;
        AppliedPatches[blueprint.AssetGuid.ToString()] = patch;
        return blueprint;
    }
    public static bool ApplyPatch(this Patch patch) {
        if (patch == null) return false;
        if (patch.DangerousOperationsEnabled && !Main.Settings.toggleEnableDangerousPatchToolPatches) {
            Mod.Warn($"Tried to apply patch {patch.PatchId} to Blueprint {patch.BlueprintGuid}, but dangerous patches are disabled!");
            return false;
        }
        Mod.Log($"Patching Blueprint {patch.BlueprintGuid} with Patch {(patch.DangerousOperationsEnabled ? "!Dangerous Patch! " : "")}{patch.PatchId}.");
        FailedPatches.Remove(patch);
        var current = ResourcesLibrary.TryGetBlueprint(patch.BlueprintGuid);
        if (current == null) {
            Mod.Warn($"Target blueprint {patch.BlueprintGuid} for patch {patch.PatchId} does not exist!");
            FailedPatches.Add(patch);
            return false;
        }

        // Consideration: DeepCopies are only necessary to allow reverting actions; meaning they are only needed if users plan to change patches in the current session
        // By adding a "Dev Mode" setting, it would be possible to completely drop DeepCopies, making this pretty performant.

        if (!OriginalBps.ContainsKey(current.AssetGuid)) {
            OriginalBps[current.AssetGuid] = DeepBlueprintCopy(current);
        } else {
            current = DeepBlueprintCopy(OriginalBps[current.AssetGuid], current);
        }
        try {
            current.ApplyPatch(patch);
        } catch (Exception) {
            RestoreOriginal(patch.BlueprintGuid);
            FailedPatches.Add(patch);
            return false;
        }
        return true;
    }
    public static void RestoreOriginal(string blueprintGuid) {
        Mod.Log($"Trying to restore original Blueprint {blueprintGuid}");
        if (OriginalBps.TryGetValue(blueprintGuid, out var copy)) {
            Mod.Log($"Found original blueprint; reverting.");
            var bp = ResourcesLibrary.TryGetBlueprint(blueprintGuid);
            DeepBlueprintCopy(copy, bp);
            AppliedPatches.Remove(blueprintGuid);
        } else {
            Mod.Error("No original blueprint found! Was it never patched?");
        }
    }
    public static void RegisterPatch(this Patch patch, bool isPatchUpdate = false) {
        if (patch == null) return;
        try {
            if (isPatchUpdate) {
                Mod.Log($"Updating patch {patch.PatchId} for blueprint {patch.BlueprintGuid}\nVersion {patch.PatchVersion} to {CurrentPatchVersion}");
            }
            var userPatchesFolder = 
            Directory.CreateDirectory(PatchDirectoryPath);
            var settings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
            settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            patch.PatchVersion = CurrentPatchVersion;
            File.WriteAllText(PatchFilePath(patch), JsonConvert.SerializeObject(patch, settings));
            KnownPatches[patch.BlueprintGuid] = patch;
            if (!isPatchUpdate) {
                patch.ApplyPatch();
            }
        } catch (Exception ex) {
            if (isPatchUpdate) {
                Mod.Log($"Error updating patch {patch.PatchId}:\n{ex.ToString()}");
            } else {
                Mod.Log($"Error registering patch for blueprint {patch.BlueprintGuid} with patch {patch.PatchId}:\n{ex.ToString()}");
            }
        }
    }
    public static SimpleBlueprint DeepBlueprintCopy(SimpleBlueprint blueprint, SimpleBlueprint target = null) {
        return blueprint.Copy(target, true);
    }
}
