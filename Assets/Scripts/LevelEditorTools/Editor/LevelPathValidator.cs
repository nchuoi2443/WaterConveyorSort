using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.LevelEditorTools
{
    internal static class LevelPathValidator
    {
        public static bool FitsBoard(LevelDataSO level, int width, int height)
        {
            foreach (Vector2Int cell in level.Path.Cells)
                if (!Contains(cell, width, height)) return false;
            foreach (BuoyNodeData node in level.BuoyNodes)
                if (!Contains(node.GridPosition, width, height)) return false;
            return true;
        }

        public static string Validate(IReadOnlyList<Vector2Int> cells, bool closed, LevelDataSO level)
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

        private static bool Contains(Vector2Int cell, int width, int height) =>
            cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;

        private static bool Adjacent(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
    }
}
