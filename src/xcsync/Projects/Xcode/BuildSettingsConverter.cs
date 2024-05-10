// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace xcsync.Projects.Xcode;

class BuildSettingsConverter : JsonConverter<IDictionary<string, IList<string>>> {
	public override IDictionary<string, IList<string>> Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var buildSettings = new Dictionary<string, IList<string>> ();

		while (reader.Read ()) {
			switch (reader.TokenType) {
			case JsonTokenType.EndObject:
				return buildSettings;
			case JsonTokenType.PropertyName:
				break;
			default:
				throw new JsonException ($"Unexpected token type '${reader.TokenType}', expected 'PropertyName'.");
			}

			var values = new List<string> ();
			string? key = reader.GetString ()
						  ?? throw new JsonException ("Expected identifier for an element in the dictionary.");

			reader.Read ();
			if (reader.TokenType == JsonTokenType.StartArray) {
				while (reader.Read ()) {
					if (reader.TokenType == JsonTokenType.EndArray) {
						break;
					}
					var value = reader.GetString ()
								?? throw new JsonException ("Deserializing json object failed.");
					values.Add (value);
				}
			} else {
				var value = reader.GetString ()
							?? throw new JsonException ("Deserializing json object failed.");
				values.Add (value);
			}

			// Add to dictionary.
			buildSettings.Add (key, values);
		}

		return buildSettings;
	}

	public override void Write (
		Utf8JsonWriter writer,
		IDictionary<string, IList<string>> objectToWrite,
		JsonSerializerOptions options)
	{

		writer.WriteStartObject ();

		foreach (var (key, value) in objectToWrite) {
			if (value.Count == 1) {
				writer.WriteString (key, value [0]);
			} else {
				writer.WriteStartArray (key);
				foreach (var item in value) {
					writer.WriteStringValue (item);
				}
				writer.WriteEndArray ();
			}
		}
		writer.WriteEndObject ();
	}
}
