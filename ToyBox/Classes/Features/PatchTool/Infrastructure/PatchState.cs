using Kingmaker.Blueprints;

namespace ToyBox.Features.PatchTool.Infrastructure;

public class PatchState {
    public SimpleBlueprint Blueprint = null!;
    public List<PatchOperation> Operations = [];
    public bool DangerousOperationsEnabled = false;
    private Patch? m_UnderlyingPatch;
    public bool IsDirty = false;
    public PatchState(SimpleBlueprint blueprint) {
        SetupFromBlueprint(blueprint);
    }
    public PatchState(Patch patch) {
        m_UnderlyingPatch = patch;
        var bp = ResourcesLibrary.TryGetBlueprint(patch.BlueprintGuid)
            ?? throw new ArgumentException($"Cannot create a PatchState for patch {patch.PatchId}: blueprint {patch.BlueprintGuid} does not exist (it may have been renamed or removed by a game update).");
        if (!Patcher.AppliedPatches.ContainsKey(patch.BlueprintGuid)) {
            _ = patch.ApplyPatch();
        }
        Operations = patch.Operations;
        SetupFromBlueprint(bp);
    }
    public void SetupFromBlueprint(SimpleBlueprint blueprint) {
        Blueprint = blueprint;
        if (Patcher.KnownPatches.TryGetValue(blueprint.AssetGuid, out m_UnderlyingPatch)) {
            Operations = m_UnderlyingPatch.Operations;
        }
    }
    public void CreateAndRegisterPatch() {
        if ((Operations?.Count ?? 0) == 0) {
            if (Patcher.AppliedPatches.TryGetValue(Blueprint.AssetGuid, out var patch)) {
                PatchListUI.DeletePatch(patch);
                IsDirty = true;
            }
            return;
        }
        CreatePatch()?.RegisterPatch();
        IsDirty = true;
    }
    public Patch? CreatePatch() {
        try {
            IsDirty = true;
            if (m_UnderlyingPatch != null) {
                m_UnderlyingPatch.Operations = Operations;
                m_UnderlyingPatch.DangerousOperationsEnabled |= DangerousOperationsEnabled;
                return m_UnderlyingPatch;
            } else {
                return new(Blueprint.AssetGuid.ToString(), Operations, DangerousOperationsEnabled);
            }
        } catch (Exception ex) {
            Warn($"Error trying to create patch for blueprint {Blueprint.AssetGuid}:\n{ex}");
        }
        return null;
    }
    public void AddOp(PatchOperation op) {
        var foD = Operations.FirstOrDefault(i => i is ModifyPrimitiveOperation && i.PatchedObjectType == op.PatchedObjectType && i.FieldName == op.FieldName);
        if (foD != default) {
            _ = Operations.Remove(foD);
        }
        Operations.Add(op);
    }
}
