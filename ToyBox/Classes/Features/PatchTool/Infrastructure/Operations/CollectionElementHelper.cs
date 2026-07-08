using System.Collections;
using ToyBox.Features.PatchTool.Utils;

namespace ToyBox.Features.PatchTool.Infrastructure;

// Index-based collection manipulation that handles arrays, IList and IList<> collections. Arrays
// are immutable in length, so Insert/Remove return the (possibly new) collection.
internal static class CollectionElementHelper {
    private static Type GetGenericListInterface(Type collectionType) {
        return collectionType.GetInterfaces().Where(i => i.IsGenericType).FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IList<>))
            ?? throw new ArgumentException($"Collection {collectionType} is not an Array, IList or IList<>.");
    }
    // index == -1 appends (arrays and IList only, matching the original behaviour).
    public static object Insert(object collection, int index, object element) {
        var type = collection.GetType();
        if (type.IsArray) {
            var array = (Array)collection;
            if (index == -1) {
                index = array.Length;
            }
            var elementType = type.GetElementType()!;
            var newArray = Array.CreateInstance(elementType, array.Length + 1);
            Array.Copy(array, 0, newArray, 0, index);
            newArray.SetValue(element, index);
            Array.Copy(array, index, newArray, index + 1, array.Length - index);
            return newArray;
        }
        if (collection is IList list) {
            if (index == -1) {
                index = list.Count;
            }
            list.Insert(index, element);
            return list;
        }
        var interfaceType = GetGenericListInterface(type);
        var insert = type.GetInterfaceMethodImplementation(interfaceType.GetMethod("Insert")) ?? throw new ArgumentException($"Could not resolve Insert on {type}.");
        _ = insert.Invoke(collection, [index, element]);
        return collection;
    }
    public static (object collection, object? removed) RemoveAt(object collection, int index) {
        var type = collection.GetType();
        if (type.IsArray) {
            var array = (Array)collection;
            var elementType = type.GetElementType()!;
            var tmpList = (IList)PatchToolUtils.CreateObjectOfType(typeof(List<>).MakeGenericType(elementType));
            foreach (var item in array) {
                _ = tmpList.Add(item);
            }
            var removed = tmpList[index];
            tmpList.RemoveAt(index);
            var resized = Array.CreateInstance(elementType, tmpList.Count);
            tmpList.CopyTo(resized, 0);
            return (resized, removed);
        }
        if (collection is IList list) {
            var removed = list[index];
            list.RemoveAt(index);
            return (list, removed);
        }
        var interfaceType = GetGenericListInterface(type);
        var getter = type.GetInterfaceMethodImplementation(interfaceType.GetProperties().First().GetGetMethod()) ?? throw new ArgumentException($"Could not resolve indexer getter on {type}.");
        var remove = type.GetInterfaceMethodImplementation(interfaceType.GetMethod("RemoveAt")) ?? throw new ArgumentException($"Could not resolve RemoveAt on {type}.");
        var removedElement = getter.Invoke(collection, [index]);
        _ = remove.Invoke(collection, [index]);
        return (collection, removedElement);
    }
    public static int Count(object collection) {
        if (collection is Array array) {
            return array.Length;
        }
        if (collection is IList list) {
            return list.Count;
        }
        return (collection as IEnumerable)?.Cast<object>().Count() ?? 0;
    }
    public static object? GetAt(object collection, int index) {
        var type = collection.GetType();
        if (type.IsArray) {
            return ((Array)collection).GetValue(index);
        }
        if (collection is IList list) {
            return list[index];
        }
        var interfaceType = GetGenericListInterface(type);
        var getter = type.GetInterfaceMethodImplementation(interfaceType.GetProperties().First().GetGetMethod()) ?? throw new ArgumentException($"Could not resolve indexer getter on {type}.");
        return getter.Invoke(collection, [index]);
    }
    public static object SetAt(object collection, int index, object? value) {
        var type = collection.GetType();
        if (type.IsArray) {
            ((Array)collection).SetValue(value, index);
            return collection;
        }
        if (collection is IList list) {
            list[index] = value;
            return collection;
        }
        var interfaceType = GetGenericListInterface(type);
        var setter = type.GetInterfaceMethodImplementation(interfaceType.GetProperties().First().GetSetMethod()) ?? throw new ArgumentException($"Could not resolve indexer setter on {type}.");
        _ = setter.Invoke(collection, [index, value]);
        return collection;
    }
}
