using System.Collections;
using System.Collections.Generic;

namespace Editor.Model
{
    public class Entity : IEnumerable<string>
    {
        private readonly HashSet<string> _properties;
        private readonly Dictionary<string, object> _propertyCurrentValues;
        
        public string Id { get; set; }
        
        public string TextureId { get; }

        public Entity(string id, string textureId)
        {
            Id = id;
            TextureId = textureId;
            _properties = new HashSet<string>(32);
            _propertyCurrentValues = new Dictionary<string, object>(32);
        }

        public void SetCurrentPropertyValue(Property property, object value)
        {
            _properties.Add(property);
            _propertyCurrentValues[property] = value;
        }

        public T GetCurrentPropertyValue<T>(Property property)
        {
            return (T)_propertyCurrentValues[property];
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _properties.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}