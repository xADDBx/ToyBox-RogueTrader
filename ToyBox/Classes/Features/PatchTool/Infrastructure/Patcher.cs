using Kingmaker.Blueprints;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Diagnostics;
using ToyBox.Features.PatchTool.Utils;

namespace ToyBox.Features.PatchTool.Infrastructure;

public static class Patcher {
    public static readonly Version CurrentPatchVersion = new(2, 0, 0, 0);
    private static JsonSerializerSettings CreateSerializerSettings(bool indented = false) {
        var settings = new JsonSerializerSettings {
            Formatting = indented ? Formatting.Indented : Formatting.None,
        };
        settings.Converters.Add(new PatchToolJsonConverter());
        settings.Converters.Add(new StringEnumConverter());
        return settings;
    }
    public static Dictionary<string, List<PatchOperation>> AppliedInverses = [];
    public static Dictionary<string, Patch> AppliedPatches = [];
    public static Dictionary<string, Patch> KnownPatches = [];
    public static HashSet<Patch> FailedPatches = [];
    public static SimpleBlueprint? CurrentlyPatching = null;
    public static bool IsInitialized = false;
    public static string PatchDirectoryPath {
        get {
            return Path.Combine(Main.ModEntry.Path, "Patches");
        }
    }

    public static string PatchFilePath(Patch patch) {
        return Path.Combine(PatchDirectoryPath, $"{patch.BlueprintGuid}_{patch.PatchId}.json");
    }

