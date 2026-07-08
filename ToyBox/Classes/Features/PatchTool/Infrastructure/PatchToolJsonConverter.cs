using Kingmaker.Blueprints;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ToyBox.Features.PatchTool.Infrastructure;

public class PatchToolJsonConverter : JsonConverter {
    public override bool CanConvert(Type objectType) {
        return typeof(PatchOperation).IsAssignableFrom(objectType);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
        var op = (PatchOperation)value!;
        writer.WriteStartObject();
        writer.WritePropertyName("Kind");
        writer.WriteValue(op.Kind);
        writer.WritePropertyName("PatchedObjectType");
        writer.WriteValue(op.PatchedObjectType.AssemblyQualifiedName);
        writer.WritePropertyName("FieldName");
        writer.WriteValue(op.FieldName);
        switch (op) {
            case ModifyPrimitiveOperation p:
                writer.WritePropertyName("NewValueType");
                writer.WriteValue(p.NewValueType.AssemblyQualifiedName);
                writer.WritePropertyName("NewValue");
                serializer.Serialize(writer, p.NewValue);
                break;
            case ModifyBlueprintReferenceOperation r:
                writer.WritePropertyName("ReferenceType");
                writer.WriteValue(r.ReferenceType.AssemblyQualifiedName);
                writer.WritePropertyName("Guid");
                writer.WriteValue(r.BlueprintGuid);
                break;
            case NullFieldOperation nf:
                writer.WritePropertyName("FieldType");
                writer.WriteValue(nf.FieldType.AssemblyQualifiedName);
                break;
            case InitializeFieldOperation i:
                writer.WritePropertyName("NewValueType");
                writer.WriteValue(i.NewValueType.AssemblyQualifiedName);
                break;
            case AddCollectionElementOperation a:
                writer.WritePropertyName("ElementType");
                writer.WriteValue(a.ElementType.AssemblyQualifiedName);
                writer.WritePropertyName("Index");
                writer.WriteValue(a.Index);
                break;
            case RemoveCollectionElementOperation rm:
                writer.WritePropertyName("Index");
                writer.WriteValue(rm.Index);
                break;
            case ModifyCollectionElementOperation m:
                writer.WritePropertyName("Index");
                writer.WriteValue(m.Index);
                writer.WritePropertyName("NestedOperation");
                serializer.Serialize(writer, m.NestedOperation);
                break;
            case ModifyComplexOperation c:
                writer.WritePropertyName("NestedOperation");
                serializer.Serialize(writer, c.NestedOperation);
                break;
            case ModifyUnityReferenceOperation u:
                writer.WritePropertyName("AssetId");
                writer.WriteValue(u.AssetId);
                writer.WritePropertyName("FileId");
                writer.WriteValue(u.FileId);
                break;
            case ModifyGameObjectComponentOperation gc:
                writer.WritePropertyName("ComponentType");
                writer.WriteValue(gc.ComponentType.AssemblyQualifiedName);
                writer.WritePropertyName("Index");
                writer.WriteValue(gc.Index);
                writer.WritePropertyName("NestedOperation");
                serializer.Serialize(writer, gc.NestedOperation);
                break;
            case ModifyGameObjectChildOperation gch:
                writer.WritePropertyName("Index");
                writer.WriteValue(gch.Index);
                writer.WritePropertyName("ChildName");
                writer.WriteValue(gch.ChildName);
                writer.WritePropertyName("NestedOperation");
                serializer.Serialize(writer, gch.NestedOperation);
                break;
            default:
                throw new JsonSerializationException($"Cannot serialize unknown PatchOperation type {op.GetType()}.");
        }
        writer.WriteEndObject();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
        var jo = JObject.Load(reader);
        if (jo["Kind"] != null) {
            return ReadCurrent(jo, serializer);
        }
        if (jo["OperationType"] != null) {
            return UpgradeLegacy(jo, serializer);
        }
        throw new JsonSerializationException("Unrecognized PatchOperation JSON (no 'Kind' or 'OperationType' property).");
    }

