using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Editor.Graphics
{
    public class PrimitiveBatch : IDisposable
    {
        public enum DrawStyle
        {
            Wireframe,
            Filled
        }

        private readonly Vector3[] _cubePositions = {
            // front
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
            // back
            new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f)
        };

        private readonly short[][] _cubeFaceIndices =
        {
            new short[] {0, 1, 2, 3}, // front
            new short[] {4, 5, 6, 7}, // back
            new short[] {7, 6, 1, 0}, // left
            new short[] {3, 2, 5, 4}, // right
            new short[] {7, 0, 3, 4}, // bottom
            new short[] {1, 6, 5, 2} // top
        };

        private readonly GraphicsDevice _graphicsDevice;
        private readonly BasicEffect _basicEffect;

        private readonly VertexPositionColor[] _triangleBuffer;
        private readonly VertexPositionColor[] _lineBuffer;
        private int _triangleBufferIndex;
        private int _lineBufferIndex;

        private bool _hasBegun;
        
        public PrimitiveBatch(GraphicsDevice graphicsDevice, int bufferSize = 1024)
        {
            _graphicsDevice = graphicsDevice;
            _basicEffect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true,
                World = Matrix.Identity
            };

            _triangleBuffer = new VertexPositionColor[bufferSize - bufferSize % 3];
            _lineBuffer = new VertexPositionColor[bufferSize - bufferSize % 2];
            _triangleBufferIndex = 0;
            _lineBufferIndex = 0;
        }

        public void Begin(Matrix view, Matrix projection)
        {
            if (_hasBegun)
                throw new InvalidOperationException("End() must be called before Begin() can be called again!");

            _basicEffect.View = view;
            _basicEffect.Projection = projection;
            _basicEffect.CurrentTechnique.Passes[0].Apply();
            _hasBegun = true;
        }

        public void End()
        {
            if (!_hasBegun)
                throw new InvalidOperationException("Begin() must be called before End() can be called!");

            FlushBatch(_triangleBuffer, PrimitiveType.TriangleList, ref _triangleBufferIndex, 3);
            FlushBatch(_lineBuffer, PrimitiveType.LineList, ref _lineBufferIndex, 2);

            _hasBegun = false;
        }

        public void Dispose()
        {
            _basicEffect?.Dispose();
        }

        private void FlushBatch(VertexPositionColor[] buffer, PrimitiveType type, ref int bufferIndex, int primitiveSize)
        {
            if (bufferIndex >= primitiveSize)
            {
                var primitiveCount = bufferIndex / primitiveSize;
                _graphicsDevice.DepthStencilState = DepthStencilState.None;
                _graphicsDevice.DrawUserPrimitives(type, buffer, 0, primitiveCount);
                bufferIndex -= primitiveCount * primitiveSize;
            }
        }

        private void AddVertex(Vector3 position, Color color, PrimitiveType type)
        {
            if (!_hasBegun)
                throw new InvalidOperationException("Begin() must be called before AddVertex() can be called!");

            switch (type)
            {
                case PrimitiveType.TriangleList:
                {
                    if (_triangleBufferIndex >= _triangleBuffer.Length)
                        FlushBatch(_triangleBuffer, PrimitiveType.TriangleList, ref _triangleBufferIndex, 3);

                    _triangleBuffer[_triangleBufferIndex].Color = color;
                    _triangleBuffer[_triangleBufferIndex].Position = position;
                    _triangleBufferIndex++;
                    break;
                }
                case PrimitiveType.LineList:
                {
                    if (_lineBufferIndex >= _lineBuffer.Length)
                        FlushBatch(_lineBuffer, PrimitiveType.LineList, ref _lineBufferIndex, 2);

                    _lineBuffer[_lineBufferIndex].Color = color;
                    _lineBuffer[_lineBufferIndex].Position = position;
                    _lineBufferIndex++;
                    break;
                }
            }
        }

        #region Drawing methods

        public void DrawCircle(Vector3 position, Vector3 normal, float radius, int segments, Color color)
        {
            if(segments < 4)
                throw new InvalidOperationException("Segements needs to be 4 or higher!");
        }
        
        /// <summary>
        /// Draw a line
        /// </summary>
        /// <param name="from">Start position of the line</param>
        /// <param name="to">End position of the line</param>
        /// <param name="color">The color to use</param>
        public void DrawLine(Vector3 from, Vector3 to, Color color)
        {
            AddVertex(from, color, PrimitiveType.LineList);
            AddVertex(to, color, PrimitiveType.LineList);
        }
        
        /// <summary>
        /// Draws a wireframe polygon
        /// </summary>
        /// <param name="vertices">The vertices to draw</param>
        /// <param name="color">The color to use</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void DrawPolygon(Vector3[] vertices, Color color)
        {
            if (vertices.Length < 3)
                throw new InvalidOperationException("vertices must have more than 3 elements!");

            var pointSize = vertices.Length;
            for (int i = 0; i < pointSize; i++)
            {
                ref Vector3 from = ref vertices[i];
                ref Vector3 to = ref vertices[(i + 1) % pointSize];

                DrawLine(from, to, color);
            }
        }

        /// <summary>
        /// Draws a filled polygon
        /// </summary>
        /// <param name="vertices">The vertices to draw</param>
        /// <param name="color">The color to use</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void DrawFilledPolygon(Vector3[] vertices, Color color)
        {
            if (vertices.Length < 3)
                throw new InvalidOperationException("vertices must have more than 3 elements!");

            ref Vector3 startPoint = ref vertices[0];
            int pointSize = vertices.Length;
            for (int i = 0; i < pointSize - 2; i++)
            {
                AddVertex(startPoint, color, PrimitiveType.TriangleList);
                AddVertex(vertices[i + 1], color, PrimitiveType.TriangleList);
                AddVertex(vertices[i + 2], color, PrimitiveType.TriangleList);
            }
        }

        /// <summary>
        /// Draws a box
        /// </summary>
        /// <param name="center">Position of the primitive in world space</param>
        /// <param name="size">Size of the primitive in all axis</param>
        /// <param name="color">Color of the primitive</param>
        /// <param name="style">The style to draw the primitive in</param>
        public void DrawBox(Vector3 center, Vector3 size, Color color, DrawStyle style = DrawStyle.Filled)
        {
            int faceCount = style == DrawStyle.Filled ? 6 : 4;
            Vector3[] quadVertices = new Vector3[4];

            for (int i = 0; i < faceCount; i++)
            {
                short[] indices = _cubeFaceIndices[i];

                for (int v = 0; v < 4; v++)
                {
                    int index = indices[v];
                    ref Vector3 corner = ref _cubePositions[index];
                    quadVertices[v] = center + corner * size;
                }

                if (style == DrawStyle.Filled)
                {
                    DrawFilledPolygon(quadVertices, color);
                }
                else
                {
                    DrawPolygon(quadVertices, color);
                }
            }
        }

        /// <summary>
        /// Draws a cube
        /// </summary>
        /// <param name="position">Position of the primitive in world space</param>
        /// <param name="size">Size of the primitive in all axis</param>
        /// <param name="color">Color of the primitive</param>
        /// <param name="style">The style to draw the primitive in</param>
        public void DrawCube(Vector3 position, float size, Color color, DrawStyle style = DrawStyle.Filled)
        {
            DrawBox(position, new Vector3(size), color, style);
        }

        /// <summary>
        /// Draws an axis aligned bounding box
        /// </summary>
        /// <param name="min">The min extend of the box</param>
        /// <param name="max">The max extend of the box</param>
        /// <param name="color">Color of the primitive</param>
        /// <param name="style">The style to draw the primitive in</param>
        public void DrawAabb(Vector3 min, Vector3 max, Color color, DrawStyle style = DrawStyle.Filled)
        {
            var position = (min + max) * 0.5f;
            var size = max - min;
            DrawBox(position, size, color, style);
        }

        #endregion
    }
}