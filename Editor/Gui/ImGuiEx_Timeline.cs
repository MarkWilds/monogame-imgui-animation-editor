using System;
using Editor.Model;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector2 = System.Numerics.Vector2;

namespace Editor.Gui
{
    public static partial class ImGuiEx
    {
        private const int MinimalLegendwidth = 196;
        private const int LineStartOffset = 8;

        private static int pixelsPerFrame = 10;
        private static int majorLinePerLines = 5;
        
        private static Vector2 timelineRegionMin;
        private static Vector2 timelineRegionMax;
        
        private static float timelineZoom = 1;
        private static int currentLegendWidth = 0;
        
        private static int visibleStartingFrame = 0;
        private static int visibleEndingFrame = 0;

        private static float accumalatedPanningDeltaX = 0f;
        private static bool isPanningTimeline = false;

        private static (string id, string text)[] toolbarButtonDefinitions =
        {
            ("First", IcoMoon.FirstIcon.ToString()),
            ("Previous", IcoMoon.PreviousIcon.ToString()),
            ("Backward", IcoMoon.BackwardIcon.ToString()),
            ("Forward", IcoMoon.ForwardIcon.ToString()),
            ("Next", IcoMoon.NextIcon.ToString()),
            ("Last", IcoMoon.LastIcon.ToString()),
            ("Loop", IcoMoon.LoopIcon.ToString())
        };

        public static void DrawUiTimeline(Animator animator)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Current:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(48f);

            var ckf = animator.CurrentKeyframe;
            ImGui.DragInt("##1", ref ckf);
            animator.CurrentKeyframe = ckf;

            ImGui.SameLine();
            ImGui.Text("FPS:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(48f);

            var fps = animator.FPS;
            ImGui.DragInt("##4", ref fps);
            animator.FPS = fps;

            ImGui.BeginChildFrame(1, Vector2.Zero);
            {
                var style = ImGui.GetStyle();
                var toolbarSize = DrawToolbar(animator, OnToolbarPressed);

                currentLegendWidth = (int) (toolbarSize.X >= MinimalLegendwidth ? toolbarSize.X : MinimalLegendwidth);
                currentLegendWidth += (int) (style.ItemSpacing.Y + style.ItemSpacing.X * 2);

                ImGui.SameLine(currentLegendWidth);

                DrawTimeline(animator, style.ItemSpacing.Y, ImGui.GetItemRectSize().Y);

                ImGui.BeginChild("##content", Vector2.Zero, false);
                {
                    // small hack to draw first keyframe correctly
                    ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + 4);
                    
                    ImGui.Columns(2, "##legend", false);
                    ImGui.SetColumnWidth(0, currentLegendWidth);
                    foreach (var group in animator)
                    {
                        if(!animator.GroupHasKeyframes(group))
                            continue;
                        
                        bool open =  ImGui.TreeNodeEx(group, ImGuiTreeNodeFlags.DefaultOpen);
                       
                        ImGui.NextColumn();
                        
                        // draw group keyframes
                        for(int i = visibleStartingFrame; i < visibleEndingFrame; i++)
                        {
                            if(animator.GroupHasKeyframeAtFrame(group, i))
                                DrawKeyFrame(i, Color.LightGray, out Vector2 min, out Vector2 max);
                        }

                        ImGui.NextColumn();

                        if (open)
                        {
                            foreach (var trackId in animator.EnumerateGroupTrackIds(group))
                            {
                                var track = animator.GetTrack(trackId);
                                if(!track.HasKeyframes())
                                    continue;

                                ImGui.Text(track.Id);
                                ImGui.NextColumn();

                                var vStartIndex = track.GetBestIndex(visibleStartingFrame);
                                var vEndIndex = track.GetBestIndex(visibleEndingFrame);

                                foreach (var frame in track.GetRange(vStartIndex, vEndIndex - vStartIndex))
                                {
                                    DrawKeyFrame(frame.Frame, Color.ForestGreen, out Vector2 min, out Vector2 max);
                                    if (ImGui.IsMouseHoveringRect(min, max))
                                    {
                                        ImGui.BeginTooltip();
                                        ImGui.Text(frame.Value.ToString());
                                        ImGui.EndTooltip();
                                    }
                                }
                            
                                ImGui.NextColumn();
                            }
                        
                            ImGui.TreePop(); 
                        }
                    }

                    ImGui.EndChild();
                }

            }
            ImGui.EndChildFrame();
        }

