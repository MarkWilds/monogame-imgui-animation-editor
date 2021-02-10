using Microsoft.Xna.Framework;

namespace Rune.MonoGame
{
    public class DynamicGridSettings
    {
        public Color MinorGridColor { get; set; } = new Color(64, 64, 64);
        public Color MajorGridColor { get; set; } = new Color(96, 96, 96);
        public Color OriginGridColor { get; set; } = new Color(160, 160, 160);

        public int MaxGridSize { get; set; } = 2 << 6;
        public int HideLinesLower { get; set; } = 4;
        public int MajorLineEvery { get; set; } = 8;
        public int GridSizeInPixels { get; set; } = 8;
    }
}