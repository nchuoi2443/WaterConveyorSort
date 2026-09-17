using System;
using System.Collections.Generic;
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

        public void BuildConveyor(BoardData boardData, PathData pathData, Transform boardRoot, float cornerRadius = 0.7f)
        {
            Validate(boardData, pathData, boardRoot);

            SplinePoint[] points = CreateRoundedPoints(boardData, pathData, boardRoot, cornerRadius);

            // Open the previous path before replacing its points, including on reinitialization.
            splineComputer.Break();
            splineComputer.type = Spline.Type.Bezier;
            // Extruded mesh copies span equal percentages, so sample by distance rather than segment index.
            splineComputer.sampleMode = SplineComputer.SampleMode.Uniform;
            splineComputer.SetPoints(points, SplineComputer.Space.World);
            if (pathData.IsClosed)
                splineComputer.Close();

            // Rebuild the existing mesh using the Inspector-authored channel configuration.
            splineComputer.RebuildImmediate();
            splineMesh.RebuildImmediate();
        }

        private static SplinePoint[] CreateRoundedPoints(BoardData board, PathData path, Transform root, float radius)
        {
            var cells = new List<Vector3>();
            foreach (Vector2Int cell in path.Cells)
            {
                Vector3 position = BoardCoordinates.CellToLocal(board, cell);
                if (cells.Count == 0 || position != cells[cells.Count - 1]) cells.Add(position);
            }
            if (path.IsClosed && cells.Count > 1 && cells[0] == cells[cells.Count - 1])
                cells.RemoveAt(cells.Count - 1);
            if (cells.Count < (path.IsClosed ? 3 : 2))
                throw new ArgumentException("Conveyor requires distinct path points.", nameof(path));

            cells = MergeStraightSegments(cells, path.IsClosed);

            var points = new List<SplinePoint>();
            for (int i = 0; i < cells.Count; i++)
            {
                Vector3 position = cells[i];
                if (!path.IsClosed && (i == 0 || i == cells.Count - 1))
                {
                    points.Add(CreatePoint(root, position));
                    continue;
                }
                Vector3 incoming = position - cells[(i + cells.Count - 1) % cells.Count];
                Vector3 outgoing = cells[(i + 1) % cells.Count] - position;
                float turn = Vector3.Angle(incoming, outgoing) * Mathf.Deg2Rad;
                if (radius <= 0f || turn < 0.001f || turn > Mathf.PI - 0.001f)
                {
                    points.Add(CreatePoint(root, position));
                    continue;
                }
                // Each corner uses less than half of its adjacent segments to avoid overlap.
                float cut = Mathf.Min(radius * Mathf.Tan(turn * 0.5f),
                    Mathf.Min(incoming.magnitude, outgoing.magnitude) * 0.45f);
                float handle = (4f / 3f) * cut / Mathf.Tan(turn * 0.5f) * Mathf.Tan(turn * 0.25f);
                Vector3 entry = position - incoming.normalized * cut;
                Vector3 exit = position + outgoing.normalized * cut;
                SplinePoint before = CreatePoint(root, entry);
                SplinePoint after = CreatePoint(root, exit);
                before.tangent2 = root.TransformPoint(entry + incoming.normalized * handle);
                after.tangent = root.TransformPoint(exit - outgoing.normalized * handle);
                points.Add(before);
                points.Add(after);
            }
            int segments = path.IsClosed ? points.Count : points.Count - 1;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % points.Count;
                SplinePoint a = points[i], b = points[next];
                Vector3 third = (b.position - a.position) / 3f;
                // Straight segments retain collinear handles; curve handles were set above.
                if (a.tangent2 == a.position) a.tangent2 = a.position + third;
                if (b.tangent == b.position) b.tangent = b.position - third;
                points[i] = a;
                points[next] = b;
            }
            return points.ToArray();
        }

        private static SplinePoint CreatePoint(Transform root, Vector3 localPosition)
        {
            return new SplinePoint(root.TransformPoint(localPosition))
            {
                type = SplinePoint.Type.Broken,
                normal = root.up,
                size = 1f,
                color = Color.white
            };
        }

        private static List<Vector3> MergeStraightSegments(List<Vector3> cells, bool closed)
        {
            var merged = new List<Vector3>();
            for (int i = 0; i < cells.Count; i++)
            {
                // Preserve open endpoints and direction reversals.
                if (!closed && (i == 0 || i == cells.Count - 1))
                {
                    merged.Add(cells[i]);
                    continue;
                }
                Vector3 incoming = cells[i] - cells[(i + cells.Count - 1) % cells.Count];
                Vector3 outgoing = cells[(i + 1) % cells.Count] - cells[i];
                if (Vector3.Angle(incoming, outgoing) >= 0.01f) merged.Add(cells[i]);
            }
            if (merged.Count < (closed ? 3 : 2))
                throw new ArgumentException("Conveyor requires distinct turns or endpoints.", nameof(cells));
            return merged;
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
