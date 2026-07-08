using Kingmaker.ElementsSystem;
using ToyBox.Features.PatchTool.Utils;

namespace ToyBox.Features.PatchTool.Infrastructure;

// Instantiates a fresh value for a (typically null) field: an empty array, a boxed nullable, or a new object.
public sealed class InitializeFieldOperation : PatchOperation {
    public Type NewValueType;
    public InitializeFieldOperation(string fieldName, Type patchedObjectType, Type newValueType) : base(fieldName, patchedObjectType) {
        NewValueType = newValueType;
    }
    public override string Kind {
        get {
            return "InitializeField";
        }
    }
    public override object? Apply(object instance, out PatchOperation? inverse) {
        var field = ResolveField(false, NewValueType)!;
        object patched;
        if (NewValueType.IsArray) {
            patched = Array.CreateInstance(NewValueType.GetElementType()!, 0);
        } else if (PatchToolUtils.IsNullableStruct(NewValueType)) {
            var t = Nullable.GetUnderlyingType(NewValueType)!;
            var @default = PatchToolUtils.CreateObjectOfType(t);
            patched = Activator.CreateInstance(NewValueType, @default);
        } else {
            patched = PatchToolUtils.CreateObjectOfType(NewValueType);
        }
        // Register the Element only after the field resolved, else a failed apply leaks it into m_AllElements.
        Element? createdElement = null;
        if (patched is Element e) {
            Patcher.CurrentlyPatching!.AddToElementsList(e);
            createdElement = e;
        }
        inverse = new RestoreValueOperation(FieldName, PatchedObjectType, field.GetValue(instance), createdElement);
        field.SetValue(instance, patched);
        return instance;
    }
}
