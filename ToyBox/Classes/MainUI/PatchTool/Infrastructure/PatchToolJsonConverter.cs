using Kingmaker.Blueprints;
using ModKit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.PatchTool; 
public class PatchToolJsonConverter : JsonConverter {
    public override bool CanConvert(Type objectType) {
        return objectType == typeof(PatchOperation);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
        var jsonObject = Newtonsoft.Json.Linq.JObject.Load(reader);
        var operation = jsonObject.ToObject<PatchOperation>();

        var typeString = (string)jsonObject["NewValueType"];
        if (!string.IsNullOrEmpty(typeString)) {
            var targetType = Type.GetType(typeString);
            if (targetType != null && !((string)jsonObject["NewValue"]).IsNullOrEmpty()) {
                if (typeof(BlueprintReferenceBase).IsAssignableFrom(targetType)) {
                    operation.NewValue = jsonObject["NewValue"].ToObject<string>(serializer);
                } else if (typeof(Enum).IsAssignableFrom(targetType)) {
                    operation.NewValue = Enum.Parse(targetType, jsonObject["NewValue"].ToObject<string>(serializer));
                } else {
                    operation.NewValue = jsonObject["NewValue"].ToObject(targetType, serializer);
                }
            }
        }
        if (jsonObject["NestedOperation"] != null && jsonObject["NestedOperation"].Type != Newtonsoft.Json.Linq.JTokenType.Null) {
            operation.NestedOperation = jsonObject["NestedOperation"].ToObject<PatchOperation>(serializer);
        }


        return operation;
    }
    public override bool CanWrite {
        get { return false; }
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
        throw new NotImplementedException("Unnecessary because CanWrite is false. The type will skip the converter.");
    }
}
