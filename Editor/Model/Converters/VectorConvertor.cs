using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using NVector2 = System.Numerics.Vector2;

namespace Editor.Model.Converters
{
    public class Vector2Convertor : JsonConverter<Vector2> 
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if(reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();
            reader.Read();

            var x = reader.GetDouble();
            reader.Read();
            var y = reader.GetDouble();
            reader.Read();

            if(reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException();
            reader.Read();
            
            return new Vector2((float) x, (float) y);
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteEndArray();
        }
    }
    
    public class NVector2Convertor : JsonConverter<NVector2> 
    {
        public override NVector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if(reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();
            reader.Read();

            var x = reader.GetDouble();
            reader.Read();
            var y = reader.GetDouble();
            reader.Read();

            if(reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException();
            reader.Read();
            
            return new NVector2((float) x, (float) y);
        }

        public override void Write(Utf8JsonWriter writer, NVector2 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteEndArray();
        }
    }
}