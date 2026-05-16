using System;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Buffers;

namespace UndertaleModLib.Project.Json;

/// <summary>
/// Converter to specifically not pretty-print a property.
/// </summary>
internal class NoPrettyPrintJsonConverter<T> : JsonConverter<T>
{
    private static readonly JsonSerializerOptions _serializeOptions = new(ProjectContext.JsonOptions)
    {
        WriteIndented = false
    };

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<T>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        ArrayBufferWriter<byte> bufferWriter = new();
        using (Utf8JsonWriter innerWriter = new(bufferWriter))
        {
            JsonSerializer.Serialize(innerWriter, value, _serializeOptions);
        }
        writer.WriteRawValue(bufferWriter.WrittenSpan, true);
    }
}