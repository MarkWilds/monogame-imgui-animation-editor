using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Editor.Geometry;
using Editor.Graphics;
using Editor.Graphics.Grid;
using Editor.Gui;
using Editor.Model;
using Editor.Model.Converters;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Rune.MonoGame;
using NVector2 = System.Numerics.Vector2;

namespace Editor
{
    public class State
    {
        public Dictionary<string, Property> PropertyDefinitions { get; set; } = new Dictionary<string, Property>(1024);
        public Dictionary<string, TextureFrame> Textures { get; set; } = new Dictionary<string, TextureFrame>(64);
        public Dictionary<string, Entity> Entities { get; set; } = new Dictionary<string, Entity>(64);
        public Animator Animator { get; set; } = new Animator();
    }
    
    public class GameApplication : Game
    {
        private SpriteBatch _spriteBatch;
        private PrimitiveBatch _primitiveBatch;

        private State _state;

        private DynamicGrid _grid;
        private Camera _camera;
        private int zoom = 2;

        private ImGuiRenderer _imguiRenderer;
        private MouseState previousMouseState;

        // view state
        private ImGuiEx.FilePickerDefinition _openFdDefinition;
        
        private string selectedEntityId = string.Empty;
        private string selectedTextureId = string.Empty;
        private string hoveredentityId = string.Empty;

        public const string POSITION_PROPERTY = "Position";
        public const string FRAMEINDEX_PROPERTY = "FrameIndex";

        public GameApplication()
        {
            new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1400,
                PreferredBackBufferHeight = 768,
                IsFullScreen = false
            };

            Content.RootDirectory = "Content";
            IsFixedTimeStep = true;
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _grid = new DynamicGrid(new DynamicGridSettings() {GridSizeInPixels = 32});

            var viewport = GraphicsDevice.Viewport;
            _camera= new Camera(viewport.Width / zoom, viewport.Height / zoom, -1, 1);
            
            // offset a bit to show origin at correct position
            _camera.Move((Vector3.UnitX - Vector3.UnitY) * 64 );

            _state = new State();
            InitializeDefaultState(_state);

            base.Initialize();
        }

        private void ResetEditor(State state, bool addDefaultProperties = true)
        {
            InitializeDefaultState(state, addDefaultProperties);
            selectedEntityId = string.Empty;
            selectedTextureId = string.Empty;
            hoveredentityId = string.Empty;
        }

        private void InitializeDefaultState(State state, bool addDefaultProperties = true)
        {
            state.Animator.AddInterpolator<Vector2>((fraction, first, second) => first * fraction + second * (1 - fraction));
            state.Animator.AddInterpolator<int>((fraction, first, second) => (int) (first * fraction + second * (1 - fraction)));
            state.Animator.OnKeyframeChanged += () =>
            {
                foreach (var entity in _state.Entities.Values)
                {
                    foreach (var propertyId in entity)
                    {
                        var trackId = _state.Animator.GetTrackKey(entity.Id, propertyId);
                        if (_state.Animator.Interpolate(trackId, out object currentValue))
                            entity.SetCurrentPropertyValue(_state.PropertyDefinitions[propertyId], currentValue);
                    }
                }
            };

            if (addDefaultProperties)
            {
                state.PropertyDefinitions[POSITION_PROPERTY] = new Property(POSITION_PROPERTY, typeof(Vector2));
                state.PropertyDefinitions[FRAMEINDEX_PROPERTY] = new Property(FRAMEINDEX_PROPERTY, typeof(int));    
            }
        }

        protected override void LoadContent()
        {
            _imguiRenderer = new ImGuiRenderer(this);
            ImGuiEx.IcoMoon.AddIconsToDefaultFont(16f);
            _imguiRenderer.RebuildFontAtlas();

            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _primitiveBatch = new PrimitiveBatch(GraphicsDevice);

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            foreach (var texturesValue in _state.Textures.Values)
            {
                texturesValue.Dispose();
            }

            base.UnloadContent();
        }

        private void Input()
        {
            MouseState newMouseState = Mouse.GetState();
            if (newMouseState != previousMouseState
                && newMouseState.RightButton == ButtonState.Pressed)
            {
                var viewport = GraphicsDevice.Viewport;
                var wPrevMouse = _camera.ScreenToWorld(viewport, previousMouseState.Position.ToVector2());
                var wNewMouse = _camera.ScreenToWorld(viewport, newMouseState.Position.ToVector2());

                _camera.Move(wPrevMouse - wNewMouse);
            }

            previousMouseState = newMouseState;
        }

