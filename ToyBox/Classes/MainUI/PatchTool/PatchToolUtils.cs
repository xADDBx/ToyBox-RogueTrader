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
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.PatchTool; 
public static class PatchToolUtils {
    [HarmonyPatch]
    public static class PatchToolPatches {
        private static bool Initialized = false;
        [HarmonyPriority(Priority.LowerThanNormal)]
        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init)), HarmonyPostfix]
        public static void Init_Postfix() {
            try {
                if (Initialized) {
                    Mod.Log("Already initialized blueprints cache.");
                    return;
                }
                Initialized = true;

                Mod.Log("Patching blueprints.");
                Patcher.PatchAll();
            } catch (Exception e) {
                Mod.Log(string.Concat("Failed to initialize.", e));
            }
        }
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
    public static PatchOperation AddOperation(this PatchOperation head, PatchOperation leaf) {
        if (head == null) {
            return leaf;
        } else {
            var copy = head.DeepCopy() as PatchOperation;
            PatchOperation cur = copy;
            while (cur.NestedOperation != null) {
                cur = cur.NestedOperation;
            }
            cur.NestedOperation = leaf;
            return copy;
        }
    }
    public static (HashSet<Type>, HashSet<Type>) GetInstantiableTypes(Type elementType, object maybeParent) {
        HashSet<Type> allowedinstantiableTypes = typeof(BlueprintComponent).IsAssignableFrom(elementType) ? new() : null;
        HashSet<Type> allinstantiableTypes = new();
        Type parentType = maybeParent?.GetType();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            Type[] types;
            try {
                types = assembly.GetTypes();
            } catch (ReflectionTypeLoadException ex) {
                types = ex.Types.Where(t => t != null).ToArray();
            }

            foreach (var type in types) {
                if (type == null) continue;

                if (elementType.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface) {
                    if (parentType != null && allowedinstantiableTypes != null) {
                        var attributes = type.GetCustomAttributes(typeof(AllowedOnAttribute), inherit: false);
                        if (attributes.Length > 0) {
                            foreach (AllowedOnAttribute attr in attributes) {
                                if (attr.Type.IsAssignableFrom(parentType)) {
                                    allowedinstantiableTypes.Add(type);
                                }
                            }
                        }
                    }
                    allinstantiableTypes.Add(type);
                }
            }
        }

        return (allinstantiableTypes, allowedinstantiableTypes);
    }

}
