// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace xcsync.Projects.Xcode;

class XcodeProjectObjectsJsonConverter : JsonConverter<IDictionary<string, XcodeObject>> {
	public override IDictionary<string, XcodeObject> Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var objects = new Dictionary<string, XcodeObject> ();

		while (reader.Read ()) {
			switch (reader.TokenType) {
			case JsonTokenType.EndObject:
				return objects;
			case JsonTokenType.PropertyName:
				break;
			default:
				throw new JsonException ($"Unexpected token type '${reader.TokenType}', expected 'PropertyName'.");
			}

			string? token = reader.GetString ()
							?? throw new JsonException ("Expected identifier for an element in the dictionary.");
			var valueConverter = (JsonConverter<XcodeObject>) options.GetConverter (typeof (XcodeObject));
			if (valueConverter is XcodeObjectConverter xcodeObjectConverter) {

				xcodeObjectConverter.Token = token;
				XcodeObject value = xcodeObjectConverter.Read (ref reader, typeof (XcodeObject), options)
									?? throw new JsonException ("Deserializing json object failed.");

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
