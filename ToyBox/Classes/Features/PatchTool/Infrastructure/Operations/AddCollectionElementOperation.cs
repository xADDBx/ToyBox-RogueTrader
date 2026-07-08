using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using ToyBox.Features.PatchTool.Utils;

namespace ToyBox.Features.PatchTool.Infrastructure;

// Inserts a newly created element into a collection field at a given index (-1 appends).
public sealed class AddCollectionElementOperation : PatchOperation {
    public Type ElementType;
    public int Index;
    public AddCollectionElementOperation(string fieldName, Type patchedObjectType, Type elementType, int index) : base(fieldName, patchedObjectType) {
        ElementType = elementType;
        Index = index;
    }
    public override string Kind {
        get {
            return "AddCollectionElement";
        }
    }
    public override object? Apply(object instance, out PatchOperation? inverse) {
        var field = ResolveField(false, null)!;
        var collection = field.GetValue(instance);
        // Index == -1 means "append"; resolve the concrete insertion index so the inverse can remove it.
        var insertIndex = Index == -1 ? CollectionElementHelper.Count(collection!) : Index;
        var newInst = PatchToolUtils.CreateObjectOfType(ElementType);
        collection = CollectionElementHelper.Insert(collection, Index, newInst);
        if (newInst is Element e && FieldName != nameof(SimpleBlueprint.m_AllElements)) {
            Patcher.CurrentlyPatching!.AddToElementsList(e);
        }
        field.SetValue(instance, collection);
        inverse = new RemoveCollectionElementOperation(FieldName, PatchedObjectType, insertIndex);
        return instance;
    }
}
