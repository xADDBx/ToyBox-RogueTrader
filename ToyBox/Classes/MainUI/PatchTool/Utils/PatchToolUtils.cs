using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
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
using UnityEngine;

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
    private static Dictionary<SimpleBlueprint, Dictionary<Type, int>> m_ComponentNameCounter = new();
    public static object CreateObjectOfType(Type type, bool isForBlueprintPatch = true) {
        object result;
        try {
            if (TypeOrBaseIsDirectlyInUnityDLL(type)) {
                if (typeof(ScriptableObject).IsAssignableFrom(type)) {
                    result = ScriptableObject.CreateInstance(type);
                } else {
                    // Mod.Error("Trying to instantiate a non-scriptable object Unity Object. In general this means someone messed up somewhere. Make sure you really know what you're doing!");
                    // result = Activator.CreateInstance(type);
                    throw new Exception("Trying to instantiate a non-scriptable object Unity Object. In general this means someone messed up somewhere.");
                }
            } else {
                result = Activator.CreateInstance(type);
            }
        } catch (Exception ex) {
            result = FormatterServices.GetUninitializedObject(type);
            Mod.Debug($"Exception while trying to Activator.CreateInstance {type.FullName}, falling back to FormatterServices.GetUninitializedObject. Exception:\n{ex}");
        }
        if (isForBlueprintPatch) {
            if (result is BlueprintComponent || result is Element) {
                if (!m_ComponentNameCounter.TryGetValue(Patcher.CurrentlyPatching, out var dict)) {
                    dict = new();
                }
                if (!dict.TryGetValue(type, out var occurences)) {
                    occurences = 0;
                }
                occurences += 1;
                dict[type] = occurences;
                m_ComponentNameCounter[Patcher.CurrentlyPatching] = dict;
                if (result is BlueprintComponent comp) {
                    comp.name = $"{Patcher.CurrentlyPatching.AssetGuid}#{type.FullName}#{occurences}";
                } else if (result is Element elem) {
                    elem.name = $"{Patcher.CurrentlyPatching.AssetGuid}#{type.FullName}#{occurences}";
                }
            }
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
    private static Dictionary<Type, bool> m_TypeIsDirectlyInUnityDLL = new();
    private static Dictionary<Type, bool> m_TypeIsInUnityDLL = new();
    public static bool TypeOrBaseIsDirectlyInUnityDLL(Type type) {
        if (m_TypeIsDirectlyInUnityDLL.TryGetValue(type, out var val)) {
            return val;
        }
        if (type.BaseType != null) {
            if (TypeOrBaseIsDirectlyInUnityDLL(type.BaseType)) {
                return m_TypeIsDirectlyInUnityDLL[type] = true;
            }
        }
        if (type.Assembly.FullName.StartsWith("Unity")) {
            return m_TypeIsDirectlyInUnityDLL[type] = true;
        }
        return m_TypeIsDirectlyInUnityDLL[type] = false;
    }
    public static bool TypeOrBaseIsInUnityDLL(Type type) {
        if (m_TypeIsInUnityDLL.TryGetValue(type, out var val)) {
            return val;
        }
        if (type.BaseType != null) {
            if (TypeOrBaseIsInUnityDLL(type.BaseType)) {
                return m_TypeIsInUnityDLL[type] = true;
            }
        }
        if (TypeOrBaseIsDirectlyInUnityDLL(type)) {
            return m_TypeIsInUnityDLL[type] = true;
        }
        if (type.IsGenericType) {
            return m_TypeIsInUnityDLL[type] = type.GenericTypeArguments.Any(TypeOrBaseIsInUnityDLL);
        }
        if (type.IsArray) {
            return m_TypeIsInUnityDLL[type] = TypeOrBaseIsInUnityDLL(type.GetElementType());
        }
        return m_TypeIsInUnityDLL[type] = false;
    }
}
