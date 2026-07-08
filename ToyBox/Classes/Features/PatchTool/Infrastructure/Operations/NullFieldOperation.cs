using ToyBox.Features.PatchTool.Utils;

namespace ToyBox.Features.PatchTool.Infrastructure;

// Clears a field: reference types become null, value types become their default.
public sealed class NullFieldOperation : PatchOperation {
    public Type FieldType;
    public NullFieldOperation(string fieldName, Type patchedObjectType, Type fieldType) : base(fieldName, patchedObjectType) {
        FieldType = fieldType;
    }
    public override string Kind {
        get {
            return "NullField";
        }
    }
    public override object? Apply(object instance, out PatchOperation? inverse) {
        var field = ResolveField(false, FieldType)!;
        var patched = FieldType.IsValueType ? PatchToolUtils.CreateObjectOfType(FieldType) : null;
        inverse = new RestoreValueOperation(FieldName, PatchedObjectType, field.GetValue(instance));
        field.SetValue(instance, patched);
        return instance;
    }
}