        private static void OnToolbarPressed(string @event, Animator animator)
        {
            switch (@event)
            {
                case "Forward":
                    animator.PlayForward();
                    break;
                case "Backward":
                    animator.PlayBackward();
                    break;
                
                case "First":
                    animator.CurrentKeyframe = animator.GetFirstFrame();
                    if (animator.CurrentKeyframe <= visibleStartingFrame)
                        visibleStartingFrame = animator.CurrentKeyframe;
                    else if (animator.CurrentKeyframe >= visibleEndingFrame)
                        visibleStartingFrame += (animator.CurrentKeyframe - visibleEndingFrame) + 1;
                    break;
                case "Last":
                    animator.CurrentKeyframe = animator.GetLastFrame();
                    if (animator.CurrentKeyframe <= visibleStartingFrame)
                        visibleStartingFrame = animator.CurrentKeyframe;
                    else if (animator.CurrentKeyframe >= visibleEndingFrame)
                        visibleStartingFrame += (animator.CurrentKeyframe - visibleEndingFrame) + 1;
                    break;
                
                case "Previous":
                    animator.CurrentKeyframe = animator.GetPreviousFrame();
                    break;
                
                case "Next":
                    animator.CurrentKeyframe = animator.GetNexFrame();
                    break;
                
                case "Loop":
                    animator.ToggleLooping();
                    break;
            }
        }

        private static bool GetToggleButtonCondition(string id, Animator animator)
        {
            switch (id)
            {
                case "Backward":
                    return animator.PlayingBackward;
                case "Forward":
                    return animator.PlayingForward;
                default:
                    return animator.Looping;
            }
        }

        private static Vector2 DrawToolbar(Animator animator, Action<string, Animator> callback)
        {
            ImGui.BeginGroup();
            {
                for (var index = 0; index < toolbarButtonDefinitions.Length; index++)
                {
                    var tlbDef = toolbarButtonDefinitions[index];
                    if(index > 0)
                        ImGui.SameLine();

                    if (tlbDef.id.Equals("Backward") || tlbDef.id.Equals("Forward") || tlbDef.id.Equals("Loop"))
                    {
                        var toggeld = GetToggleButtonCondition(tlbDef.id, animator);
                        if (ToggleButton(tlbDef.text, tlbDef.id, ref toggeld))
                            callback?.Invoke(tlbDef.id, animator);
                    }
                    else 
                        DelegateButton(tlbDef.id, tlbDef.text, tlbDef.id, (s => callback?.Invoke(s, animator)));
                    
                }
            }
            ImGui.EndGroup();

            return ImGui.GetItemRectSize();
        }

