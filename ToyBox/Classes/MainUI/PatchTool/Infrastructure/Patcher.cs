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
    public static readonly Version CurrentPatchVersion = new(1, 1, 0, 0);
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
                    if ((patch.PatchVersion ?? new(1, 0)) < new Version(1, 1)) {
                        patch.RegisterPatch(false);
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
            operation.Apply(blueprint);
            blueprint.OnEnable();
        }
        CurrentlyPatching = null;
        AppliedPatches[blueprint.AssetGuid.ToString()] = patch;
        return blueprint;
    }
    public static bool ApplyPatch(this Patch patch) {
        if (patch == null) return false;
        Mod.Log($"Patching Blueprint {patch.BlueprintGuid} with Patch {patch.PatchId}.");
        FailedPatches.Remove(patch);
        var current = ResourcesLibrary.TryGetBlueprint(patch.BlueprintGuid);

        // Consideration: DeepCopies are only necessary to allow reverting actions; meaning they are only needed if users plan to change patches in the current session
        // By adding a "Dev Mode" setting, it would be possible to completely drop DeepCopies, making this pretty performant.

        if (!OriginalBps.ContainsKey(current.AssetGuid)) {
            OriginalBps[current.AssetGuid] = DeepBlueprintCopy(current);
        } else {
            current = DeepBlueprintCopy(OriginalBps[current.AssetGuid], current);
        }
        try {
            current.ApplyPatch(patch);
        } catch (Exception ex) {
            RestoreOriginal(patch.BlueprintGuid);
            FailedPatches.Add(patch);
            Mod.Log($"Error trying to patch blueprint {patch.BlueprintGuid} with patch {patch.PatchId}:\n{ex.ToString()}");
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
    public static void RegisterPatch(this Patch patch, bool apply = true) {
        if (patch == null) return;
        try {
            var userPatchesFolder = 
            Directory.CreateDirectory(PatchDirectoryPath);
            File.WriteAllText(PatchFilePath(patch), JsonConvert.SerializeObject(patch, Formatting.Indented));
            KnownPatches[patch.BlueprintGuid] = patch;
            if (apply) {
                patch.ApplyPatch();
            }
        } catch (Exception ex) {
            Mod.Log($"Error registering patch for blueprint {patch.BlueprintGuid} with patch {patch.PatchId}:\n{ex.ToString()}");
        }
    }
    public static SimpleBlueprint DeepBlueprintCopy(SimpleBlueprint blueprint, SimpleBlueprint target = null) {
        return blueprint.Copy(target, true);
    }
}
