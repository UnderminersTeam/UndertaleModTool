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

internal class IMacroTypeConverter(GameSpecificRegistry registry) : JsonConverter<IMacroType>
{
    public GameSpecificRegistry Registry { get; } = registry;

    public override IMacroType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            // Valid token type is just nothing
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            // Read type name - access registry (or None to use none)
            string typeName = reader.GetString() ?? throw new JsonException();
            if (typeName == "None")
            {
                return NoneMacroType.ReusableInstance;
            }
            return Registry.FindType(typeName);
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            // Read array of macro types as function arguments macro type
            List<IMacroType?> subMacroTypes = [];
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return new FunctionArgsMacroType(subMacroTypes);
                }

                subMacroTypes.Add(Read(ref reader, typeToConvert, options));
            }

            throw new JsonException();
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            // Read macro type! Ensure we start with type discriminator
            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            string? propertyName = reader.GetString();
            if (propertyName is not "MacroType")
            {
                throw new JsonException();
            }

            // Read data for relevant type
            reader.Read();
            switch (reader.GetString())
            {
                case "Enum":
                    return EnumMacroTypeConverter.ReadContents(ref reader);
                case "Constants":
                    return ConstantsMacroTypeConverter.ReadContents(ref reader);
                case "Union":
                    return UnionMacroTypeConverter.ReadContents(ref reader, this, options);
                case "Intersect":
                    return IntersectMacroTypeConverter.ReadContents(ref reader, this, options);
                case "ArrayInit":
                    return ArrayInitMacroTypeConverter.ReadContents(ref reader, this, options);
                case "Match":
                    return MatchMacroTypeConverter.ReadContents(ref reader, this, options);
                case "MatchNot":
                    return MatchNotMacroTypeConverter.ReadContents(ref reader, this, options);
                case "None":
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.EndObject)
                    {
                        throw new JsonException();
                    }
                    return NoneMacroType.ReusableInstance;
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, IMacroType value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
