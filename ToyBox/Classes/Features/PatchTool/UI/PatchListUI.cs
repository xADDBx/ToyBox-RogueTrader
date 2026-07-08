using Kingmaker.Blueprints;
using ToyBox.Features.PatchTool.Infrastructure;
using static ToyBox.Infrastructure.UI;
using PatchToolPatch = ToyBox.Features.PatchTool.Infrastructure.Patch;

namespace ToyBox.Features.PatchTool;

public static class PatchListUI {
    private static Browser<PatchToolPatch>? m_PatchBrowser;
    private static int m_LastKnownCount = -1;
    private static readonly Dictionary<string, string> m_NameCache = [];
    private static string PatchName(PatchToolPatch p) {
        return m_NameCache.TryGetValue(p.BlueprintGuid, out var name) ? name : p.BlueprintGuid;
    }
    public static void OnGUI() {
        if (!Patcher.IsInitialized) {
            Label(PatchToolStrings.PatchesNotLoaded);
            return;
        }
        RefreshNameCache();
        m_PatchBrowser ??= new(PatchName, p => $"{PatchName(p)} {p.BlueprintGuid} {p.PatchId}", [.. Patcher.KnownPatches.Values], null, true);
        if (m_LastKnownCount != Patcher.KnownPatches.Count) {
            m_LastKnownCount = Patcher.KnownPatches.Count;
            m_PatchBrowser.QueueUpdateItems([.. Patcher.KnownPatches.Values]);
        }
        m_PatchBrowser.OnGUI(patch => {
            using (HorizontalScope()) {
                Label($"{PatchName(patch)} ({patch.BlueprintGuid})", Width(600));
                Space(50);
                Label($"{patch.PatchId}", Width(300));
                Space(50);
                if (Patcher.AppliedPatches.TryGetValue(patch.BlueprintGuid, out var patch2) && patch2.PatchId == patch.PatchId) {
                    Label(PatchToolStrings.Yes, Width(50));
                } else if (Patcher.FailedPatches.Contains(patch)) {
                    Label(PatchToolStrings.Failed.Red(), Width(50));
                } else {
                    Label(PatchToolStrings.No, Width(50));
                }
                Space(50);
                if (Settings.DisabledPatches.Contains(patch.PatchId)) {
                    _ = Button(PatchToolStrings.Enable, () => {
                        _ = Settings.DisabledPatches.Remove(patch.PatchId);
                        _ = patch.ApplyPatch();
                    }, null, Width(100));
                } else {
                    _ = Button(PatchToolStrings.Disable, () => {
                        _ = Settings.DisabledPatches.Add(patch.PatchId);
                        if (Patcher.AppliedPatches.ContainsValue(patch)) {
                            Patcher.RestoreOriginal(patch.BlueprintGuid);
                        }
                    }, null, Width(100));
                }
                Space(50);
                _ = Button(PatchToolStrings.OpenInTab, () => {
                    PatchToolUIManager.OpenBlueprintInTab(patch.BlueprintGuid);
                });
                Space(100);
                _ = Button(PatchToolStrings.Delete, () => {
                    DeletePatch(patch);
                });
            }
        }, () => {
            using (HorizontalScope()) {
                Label(PatchToolStrings.Blueprint.Green(), Width(600));
                Space(50);
                Label(PatchToolStrings.PatchId.Green(), Width(300));
                Space(50);
                Label(PatchToolStrings.Applied.Green(), Width(100));
            }
        });
    }
    private static void RefreshNameCache() {
        foreach (var p in Patcher.KnownPatches.Values) {
            if (!m_NameCache.ContainsKey(p.BlueprintGuid)) {
                var bp = ResourcesLibrary.BlueprintsCache.Load(p.BlueprintGuid);
                if (bp != null) {
                    m_NameCache[p.BlueprintGuid] = bp.NameSafe();
                }
            }
        }
    }
    public static void DeletePatch(PatchToolPatch patch) {
        _ = Patcher.KnownPatches.Remove(patch.BlueprintGuid);
        m_LastKnownCount = Patcher.KnownPatches.Count;
        m_PatchBrowser?.QueueUpdateItems([.. Patcher.KnownPatches.Values]);
        var patchFile = Patcher.PatchFilePath(patch);
        if (File.Exists(patchFile)) {
            File.Delete(patchFile);
        }
        if (Patcher.AppliedPatches.ContainsKey(patch.BlueprintGuid)) {
            Patcher.RestoreOriginal(patch.BlueprintGuid);
        }
    }
}
