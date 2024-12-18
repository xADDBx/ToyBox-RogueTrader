using System.Collections.Generic;
using System.Reflection;
using System.ArrayExtensions;
using System.Runtime.CompilerServices;

namespace System {
    public static class ObjectExtensions {
        private static readonly MethodInfo CloneMethod = typeof(Object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool IsPrimitive(this Type type) {
            if (type == typeof(String)) return true;
            return (type.IsValueType & type.IsPrimitive);
        }

        public static Object DeepCopy(Object originalObject, Object targetObject = null) {
            return InternalCopy(originalObject, new Dictionary<Object, Object>(new ReferenceEqualityComparer()), targetObject);
        }
        private static Object InternalCopy(Object originalObject, IDictionary<Object, Object> visited, Object targetObject = null) {
            if (originalObject == null) return null;
            var typeToReflect = originalObject.GetType();
            if (IsPrimitive(typeToReflect)) return originalObject;
            if (visited.ContainsKey(originalObject)) return visited[originalObject];
            if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;
            var cloneObject = targetObject ?? CloneMethod.Invoke(originalObject, null);
            visited.Add(originalObject, cloneObject);
            if (typeToReflect.IsArray) {
                var arrayType = typeToReflect.GetElementType();
                if (IsPrimitive(arrayType) == false) {
                    Array clonedArray = (Array)cloneObject;
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

        private static void CopyFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy, Func<FieldInfo, bool> filter = null) {
            foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags)) {
                if (filter != null && filter(fieldInfo) == false) continue;
                if (IsPrimitive(fieldInfo.FieldType)) {
                    fieldInfo.SetValue(cloneObject, fieldInfo.GetValue(originalObject));
                    continue;
                }
                var originalFieldValue = fieldInfo.GetValue(originalObject);
                var clonedFieldValue = InternalCopy(originalFieldValue, visited);
                fieldInfo.SetValue(cloneObject, clonedFieldValue);
            }
        }
        public static T Copy<T>(this T original, T target = null) where T : class {
            return (T)DeepCopy(original, target);
        }
        public static T Copy<T>(this T original) {
            return (T)DeepCopy(original);
        }
    }

    public class ReferenceEqualityComparer : EqualityComparer<Object> {
        public override bool Equals(object x, object y) {
            return ReferenceEquals(x, y);
        }
        public override int GetHashCode(object obj) {
            if (obj == null) return 0;
            // E.g. WeakResourceLink can throw on GetHashCode()
            // return obj.GetHashCode();
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

    namespace ArrayExtensions {
        public static class ArrayExtensions {
            public static void ForEach(this Array array, Action<Array, int[]> action) {
                if (array.LongLength == 0) return;
                ArrayTraverse walker = new ArrayTraverse(array);
                do action(array, walker.Position);
                while (walker.Step());
            }
        }

        internal class ArrayTraverse {
            public int[] Position;
            private int[] maxLengths;

            public ArrayTraverse(Array array) {
                maxLengths = new int[array.Rank];
                for (int i = 0; i < array.Rank; ++i) {
                    maxLengths[i] = array.GetLength(i) - 1;
                }
                Position = new int[array.Rank];
            }

            public bool Step() {
                for (int i = 0; i < Position.Length; ++i) {
                    if (Position[i] < maxLengths[i]) {
                        Position[i]++;
                        for (int j = 0; j < i; j++) {
                            Position[j] = 0;
                        }
                        return true;
                    }
                }
                return false;
            }
        }
    }

}