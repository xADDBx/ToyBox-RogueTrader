using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class FancyNameConverter : JsonConverter {
    public override bool CanConvert(Type objectType) {
        return objectType == typeof((string, string));
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
        var jo = JObject.Load(reader);

        var orig = jo["Original"]?.ToString();
        var trans = jo["Translated"]?.ToString();
        if (string.IsNullOrEmpty(trans)) trans = orig;
        return (orig, trans);
    }


    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }
        var value2 = ((string, string))value;
        writer.WriteStartObject();
        writer.WritePropertyName("Original");
        writer.WriteValue(value2.Item1);
        writer.WritePropertyName("Translated");
        var toWrite = (value2.Item1 == value2.Item2 || value2.Item2 == "") ? null! : value2.Item2;
        writer.WriteValue(toWrite);
        writer.WriteEndObject();
    }
}
