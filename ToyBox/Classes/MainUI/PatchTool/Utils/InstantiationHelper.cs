using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ToyBox.PatchTool;
public static partial class PatchToolUtils {
    public static (HashSet<Type>, HashSet<Type>) GetInstantiableTypes(Type elementType, object maybeParent, bool skipRecursion = false) {
        if (!skipRecursion) {
            if (elementType.IsArray) {
                return (GetInstantiableArrayTypes(elementType), null);
            }
            if (elementType.IsGenericType) {
                return (GetInstantiableGenericTypes(elementType), null);
            }
        }
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
    public static HashSet<Type> GetInstantiableArrayTypes(Type elementType) {
        if (!elementType.IsArray) return new HashSet<Type>();

        Type arrayElementType = elementType.GetElementType();
        if (arrayElementType == null) return new HashSet<Type>();

        HashSet<Type> elementInstantiableTypes = GetInstantiableTypes(arrayElementType, null).Item1;
        HashSet<Type> arrayTypes = new();
        foreach (var type in elementInstantiableTypes) {
            arrayTypes.Add(type.MakeArrayType());
        }

        return arrayTypes;
    }
    public static HashSet<Type> GetInstantiableGenericTypes(Type elementType) {
        if (!elementType.IsGenericType) return new HashSet<Type>();

        HashSet<Type> genericTypes = new();

        var genericArguments = elementType.GetGenericArguments();
        List<HashSet<Type>> possibleArguments = new();

        foreach (var arg in genericArguments) {
            possibleArguments.Add(GetValidTypesForGenericParameter(arg));
        }

        Type typeToModify = elementType.IsGenericTypeDefinition ? elementType : elementType.GetGenericTypeDefinition();
        foreach (var combination in GenerateCombinations(possibleArguments)) {
            try {
                var concreteType = typeToModify.MakeGenericType(combination.ToArray());
                if (concreteType != null) {
                    genericTypes.Add(concreteType);
                }
            } catch {
            }
        }

        return genericTypes;
    }
    private static HashSet<Type> GetValidTypesForGenericParameter(Type genericParameter) {
        HashSet<Type> validTypes = new();

        if (genericParameter.IsGenericParameter) {
            var constraints = genericParameter.GetGenericParameterConstraints();

            foreach (var type in GetInstantiableTypes(typeof(object), null, true).Item1) {

                bool satisfiesConstraints = true;
                foreach (var constraint in constraints) {
                    if (!constraint.IsAssignableFrom(type)) {
                        satisfiesConstraints = false;
                        break;
                    }
                }

                if (satisfiesConstraints) validTypes.Add(type);
            }

            return validTypes;
        } else {
            return GetInstantiableTypes(genericParameter, null).Item1;
        }
    }
    private static IEnumerable<List<Type>> GenerateCombinations(List<HashSet<Type>> lists) {
        if (lists.Count == 0) {
            yield return new List<Type>();
        } else {
            var first = lists[0];
            var rest = lists.Skip(1).ToList();

            foreach (var type in first) {
                foreach (var combination in GenerateCombinations(rest)) {
                    yield return [type, .. combination];
                }
            }
        }
    }
}