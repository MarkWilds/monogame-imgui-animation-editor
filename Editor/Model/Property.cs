using System;

namespace Editor.Model
{
    public class Property
    {
        private readonly string _id;

        public Type Type { get; }

        public Property(string id, Type type)
        {
            _id = id;
            Type = type;
        }

        public object CreateInstance()
        {
            return Activator.CreateInstance(Type);
        }

        public override bool Equals(object obj)
        {
            return _id.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }
        
        public static implicit operator string(Property property)
        {
            return property._id;
        }
    }
}