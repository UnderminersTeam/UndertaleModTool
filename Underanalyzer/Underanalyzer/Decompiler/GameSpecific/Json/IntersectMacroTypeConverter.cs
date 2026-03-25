/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using System.Text.Json;

namespace Underanalyzer.Decompiler.GameSpecific.Json;

internal class IntersectMacroTypeConverter
{
    public static IntersectMacroType ReadContents(ref Utf8JsonReader reader, IMacroTypeConverter macroTypeConverter, JsonSerializerOptions options)
    {
        reader.Read();
        if (reader.TokenType != JsonTokenType.PropertyName)
        {
            throw new JsonException();
        }
        if (reader.GetString() != "Macros")
        {
            throw new JsonException();
        }

        reader.Read();
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }

        List<IMacroType> types = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                reader.Read();
                if (reader.TokenType != JsonTokenType.EndObject)
                {
                    throw new JsonException();
                }

                return new IntersectMacroType(types);
            }

            types.Add(macroTypeConverter.Read(ref reader, typeof(IMacroType), options) ?? throw new JsonException());
        }

        throw new JsonException();
    }
}