using ToyBox.Features.PatchTool.Utils;

namespace ToyBox.Features.PatchTool.Infrastructure;

// Recurses into a complex (nested object) field and applies the nested operation to its value.
public sealed class ModifyComplexOperation : NestingPatchOperation {
    public ModifyComplexOperation(string fieldName, Type patchedObjectType) : base(fieldName, patchedObjectType) { }
    public override string Kind {
        get {
            return "ModifyComplex";
        }
    }
    public override object? Apply(object instance, out PatchOperation? inverse) {
        var nested = RequireNested();
        var field = ResolveField(true, null);
        var @object = field == null ? instance : field.GetValue(instance);
        var patched = nested.Apply(@object, out var nestedInverse);
        if (PatchToolUtils.IsNullableStruct(nested.PatchedObjectType)) {
            patched = Activator.CreateInstance(nested.PatchedObjectType, patched);
        }
        inverse = nestedInverse == null ? null : new ModifyComplexOperation(FieldName, PatchedObjectType) {
            NestedOperation = nestedInverse
        };
        if (field != null) {
            field.SetValue(instance, patched);
            return instance;
        }
        return patched;
    }
}
