using Kingmaker.Blueprints.Base;
using ToyBox.Features.PatchTool.Utils;

namespace ToyBox.Features.PatchTool.Infrastructure;

// Repoints a blueprint reference field (BlueprintReference<T>) to a different blueprint guid.
public sealed class ModifyBlueprintReferenceOperation : PatchOperation {
    public Type ReferenceType;
    public string BlueprintGuid;
    public ModifyBlueprintReferenceOperation(string fieldName, Type patchedObjectType, Type referenceType, string blueprintGuid) : base(fieldName, patchedObjectType) {
        ReferenceType = referenceType;
        BlueprintGuid = blueprintGuid;
    }
    public override string Kind {
        get {
            return "ModifyBlueprintReference";
        }
    }
    public override object? Apply(object instance, out PatchOperation? inverse) {
        var field = ResolveField(true, ReferenceType);
        var bpRef = (IReferenceBase)PatchToolUtils.CreateObjectOfType(ReferenceType);
        bpRef.ReadGuidFromJson(BlueprintGuid);
        var patched = Convert.ChangeType(bpRef, ReferenceType);
        if (field != null) {
            inverse = new RestoreValueOperation(FieldName, PatchedObjectType, field.GetValue(instance));
            field.SetValue(instance, patched);
            return instance;
        }
        inverse = new RestoreValueOperation(FieldName, PatchedObjectType, instance, isElementMode: true);
        return patched;
    }
}
