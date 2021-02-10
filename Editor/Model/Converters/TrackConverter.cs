using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Editor.Model.Converters
{
    public class TrackConverter : JsonConverter<AnimationTrack>
    {
        private readonly Dictionary<string, Property> _propertyDefinitions;

        public TrackConverter(Dictionary<string, Property> propertyDefinitions)
        {
            _propertyDefinitions = propertyDefinitions;
        }
        
        public override AnimationTrack Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var animationTrack = new AnimationTrack(typeToConvert, String.Empty);
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();
            reader.Read();
            {
                // id is the same as the property id
                var id = reader.ReadStringProperty(nameof(animationTrack.Id));
                reader.Read();

                var property = _propertyDefinitions[id];
                animationTrack = new AnimationTrack(property.Type, id);
                              
                // keyframes
                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();
                reader.Read();
                
                if (reader.TokenType != JsonTokenType.StartArray)
                    throw new JsonException();

                var dummyKeyframe = new Keyframe(0, null);
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;
                    
                    if (reader.TokenType != JsonTokenType.StartObject)
                        throw new JsonException();
                    reader.Read();
                    {
                        // frame
                        var frame = reader.ReadIntegerProperty(nameof(dummyKeyframe.Frame));
                        reader.Read();
                        
                        // value
                        if (reader.TokenType != JsonTokenType.PropertyName)
                            throw new JsonException();
                        reader.Read();
                        
                        var value = JsonSerializer.Deserialize(ref reader, property.Type, options);
                        reader.Read();
                        
                        animationTrack.Add(new Keyframe(frame, value));
                    }
                    if (reader.TokenType != JsonTokenType.EndObject)
                        throw new JsonException();
                }
                
                if (reader.TokenType != JsonTokenType.EndArray)
                    throw new JsonException();
                reader.Read();
            }
            if (reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException();

            return animationTrack;
        }

        public override void Write(Utf8JsonWriter writer, AnimationTrack track, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(track.Id), track.Id);

            writer.WriteStartArray("Keyframes");
            foreach (var keyframe in track)
            {
                writer.WriteStartObject();
                writer.WriteNumber(nameof(keyframe.Frame), keyframe.Frame);
                writer.WritePropertyName(nameof(keyframe.Value));
                JsonSerializer.Serialize(writer, keyframe.Value, track.Type, options);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}