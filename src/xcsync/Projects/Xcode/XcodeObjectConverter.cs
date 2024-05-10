// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace xcsync.Projects.Xcode;

class XcodeObjectConverter : JsonConverter<XcodeObject> {

	public string? Token { get; set; }

	public override XcodeObject Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using var jsonDoc = JsonDocument.ParseValue (ref reader);
		var jsonObject = jsonDoc.RootElement;
		var isa = jsonObject.GetProperty ("isa").GetString ();
		var xcodeObjectType = typeof (XcodeObjectConverter).Assembly.GetType ($"xcsync.Projects.Xcode.{isa}")
							  ?? throw new JsonException ($"Unknown isa value: {isa}");
		var xcodeObject = JsonSerializer.Deserialize (jsonObject, xcodeObjectType) as XcodeObject
						  ?? throw new JsonException ($"Failed to deserialize {isa}");
		xcodeObject.Token = Token!;
		return xcodeObject;
	}

	public override void Write (Utf8JsonWriter writer, XcodeObject value, JsonSerializerOptions options)
	{
		JsonSerializer.Serialize (writer, value, value.GetType (), options);
	}
}