    #region Current format
    private static PatchOperation ReadCurrent(JObject jo, JsonSerializer serializer) {
        var kind = jo["Kind"]!.Value<string>()!.ToUpperInvariant();
        var fieldName = jo["FieldName"]!.Value<string>()!;
        var patchedType = ResolveTypeRequired(jo["PatchedObjectType"], "PatchedObjectType");
        switch (kind) {
            case "MODIFYPRIMITIVE": {
                    var nvt = ResolveTypeRequired(jo["NewValueType"], "NewValueType");
                    return new ModifyPrimitiveOperation(fieldName, patchedType, nvt, ReadValue(jo["NewValue"], nvt, serializer)!);
                }
            case "MODIFYBLUEPRINTREFERENCE":
                return new ModifyBlueprintReferenceOperation(fieldName, patchedType, ResolveTypeRequired(jo["ReferenceType"], "ReferenceType"), jo["Guid"]!.Value<string>()!);
            case "NULLFIELD":
                return new NullFieldOperation(fieldName, patchedType, ResolveTypeRequired(jo["FieldType"], "FieldType"));
            case "INITIALIZEFIELD":
                return new InitializeFieldOperation(fieldName, patchedType, ResolveTypeRequired(jo["NewValueType"], "NewValueType"));
            case "ADDCOLLECTIONELEMENT":
                return new AddCollectionElementOperation(fieldName, patchedType, ResolveTypeRequired(jo["ElementType"], "ElementType"), jo["Index"]!.Value<int>());
            case "REMOVECOLLECTIONELEMENT":
                return new RemoveCollectionElementOperation(fieldName, patchedType, jo["Index"]!.Value<int>());
            case "MODIFYCOLLECTIONELEMENT":
                return new ModifyCollectionElementOperation(fieldName, patchedType, jo["Index"]!.Value<int>()) {
                    NestedOperation = ReadNested(jo["NestedOperation"], serializer)
                };
            case "MODIFYCOMPLEX":
                return new ModifyComplexOperation(fieldName, patchedType) {
                    NestedOperation = ReadNested(jo["NestedOperation"], serializer)
                };
            case "MODIFYUNITYREFERENCE":
                return new ModifyUnityReferenceOperation(fieldName, patchedType, jo["AssetId"]!.Value<string>()!, jo["FileId"]!.Value<long>());
            case "MODIFYGAMEOBJECTCOMPONENT":
                return new ModifyGameObjectComponentOperation(fieldName, patchedType, ResolveTypeRequired(jo["ComponentType"], "ComponentType"), jo["Index"]!.Value<int>()) {
                    NestedOperation = ReadNested(jo["NestedOperation"], serializer)
                };
            case "MODIFYGAMEOBJECTCHILD":
                return new ModifyGameObjectChildOperation(fieldName, patchedType, jo["Index"]!.Value<int>(), jo["ChildName"]?.Value<string>() ?? "") {
                    NestedOperation = ReadNested(jo["NestedOperation"], serializer)
                };
            default:
                throw new JsonSerializationException($"Unknown PatchOperation Kind '{kind}'.");
        }
    }
    #endregion

