using System.Text.Json;

namespace Editor
{
    public static class Utf8JsonReaderExtensions
    {
        public static string ReadStringProperty(this ref Utf8JsonReader reader, string name = null)
        {
            if(reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            var propertyName = reader.GetString();
            if(!string.IsNullOrEmpty(name) && !propertyName.Equals(name))
                throw new JsonException();
            reader.Read();

            var value = reader.GetString();
            // reader.Read();

            return value;
        }
        
        public static int ReadIntegerProperty(this ref Utf8JsonReader reader, string name = null)
        {
            if(reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            var propertyName = reader.GetString();
            if(!string.IsNullOrEmpty(name) && !propertyName.Equals(name))
                throw new JsonException();
            reader.Read();

            var value = reader.GetInt32();

            return value;
        }
    }
}