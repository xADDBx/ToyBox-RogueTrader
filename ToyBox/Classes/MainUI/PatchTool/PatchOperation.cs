using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.Converters;
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
        if (!(OperationType == PatchOperationType.ModifyCollection) && !PatchedObjectType.IsAssignableFrom(instance.GetType())) throw new ArgumentException($"Type to patch {PatchedObjectType} is not assignable from instance type {instance.GetType()}");
        bool IsPatchingCollectionDirectly = PatchToolUtils.IsListOrArray(instance?.GetType());

        var field = IsPatchingCollectionDirectly ? null : GetFieldInfo(PatchedObjectType);

        switch (OperationType) {
            case PatchOperationType.ModifyCollection: {
                    object collection;
                    if (IsPatchingCollectionDirectly) {
                        collection = instance;
                    } else {
                        collection = field.GetValue(instance);
                    }
                    switch (CollectionOperationType) {
                        case CollectionPatchOperationType.AddAtIndex: {
                                if (collection.GetType() is Type type && type.IsArray) {
                                    Array array = collection as Array;
                                    if (CollectionIndex == -1) CollectionIndex = array.Length;
                                    var elementType = type.GetElementType();
                                    Array newArray = Array.CreateInstance(elementType, array.Length + 1);
                                    Array.Copy(array, 0, newArray, 0, CollectionIndex);
                                    newArray.SetValue(Activator.CreateInstance(NewValueType), CollectionIndex);
                                    Array.Copy(array, CollectionIndex, newArray, CollectionIndex + 1, array.Length - CollectionIndex);
                                    collection = newArray;
                                } else if (collection is IList list) {
                                    if (CollectionIndex == -1) CollectionIndex = list.Count;
                                    list.Insert(CollectionIndex, Activator.CreateInstance(NewValueType));
                                    collection = list;
                                }
                            } 
                            break;
                        case CollectionPatchOperationType.RemoveAtIndex: {
                                if (collection.GetType() is Type type && type.IsArray) {
                                    Array array = collection as Array;
                                    var elementType = type.GetElementType();
                                    var tmpList = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType)) as IList;
                                    foreach (var item in array)
                                        tmpList.Add(item);
                                    tmpList.RemoveAt(CollectionIndex);
                                    Array resizedArray = Array.CreateInstance(elementType, tmpList.Count);
                                    tmpList.CopyTo(resizedArray, 0);
                                    collection = resizedArray;
                                } else if (collection is IList list) {
                                    list.RemoveAt(CollectionIndex);
                                    collection = list;
                                }
                            } 
                            break;
                        case CollectionPatchOperationType.ModifyAtIndex: {
                                if (collection.GetType() is Type type && type.IsArray) {
                                    Array array = collection as Array;
                                    var orig = array.GetValue(CollectionIndex);
                                    var modified = NestedOperation.Apply(orig);
                                    array.SetValue(modified, CollectionIndex);
                                    collection = array;
                                } else if (collection is IList list) {
                                    var orig = list[CollectionIndex];
                                    var modified = NestedOperation.Apply(orig);
                                    list[CollectionIndex] = modified;
                                    collection = list;
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
                break;
            case PatchOperationType.ModifyComplex: {
                    var @object = field.GetValue(instance);
                    NestedOperation.Apply(@object);
                    field.SetValue(instance, @object);
                }
                break;
            case PatchOperationType.ModifyPrimitive: {
                    field.SetValue(instance, Convert.ChangeType(NewValue, NewValueType));
                } 
                break;
            case PatchOperationType.ModifyBlueprintReference: {
                    throw new NotImplementedException("Blueprint References not yet implemented");
                }
                break;
            default: throw new NotImplementedException($"Unknown PatchOperation: {OperationType}");
        }
        return instance;
    }
}
