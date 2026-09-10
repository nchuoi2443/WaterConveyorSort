using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.LevelEditorTools
{
    // Owns draft edits; the asset is untouched until Save is selected.
    public sealed partial class LevelPathDraft
    {
        private readonly List<Vector2Int> cells = new List<Vector2Int>();
        public IReadOnlyList<Vector2Int> Cells => cells;
        public bool IsEditing { get; private set; }
        public bool IsClosed { get; set; }

        public void Begin(PathData path)
        {
            cells.Clear();
            cells.AddRange(path.Cells);
            IsClosed = path.IsClosed;
            IsEditing = true;
        }

        public void Cancel() { IsEditing = false; cells.Clear(); }
        public void Clear() { cells.Clear(); IsClosed = false; }
        public void RemoveLast()
        {
            if (cells.Count > 0) cells.RemoveAt(cells.Count - 1);
        }

        public void Visit(Vector2Int target)
        {
            int existing = cells.IndexOf(target);
            if (existing >= 0)
            {
                cells.RemoveRange(existing + 1, cells.Count - existing - 1);
                return;
            }
            if (cells.Count == 0) { cells.Add(target); return; }

            // Build the full candidate before committing so an intersection never adds a partial stroke.
            var additions = new List<Vector2Int>();
            Vector2Int current = cells[cells.Count - 1];
            while (current != target)
            {
                if (current.x != target.x) current.x += target.x > current.x ? 1 : -1;
                else current.y += target.y > current.y ? 1 : -1;
                if (cells.Contains(current)) return;
                additions.Add(current);
            }
            cells.AddRange(additions);
        }
    }
}