        protected override void Update(GameTime gameTime)
        {
            if (!ImGui.GetIO().WantCaptureMouse && !ImGui.GetIO().WantCaptureKeyboard)
                Input();

            _grid.CalculateBestGridSize(zoom);
            _grid.CalculateGridData(data =>
            {
                var viewport = GraphicsDevice.Viewport;
                data.GridDim = viewport.Height;

                var worldTopLeft = _camera.ScreenToWorld(viewport, new Vector2(0, 0));
                var worldTopRight = _camera.ScreenToWorld(viewport, new Vector2(viewport.Width, 0));
                var worldBottomRight = _camera.ScreenToWorld(viewport, new Vector2(viewport.Width, viewport.Height));
                var worldBottomLeft = _camera.ScreenToWorld(viewport, new Vector2(0, viewport.Height));

                Aabb bounds = new Aabb();
                bounds.Grow(worldTopLeft);
                bounds.Grow(worldTopRight);
                bounds.Grow(worldBottomRight);
                bounds.Grow(worldBottomLeft);

                return bounds;
            });
            
            _state.Animator.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(32, 32, 32));

            _primitiveBatch.Begin(_camera.View, _camera.Projection);
            _grid.Render(_primitiveBatch, Matrix.Identity);
            _primitiveBatch.End();

            var viewport = GraphicsDevice.Viewport;
            var translation = _camera.View.Translation;
            var spriteBatchTransformation = Matrix.CreateTranslation(viewport.Width / 2 / zoom, viewport.Height / 2 / zoom, 0) *
                                            Matrix.CreateTranslation(translation.X, -translation.Y, 0)
                                            * Matrix.CreateScale(zoom);
            
            _spriteBatch.Begin(transformMatrix: spriteBatchTransformation, samplerState: SamplerState.PointClamp);

            foreach (var entity in _state.Entities.Values)
            {
                var posProperty = _state.PropertyDefinitions[POSITION_PROPERTY];
                var position = entity.GetCurrentPropertyValue<Vector2>(posProperty);
            
                var frameIndexProperty = _state.PropertyDefinitions[FRAMEINDEX_PROPERTY];
                var frameIndex = entity.GetCurrentPropertyValue<int>(frameIndexProperty);
            
                var texture = _state.Textures[entity.TextureId];
                int framesX = (int) (texture.Width / texture.FrameSize.X);
            
                int x = frameIndex % framesX;
                int y = frameIndex / framesX;

                var sourceRect = new Rectangle((int) (x * texture.FrameSize.X), (int) (y * texture.FrameSize.Y),
                    (int) texture.FrameSize.X, (int) texture.FrameSize.Y);

                _spriteBatch.Draw(texture, position, sourceRect, Color.White,
                    0f, new Vector2(texture.Pivot.X, texture.Pivot.Y),
                    1.0f, SpriteEffects.None, 0f);
            }

            _spriteBatch.End();

