/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Text.Json;

namespace Underanalyzer.Decompiler.GameSpecific.Json;

internal class ArrayInitMacroTypeConverter
{
    public static ArrayInitMacroType ReadContents(ref Utf8JsonReader reader, IMacroTypeConverter macroTypeConverter, JsonSerializerOptions options)
    {
        reader.Read();
        if (reader.TokenType != JsonTokenType.PropertyName)
        {
            throw new JsonException();
        }
        if (reader.GetString() != "Macro")
        {
            throw new JsonException();
        }

        reader.Read();
        ArrayInitMacroType res = new(macroTypeConverter.Read(ref reader, typeof(IMacroType), options) ?? throw new JsonException());

        reader.Read();
        if (reader.TokenType != JsonTokenType.EndObject)
        {
            throw new JsonException();
        }

        return res;
    }
}