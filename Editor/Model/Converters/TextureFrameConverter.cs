using System;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework.Graphics;
using NVector2 = System.Numerics.Vector2;

namespace Editor.Model.Converters
{
    public class TextureFrameConverter : JsonConverter<TextureFrame>
    {
        private readonly GraphicsDevice _graphicsDevice;

        public TextureFrameConverter(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
        }
        
        public override TextureFrame Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var textureFrame = new TextureFrame(null, String.Empty, Vector2.One);
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();
            reader.Read();
            {
                var path = reader.ReadStringProperty(nameof(textureFrame.Path));
                reader.Read();

                Texture2D texture = Texture2D.FromFile(_graphicsDevice, path);
                
                if(reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();
                reader.Read();
                var pivot = (NVector2)JsonSerializer.Deserialize(ref reader, typeof(NVector2), options);
                reader.Read();

                if(reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();
                reader.Read();
                var frameSize = (NVector2)JsonSerializer.Deserialize(ref reader, typeof(NVector2), options);
                reader.Read();
                
                textureFrame = new TextureFrame(texture, path, frameSize, pivot);
            }
            if (reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException();

            return textureFrame;
        }

        public override void Write(Utf8JsonWriter writer, TextureFrame value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            
            writer.WriteString(nameof(value.Path), value.Path);
            writer.WritePropertyName(nameof(value.Pivot));
            JsonSerializer.Serialize(writer, value.Pivot, options);
            writer.WritePropertyName(nameof(value.FrameSize));
            JsonSerializer.Serialize(writer, value.FrameSize, options);
            
            writer.WriteEndObject();
        }
    }
}