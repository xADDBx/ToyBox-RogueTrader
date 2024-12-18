using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.Converters;
using Kingmaker.ElementsSystem;
using Newtonsoft.Json.Linq;
using RogueTrader.SharedTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.PatchTool; 
public class PatchOperation {
    public enum PatchOperationType {
        ModifyPrimitive,
        ModifyUnityReference,
        ModifyBlueprintReference,
        ModifyComplex,
        ModifyCollection
    }
    public enum CollectionPatchOperationType {
        AddAtIndex,
        RemoveAtIndex,
        ModifyAtIndex
    }
    public string FieldName;
    public Type NewValueType;
    public object NewValue;
    public int CollectionIndex;
    public PatchOperation NestedOperation;
    public Type PatchedObjectType;
    public PatchOperationType OperationType;
    public CollectionPatchOperationType CollectionOperationType;
    public PatchOperation() { }
    public PatchOperation(PatchOperationType operationType, string fieldName, Type newValueType, object newValue, Type patchedObjectType, PatchOperation nestedOperation = null) {
        OperationType = operationType;
        FieldName = fieldName;
        NewValue = newValue;
        PatchedObjectType = patchedObjectType;
        NestedOperation = nestedOperation;
        NewValueType = newValueType;
    }
    public PatchOperation(PatchOperationType operationType, string fieldName, Type newValueType, object newValue, Type patchedObjectType, CollectionPatchOperationType collectionOperationType, int collectionIndex, PatchOperation nestedOperation = null) {
        OperationType = operationType;
        FieldName = fieldName;
        NewValue = newValue;
        PatchedObjectType = patchedObjectType;
        CollectionOperationType = collectionOperationType;
        CollectionIndex = collectionIndex;
        NestedOperation = nestedOperation;
        NewValueType = newValueType;
    }
    public FieldInfo GetFieldInfo(Type type) {
        return AccessTools.Field(type, FieldName);
    }
    public object Apply(object instance) {
        var field = GetFieldInfo(PatchedObjectType);
        if (PatchToolUtils.IsListOrArray(field.FieldType) && (OperationType != PatchOperationType.ModifyCollection)) {
            // We're in a collection, so the patched field will point to a collection, meaning we will need to work on the instance itself.
            // By returning the changed instance, the ModifyCollection operation will set the returned value itself.
            field = null;
        }
        if (!(OperationType == PatchOperationType.ModifyCollection || OperationType == PatchOperationType.ModifyComplex) && (field != null && !field.FieldType.IsAssignableFrom(NewValueType))) throw new ArgumentException($"Type to patch {PatchedObjectType}, field {field.Name} with type {field.FieldType} is not assignable from instance type {instance.GetType()}\nField: {FieldName ?? null}, OperationType: {OperationType}, NestedOperationType: {NestedOperation?.OperationType.ToString() ?? "Null"} ");

