using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.Blueprints.JsonSystem;
using ModKit;
using ModKit.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.PatchTool; 
public static partial class PatchToolUtils {
    public static MethodInfo? GetInterfaceMethodImplementation(this Type declaringType, MethodInfo interfaceMethod) {
        var map = declaringType.GetInterfaceMap(interfaceMethod.DeclaringType);
        return map.InterfaceMethods
            ?.Zip(map.TargetMethods, (i, t) => (i, t))
            .FirstOrDefault(pair => pair.i == interfaceMethod)
            .t;
    }
    public static bool IsListOrArray(Type t) {
        return t.IsArray || typeof(IList<>).IsAssignableFrom(t) || t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
    }
    private static Dictionary<Type, List<FieldInfo>> _fieldsCache = new();
    public static List<FieldInfo> GetFields(Type t) {
        List<FieldInfo> fields;
        if (!_fieldsCache.TryGetValue(t, out fields)) {
            fields = new();
            HashSet<string> tmp = new();
            var t2 = t;
            do {
                foreach (var field in t2.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)) {
                    if (!tmp.Contains(field.Name)) {
                        tmp.Add(field.Name);
                        fields.Add(field);
                    }
                }
                t2 = t2.BaseType;
            } while (t2 != null);
            fields.Sort((a, b) => { 
                return a.Name.CompareTo(b.Name);
            });
            _fieldsCache[t] = fields;
        }
        return fields;
    }
    public static bool IsNullableStruct(Type type) => Nullable.GetUnderlyingType(type) != null;
    public static object CreateObjectOfType(Type type) {
        object result;
        try {
            result = Activator.CreateInstance(type);
        } catch (Exception ex) {
            result = FormatterServices.GetUninitializedObject(type);
            Mod.Debug($"Exception while trying to Activator.CreateInstance {type.FullName}, falling back to FormatterServices.GetUninitializedObject. Exception:\n{ex}");
        }
        return result;
    }
    public static PatchOperation AddOperation(this PatchOperation head, PatchOperation leaf) {
        if (head == null) {
            return leaf;
        } else {
            var copy = head.Copy();
            PatchOperation cur = copy;
            while (cur.NestedOperation != null) {
                cur = cur.NestedOperation;
            }
            cur.NestedOperation = leaf;
            return copy;
        }
    }
    public static Type GetBlueprintReferenceKind(Type type) {
        Type currentType = type;

        while (currentType != null && currentType != typeof(BlueprintReferenceBase)) {
            if (currentType.IsGenericType) {
                Type genericTypeDefinition = currentType.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(BlueprintReference<>)) {
                    return currentType.GetGenericArguments()[0];
                }
            }
            currentType = currentType.BaseType;
        }
        return null;
    }
}
