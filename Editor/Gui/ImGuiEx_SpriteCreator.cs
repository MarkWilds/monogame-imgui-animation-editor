using System;
using ImGuiNET;
using NVector2 = System.Numerics.Vector2;

namespace Editor.Gui
{
    public partial class ImGuiEx
    {
        private static int selectedTexture = 0;
        private static string spriteName = String.Empty;

        public static void DoEntityCreatorReset()
        {
            selectedTexture = 0;
            spriteName = "Sprite" + new Random().Next();
        }
        
        public static void DoEntityCreatorModal(string[] textureNames, Action<string, string> onCreatePressed)
        {
            var open_create_sprite = true;
            var ch = ImGui.GetContentRegionAvail();
            var frameHeight = ch.Y - (ImGui.GetTextLineHeight() + ImGui.GetStyle().WindowPadding.Y * 1.5f);
            if (ImGui.BeginPopupModal("Create entity", ref open_create_sprite, ImGuiWindowFlags.NoResize))
            {
                ImGui.BeginChildFrame(1337, NVector2.UnitX * 400 + NVector2.UnitY * frameHeight);
                ImGui.InputText("Entity name", ref spriteName, 64);
                ImGui.ListBox("Textures", ref selectedTexture, textureNames, textureNames.Length);

                ImGui.EndChildFrame();

                if (ImGui.Button("Create entity##2"))
                {
                    onCreatePressed?.Invoke(spriteName, textureNames[selectedTexture]);

                    ImGui.CloseCurrentPopup();
                }
                
                ImGui.EndPopup();
            }
        }
    }
}