        switch (OperationType) {
            case PatchOperationType.ModifyCollection: {
                    object collection;
                    if (field == null) {
                        collection = instance;
                    } else {
                        collection = field.GetValue(instance);
                    }
                    switch (CollectionOperationType) {
                        case CollectionPatchOperationType.AddAtIndex: {
                                var newInst = Activator.CreateInstance(NewValueType);
                                if (collection.GetType() is Type type && type.IsArray) {
                                    Array array = collection as Array;
                                    if (CollectionIndex == -1) CollectionIndex = array.Length;
                                    var elementType = type.GetElementType();
                                    Array newArray = Array.CreateInstance(elementType, array.Length + 1);
                                    Array.Copy(array, 0, newArray, 0, CollectionIndex);
                                    newArray.SetValue(newInst, CollectionIndex);
                                    Array.Copy(array, CollectionIndex, newArray, CollectionIndex + 1, array.Length - CollectionIndex);
                                    collection = newArray;
                                } else if (collection is IList list) {
                                    if (CollectionIndex == -1) CollectionIndex = list.Count;
                                    list.Insert(CollectionIndex, newInst);
                                    collection = list;
                                } else if (PatchToolUtils.IsListOrArray(collection.GetType())) {
                                    var interfaceType = collection.GetType().GetInterfaces().Where(i => i.IsGenericType).FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IList<>));
                                    if (interfaceType != null) {
                                        var m = collection.GetType().GetInterfaceMethodImplementation(interfaceType.GetMethod("Insert"));
                                        if (m != null) {
                                            m.Invoke(collection, [CollectionIndex, newInst]);
                                        } else {
                                            throw new ArgumentException($"Error while trying to use ArrayPatchOperation. Weird error. {field?.Name ?? "Null Field"}, {collection.GetType()}, {PatchedObjectType}");
                                        }
                                    } else {
                                        throw new ArgumentException($"Error while trying to use ArrayPatchOperation. Weird error2. {field?.Name ?? "Null Field"}, {collection.GetType()}, {PatchedObjectType}");
                                    }
                                } else {
                                    throw new ArgumentException($"Error while trying to use patch \"AddAtIndex\". Collection is not Array, IList or ListOrArray. {field?.Name ?? "Null Field"}, {collection.GetType()}, {PatchedObjectType}");
                                }
                                if (newInst is Element e && FieldName != nameof(SimpleBlueprint.m_AllElements)) {
                                    Patcher.CurrentlyPatching.AddToElementsList(e);
                                }
                            } 
                            break;
                        case CollectionPatchOperationType.RemoveAtIndex: {
                                object oldInst = null;
                                if (collection.GetType() is Type type && type.IsArray) {
                                    Array array = collection as Array;
                                    var elementType = type.GetElementType();
                                    var tmpList = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType)) as IList;
                                    foreach (var item in array)
                                        tmpList.Add(item);
                                    oldInst = tmpList[CollectionIndex];
                                    tmpList.RemoveAt(CollectionIndex);
                                    Array resizedArray = Array.CreateInstance(elementType, tmpList.Count);
                                    tmpList.CopyTo(resizedArray, 0);
                                    collection = resizedArray;
                                } else if (collection is IList list) {
                                    oldInst = list[CollectionIndex];
                                    list.RemoveAt(CollectionIndex);
                                    collection = list;
                                } else if (PatchToolUtils.IsListOrArray(collection.GetType())) {
                                    var interfaceType = collection.GetType().GetInterfaces().Where(i => i.IsGenericType).FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IList<>));
                                    if (interfaceType != null) {
                                        var index_getter = collection.GetType().GetInterfaceMethodImplementation(interfaceType.GetProperties().First().GetGetMethod());
                                        var m = collection.GetType().GetInterfaceMethodImplementation(interfaceType.GetMethod("RemoveAt"));
                                        if (m != null && index_getter != null) {
                                            oldInst = index_getter.Invoke(collection, [CollectionIndex]);
                                            m.Invoke(collection, [CollectionIndex]);
                                        } else {
                                            throw new ArgumentException($"Error while trying to use ArrayPatchOperation. Weird error. {field?.Name ?? "Null Field"}, {collection.GetType()}, {PatchedObjectType}");
                                        }
                                    } else {
                                        throw new ArgumentException($"Error while trying to use ArrayPatchOperation. Weird error2. {field?.Name ?? "Null Field"}, {collection.GetType()}, {PatchedObjectType}");
                                    }
                                } else {
                                    throw new ArgumentException($"Error while trying to use patch \"ModifyAtIndex\". Collection is not Array, IList or ListOrArray. {field?.Name ?? "Null Field"}, {collection.GetType()}, {PatchedObjectType}");
                                }
                                if (oldInst is Element e && FieldName != nameof(SimpleBlueprint.m_AllElements)) {
                                    Patcher.CurrentlyPatching.RemoveFromElementsList(e);
                                }
                            } 
                            break;
                        case CollectionPatchOperationType.ModifyAtIndex: {
                                object orig = null;
                                object modified = null;
                                if (collection.GetType() is Type type && type.IsArray) {
                                    Array array = collection as Array; 
                                    orig = array.GetValue(CollectionIndex);
                                    modified = NestedOperation.Apply(orig);
                                    array.SetValue(modified, CollectionIndex);
                                    collection = array;
                                } else if (collection is IList list) {
                                    orig = list[CollectionIndex];
                                    modified = NestedOperation.Apply(orig);
                                    list[CollectionIndex] = modified;
                                    collection = list;
                                } else if (PatchToolUtils.IsListOrArray(collection.GetType())) {
                                    var interfaceType = collection.GetType().GetInterfaces().Where(i => i.IsGenericType).FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IList<>));
                                    if (interfaceType != null) {
                                        var index_getter = collection.GetType().GetInterfaceMethodImplementation(interfaceType.GetProperties().First().GetGetMethod());
                                        var index_setter = collection.GetType().GetInterfaceMethodImplementation(interfaceType.GetProperties().First().GetSetMethod());
                                        if (index_getter != null && index_setter != null) {
                                            orig = index_getter.Invoke(collection, [CollectionIndex]);
                                            modified = NestedOperation.Apply(orig);
                                            index_setter.Invoke(collection, [CollectionIndex, modified]);
                                        } else {
                                            throw new ArgumentException($"Error while trying to use ArrayPatchOperation. Weird error. {field?.Name ?? "Null Field"}, {collection.GetType()}, {PatchedObjectType}");
                                        }
                                    } else {
                                        throw new ArgumentException($"Error while trying to use ArrayPatchOperation. Weird error2. {field?.Name ?? "Null Field"}, {collection.GetType()}, {PatchedObjectType}");
                                    }
                                } else {
                                    throw new ArgumentException($"Error while trying to use patch \"ModifyAtIndex\". Collection is not Array, IList or ListOrArray. {field?.Name ?? "Null Field"}, {collection.GetType()}, {PatchedObjectType}");
                                }
                                if (orig is Element e && FieldName != nameof(SimpleBlueprint.m_AllElements)) {
                                    Patcher.CurrentlyPatching.RemoveFromElementsList(e);
                                }
                                if (modified is Element e2 && FieldName != nameof(SimpleBlueprint.m_AllElements)) {
                                    Patcher.CurrentlyPatching.AddToElementsList(e2);
                                }
                            }
                            break;
                        default: throw new NotImplementedException($"Unknown CollectionOperation: {CollectionOperationType}");
                    }
                    field.SetValue(instance, collection);
                } 
                break;
            case PatchOperationType.ModifyUnityReference: {
                    throw new NotImplementedException("Modifying Unity Objects is not supported.");
                }
