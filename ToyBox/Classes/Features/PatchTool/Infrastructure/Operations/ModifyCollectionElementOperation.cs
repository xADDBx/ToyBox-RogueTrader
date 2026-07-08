using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;

namespace ToyBox.Features.PatchTool.Infrastructure;

// Applies the nested operation to the element at a given index within a collection field.
public sealed class ModifyCollectionElementOperation : NestingPatchOperation {
    public int Index;
    public ModifyCollectionElementOperation(string fieldName, Type patchedObjectType, int index) : base(fieldName, patchedObjectType) {
        Index = index;
    }
    public override string Kind {
        get {
            return "ModifyCollectionElement";
        }
    }
    public override object? Apply(object instance, out PatchOperation? inverse) {
        var field = ResolveField(false, null)!;
        var collection = field.GetValue(instance);
        var orig = CollectionElementHelper.GetAt(collection, Index);
        var modified = RequireNested().Apply(orig!, out var nestedInverse);
        collection = CollectionElementHelper.SetAt(collection, Index, modified);
        if (orig is Element e && FieldName != nameof(SimpleBlueprint.m_AllElements)) {
            Patcher.CurrentlyPatching!.RemoveFromElementsList(e);
        }
        if (modified is Element e2 && FieldName != nameof(SimpleBlueprint.m_AllElements)) {
            Patcher.CurrentlyPatching!.AddToElementsList(e2);
        }
        field.SetValue(instance, collection);
        inverse = nestedInverse == null ? null : new ModifyCollectionElementOperation(FieldName, PatchedObjectType, Index) {
            NestedOperation = nestedInverse
        };
        return instance;
    }
}
