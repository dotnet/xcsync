// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace xcsync.Projects.Xcode;

public class BuildSettingsConverter : JsonConverter<IDictionary<string, IList<string>>>
{
    public override IDictionary<string, IList<string>> Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var buildSettings = new Dictionary<string, IList<string>>();

        while (reader.Read ()) {
            
            if (reader.TokenType == JsonTokenType.EndObject) {
                return buildSettings;
            }

            // Get the key.
            if (reader.TokenType != JsonTokenType.PropertyName) {
                throw new JsonException();
            }

            var value = new List<string>();
            string key = reader.GetString();

            reader.Read();
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }

                    value.Add(reader.GetString());
                }
            }
            else
            {
                value.Add(reader.GetString());
            }

            // Add to dictionary.
            buildSettings.Add(key, value);
        }        
        
        return buildSettings;
    }

    public override void Write(
        Utf8JsonWriter writer,
        IDictionary<string, IList<string>> objectToWrite,
        JsonSerializerOptions options) {
            
            writer.WriteStartObject();

            foreach (var (key, value) in objectToWrite)
            {
                if (value.Count == 1)
                {
                    writer.WriteString(key, value[0]);
                }
                else
                {
                    writer.WriteStartArray(key);
                    foreach (var item in value)
                    {
                        writer.WriteStringValue(item);
                    }
                    writer.WriteEndArray();
                }
            }
            writer.WriteEndObject();        
        //  JsonSerializer.Serialize(writer, objectToWrite, objectToWrite.GetType(), options);
        }
}