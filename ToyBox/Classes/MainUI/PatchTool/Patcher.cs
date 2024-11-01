using HarmonyLib;
using Kingmaker.Blueprints;
using ModKit;
using ModKit.Utility.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.PatchTool; 
public static class Patcher {
    public static Dictionary<string, SimpleBlueprint> OriginalBps = new();
    public static Dictionary<string, Patch> AppliedPatches = new();
    public static Dictionary<string, Patch> KnownPatches = new();
    private static bool _init = false;
    public static void PatchAll() {
        if (!_init) {
            var userPatchesFolder = Path.Combine(Main.ModEntry.Path, "Patches");
            Directory.CreateDirectory(userPatchesFolder);
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new PatchToolJsonConverter());
            foreach (var file in Directory.GetFiles(userPatchesFolder)) {
                try {
                    var patch = JsonConvert.DeserializeObject<Patch>(File.ReadAllText(file), settings);
                    KnownPatches[patch.BlueprintGuid] = patch;
                } catch (Exception ex) {
                    Mod.Log($"Error trying to load patch file {file}:\n{ex.ToString()}");
                }
            }
            _init = true;
        }
        foreach (var patch in KnownPatches.Values) {
            try {
                patch.ApplyPatch();
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
    public static SimpleBlueprint ApplyPatch(this Patch patch) {
        if (patch == null) return null;
        var current = ResourcesLibrary.TryGetBlueprint(patch.BlueprintGuid);
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
    public static void RegisterPatch(this Patch patch) {
        try {
            var userPatchesFolder = Path.Combine(Main.ModEntry.Path, "Patches");
            Directory.CreateDirectory(userPatchesFolder);
            File.WriteAllText(Path.Combine(userPatchesFolder, $"{patch.BlueprintGuid}_{patch.PatchId}.json"), JsonConvert.SerializeObject(patch, Formatting.Indented));
            KnownPatches[patch.BlueprintGuid] = patch;
            patch.ApplyPatch();
        } catch (Exception ex) {
            Mod.Log($"Error registering patch for blueprint {patch.BlueprintGuid} with patch {patch.PatchId}:\n{ex.ToString()}");
        }
    }
    public static SimpleBlueprint DeepBlueprintCopy(SimpleBlueprint blueprint) {
        return blueprint.DeepCopy() as SimpleBlueprint;
    }
}