            DrawUi(gameTime);
        }

        private void DrawSpriteBounds(string entityId, uint color)
        {
            var bgDrawList = ImGui.GetBackgroundDrawList();
            var entity = _state.Entities[entityId];
            var texture = _state.Textures[entity.TextureId];
            var propDefinition = _state.PropertyDefinitions[POSITION_PROPERTY];
            var position = entity.GetCurrentPropertyValue<Vector2>(propDefinition);

            var sp = _camera.WorldToScreen(GraphicsDevice.Viewport, new Vector3(position.X, -position.Y, 0));

            var frameSize = new NVector2(texture.FrameSize.X, texture.FrameSize.Y) * zoom;
            var start = new NVector2(sp.X, sp.Y) -
                        new NVector2(texture.Pivot.X, texture.Pivot.Y) * zoom;

            bgDrawList.AddRect(start, start + frameSize, color);
        }

        private void DrawUi(GameTime gameTime)
        {
            _imguiRenderer.BeforeLayout(gameTime);

            // Draw viewport overlays
            if (!string.IsNullOrEmpty(hoveredentityId))
                DrawSpriteBounds(hoveredentityId, Color.CornflowerBlue.PackedValue);
            else if (!string.IsNullOrEmpty(selectedEntityId))
                DrawSpriteBounds(selectedEntityId, Color.GreenYellow.PackedValue);
            
            ImGui.Begin("timeline");
            ImGuiEx.DrawUiTimeline(_state.Animator);
            ImGui.End();

            var hierarchyWindowWidth = 256;
            ImGui.SetNextWindowPos(new NVector2(GraphicsDevice.Viewport.Width - hierarchyWindowWidth, 0), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(NVector2.UnitX * hierarchyWindowWidth +
                                    NVector2.UnitY * GraphicsDevice.Viewport.Height, ImGuiCond.FirstUseEver);

            ImGui.Begin("Management");
            {
                DrawUiActions();
                DrawUiHierarchyFrame();
                DrawUiProperties(); 
            }
            ImGui.End();

            _imguiRenderer.AfterLayout();

            if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow))
            {
                ImGui.CaptureKeyboardFromApp();
                ImGui.CaptureMouseFromApp();
            }
        }

        private JsonSerializerOptions CreateJsonSerializerOptions(Dictionary<string, Property> propertyDefs = null)
        {
            var options = new JsonSerializerOptions() {WriteIndented = true};
            options.Converters.Add(new Vector2Convertor());
            options.Converters.Add(new NVector2Convertor());
            if (propertyDefs != null)
            {
                options.Converters.Add(new TrackConverter(propertyDefs));
                options.Converters.Add(new EntityConverter(propertyDefs));
            }
            options.Converters.Add(new AnimatorConverter());
            options.Converters.Add(new PropertyConverter());
            options.Converters.Add(new TextureFrameConverter(GraphicsDevice));
            return options;
        }

        private void DrawUiActions()
        {
            var toolbarSize = NVector2.UnitY * (ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y * 2);
            ImGui.Text($"{ImGuiEx.IcoMoon.HammerIcon} Actions");
            ImGui.BeginChildFrame(1, toolbarSize);
            {
                if (ImGuiEx.DelegateButton("New project", $"{ImGuiEx.IcoMoon.HammerIcon}", "New project"))
                {
                    _state = new State();
                    ResetEditor(_state);
                }
                ImGui.SameLine();
                
                if (ImGuiEx.DelegateButton("Save project", $"{ImGuiEx.IcoMoon.FloppyDiskIcon}", "Save project"))
                {
                    _openFdDefinition = ImGuiEx.CreateFilePickerDefinition(Assembly.GetExecutingAssembly()
                        .Location, "Save", ".json");
                    ImGui.OpenPopup("Save project");

                }
                DoPopup("Save project", ref _openFdDefinition, () =>
                {
                    var json = JsonSerializer.Serialize(_state, CreateJsonSerializerOptions(_state.PropertyDefinitions));
                    File.WriteAllText(_openFdDefinition.SelectedRelativePath, json);
                });
                
                ImGui.SameLine();
                if (ImGuiEx.DelegateButton("Open project", $"{ImGuiEx.IcoMoon.FolderOpenIcon}", "Open project"))
                {
                    _openFdDefinition = ImGuiEx.CreateFilePickerDefinition(Assembly.GetExecutingAssembly()
                        .Location, "Open", ".json");
                    ImGui.OpenPopup("Open project");
                }
                DoPopup("Open project", ref _openFdDefinition, () =>
                {
                    // load json
                    var json = File.ReadAllText(_openFdDefinition.SelectedRelativePath);

                    using var jsonDocument = JsonDocument.Parse(json);
                    var propJson = jsonDocument.RootElement.GetProperty(nameof(_state.PropertyDefinitions)).ToString();
                    var properties = JsonSerializer.Deserialize<Dictionary<string, Property>>(propJson, CreateJsonSerializerOptions());
                    var newState = JsonSerializer.Deserialize<State>(json, CreateJsonSerializerOptions(properties));

                    // clean animator and sprites/textures
                    ResetEditor(newState, false);
                    _state = newState;
                });
                
                
                ImGui.SameLine();
            }
            ImGui.EndChildFrame();
        }

        private void DrawUiHierarchyFrame()
        {
            var size = ImGui.GetContentRegionAvail();
            var itemSpacing = ImGui.GetStyle().ItemSpacing + NVector2.UnitY * 4;
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, itemSpacing);
            
            ImGui.Text($"{ImGuiEx.IcoMoon.ListIcon} Hierarchy");
            ImGui.BeginChildFrame(2, size - NVector2.UnitY * 256);
            {
                // create sprite
                bool itemHovered = false;
                ImGui.Text($"{ImGuiEx.IcoMoon.ImagesIcon} Entities");
                ImGui.SameLine();
                
                if (_state.Textures.Count > 0)
                {
                    if (ImGui.SmallButton($"{ImGuiEx.IcoMoon.PlusIcon}##1"))
                    {
                        ImGui.OpenPopup("Create entity");
                        ImGuiEx.DoEntityCreatorReset();
                    }
                }
                else
                {
                    ImGuiEx.DisabledButton($"{ImGuiEx.IcoMoon.PlusIcon}");
                }
                
                ImGuiEx.DoEntityCreatorModal(_state.Textures.Keys.ToArray(), (name, selectedTexture) =>
                {
                    Entity entity = new Entity(name, selectedTexture);

                    var propDef = _state.PropertyDefinitions[POSITION_PROPERTY];
                    entity.SetCurrentPropertyValue(propDef, propDef.CreateInstance());
                    _state.Animator.CreateTrack(propDef.Type, name, POSITION_PROPERTY);

                    var fiPropDef = _state.PropertyDefinitions[FRAMEINDEX_PROPERTY];
                    entity.SetCurrentPropertyValue(fiPropDef, fiPropDef.CreateInstance());
                    _state.Animator.CreateTrack(fiPropDef.Type, name, FRAMEINDEX_PROPERTY);
                    
                    _state.Entities[entity.Id] = entity;
                });

                // show all created entities
                ImGui.Indent();
                foreach (var entity in _state.Entities.Values)
                {
                    bool selected = selectedEntityId == entity.Id;
                    ImGui.Selectable(entity.Id, ref selected);

                    if (selected)
                    {
                        selectedTextureId = string.Empty;
                        selectedEntityId = entity.Id;
                    }

                    if (ImGui.IsItemHovered())
                    {
                        itemHovered = true;
                        hoveredentityId = entity.Id;
                    }
                }
                ImGui.Unindent();

                if (!itemHovered)
                    hoveredentityId = string.Empty;
                
                // Add textures
                ImGui.Text($"{ImGuiEx.IcoMoon.TextureIcon} Textures");
                ImGui.SameLine();

                if (ImGui.SmallButton($"{ImGuiEx.IcoMoon.PlusIcon}##2"))
                {
                    _openFdDefinition = ImGuiEx.CreateFilePickerDefinition(Assembly.GetExecutingAssembly()
                        .Location, "Open", ".png");
                    ImGui.OpenPopup("Load texture");
                }

                DoPopup("Load texture", ref _openFdDefinition, () =>
                {
                    var key = Path.GetFileNameWithoutExtension(_openFdDefinition.SelectedFileName);
                    if (!_state.Textures.ContainsKey(key))
                    {
                        var path = _openFdDefinition.SelectedRelativePath;
                        var texture = Texture2D.FromFile(GraphicsDevice, path);
                        _state.Textures[key] = new TextureFrame(texture, path,
                            new NVector2(32, 32), 
                            new NVector2(16, 16));
                    }
                });
                
                // show all loaded textures
                ImGui.Indent();
                foreach (var texture in _state.Textures.Keys)
                {
                    bool selected = selectedTextureId == texture;
                    ImGui.Selectable(texture, ref selected);

                    if (selected)
                    {
                        selectedEntityId = string.Empty;
                        selectedTextureId = texture;
                    }
                }
                ImGui.Unindent();

                ImGui.TreePop();
            }
            ImGui.EndChildFrame();
            ImGui.PopStyleVar();
        }
        
        private void DrawUiProperties()
        {
            void InsertKeyframe(string entityId, string propertyId)
            {
                var entity = _state.Entities[entityId];
                var propDef = _state.PropertyDefinitions[propertyId];
                var trackId = _state.Animator.GetTrackKey(entityId, propertyId);
                var value = entity.GetCurrentPropertyValue<object>(propDef);
                
                _state.Animator.InsertKeyframe(trackId, value);
            }
            
            ImGui.Text($"{ImGuiEx.IcoMoon.EqualizerIcon} Properties");
            ImGui.BeginChildFrame(3, NVector2.UnitY * 208);
            if (!string.IsNullOrEmpty(selectedEntityId))
            {
                var selectedEntity = _state.Entities[selectedEntityId];
                
                var tempEntityName = ImGuiEx.SavedInput(String.Empty, selectedEntity.Id);
                ImGui.SameLine();
                if (ImGui.Button("Rename") && !_state.Entities.ContainsKey(tempEntityName))
                {
                    RenameEntity(selectedEntity, tempEntityName);
                    ImGuiEx.ResetSavedInput();
                }
                
                ImGui.Separator();
                
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 28);
                if (ImGui.Button($"{ImGuiEx.IcoMoon.KeyIcon}##group"))
                {
                    foreach (var propertyId in selectedEntity)
                    {
                        InsertKeyframe(selectedEntityId, propertyId);
                    }
                }
                ImGui.NextColumn();
                ImGui.Text("All properties");
                ImGui.Separator();
                ImGui.NextColumn();
                
                var keyframeButtonId = 0;
                foreach (var propertyId in selectedEntity)
                {
                    ImGui.PushID(keyframeButtonId++);
                    if (ImGui.Button($"{ImGuiEx.IcoMoon.KeyIcon}"))
                        InsertKeyframe(selectedEntityId, propertyId);
                    ImGui.PopID();
                    
                    ImGui.NextColumn();
                    
                    var propDefinition = _state.PropertyDefinitions[propertyId];
                    switch (propertyId)
                    {
                        case POSITION_PROPERTY:
                            Vector2 value = selectedEntity.GetCurrentPropertyValue<Vector2>(propDefinition);

                            var pos = new NVector2(value.X, value.Y);
                            ImGui.DragFloat2(propertyId, ref pos);

                            value.X = pos.X;
                            value.Y = pos.Y;
                            
                            selectedEntity.SetCurrentPropertyValue(propDefinition, value);

                            break;
                        case FRAMEINDEX_PROPERTY:
                            int frameIndex = selectedEntity.GetCurrentPropertyValue<int>(propDefinition);

                            var texture = _state.Textures[selectedEntity.TextureId];
                            int framesX = (int) (texture.Width / texture.FrameSize.X);
                            int framesY = (int) (texture.Height / texture.FrameSize.Y);

                            ImGui.SliderInt(propertyId, ref frameIndex, 0, framesX * framesY - 1);
                            
                            selectedEntity.SetCurrentPropertyValue(propDefinition, frameIndex);
                            break;
                    }
                    
                    ImGui.NextColumn();
                }
            }
            else if (!string.IsNullOrEmpty(selectedTextureId))
            {
                var scale = 2f;
                var selectedTexture = _state.Textures[selectedTextureId];
                var currentFrameSize = selectedTexture.FrameSize;
                var currentPivot = selectedTexture.Pivot;
                
                ImGui.DragFloat2("Framesize", ref currentFrameSize);
                ImGui.DragFloat2("Pivot", ref currentPivot);
                
                selectedTexture.FrameSize = currentFrameSize;
                selectedTexture.Pivot = currentPivot;
                
                var scaledFrameSize = currentFrameSize * scale;
                var scaledPivot = currentPivot * scale;

                ImGui.BeginChildFrame(2, NVector2.UnitY * 154f);

                var contentSize = ImGui.GetContentRegionAvail();
                var center = ImGui.GetCursorScreenPos() + contentSize * 0.5f;
                var frameStart = center - scaledFrameSize * 0.5f;

                // draw frame size
                var drawList = ImGui.GetWindowDrawList();
                drawList.AddRect(frameStart, frameStart + scaledFrameSize, Color.GreenYellow.PackedValue);

                // horizontal line
                drawList.AddLine(center - NVector2.UnitX * scaledFrameSize * 0.5f, 
                    center + NVector2.UnitX * scaledFrameSize * 0.5f, 
                    Color.ForestGreen.PackedValue);
                
                // vertical line
                drawList.AddLine(center - NVector2.UnitY * scaledFrameSize * 0.5f, 
                    center + NVector2.UnitY * scaledFrameSize * 0.5f, 
                    Color.ForestGreen.PackedValue);
                
                // draw pivot
                drawList.AddCircleFilled(frameStart + scaledPivot, 4, Color.White.PackedValue);
            
                ImGui.EndChildFrame();
            }

            ImGui.EndChildFrame();
        }

        private void RenameEntity(Entity entity, string newName)
        {
            // re-add entity
            var oldName = entity.Id;
            _state.Entities.Remove(oldName);
            _state.Entities[newName] = entity;
            entity.Id = newName;

            if (_state.Animator.ChangeGroupName(oldName, newName))
            {
                foreach (var property in entity)
                {
                    var oldId = _state.Animator.GetTrackKey(oldName, property);
                    _state.Animator.ChangeTrackId(newName, property, oldId);
                }
            }
            
            selectedEntityId = newName;
            if (!string.IsNullOrEmpty(hoveredentityId))
                hoveredentityId = newName;
        }
        
        private void DoPopup(string id, ref ImGuiEx.FilePickerDefinition fpd, Action onDone)
        {
            bool popupOpen = true;
            ImGui.SetNextWindowContentSize(NVector2.One * 400);
            if (ImGui.BeginPopupModal(id, ref popupOpen, ImGuiWindowFlags.NoResize))
            {
                if (ImGuiEx.DoFilePicker(ref fpd))
                    onDone?.Invoke();

                ImGui.EndPopup();
            }
        }
    }
}