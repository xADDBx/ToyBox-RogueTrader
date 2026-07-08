using UnityEngine;

namespace ToyBox.Features.PatchTool.Infrastructure;

// Applies the nested operation to a transform child (by index). The child lives on the shared prefab, so
// edits are global (hence dangerous) but still reverted in-session via the nested inverse. ChildName is
// only used to warn when a reordered hierarchy makes the index point elsewhere.
public sealed class ModifyGameObjectChildOperation : NestingPatchOperation {
    public int Index;
    public string ChildName;
    public ModifyGameObjectChildOperation(string fieldName, Type patchedObjectType, int index, string childName) : base(fieldName, patchedObjectType) {
        Index = index;
        ChildName = childName;
    }
    public override string Kind {
        get {
            return "ModifyGameObjectChild";
        }
    }
    public override object? Apply(object instance, out PatchOperation? inverse) {
        var go = instance as GameObject ?? throw new ArgumentException($"ModifyGameObjectChild expected a GameObject but got {instance?.GetType().ToString() ?? "null"}.");
        var transform = go.transform;
        if (Index < 0 || Index >= transform.childCount) {
            throw new ArgumentException($"GameObject '{go.name}' has no child at index {Index} (childCount {transform.childCount}).");
        }
        var child = transform.GetChild(Index).gameObject;
        // The index may point elsewhere if the hierarchy was reordered; don't silently edit the wrong child.
        if (!string.IsNullOrEmpty(ChildName) && child.name != ChildName) {
            Warn($"ModifyGameObjectChild on '{go.name}' index {Index}: expected child '{ChildName}' but found '{child.name}'. Applying anyway.");
        }
        _ = RequireNested().Apply(child, out var nestedInverse);
        inverse = nestedInverse == null ? null : new ModifyGameObjectChildOperation(FieldName, PatchedObjectType, Index, ChildName) {
            NestedOperation = nestedInverse
        };
        return instance;
    }
}
