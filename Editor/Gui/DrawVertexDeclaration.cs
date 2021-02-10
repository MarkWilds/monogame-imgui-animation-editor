using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;

namespace Editor.Gui
{
    public class DrawVertexDeclaration
    {
        public static class DrawVertDeclaration
        {
            public static readonly VertexDeclaration Declaration;

            public static readonly int Size;

            static unsafe DrawVertDeclaration()
            {
                Size = sizeof(ImDrawVert); // ImDrawVert is currently 12 bytes
                Declaration = new VertexDeclaration(
                    Size,

                    // Position
                    new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),

                    // UV
                    new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),

                    // Color
                    new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
                );
            }
        }
    }
}