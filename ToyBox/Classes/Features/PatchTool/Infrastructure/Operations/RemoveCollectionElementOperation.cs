using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;

namespace ToyBox.Features.PatchTool.Infrastructure;

// Removes the element at a given index from a collection field.
public sealed class RemoveCollectionElementOperation : PatchOperation {
    public int Index;
    public RemoveCollectionElementOperation(string fieldName, Type patchedObjectType, int index) : base(fieldName, patchedObjectType) {
        Index = index;
    }
    public override string Kind {
        get {
            return "RemoveCollectionElement";
        }
    }
    public override object? Apply(object instance, out PatchOperation? inverse) {
        var field = ResolveField(false, null)!;
        var collection = field.GetValue(instance);
        var (newCollection, removed) = CollectionElementHelper.RemoveAt(collection, Index);
        if (removed is Element e && FieldName != nameof(SimpleBlueprint.m_AllElements)) {
            Patcher.CurrentlyPatching!.RemoveFromElementsList(e);
        }
        field.SetValue(instance, newCollection);
        inverse = new InsertElementOperation(FieldName, PatchedObjectType, removed, Index);
        return instance;
    }
}
