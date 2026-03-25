/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Underanalyzer.Decompiler.GameSpecific.Json;

internal class EnumMacroTypeConverter : JsonConverter<EnumMacroType>
{
    public override EnumMacroType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        return ReadContents(ref reader);
    }

    public static EnumMacroType ReadContents(ref Utf8JsonReader reader)
    {
        string? name = null;
        Dictionary<long, string>? values = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (name is null || values is null)
                {
                    throw new JsonException();
                }
                return new EnumMacroType(name, values);
            }

            // Read property name
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            string propertyName = reader.GetString() ?? throw new JsonException();

            // Read either name or values
            switch (propertyName)
            {
                case "Name":
                    reader.Read();
                    name = reader.GetString();
                    break;
                case "Values":
                    reader.Read();
                    values = ReadValues(ref reader);
                    break;
                default:
                    throw new JsonException($"Unknown property name {propertyName}");
            }
        }

        throw new JsonException();
    }

    private static Dictionary<long, string> ReadValues(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        Dictionary<long, string> values = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return values;
            }

            // Read property name
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            string propertyName = reader.GetString() ?? throw new JsonException();

            // Read value
            reader.Read();
            values[reader.GetInt64()] = propertyName;
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, EnumMacroType value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}