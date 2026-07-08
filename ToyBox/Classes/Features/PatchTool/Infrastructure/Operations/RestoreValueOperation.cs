using Kingmaker.ElementsSystem;

namespace ToyBox.Features.PatchTool.Infrastructure;

// Runtime-only inverse operation: puts a previously captured value back into a field (or returns it as
// the new element value in element mode). Handles unregistering elements.
public sealed class RestoreValueOperation : PatchOperation {
    public object? OldValue;
    public Element? ElementToRemove;
    public bool IsElementMode;
    public RestoreValueOperation(string fieldName, Type patchedObjectType, object? oldValue, Element? elementToRemove = null, bool isElementMode = false) : base(fieldName, patchedObjectType) {
        OldValue = oldValue;
        ElementToRemove = elementToRemove;
        IsElementMode = isElementMode;
    }
    public override string Kind {
        get {
            return "RestoreValue";
        }
    }
    public override object? Apply(object instance, out PatchOperation? inverse) {
        inverse = null;
        if (ElementToRemove != null && Patcher.CurrentlyPatching != null) {
            Patcher.CurrentlyPatching.RemoveFromElementsList(ElementToRemove);
        }
        if (IsElementMode) {
            return OldValue;
        }
        var field = ResolveField(false, null)!;
        field.SetValue(instance, OldValue);
        return instance;
    }
}
