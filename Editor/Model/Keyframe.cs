using System;

namespace Editor.Model
{
    public class Keyframe : IComparable<Keyframe>
    {
        public int Frame { get; set; }

        public object Value { get; set; }

        public static implicit operator Keyframe(int value) => new Keyframe(value, null);

        public Keyframe(int frame, object data)
        {
            this.Frame = frame;
            Value = data;
        }

        public int CompareTo(Keyframe other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Frame.CompareTo(other.Frame);
        }
    }
}