// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace xcsync.Projects.Xcode;

public class XcodeProjectObjectsJsonConverter : JsonConverter<IDictionary<string, XcodeObject>> {
	public override IDictionary<string, XcodeObject> Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var objects = new Dictionary<string, XcodeObject> ();

		while (reader.Read ()) {
			if (reader.TokenType == JsonTokenType.EndObject) {
				return objects;
			}

			// Get the key.
			if (reader.TokenType != JsonTokenType.PropertyName) {
				throw new JsonException ();
			}

			string token = reader.GetString ();
			var valueConverter = (JsonConverter<XcodeObject>)options.GetConverter(typeof(XcodeObject));
			if (valueConverter is XcodeObjectConverter xcodeObjectConverter) {

				xcodeObjectConverter.Token = token;
				XcodeObject value = xcodeObjectConverter.Read(ref reader, typeof(XcodeObject), options);

				// Add to dictionary.
				objects.Add (token, value);
			}
		}
		return objects;
	}

	public override void Write (Utf8JsonWriter writer, IDictionary<string, XcodeObject> value, JsonSerializerOptions options)
	{
		JsonSerializer.Serialize (writer, value, value.GetType (), options);
	}
}