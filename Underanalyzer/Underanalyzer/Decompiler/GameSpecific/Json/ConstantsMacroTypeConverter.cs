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

internal class ConstantsMacroTypeConverter : JsonConverter<ConstantsMacroType>
{
    public override ConstantsMacroType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        return ReadContents(ref reader);
    }

    public static ConstantsMacroType ReadContents(ref Utf8JsonReader reader)
    {
        Dictionary<int, string> values = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new ConstantsMacroType(values);
            }

            // Read property name
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            string propertyName = reader.GetString() ?? throw new JsonException();

            // Read value
            reader.Read();
            values[reader.GetInt32()] = propertyName;
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, ConstantsMacroType value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}