#pragma warning disable CS0162 // Unreachable code detected
                break;
#pragma warning restore CS0162 // Unreachable code detected
            case PatchOperationType.ModifyComplex: {
                    object @object;
                    if (field == null) {
                        @object = instance;
                    } else {
                        @object = field.GetValue(instance);
                    }
                    var patched = NestedOperation.Apply(@object);
                    if (field != null) {
                        field.SetValue(instance, patched);
                    } else {
                        return patched;
                    }
                } break;
            case PatchOperationType.ModifyPrimitive: {
                    object patched;
                    if (typeof(Enum).IsAssignableFrom(NewValueType)) {
                        var tmp = Convert.ChangeType(NewValue, Enum.GetUnderlyingType(NewValueType));
                        patched = Enum.ToObject(NewValueType, tmp);
                    } else {
                        patched = Convert.ChangeType(NewValue, NewValueType);
                    }
                    if (field != null) {
                        field.SetValue(instance, patched);
                    } else {
                        return patched;
                    }
                } break;
            case PatchOperationType.ModifyBlueprintReference: {
                    var bpRef = Activator.CreateInstance(NewValueType) as BlueprintReferenceBase;
                    bpRef.ReadGuidFromJson(NewValue as string);
                    var patched = Convert.ChangeType(bpRef, NewValueType);
                    if (field != null) {
                        field.SetValue(instance, patched);
                    } else {
                        return patched;
                    }
                }
                break;
            default: throw new NotImplementedException($"Unknown PatchOperation: {OperationType}");
        }
        return instance;
    }
}
