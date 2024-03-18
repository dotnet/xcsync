#nullable disable

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace xcsync.Projects.Xcode;

public class XcodeObjectConverter : JsonConverter<XcodeObject> {

    public string Token { get; set; }

    public override XcodeObject Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue (ref reader);
        var jsonObject = jsonDoc.RootElement.Clone ();
        var isa = jsonObject.GetProperty ("isa").GetString ();
        var xcodeObjectType = System.Reflection.Assembly.GetExecutingAssembly ().GetType ($"xcsync.Projects.Xcode.{isa}")
                              ?? throw new JsonException ($"Unknown isa value: {isa}");
        var jti = JsonTypeInfo.CreateJsonTypeInfo (xcodeObjectType, JsonSerializerOptions.Default);
        var jsonText = jsonObject.GetRawText ();
        var xcodeObject = JsonSerializer.Deserialize (jsonObject, xcodeObjectType) as XcodeObject 
                          ?? throw new JsonException ($"Failed to deserialize {isa}");
        xcodeObject.Token = Token;
        return xcodeObject;
    }

    public override void Write (Utf8JsonWriter writer, XcodeObject value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize (writer, value, value.GetType (), options);
    }
}