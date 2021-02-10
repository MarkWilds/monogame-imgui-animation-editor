using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Editor.Model.Converters
{
    public class EntityConverter : JsonConverter<Entity>
    {
        private readonly Dictionary<string, Property> _propertyDefinitions;

        public EntityConverter(Dictionary<string, Property> propertyDefinitions)
        {
            _propertyDefinitions = propertyDefinitions;
        }

        public override Entity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var entity = new Entity(String.Empty, String.Empty);

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();
            reader.Read();
            {
                var id = reader.ReadStringProperty(nameof(entity.Id));
                reader.Read();

                var textureId = reader.ReadStringProperty(nameof(entity.TextureId));
                reader.Read();

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();
                reader.Read();

                if (reader.TokenType != JsonTokenType.StartArray)
                    throw new JsonException();

                entity = new Entity(id, textureId);
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;
                    
                    var property = reader.GetString();
                    var propertyDefinition = _propertyDefinitions[property];
                    entity.SetCurrentPropertyValue(propertyDefinition, propertyDefinition.CreateInstance());
                }

                if (reader.TokenType != JsonTokenType.EndArray)
                    throw new JsonException();
                reader.Read();
            }
            if (reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException();

            return entity;
        }

        public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(value.Id), value.Id);
            writer.WriteString(nameof(value.TextureId), value.TextureId);
            writer.WriteStartArray("Properties");
            foreach (var property in value)
            {
                writer.WriteStringValue(property);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}