    public static void PatchAll() {
        if (!IsInitialized) {
            _ = Directory.CreateDirectory(PatchDirectoryPath);
            var settings = CreateSerializerSettings();
            foreach (var file in Directory.GetFiles(PatchDirectoryPath)) {
                try {
                    var patch = JsonConvert.DeserializeObject<Patch>(File.ReadAllText(file), settings);

                    // Update old patches:
                    //  - 1.0 => 1.1: Serialize enums as strings
                    //  - 1.1 => 2.0: Use polymorphism for better serialization
                    if ((patch!.PatchVersion ?? new(1, 0)) < CurrentPatchVersion) {
                        patch.RegisterPatch(true);
                    }

                    KnownPatches[patch.BlueprintGuid] = patch;
                } catch (Exception ex) {
                    Log($"Error trying to load patch file {file}:\n{ex}");
                }
            }
            IsInitialized = true;
        }
        Stopwatch watch = new();
        watch.Start();
        var applied = 0;
        foreach (var patch in KnownPatches.Values) {
            if (!Settings.DisabledPatches.Contains(patch.PatchId)) {
                try {
                    if (patch.ApplyPatch()) {
                        applied++;
                    }
                } catch (Exception ex) {
                    Error($"Unexpected error applying patch {patch.PatchId} to blueprint {patch.BlueprintGuid}:\n{ex}");
                    _ = FailedPatches.Add(patch);
                }
            }
        }
        watch.Stop();
        Log($"Successfully applied {applied} of {KnownPatches.Values.Count} patches in {watch.ElapsedMilliseconds}ms");
    }
    private static SimpleBlueprint ApplyPatch(this SimpleBlueprint blueprint, Patch patch) {
        var guid = blueprint.AssetGuid.ToString();
        CurrentlyPatching = blueprint;
        var inverses = new List<PatchOperation>();
        for (var i = 0; i < patch.Operations.Count; i++) {
            try {
                _ = patch.Operations[i].Apply(blueprint, out var inverse);
                if (inverse != null) {
                    inverses.Add(inverse);
                }
                blueprint.OnEnable();
            } catch (Exception ex) {
                Warn($"Error trying to patch blueprint {patch.BlueprintGuid} with patch {patch.PatchId}:\n{ex}, Operation {i + 1}/{patch.Operations.Count}");
                ApplyInverses(blueprint, inverses);
                blueprint.OnEnable();
                CurrentlyPatching = null;
                throw;
            }
        }
        CurrentlyPatching = null;
        AppliedPatches[guid] = patch;
        AppliedInverses[guid] = inverses;
        return blueprint;
    }
    private static void ApplyInverses(SimpleBlueprint blueprint, List<PatchOperation> inverses) {
        for (var i = inverses.Count - 1; i >= 0; i--) {
            try {
                _ = inverses[i].Apply(blueprint, out _);
            } catch (Exception ex) {
                Error($"Error while reverting operation {i + 1}/{inverses.Count} on blueprint {blueprint.AssetGuid}:\n{ex}");
            }
        }
    }
    public static bool ApplyPatch(this Patch patch) {
        if (patch == null) {
            return false;
        }

        if (patch.DangerousOperationsEnabled && !Settings.EnableDangerousPatchToolPatches) {
            Warn($"Tried to apply patch {patch.PatchId} to Blueprint {patch.BlueprintGuid}, but dangerous patches are disabled!");
            return false;
        }
        Log($"Patching Blueprint {patch.BlueprintGuid} with Patch {(patch.DangerousOperationsEnabled ? "!Dangerous Patch! " : "")}{patch.PatchId}.");
        _ = FailedPatches.Remove(patch);
        var current = ResourcesLibrary.TryGetBlueprint(patch.BlueprintGuid);
        if (current == null) {
            Warn($"Target blueprint {patch.BlueprintGuid} for patch {patch.PatchId} does not exist!");
            _ = FailedPatches.Add(patch);
            return false;
        }

        // If this blueprint already has a patch applied this session, revert it first (via its tracked inverses)
        if (AppliedInverses.ContainsKey(patch.BlueprintGuid)) {
            RestoreOriginal(patch.BlueprintGuid);
            current = ResourcesLibrary.TryGetBlueprint(patch.BlueprintGuid);
            if (current == null) {
                Warn($"Target blueprint {patch.BlueprintGuid} for patch {patch.PatchId} disappeared after revert!");
                _ = FailedPatches.Add(patch);
                return false;
            }
        }
        try {
            _ = current.ApplyPatch(patch);
        } catch (Exception) {
            // ApplyPatch already rolled back its own partial application before rethrowing.
            _ = FailedPatches.Add(patch);
            return false;
        }
        return true;
    }
    public static void RestoreOriginal(string blueprintGuid) {
        Log($"Trying to restore original Blueprint {blueprintGuid}");
        if (AppliedInverses.TryGetValue(blueprintGuid, out var inverses)) {
            var bp = ResourcesLibrary.TryGetBlueprint(blueprintGuid);
            if (bp == null) {
                Error($"Blueprint {blueprintGuid} not found while restoring; dropping its tracked state.");
            } else {
                Log($"Found {inverses.Count} tracked inverse operations; reverting.");
                CurrentlyPatching = bp;
                try {
                    ApplyInverses(bp, inverses);
                    bp.OnEnable();
                } catch (Exception ex) {
                    Error($"Error while restoring original blueprint {blueprintGuid}:\n{ex}");
                } finally {
                    CurrentlyPatching = null;
                }
            }
            _ = AppliedInverses.Remove(blueprintGuid);
            _ = AppliedPatches.Remove(blueprintGuid);
        } else {
            Error("No tracked inverse operations found! Was it never patched?");
        }
    }
    public static void RegisterPatch(this Patch patch, bool isPatchUpdate = false) {
        if (patch == null) {
            return;
        }

        try {
            if (isPatchUpdate) {
                Log($"Updating patch {patch.PatchId} for blueprint {patch.BlueprintGuid}\nVersion {patch.PatchVersion} to {CurrentPatchVersion}");
            }
            var userPatchesFolder = Directory.CreateDirectory(PatchDirectoryPath);
            var settings = CreateSerializerSettings(true);
            patch.PatchVersion = CurrentPatchVersion;
            File.WriteAllText(PatchFilePath(patch), JsonConvert.SerializeObject(patch, settings));
            KnownPatches[patch.BlueprintGuid] = patch;
            if (!isPatchUpdate) {
                _ = patch.ApplyPatch();
            }
        } catch (Exception ex) {
            if (isPatchUpdate) {
                Log($"Error updating patch {patch.PatchId}:\n{ex}");
            } else {
                Log($"Error registering patch for blueprint {patch.BlueprintGuid} with patch {patch.PatchId}:\n{ex}");
            }
        }
    }
}
