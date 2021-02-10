using System;
using System.Runtime.InteropServices;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector2 = System.Numerics.Vector2;

namespace Editor.Gui
{
    public static partial class ImGuiEx
    {
        private static string savingInputString;

        public static class IcoMoon
        {
            public const char BackwardIcon = '\uea1f';
            public const char ForwardIcon = '\uea20';
            
            public const char PreviousIcon = '\uea23';
            public const char NextIcon = '\uea24';
            
            public const char FirstIcon = '\uea21';
            public const char LastIcon = '\uea22';
            
            public const char LoopIcon = '\uea2d';

            public const char TextureIcon = '\ueacd';
            public const char ImageIcon = '\ue90d';
            public const char ImagesIcon = '\ue90e';
            
            public const char BranchesIcon = '\ue9bc';
            public const char ListIcon = '\ue9ba';
            public const char EqualizerIcon = '\ue992';
            public const char HammerIcon = '\ue996';
            public const char KeyIcon = '\ue98d';
            
            public const char FloppyDiskIcon = '\ue962';
            public const char FolderOpenIcon = '\ue930';
            
            public const char PlusIcon = '\uea0a';
            public const char MinusIcon = '\uea0b';

            public static void AddIconsToDefaultFont(float fontSize)
            {
                var fontFilePath = "IcoMoon-Free.ttf";
                var ranges = new []{ '\ue900', '\ueaea' };
                var rangesIntPtr = Marshal.UnsafeAddrOfPinnedArrayElement(ranges, 0);

                ImFontConfig config = new ImFontConfig {
                    MergeMode = 1, 
                    OversampleH = 3, OversampleV = 3,
                    GlyphOffset = Vector2.UnitY * 2,
                    FontDataOwnedByAtlas = 1,
                    PixelSnapH = 1,
                    GlyphMaxAdvanceX = float.MaxValue,
                    RasterizerMultiply =  1.0f
                };
            
                var configHandle = GCHandle.Alloc(config, GCHandleType.Pinned);
                ImGui.GetIO().Fonts.AddFontFromFileTTF(fontFilePath, fontSize, configHandle.AddrOfPinnedObject(), rangesIntPtr);
            
                configHandle.Free();
            }
        }

        public static string SavedInput(string id, string defaultInput)
        {
            if(string.IsNullOrEmpty(savingInputString))
                savingInputString = defaultInput;
            
            ImGui.InputText(id, ref savingInputString, 64);

            return savingInputString;
        }

        public static void ResetSavedInput()
        {
            savingInputString = String.Empty;    
        }
        
        public static bool ToggleButton(string id, string descr, ref bool toggled)
        { 
            uint backgroundColor = toggled ? ImGui.GetColorU32(ImGuiCol.ButtonActive) 
                : ImGui.GetColorU32(ImGuiCol.Button);

            ImGui.PushStyleColor(ImGuiCol.Button, backgroundColor);
            var pressed = ImGui.Button(id);
            if (pressed)
                toggled = !toggled;
            
            ImGui.PopStyleColor();
            
            if (!string.IsNullOrEmpty(descr) && ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(descr);
                ImGui.EndTooltip();    
            }

            return pressed;
        }

        public static bool RoundedToggleButton(string id, ref bool toggled)
        {
            ImGui.Text(id);
            ImGui.SameLine();
            
            var screenCursor = ImGui.GetCursorScreenPos();
            var drawList = ImGui.GetWindowDrawList();
            var pressed = false;

            var height = ImGui.GetFrameHeight();
            var width = height * 1.55f;
            var radius = height * 0.5f;

            if (ImGui.InvisibleButton(id, new Vector2(width, height)))
            {
                toggled = !toggled;
                pressed = true;
            }

            uint backgroundColor;
            if (ImGui.IsItemHovered())
            {
                backgroundColor = ImGui.GetColorU32(ImGuiCol.ChildBg);
            }
            else
            {
                backgroundColor = toggled
                    ? ImGui.GetColorU32(ImGuiCol.ButtonHovered)
                    : ImGui.GetColorU32(ImGuiCol.Button);
            }
            
            drawList.AddRectFilled(screenCursor, new Vector2(screenCursor.X + width, screenCursor.Y + height),
                backgroundColor, radius);

            var centre = toggled ? new Vector2(screenCursor.X + width - radius, screenCursor.Y + radius) 
                : new Vector2(screenCursor.X + radius, screenCursor.Y + radius);
            drawList.AddCircleFilled(centre, radius - 1.5f, Color.White.PackedValue);

            return pressed;
        }
        
        public static bool DelegateButton(string id, string text, string descr = null, Action<string> callback = null)
        {
            var buttonPressed = ImGui.Button(text);
            if (!string.IsNullOrEmpty(descr) && ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(descr);
                ImGui.EndTooltip();    
            }
            
            if(buttonPressed)
                callback?.Invoke(id);

            return buttonPressed;
        }

        public static void DisabledButton(string id)
        {
            Vector2 pos = ImGui.GetCursorScreenPos();
            var style = ImGui.GetStyle();
            var textSize = ImGui.CalcTextSize(id);
            textSize.X += style.FramePadding.X * 2;
            // textSize.Y += style.FramePadding.Y * 2;

            ImGui.GetWindowDrawList()
                .AddRectFilled(pos, pos + textSize, ImGui.GetColorU32(ImGuiCol.FrameBg));

            ImGui.SetCursorScreenPos(pos + Vector2.UnitX * style.FramePadding.X);
            ImGui.TextDisabled(id);
        }
    }
}