using HarmonyLib;
using Kingmaker.Blueprints;
using ModKit;
using ModKit.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.PatchTool; 
public class PatchState {
    public SimpleBlueprint Blueprint;
    public List<PatchOperation> Operations = new();
    private Patch UnderlyingPatch;
    public bool IsDirty = false;
    public PatchState(SimpleBlueprint blueprint) {
        SetupFromBlueprint(blueprint);
    }
    public PatchState(Patch patch) {
        UnderlyingPatch = patch;
        var bp = ResourcesLibrary.TryGetBlueprint(patch.BlueprintGuid);
        if (!Patcher.AppliedPatches.ContainsKey(patch.BlueprintGuid)) {
            patch.ApplyPatch();
        }
        Operations = patch.Operations;
        SetupFromBlueprint(bp);
    }
    public void SetupFromBlueprint(SimpleBlueprint blueprint) {
        Blueprint = blueprint;
        if (Patcher.KnownPatches.TryGetValue(blueprint.AssetGuid, out UnderlyingPatch)) {
            Operations = UnderlyingPatch.Operations;
        }
    }
    public Patch CreatePatchFromState() {
        try {
            IsDirty = true;
            if (UnderlyingPatch != null) {
                UnderlyingPatch.Operations = Operations;
                return UnderlyingPatch;
            } else {
                return new(Blueprint.AssetGuid, Operations);
            }
        } catch (Exception ex) {
            Mod.Log($"Error trying to create patch for blueprint {Blueprint.AssetGuid}:\n{ex.ToString()}");
        }
        return null;
    }
    public void AddOp(PatchOperation op) {
        var foD = Operations.FirstOrDefault(i => i.OperationType == PatchOperation.PatchOperationType.ModifyPrimitive && i.PatchedObjectType == op.PatchedObjectType && i.FieldName == op.FieldName);
        if (foD != default) {
            Operations.Remove(foD);
        }
        Operations.Add(op);
    }
}
