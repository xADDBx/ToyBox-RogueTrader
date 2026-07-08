using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Base;
using Kingmaker.ElementsSystem;
using System.Reflection;
using System.Runtime.Serialization;
using ToyBox.Features.PatchTool.Infrastructure;
using UnityEngine;

namespace ToyBox.Features.PatchTool.Utils;

public static partial class PatchToolUtils {
    public static MethodInfo? GetInterfaceMethodImplementation(this Type declaringType, MethodInfo interfaceMethod) {
        var map = declaringType.GetInterfaceMap(interfaceMethod.DeclaringType);
        return map.InterfaceMethods?.Zip(map.TargetMethods, (i, t) => (i, t)).FirstOrDefault(pair => pair.i == interfaceMethod).t;
    }
    public static bool IsListOrArray(Type t) {
        return t.IsArray || t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
    }
    // Declared element type of a collection type (array element or IEnumerable<T> argument), or null.
    public static Type? DeclaredElementType(Type collectionType) {
        if (collectionType.IsArray) {
            return collectionType.GetElementType();
        }
        return collectionType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))?.GetGenericArguments()[0];
    }
    private static readonly Dictionary<Type, List<FieldInfo>> m_FieldsCache = [];
    public static List<FieldInfo> GetFields(Type t) {
        if (!m_FieldsCache.TryGetValue(t, out var fields)) {
            fields = [];
            HashSet<string> tmp = [];
            var t2 = t;
            do {
                foreach (var field in t2.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)) {
                    if (!tmp.Contains(field.Name)) {
                        _ = tmp.Add(field.Name);
                        fields.Add(field);
                    }
                }
                t2 = t2.BaseType;
            } while (t2 != null);
            fields.Sort((a, b) => {
                return a.Name.CompareTo(b.Name);
            });
            m_FieldsCache[t] = fields;
        }
        return fields;
    }
    public static bool IsNullableStruct(Type type) {
        return Nullable.GetUnderlyingType(type) != null;
    }

    // Resolves a UnityEngine.Object from Owlcat's asset resolution system by the same (guid, fileid)
    // pair that UnityObjectConverter serializes blueprint Unity references as. Mirrors the lookup order
    // of UnityObjectConverter.ReadJson: the main AssetList first, then any modification asset lists.
    public static UnityEngine.Object? ResolveUnityAsset(string assetId, long fileId) {
        if (string.IsNullOrEmpty(assetId)) {
            return null;
        }
        var asset = Kingmaker.Blueprints.JsonSystem.Converters.UnityObjectConverter.AssetList?.Get(assetId, fileId);
        if (asset != null) {
            return asset;
        }
        foreach (var list in Kingmaker.Blueprints.JsonSystem.Converters.UnityObjectConverter.ModificationAssetLists) {
            asset = list.Get(assetId, fileId);
            if (asset != null) {
                return asset;
            }
        }
        return null;
    }

    // Reverse of ResolveUnityAsset: finds the (guid, fileid) that identifies an already-registered
    // Unity asset, so the UI can show the user what they'd need to type to reproduce the current value.
    public static (string AssetId, long FileId)? GetUnityAssetId(UnityEngine.Object asset) {
        if (asset == null) {
            return null;
        }
        var id = Kingmaker.Blueprints.JsonSystem.Converters.UnityObjectConverter.AssetList?.GetAssetId(asset);
        if (id.HasValue) {
            return id;
        }
        foreach (var list in Kingmaker.Blueprints.JsonSystem.Converters.UnityObjectConverter.ModificationAssetLists) {
            id = list.GetAssetId(asset);
            if (id.HasValue) {
                return id;
            }
        }
        return null;
    }

    private static readonly Dictionary<SimpleBlueprint, Dictionary<Type, int>> m_ComponentNameCounter = [];
    public static object CreateObjectOfType(Type type, bool isForBlueprintPatch = true) {
        object result;
        try {
            if (TypeOrBaseIsDirectlyInUnityDLL(type)) {
                if (typeof(ScriptableObject).IsAssignableFrom(type)) {
                    result = ScriptableObject.CreateInstance(type);
                } else {
                    // Mod.Error("Trying to instantiate a non-scriptable object Unity Object. In general this means someone messed up somewhere. Make sure you really know what you're doing!");
                    // result = Activator.CreateInstance(type);
                    throw new ArgumentException("Trying to instantiate a non-scriptable object Unity Object. In general this means someone messed up somewhere.");
                }
            } else {
                result = Activator.CreateInstance(type);
            }
        } catch (Exception ex) {
            result = FormatterServices.GetUninitializedObject(type);
            Debug($"Exception while trying to Activator.CreateInstance {type.FullName}, falling back to FormatterServices.GetUninitializedObject. Exception:\n{ex}");
        }
        if (isForBlueprintPatch) {
            if (result is BlueprintComponent or Element) {
                if (!m_ComponentNameCounter.TryGetValue(Patcher.CurrentlyPatching!, out var dict)) {
                    dict = [];
                }
                if (!dict.TryGetValue(type, out var occurences)) {
                    occurences = 0;
                }
                occurences += 1;
                dict[type] = occurences;
                m_ComponentNameCounter[Patcher.CurrentlyPatching!] = dict;
                if (result is BlueprintComponent comp) {
                    comp.name = $"{Patcher.CurrentlyPatching!.AssetGuid}#{type.FullName}#{occurences}";
                } else if (result is Element elem) {
                    elem.name = $"{Patcher.CurrentlyPatching!.AssetGuid}#{type.FullName}#{occurences}";
                }
            }
        }
        return result;
    }
    public static PatchOperation AddOperation(this PatchOperation? head, PatchOperation leaf) {
        if (head == null) {
            return leaf;
        }
        var copy = head.Copy();
        var cur = copy;
        while (cur is NestingPatchOperation nesting) {
            if (nesting.NestedOperation == null) {
                nesting.NestedOperation = leaf;
                return copy;
            }
            cur = nesting.NestedOperation;
        }
        return copy;
    }
    public static Type? GetBlueprintReferenceKind(Type type) {
        var currentType = type;

        while (currentType != null && currentType != typeof(BlueprintReferenceBase) && currentType != typeof(IReferenceBase)) {
            if (currentType.IsGenericType) {
                var genericTypeDefinition = currentType.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(BlueprintReference<>)) {
                    return currentType.GetGenericArguments()[0];
                }
            }
            currentType = currentType.BaseType;
        }
        return null;
    }
    private static readonly Dictionary<Type, bool> m_TypeIsDirectlyInUnityDLL = [];
    private static readonly Dictionary<Type, bool> m_TypeIsInUnityDLL = [];
    private static readonly HashSet<Type> m_SafeExceptions = [typeof(Vector2), typeof(Vector2Int), typeof(Vector3), typeof(Vector3Int), typeof(Vector4), typeof(Color), typeof(Color32), typeof(Rect), typeof(RectInt)];
    public static bool TypeOrBaseIsDirectlyInUnityDLL(Type type) {
        if (m_TypeIsDirectlyInUnityDLL.TryGetValue(type, out var val)) {
            return val;
        }
        if (m_SafeExceptions.Contains(type)) {
            return m_TypeIsDirectlyInUnityDLL[type] = false;
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
