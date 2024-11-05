using Kingmaker.Blueprints;
using ModKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.PatchTool; 
public static class PatchListUI {
    private static Browser<Patch, Patch> _patchBrowser = new(true) { DisplayShowAllGUI = false };
    public static void OnGUI() {
        if (!Patcher.IsInitialized) {
            Label("Patches not loaded yet...".localize());
        } else {
            _patchBrowser.OnGUI(Patcher.KnownPatches.Values, () => Patcher.KnownPatches.Values, p => p, p => $"{ResourcesLibrary.BlueprintsCache.Load(p.BlueprintGuid).NameSafe()} {p.BlueprintGuid} {p.PatchId}", p => [$"{ResourcesLibrary.BlueprintsCache.Load(p.BlueprintGuid).name}", p.BlueprintGuid], 
                () => {
                    Label("Blueprint".localize().Green(), Width(600));
                    Space(50);
                    Label("PatchId".localize().Green(), Width(300));
                    Space(50);
                    Label("Applied?".Green());
                },
                (patch, maybePatch) => {
                    var bp = ResourcesLibrary.BlueprintsCache.Load(patch.BlueprintGuid);
                    Label($"{bp.NameSafe()} ({patch.BlueprintGuid})", Width(600));
                    Space(50);
                    Label($"{patch.PatchId}", Width(300));
                    Space(50);
                    if (Patcher.AppliedPatches.TryGetValue(patch.BlueprintGuid, out var patch2) && patch2.PatchId == patch.PatchId) {
                        Label("Yes".localize(), Width(50));
                    } else {
                        Label("No".localize(), Width(50));
                    }
                    Space(50);
                    if (Main.Settings.disabledPatches.Contains(patch.PatchId)) {
                        ActionButton("Enable".localize(), () => {
                            Main.Settings.disabledPatches.Remove(patch.PatchId);
                            patch.ApplyPatch();
                        }, Width(100));
                    } else {
                        ActionButton("Disable".localize(), () => {
                            Main.Settings.disabledPatches.Add(patch.PatchId);
                            Patcher.RestoreOriginal(patch.BlueprintGuid);
                        }, Width(100));
                    }
                    Space(50);
                    ActionButton("Delete".localize(), () => {
                        DeletePatch(patch);
                    });
                });
        }
    }
    public static void DeletePatch(Patch patch) {
        Patcher.KnownPatches.Remove(patch.BlueprintGuid);
        _patchBrowser.ReloadData();
        var patchFile = Patcher.PatchFilePath(patch);
        if (File.Exists(patchFile)) {
            File.Delete(patchFile);
        }
        if (Patcher.AppliedPatches.ContainsKey(patch.BlueprintGuid)) {
            Patcher.RestoreOriginal(patch.BlueprintGuid);
        }
    }
}
