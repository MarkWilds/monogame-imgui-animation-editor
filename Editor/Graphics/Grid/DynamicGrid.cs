using System;
using Editor.Geometry;
using Microsoft.Xna.Framework;
using Rune.MonoGame;

namespace Editor.Graphics.Grid
{
    public class DynamicGrid
    {
        private DynamicGridSettings _settings;
        private DynamicGridData _dynamicGridData;
        private int _currentGridSize;

        public DynamicGrid(DynamicGridSettings settings)
        {
            _settings = settings;
            _dynamicGridData = new DynamicGridData();
            _currentGridSize = _dynamicGridData.GridSize = settings.GridSizeInPixels;
        }

        public void CalculateBestGridSize(float zoomScale)
        {
            // hide lines if grid is smaller than specified number
            var gridSize = _currentGridSize;
            while (gridSize / zoomScale < _settings.HideLinesLower)
            {
                gridSize <<= 2;
                if (gridSize >= _settings.MaxGridSize << 1)
                    gridSize = _settings.MaxGridSize;
            }

            _dynamicGridData.GridSize = gridSize;
        }

        /// <summary>
        /// Data is always calculated for x,y space
        /// </summary>
        /// <param name="calculateGridBounds">Delegate expecting correct Aabb values in x,y space</param>
        public void CalculateGridData(Func<DynamicGridData, Aabb> calculateGridBounds)
        {
            var gridBounds = calculateGridBounds(_dynamicGridData);
            var gridSize = _dynamicGridData.GridSize;

            float gridWidth = gridBounds.Max.X - gridBounds.Min.X;
            float gridHeight = gridBounds.Max.Y - gridBounds.Min.Y;

            int gridCountX = (int) gridWidth / gridSize + gridSize;
            int gridCountY = (int) gridHeight / gridSize + gridSize;
            int gridStartX = (int) gridBounds.Min.X / gridSize - 1;
            int gridStartY = (int) gridBounds.Min.Y / gridSize - 1;
            
            _dynamicGridData.GridCount = new Vector2i(gridCountX, gridCountY);
            _dynamicGridData.GridStart = new Vector2i(gridStartX, gridStartY);

            // Set line start and line end in world space coordinates
            float lineStartX = gridStartX * gridSize;
            float lineStartY = gridStartY * gridSize;
            float lineEndX = (gridStartX + (gridCountX - 1)) * gridSize;
            float lineEndY = (gridStartY + (gridCountY - 1)) * gridSize;

            // keep line start and line end inside the grid dimensions
            var finalLineStartX = Math.Max(lineStartX, -_dynamicGridData.GridDim);
            var finalLineStartY = Math.Max(lineStartY, -_dynamicGridData.GridDim);
            var finalLineEndX = Math.Min(lineEndX, _dynamicGridData.GridDim);
            var finalLineEndY = Math.Min(lineEndY, _dynamicGridData.GridDim);
            
            _dynamicGridData.LineStart = new Vector2(finalLineStartX, finalLineStartY);
            _dynamicGridData.LineEnd = new Vector2(finalLineEndX, finalLineEndY);
        }

        /// <summary>
        /// The grid is rendered in the x,y plane by default
        /// </summary>
        /// <param name="batch">The renderbatch to render this grid</param>
        /// <param name="transform">The transformation to transform to a different space</param>
        public void Render(PrimitiveBatch batch, Matrix transform)
        {
            Vector2i gridStart = _dynamicGridData.GridStart;
            Vector2i gridCount = _dynamicGridData.GridCount;
            Vector2 lineStart = _dynamicGridData.LineStart;
            Vector2 lineEnd = _dynamicGridData.LineEnd;
            int gridSize = _dynamicGridData.GridSize;
            int gridDim = _dynamicGridData.GridDim;

            // the grid lines are ordered as minor, major, origin
            for (int lineType = 0; lineType < 3; lineType++)
            {
                Color lineColor = _settings.MinorGridColor;
                if (lineType == 1)
                    lineColor = _settings.MajorGridColor;
                else if (lineType == 2)
                    lineColor = _settings.OriginGridColor;

                // draw horizontal lines
                for (int i = gridStart.Y; i < gridStart.Y + gridCount.Y; ++i)
                {
                    // skip lines that are out of bound
                    if (i * gridSize < -gridDim || i * gridSize > gridDim)
                        continue;

                    // skip any line that don't match the line type we're adding
                    if (lineType == 0 && (i == 0 || (i % _settings.MajorLineEvery) == 0))
                        continue;

                    if (lineType == 1 && (i == 0 || (i % _settings.MajorLineEvery) != 0))
                        continue;

                    if (lineType == 2 && i != 0)
                        continue;

                    Vector3 from = default;
                    Vector3 to = default;
                    to.X = lineEnd.X;
                    from.X = lineStart.X;
                    from.Y = to.Y = i * gridSize;

                    Vector3.Transform(ref to, ref transform, out to);
                    Vector3.Transform(ref from, ref transform, out from);

                    batch.DrawLine(from, to, lineColor);
                }

                // draw vertical lines
                for (int i = gridStart.X; i <  gridStart.X + gridCount.X; ++i)
                {
                    // skip lines that are out of bound
                    if (i * gridSize < -gridDim || i * gridSize > gridDim)
                        continue;

                    // skip any line that don't match the line type we're adding
                    if (lineType == 0 && (i == 0 || (i % _settings.MajorLineEvery) == 0))
                        continue;

                    if (lineType == 1 && (i == 0 || (i % _settings.MajorLineEvery) != 0))
                        continue;

                    if (lineType == 2 && i != 0)
                        continue;

                    Vector3 from = default;
                    Vector3 to = default;
                    to.Y = lineEnd.Y; 
                    from.Y = lineStart.Y;
                    from.X = to.X = i * gridSize;
                    
                    Vector3.Transform(ref to, ref transform, out to);
                    Vector3.Transform(ref from, ref transform, out from);

                    batch.DrawLine(from, to, lineColor);
                }
            }
        }

        public void IncreaseGridSize()
        {
            _currentGridSize = _currentGridSize << 1;
            if (_currentGridSize == _settings.MaxGridSize << 1)
            {
                _currentGridSize = _settings.MaxGridSize;
            }
        }

        public void DecreaseGridSize()
        {
            _currentGridSize = _currentGridSize >> 1;
            if (_currentGridSize == 0)
            {
                _currentGridSize = 1;
            }
        }
    }
}