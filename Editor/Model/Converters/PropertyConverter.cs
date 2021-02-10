using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Editor.Model.Converters
{
    public class PropertyConverter : JsonConverter<Property>
    {
        public override Property Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var property = new Property(String.Empty, typeToConvert);
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();
            reader.Read();
            {
                var id = reader.ReadStringProperty("Id");
                reader.Read();
                
                var typeName = reader.ReadStringProperty(nameof(property.Type));
                reader.Read();

                property = new Property(id, Type.GetType(typeName));
            }
            if (reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException();

            return property;
        }

        public override void Write(Utf8JsonWriter writer, Property value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Id", value);
            
            var assemblyName = value.Type.Assembly.GetName().Name;
            
            writer.WriteString(nameof(value.Type), $"{value.Type.FullName}, {assemblyName}");
            writer.WriteEndObject();
        }
    }
}