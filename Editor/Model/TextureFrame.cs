using System;
using System.Numerics;
using Microsoft.Xna.Framework.Graphics;

namespace Editor.Model
{
    public class TextureFrame : IDisposable
    {
        private readonly Texture2D _texture;
        
        public string Path { get; set; }

        public Vector2 Pivot { get; set; }
        
        public Vector2 FrameSize { get; set; }

        public TextureFrame(Texture2D texture, string path, Vector2 frameSize)
        : this(texture, path, frameSize, Vector2.Zero)
        {
        }
        
        public TextureFrame(Texture2D texture, string path, Vector2 framesize, Vector2 pivot)
        {
            Path = path;
            FrameSize = framesize;
            Pivot = pivot;
            _texture = texture;
        }

        public int Width => _texture.Width;
        public int Height  => _texture.Height;
        
        public static implicit operator Texture2D(TextureFrame f)
        {
            return f._texture;
        }

        public void Dispose()
        {
            _texture?.Dispose();
        }
    }
}