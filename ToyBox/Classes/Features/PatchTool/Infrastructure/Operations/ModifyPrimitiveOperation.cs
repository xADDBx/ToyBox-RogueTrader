namespace ToyBox.Features.PatchTool.Infrastructure;

// Sets a primitive or enum field to a new value.
public sealed class ModifyPrimitiveOperation : PatchOperation {
    public Type NewValueType;
    public object NewValue;
    public ModifyPrimitiveOperation(string fieldName, Type patchedObjectType, Type newValueType, object newValue) : base(fieldName, patchedObjectType) {
        NewValueType = newValueType;
        NewValue = newValue;
    }
    public override string Kind {
        get {
            return "ModifyPrimitive";
        }
    }
    public override object? Apply(object instance, out PatchOperation? inverse) {
        var field = ResolveField(true, NewValueType);
        object patched;
        if (typeof(Enum).IsAssignableFrom(NewValueType)) {
            var tmp = Convert.ChangeType(NewValue, Enum.GetUnderlyingType(NewValueType));
            patched = Enum.ToObject(NewValueType, tmp);
        } else {
            patched = Convert.ChangeType(NewValue, NewValueType);
        }
        if (field != null) {
            inverse = new RestoreValueOperation(FieldName, PatchedObjectType, field.GetValue(instance));
            field.SetValue(instance, patched);
            return instance;
        }
        inverse = new RestoreValueOperation(FieldName, PatchedObjectType, instance, isElementMode: true);
        return patched;
    }
}
