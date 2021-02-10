using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector2 = System.Numerics.Vector2;

namespace Editor.Gui
{
    public static partial class ImGuiEx
    {
        public struct FilePickerDefinition
        {
            public string ActionButtonLabel;
            public string RootFolder;
            public string CurrentFolder;
            public string SelectedFileName;
            public string SelectedAbsolutePath;
            public string SelectedRelativePath;
            public string executingPath;
            public bool OnlyAllowFolders;
            public List<string> AllowedExtensions;
        }

        public static FilePickerDefinition CreateFilePickerDefinition(string startingPath, string actionLabel, string searchFilter = null,
            bool onlyAllowFolders = false)
        {
            if (File.Exists(startingPath))
            {
                startingPath = new FileInfo(startingPath).DirectoryName;
            }
            else if (string.IsNullOrEmpty(startingPath) || !Directory.Exists(startingPath))
            {
                startingPath = Environment.CurrentDirectory;
                if (string.IsNullOrEmpty(startingPath))
                    startingPath = AppContext.BaseDirectory;
            }

            var fp = new FilePickerDefinition
            {
                RootFolder = "/", CurrentFolder = startingPath, OnlyAllowFolders = onlyAllowFolders,
                ActionButtonLabel = actionLabel,
                executingPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
            };

            if (searchFilter != null)
            {
                if (fp.AllowedExtensions != null)
                    fp.AllowedExtensions.Clear();
                else
                    fp.AllowedExtensions = new List<string>();

                fp.AllowedExtensions.AddRange(searchFilter.Split(new[] {'|'},
                    StringSplitOptions.RemoveEmptyEntries));
            }

            return fp;
        }

        public static bool DoFilePicker(ref FilePickerDefinition fpDef)
        {
            ImGui.Text(Path.GetFileName(fpDef.RootFolder) + fpDef.CurrentFolder.Replace(fpDef.RootFolder, ""));
            bool result = false;

            var ch = ImGui.GetContentRegionAvail();
            var frameHeight = ch.Y - (ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().WindowPadding.Y * 3.5f);
            if (ImGui.BeginChildFrame(1, new Vector2(0, frameHeight),
                ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.NoResize))
            {
                var di = new DirectoryInfo(fpDef.CurrentFolder);
                if (di.Exists)
                {
                    if (di.Parent != null && fpDef.CurrentFolder != fpDef.RootFolder)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, Color.Yellow.PackedValue);
                        if (ImGui.Selectable("../", false, ImGuiSelectableFlags.DontClosePopups))
                            fpDef.CurrentFolder = di.Parent.FullName;

                        ImGui.PopStyleColor();
                    }

                    var fileSystemEntries = GetFileSystemEntries(ref fpDef, di.FullName);
                    foreach (var fse in fileSystemEntries)
                    {
                        if (Directory.Exists(fse))
                        {
                            var name = Path.GetFileName(fse);
                            ImGui.PushStyleColor(ImGuiCol.Text, Color.Yellow.PackedValue);
                            if (ImGui.Selectable(name + "/", false, ImGuiSelectableFlags.DontClosePopups))
                                fpDef.CurrentFolder = fse;
                            ImGui.PopStyleColor();
                        }
                        else
                        {
                            var name = Path.GetFileName(fse);
                            bool isSelected = fpDef.SelectedAbsolutePath == fse;
                            if (ImGui.Selectable(name, isSelected, ImGuiSelectableFlags.DontClosePopups))
                                fpDef.SelectedAbsolutePath = fse;

                            if (ImGui.IsMouseDoubleClicked(0) && ImGui.IsItemHovered())
                            {
                                result = true;
                                ImGui.CloseCurrentPopup();
                            }
                        }
                    }
                }
            }

            ImGui.EndChildFrame();

            if (fpDef.OnlyAllowFolders)
            {
                fpDef.SelectedAbsolutePath = fpDef.CurrentFolder;
                fpDef.SelectedRelativePath = fpDef.SelectedAbsolutePath.Substring(fpDef.executingPath.Length + 1);
            }
            else 
            {
                if (!string.IsNullOrEmpty(fpDef.SelectedAbsolutePath))
                {
                    fpDef.SelectedRelativePath = fpDef.SelectedAbsolutePath.Substring(fpDef.executingPath.Length + 1);
                    fpDef.SelectedFileName = Path.GetFileName(fpDef.SelectedAbsolutePath);
                }
                
                ImGui.SetNextItemWidth(ch.X);
                string fileName = fpDef.SelectedFileName ?? string.Empty;
                ImGui.InputText(String.Empty, ref fileName, 64);

                if(!string.IsNullOrEmpty(fileName))
                {
                    fpDef.SelectedAbsolutePath = Path.Combine(fpDef.CurrentFolder, fileName);
                    fpDef.SelectedRelativePath =
                        Path.Combine(Path.GetDirectoryName(fpDef.SelectedRelativePath), fileName);
                }
            }
            
            if (ImGui.Button("Cancel"))
            {
                result = false;
                ImGui.CloseCurrentPopup();
            }

            if (fpDef.SelectedAbsolutePath != null)
            {
                ImGui.SameLine();
                if (ImGui.Button(fpDef.ActionButtonLabel))
                {
                    result = true;
                    ImGui.CloseCurrentPopup();
                }
            }

            return result;
        }

        private static List<string> GetFileSystemEntries(ref FilePickerDefinition fpDef, string fullName)
        {
            var files = new List<string>();
            var dirs = new List<string>();

            foreach (var fse in Directory.GetFileSystemEntries(fullName, ""))
            {
                if (Directory.Exists(fse))
                {
                    dirs.Add(fse);
                }
                else if (!fpDef.OnlyAllowFolders)
                {
                    if (fpDef.AllowedExtensions != null)
                    {
                        var ext = Path.GetExtension(fse);
                        if (fpDef.AllowedExtensions.Contains(ext))
                            files.Add(fse);
                    }
                    else
                    {
                        files.Add(fse);
                    }
                }
            }

            var ret = new List<string>(dirs);
            ret.AddRange(files);

            return ret;
        }
    }
}