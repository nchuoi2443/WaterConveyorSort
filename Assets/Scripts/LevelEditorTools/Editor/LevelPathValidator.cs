using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.LevelEditorTools
{
    public sealed partial class LevelEditorTool
    {
        private bool FitsBoard(LevelDataSO level, int width, int height)
        {
            foreach (Vector2Int cell in level.Path.Cells)
                if (!Contains(cell, width, height)) return false;
            foreach (BuoyNodeData node in level.BuoyNodes)
                if (!Contains(node.GridPosition, width, height)) return false;
            return true;
        }

        private string ValidatePath(IReadOnlyList<Vector2Int> cells, bool closed, LevelDataSO level)
        {
            if (cells.Count < 2) return "Đường cần ít nhất 2 ô.";
            var occupied = new HashSet<Vector2Int>();
            for (int i = 0; i < cells.Count; i++)
            {
                if (!Contains(cells[i], level.Board.Width, level.Board.Height)) return "Đường nằm ngoài board.";
                if (!occupied.Add(cells[i])) return "Đường không được lặp lại ô.";
                if (i > 0 && !Adjacent(cells[i - 1], cells[i])) return "Các ô đường phải liền cạnh.";
            }
            if (closed && (cells.Count < 4 || !Adjacent(cells[0], cells[cells.Count - 1])))
                return "Đường kín cần ít nhất 4 ô, ô cuối phải liền cạnh ô đầu.";
            foreach (BuoyNodeData node in level.BuoyNodes)
            {
                Vector2Int cell = node.GridPosition;
                if (occupied.Contains(cell)) return "Đường đang đè lên node phao.";
                if (!occupied.Contains(cell + Vector2Int.left) && !occupied.Contains(cell + Vector2Int.right) &&
                    !occupied.Contains(cell + Vector2Int.up) && !occupied.Contains(cell + Vector2Int.down))
                    return "Có node phao không còn nằm sát đường.";
            }
            return null;
        }

        private bool CanPlaceNode(LevelDataSO level, Vector2Int cell)
        {
            if (!Contains(cell, level.Board.Width, level.Board.Height))
                return false;
            if (ContainsPathCell(level.Path.Cells, cell))
                return false;
            if (FindNodeIndex(level, cell) >= 0)
                return true;

            return ContainsPathCell(level.Path.Cells, cell + Vector2Int.left) ||
                ContainsPathCell(level.Path.Cells, cell + Vector2Int.right) ||
                ContainsPathCell(level.Path.Cells, cell + Vector2Int.up) ||
                ContainsPathCell(level.Path.Cells, cell + Vector2Int.down);
        }

        private bool CanMoveNode(LevelDataSO level, int nodeIndex, Vector2Int cell)
        {
            if (nodeIndex < 0 || nodeIndex >= level.BuoyNodes.Count)
                return false;
            if (!Contains(cell, level.Board.Width, level.Board.Height))
                return false;
            if (ContainsPathCell(level.Path.Cells, cell))
                return false;

            int existing = FindNodeIndex(level, cell);
            if (existing >= 0 && existing != nodeIndex)
                return false;

            return ContainsPathCell(level.Path.Cells, cell + Vector2Int.left) ||
                ContainsPathCell(level.Path.Cells, cell + Vector2Int.right) ||
                ContainsPathCell(level.Path.Cells, cell + Vector2Int.up) ||
                ContainsPathCell(level.Path.Cells, cell + Vector2Int.down);
        }

        private int FindNodeIndex(LevelDataSO level, Vector2Int cell)
        {
            for (int i = 0; i < level.BuoyNodes.Count; i++)
            {
                if (level.BuoyNodes[i].GridPosition == cell)
                    return i;
            }

            return -1;
        }

        private bool ContainsPathCell(IReadOnlyList<Vector2Int> cells, Vector2Int target)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == target)
                    return true;
            }

            return false;
        }

        private bool Contains(Vector2Int cell, int width, int height) =>
            cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;

        private bool Adjacent(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
    }
}
