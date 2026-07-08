using UnityEngine;

namespace ToyBox.Features.PatchTool.Infrastructure;

// Applies the nested operation to a Component, matched by concrete type + type-relative index (e.g. the
// 2nd MyBehaviour). Components live on the shared prefab, so edits are global (hence dangerous) but still
// reverted in-session via the nested inverse.
public sealed class ModifyGameObjectComponentOperation : NestingPatchOperation {
    public Type ComponentType;
    public int Index;
    public ModifyGameObjectComponentOperation(string fieldName, Type patchedObjectType, Type componentType, int index) : base(fieldName, patchedObjectType) {
        ComponentType = componentType;
        Index = index;
    }
    public override string Kind {
        get {
            return "ModifyGameObjectComponent";
        }
    }
    public override object? Apply(object instance, out PatchOperation? inverse) {
        var go = instance as GameObject ?? throw new ArgumentException($"ModifyGameObjectComponent expected a GameObject but got {instance?.GetType().ToString() ?? "null"}.");
        var matches = go.GetComponents<Component>().Where(c => c != null && c.GetType() == ComponentType).ToList();
        if (Index < 0 || Index >= matches.Count) {
            throw new ArgumentException($"GameObject '{go.name}' has no component of type {ComponentType} at index {Index} (found {matches.Count}).");
        }
        _ = RequireNested().Apply(matches[Index], out var nestedInverse);
        inverse = nestedInverse == null ? null : new ModifyGameObjectComponentOperation(FieldName, PatchedObjectType, ComponentType, Index) {
            NestedOperation = nestedInverse
        };
        return instance;
    }
}
