using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Editor.Model.Converters
{
    public class AnimatorConverter : JsonConverter<Animator>
    {
        public override Animator Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Animator animator = new Animator();
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();
            reader.Read();
            
            animator.FPS = reader.ReadIntegerProperty(nameof(animator.FPS));

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;
                
                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                var groupName = reader.GetString();
                reader.Read();
                
                if (reader.TokenType != JsonTokenType.StartArray)
                    throw new JsonException();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;
                    
                    var track = JsonSerializer.Deserialize(ref reader, typeof(AnimationTrack), options) as AnimationTrack;
                    animator.AddTrack(groupName, track);
                }

                if (reader.TokenType != JsonTokenType.EndArray)
                    throw new JsonException();
            }

            if (reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException();

            return animator;
        }

        public override void Write(Utf8JsonWriter writer, Animator animator, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            
            writer.WriteNumber(nameof(animator.FPS), animator.FPS);

            foreach (var groupName in animator)
            {
                writer.WriteStartArray(groupName);
                foreach (var trackId in animator.EnumerateGroupTrackIds(groupName))
                {
                    var track = animator.GetTrack(trackId);
                    JsonSerializer.Serialize(writer, track, options);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
    }
}