        private static void DrawTimeline(Animator animator, float headerYPadding, float headerHeight = 24f)
        {
            var drawList = ImGui.GetWindowDrawList();
            var style = ImGui.GetStyle();
            var contentRegion = ImGui.GetContentRegionAvail();
            var headerSize = Vector2.Zero;
            headerSize.X = contentRegion.X - (style.ScrollbarSize);
            headerSize.Y = headerHeight + headerYPadding;
            
            visibleEndingFrame = GetFrameForTimelinePos(headerSize.X);

            // create rectangle for total timeline header area
            timelineRegionMin = ImGui.GetCursorScreenPos();;
            timelineRegionMax = timelineRegionMin + headerSize;
            timelineRegionMax.Y = timelineRegionMin.Y + contentRegion.Y;

            ImGui.PushClipRect(timelineRegionMin, timelineRegionMax, false);
            {
                ImGui.InvisibleButton("##header-region", headerSize);
                
                // set frame
                if (ImGui.IsItemHovered())
                {
                    var hoveringFrame = GetFrameForTimelinePos(ImGui.GetMousePos().X - timelineRegionMin.X);
                    ImGui.BeginTooltip();
                    ImGui.Text(hoveringFrame.ToString());
                    ImGui.EndTooltip();

                    if (ImGui.IsMouseDown(0))
                        animator.CurrentKeyframe = hoveringFrame;
                }
                
                // panning the timeline
                if (ImGui.IsMouseHoveringRect(timelineRegionMin, timelineRegionMax, false))
                {
                    if (ImGui.IsMouseDragging(1, 0))
                    {
                        accumalatedPanningDeltaX += ImGui.GetIO().MouseDelta.X;
                        
                        // focus window if not panning before
                        if(!ImGui.IsWindowFocused())
                            ImGui.SetWindowFocus();

                        var framesToMove = (int) Math.Floor(accumalatedPanningDeltaX / pixelsPerFrame);
                        if (framesToMove != 0)
                        {
                            isPanningTimeline = true;
                            accumalatedPanningDeltaX -= framesToMove * pixelsPerFrame;
                            visibleStartingFrame -= framesToMove;
                        }
                    }
                    else
                    {
                        isPanningTimeline = false;
                        accumalatedPanningDeltaX = 0f;
                    }
                }
                
                // draw all timeline lines
                var frames = visibleEndingFrame - visibleStartingFrame;
                for (int f = 0; f < frames; f++)
                {
                    var frame = f + visibleStartingFrame;
                    var lineStart = timelineRegionMin;
                    lineStart.X += LineStartOffset + f * pixelsPerFrame;
                    var lineEnd = lineStart + Vector2.UnitY * headerSize.Y;

                    if (frame % majorLinePerLines == 0)
                    {
                        var numberString = frame.ToString();
                        var frameTextOffset = (float) Math.Floor(ImGui.CalcTextSize(numberString).X / 2);

                        drawList.AddText(lineStart - Vector2.UnitX * frameTextOffset,
                            Color.White.PackedValue, numberString);

                        lineEnd.Y += timelineRegionMax.Y - headerSize.Y;
                        lineStart.Y += headerSize.Y * 0.5f;
                        drawList.AddLine(lineStart, lineEnd, ImGui.GetColorU32(ImGuiCol.Border));
                    }
                    else
                    {
                        lineStart.Y += headerSize.Y * 0.65f;
                        drawList.AddLine(lineStart, lineEnd, ImGui.GetColorU32(ImGuiCol.Border));
                    }
                }

                // draw currentFrame line if within range
                if (animator.CurrentKeyframe >= visibleStartingFrame && animator.CurrentKeyframe <= visibleEndingFrame)
                {
                    var frameLineStart = timelineRegionMin;
                    frameLineStart.X += GetTimelinePosForFrame(animator.CurrentKeyframe);
                    frameLineStart.Y += headerSize.Y * 0.5f;
                    
                    var frameLineEnd = frameLineStart;
                    frameLineEnd.Y += timelineRegionMax.Y;

                    drawList.AddLine(frameLineStart, frameLineEnd, Color.Pink.PackedValue);

                    var radius = 5;
                    frameLineStart.Y += radius;
                    drawList.AddCircleFilled(frameLineStart, radius, Color.Pink.PackedValue);
                }
            }
            ImGui.PopClipRect();
            
            // draw separator
            var separatorY = timelineRegionMin.Y + headerSize.Y;
            drawList.AddLine(new Vector2(ImGui.GetWindowPos().X, separatorY), 
                new Vector2(timelineRegionMin.X + contentRegion.X, separatorY), 
                ImGui.GetColorU32(ImGuiCol.Border));
            
            if (isPanningTimeline)
            {
                // draw shadow for panning
                var start = timelineRegionMin - Vector2.UnitY * style.WindowPadding.Y;
                var size = new Vector2(LineStartOffset + 8, timelineRegionMin.Y + contentRegion.Y + style.ItemSpacing.Y * 2);

                drawList.AddRectFilledMultiColor(start, start + size, 
                    0xFF000000, 0u, 0u, 0xFF000000 );    
            }
        }
        
        private static void DrawKeyFrame(int frame, Color color, out Vector2 min, out Vector2 max)
        {
            var cursorPos = ImGui.GetCursorScreenPos();
            var keyframeSize = 7f;
            var halfKeyFrameWidth = (int) Math.Floor(keyframeSize / 2);

            // 12 seems to be the offset from start of timelime region, dont know why it happens with columns
            cursorPos.X += GetTimelinePosForFrame(frame) - (halfKeyFrameWidth + 12);
            cursorPos.Y -= 2;
            
            var size = Vector2.UnitX * keyframeSize + Vector2.UnitY * (keyframeSize + 4);

            min = cursorPos;
            max = min + size;
            
            ImGui.GetWindowDrawList().AddRectFilled(cursorPos, 
                cursorPos + size, color.PackedValue);
        }

        private static int GetFrameForTimelinePos(float x)
        {
            return (int)Math.Floor( (x - LineStartOffset) / pixelsPerFrame + 0.5f) + visibleStartingFrame;
        }

        private static float GetTimelinePosForFrame(int frame)
        {
            return (frame - visibleStartingFrame) * pixelsPerFrame + LineStartOffset;
        }
    }
}