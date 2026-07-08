using Kingmaker.Blueprints;
using System.Reflection;

namespace ToyBox.Features.PatchTool.Utils;

public static class PatchToolExtensions {
    private static readonly MethodInfo m_CloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly Dictionary<(Type, BindingFlags), FieldInfo[]> m_FieldsByTypeCache = [];

    public static bool IsPrimitive(this Type type) {
        if (type == typeof(string)) {
            return true;
        }

        return type.IsValueType & type.IsPrimitive;
    }

    public static object DeepCopy(object originalObject, object? targetObject = null, bool cloneTopBlueprint = false) {
        return InternalCopy(originalObject, new Dictionary<object, object>(new ReferenceEqualityComparer()), targetObject, cloneTopBlueprint)!;
    }
    private static object? InternalCopy(object? originalObject, IDictionary<object, object> visited, object? targetObject = null, bool cloneTopBlueprint = false) {
        if (originalObject == null) {
            return null;
        }

        var typeToReflect = originalObject.GetType();
        if (IsPrimitive(typeToReflect)) {
            return originalObject;
        }

        if (visited.TryGetValue(originalObject, out var value)) {
            return value;
        }

        // Not copying this would result in weird side effects, like the m_Factory of a StaticCache being lost.
        // if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;
        if (typeof(Delegate).IsAssignableFrom(typeToReflect)) {
            return originalObject;
        }

        // Prevent messing up references by copying the cached instance of the blueprints.
        if (!cloneTopBlueprint && typeof(SimpleBlueprint).IsAssignableFrom(typeToReflect)) {
            return originalObject;
        }

        if (PatchToolUtils.TypeOrBaseIsDirectlyInUnityDLL(typeToReflect)) {
            return originalObject;
        }

        var cloneObject = targetObject ?? m_CloneMethod.Invoke(originalObject, null);
        visited.Add(originalObject, cloneObject);
        if (typeToReflect.IsArray) {
            var arrayType = typeToReflect.GetElementType();
            if (!IsPrimitive(arrayType)) {
                var clonedArray = (Array)cloneObject;
                clonedArray.ForEach((array, indices) => array.SetValue(InternalCopy(clonedArray.GetValue(indices), visited), indices));
            }
        }
        CopyFields(originalObject, visited, cloneObject, typeToReflect);
        RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
        return cloneObject;
    }

    private static void RecursiveCopyBaseTypePrivateFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect) {
        if (typeToReflect.BaseType != null) {
            RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
            CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType, BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
        }
    }

    private static void CopyFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy, Func<FieldInfo, bool>? filter = null) {
        if (!m_FieldsByTypeCache.TryGetValue((typeToReflect, bindingFlags), out var fields)) {
            fields = m_FieldsByTypeCache[(typeToReflect, bindingFlags)] = typeToReflect.GetFields(bindingFlags);
        }
        foreach (var fieldInfo in fields) {
            if (filter != null && filter(fieldInfo) == false) {
                continue;
            }

            if (IsPrimitive(fieldInfo.FieldType)) {
                fieldInfo.SetValue(cloneObject, fieldInfo.GetValue(originalObject));
                continue;
            }
            var originalFieldValue = fieldInfo.GetValue(originalObject);
            var clonedFieldValue = InternalCopy(originalFieldValue, visited);
            fieldInfo.SetValue(cloneObject, clonedFieldValue);
        }
    }
    public static T Copy<T>(this T original, T? target = null, bool cloneTopBlueprint = false) where T : class {
        return (T)DeepCopy(original, target, cloneTopBlueprint);
    }
    public static T Copy<T>(this T original) {
        return (T)DeepCopy(original!);
    }
    private class ArrayTraverse {
        public int[] Position;
        private readonly int[] m_MaxLengths;

        public ArrayTraverse(Array array) {
            m_MaxLengths = new int[array.Rank];
            for (var i = 0; i < array.Rank; ++i) {
                m_MaxLengths[i] = array.GetLength(i) - 1;
            }
            Position = new int[array.Rank];
        }

        public bool Step() {
            for (var i = 0; i < Position.Length; ++i) {
                if (Position[i] < m_MaxLengths[i]) {
                    Position[i]++;
                    for (var j = 0; j < i; j++) {
                        Position[j] = 0;
                    }
                    return true;
                }
            }
            return false;
        }
    }
    public static void ForEach(this Array array, Action<Array, int[]> action) {
        if (array.LongLength == 0) {
            return;
        }

        var walker = new ArrayTraverse(array);
        do {
            action(array, walker.Position);
        } while (walker.Step());
    }
}
