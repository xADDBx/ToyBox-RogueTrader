using System.Reflection;
using System.Runtime.CompilerServices;
using ToyBox.Features.PatchTool.Utils;

namespace ToyBox.Features.PatchTool.Infrastructure;

public abstract class PatchOperation {
    public string FieldName;
    public Type PatchedObjectType;
    protected PatchOperation(string fieldName, Type patchedObjectType) {
        FieldName = fieldName;
        PatchedObjectType = patchedObjectType;
    }
    public abstract string Kind { get; }
    // Applies the operation to instance. Additionally emits a runtime-only inverse operation that,
    // when applied to the (then-current) instance, restores the state that existed before this Apply.
    public abstract object? Apply(object instance, out PatchOperation? inverse);

    // Resolves the target field, or returns null in "element mode": when canOperateOnElement is set and
    // the named field is itself a collection, the operation is being applied to an element of that
    // collection (via ModifyCollectionElementOperation), so it must operate on instance directly and
    // return the new value instead of writing a field.
    protected FieldInfo? ResolveField(bool canOperateOnElement, Type? mustBeAssignableFrom) {
        var field = AccessTools.Field(PatchedObjectType, FieldName)
            ?? throw new ArgumentException($"Field '{FieldName}' does not exist on type {PatchedObjectType} (operation: {Kind}). It may have been renamed or removed by a game update.");
        if (canOperateOnElement && PatchToolUtils.IsListOrArray(field.FieldType)) {
            return null;
        }
        if (mustBeAssignableFrom != null && !field.FieldType.IsAssignableFrom(mustBeAssignableFrom)) {
            throw new ArgumentException($"Field {PatchedObjectType}.{FieldName} of type {field.FieldType} is not assignable from {mustBeAssignableFrom} (operation: {Kind}).");
        }
        return field;
    }
}

public abstract class NestingPatchOperation : PatchOperation {
    public PatchOperation? NestedOperation;
    protected NestingPatchOperation(string fieldName, Type patchedObjectType) : base(fieldName, patchedObjectType) { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected PatchOperation RequireNested() {
        return NestedOperation ?? throw new InvalidOperationException($"{Kind} operation on {PatchedObjectType?.Name}.{FieldName} has no nested operation to apply.");
    }
}