    #region Legacy upgrade
    private static readonly string[] m_LegacyOpTypeNames = ["ModifyPrimitive", "ModifyUnityReference", "ModifyBlueprintReference", "ModifyComplex", "ModifyCollection", "NullField", "InitializeField"];
    private static readonly string[] m_LegacyCollectionTypeNames = ["AddAtIndex", "RemoveAtIndex", "ModifyAtIndex"];
    private static PatchOperation UpgradeLegacy(JObject jo, JsonSerializer serializer) {
        var opType = LegacyEnumName(jo["OperationType"], m_LegacyOpTypeNames);
        var fieldName = jo["FieldName"]?.Value<string>() ?? "";
        var patchedType = ResolveTypeRequired(jo["PatchedObjectType"], "PatchedObjectType");
        switch (opType) {
            case "ModifyPrimitive": {
                    var nvt = ResolveTypeRequired(jo["NewValueType"], "NewValueType");
                    return new ModifyPrimitiveOperation(fieldName, patchedType, nvt, ReadValue(jo["NewValue"], nvt, serializer)!);
                }
            case "ModifyBlueprintReference":
                return new ModifyBlueprintReferenceOperation(fieldName, patchedType, ResolveTypeRequired(jo["NewValueType"], "NewValueType"), jo["NewValue"]?.Value<string>() ?? "");
            case "ModifyComplex":
                return new ModifyComplexOperation(fieldName, patchedType) {
                    NestedOperation = ReadNested(jo["NestedOperation"], serializer)
                };
            case "NullField":
                return new NullFieldOperation(fieldName, patchedType, ResolveTypeRequired(jo["NewValueType"], "NewValueType"));
            case "InitializeField":
                return new InitializeFieldOperation(fieldName, patchedType, ResolveTypeRequired(jo["NewValueType"], "NewValueType"));
            case "ModifyCollection": {
                    var collType = LegacyEnumName(jo["CollectionOperationType"], m_LegacyCollectionTypeNames);
                    var index = jo["CollectionIndex"]?.Value<int>() ?? 0;
                    return collType switch {
                        "AddAtIndex" => new AddCollectionElementOperation(fieldName, patchedType, ResolveTypeRequired(jo["NewValueType"], "NewValueType"), index),
                        "RemoveAtIndex" => new RemoveCollectionElementOperation(fieldName, patchedType, index),
                        "ModifyAtIndex" => new ModifyCollectionElementOperation(fieldName, patchedType, index) {
                            NestedOperation = ReadNested(jo["NestedOperation"], serializer)
                        },
                        _ => throw new JsonSerializationException($"Unknown legacy CollectionOperationType '{collType}'."),
                    };
                }
            case "ModifyUnityReference":
                throw new JsonSerializationException("Legacy 'ModifyUnityReference' operations were never supported and cannot be upgraded.");
            default:
                throw new JsonSerializationException($"Unknown legacy OperationType '{opType}'.");
        }
    }
    private static string LegacyEnumName(JToken? token, string[] names) {
        if (token == null) {
            return "";
        }
        if (token.Type == JTokenType.Integer) {
            var i = token.Value<int>();
            return i >= 0 && i < names.Length ? names[i] : "";
        }
        return token.Value<string>() ?? "";
    }
    #endregion

    #region Shared
    private static PatchOperation? ReadNested(JToken? token, JsonSerializer serializer) {
        if (token == null || token.Type == JTokenType.Null) {
            return null;
        }
        return token.ToObject<PatchOperation>(serializer);
    }
    private static Type ResolveTypeRequired(JToken? token, string propertyName) {
        var name = (token?.Value<string>()) ?? throw new JsonSerializationException($"PatchOperation is missing required type property '{propertyName}'.");
        return Type.GetType(name) ?? throw new JsonSerializationException($"Could not resolve type '{name}' for PatchOperation property '{propertyName}' (assembly/type may have been renamed by a game update).");
    }
    private static object? ReadValue(JToken? token, Type targetType, JsonSerializer serializer) {
        if (token == null || token.Type == JTokenType.Null) {
            return null;
        }
        if (typeof(BlueprintReferenceBase).IsAssignableFrom(targetType)) {
            return token.ToObject<string>(serializer);
        }
        if (typeof(Enum).IsAssignableFrom(targetType)) {
            return Enum.Parse(targetType, token.ToObject<string>(serializer));
        }
        return token.ToObject(targetType, serializer);
    }
    #endregion

    public override bool CanWrite {
        get {
            return true;
        }
    }
    public override bool CanRead {
        get {
            return true;
        }
    }
}
