using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;

namespace ToyBox.Features.PatchTool.Infrastructure;

// Runtime-only inverse operation: re-inserts a previously removed element back into a collection field
// at a given index (the inverse of RemoveCollectionElement). Re-registers the element in the blueprint's
// element list if it is an Element. Never serialized -- only lives in Patcher.AppliedInverses.
public sealed class InsertElementOperation : PatchOperation {
    public object? ElementValue;
    public int Index;
    public InsertElementOperation(string fieldName, Type patchedObjectType, object? elementValue, int index) : base(fieldName, patchedObjectType) {
        ElementValue = elementValue;
        Index = index;
    }
    public override string Kind {
        get {
            return "InsertElement";
        }
    }
    public override object? Apply(object instance, out PatchOperation? inverse) {
        inverse = null;
        var field = ResolveField(false, null)!;
        var collection = field.GetValue(instance);
        collection = CollectionElementHelper.Insert(collection, Index, ElementValue!);
        if (ElementValue is Element e && FieldName != nameof(SimpleBlueprint.m_AllElements)) {
            Patcher.CurrentlyPatching!.AddToElementsList(e);
        }
        field.SetValue(instance, collection);
        return instance;
    }
}
