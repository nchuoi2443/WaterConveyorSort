using System;
using Dreamteck.Splines;
using UnityEngine;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.BoardSystem.Conveyor
{
    public sealed class ConveyorBuilder
    {
        private readonly SplineComputer splineComputer;
        private readonly SplineMesh splineMesh;

        public ConveyorBuilder(SplineComputer splineComputer, SplineMesh splineMesh)
        {
            this.splineComputer = splineComputer;
            this.splineMesh = splineMesh;
        }

        public void BuildConveyor(BoardData boardData, PathData pathData, Transform boardRoot)
        {
            Validate(boardData, pathData, boardRoot);

            var points = new SplinePoint[pathData.Cells.Count];
            for (int i = 0; i < points.Length; i++)
            {
                Vector2Int cell = pathData.Cells[i];
                Vector3 localPosition = BoardCoordinates.CellToLocal(boardData, cell);

                points[i] = new SplinePoint(boardRoot.TransformPoint(localPosition))
                {
                    normal = boardRoot.up,
                    size = 1f,
                    color = Color.white
                };
            }

            // Open the previous path before replacing its points, including on reinitialization.
            splineComputer.Break();
            splineComputer.type = Spline.Type.Linear;
            splineComputer.SetPoints(points, SplineComputer.Space.World);
            if (pathData.IsClosed)
                splineComputer.Close();

            // Rebuild the existing mesh using the Inspector-authored channel configuration.
            splineComputer.RebuildImmediate();
            splineMesh.RebuildImmediate();
        }

        private void Validate(BoardData boardData, PathData pathData, Transform boardRoot)
        {
            if (splineComputer == null || splineMesh == null)
                throw new InvalidOperationException("ConveyorBuilder requires a SplineComputer and SplineMesh.");
            if (splineMesh.spline != splineComputer)
                throw new InvalidOperationException("SplineMesh must reference the ConveyorBuilder's SplineComputer.");
            if (boardRoot == null)
                throw new ArgumentNullException(nameof(boardRoot));
            if (boardData == null)
                throw new ArgumentNullException(nameof(boardData));
            if (pathData == null)
                throw new ArgumentNullException(nameof(pathData));
            if (boardData.Width < 1 || boardData.Height < 1 ||
                boardData.CellSize <= 0f || float.IsNaN(boardData.CellSize) || float.IsInfinity(boardData.CellSize))
                throw new ArgumentException("Board dimensions and CellSize must be positive and finite.", nameof(boardData));
            if (pathData.Cells == null || pathData.Cells.Count < (pathData.IsClosed ? 3 : 2))
                throw new ArgumentException("A path requires at least two cells, or three for a closed path.", nameof(pathData));

            for (int i = 0; i < pathData.Cells.Count; i++)
            {
                Vector2Int cell = pathData.Cells[i];
                if (cell.x < 0 || cell.x >= boardData.Width || cell.y < 0 || cell.y >= boardData.Height)
                    throw new ArgumentException($"Path cell {i} ({cell}) is outside the board.", nameof(pathData));
            }
        }
    }
}
