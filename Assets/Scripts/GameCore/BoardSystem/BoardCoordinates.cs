using UnityEngine;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.BoardSystem
{
    public static class BoardCoordinates
    {
        public static Vector3 CellToLocal(BoardData board, Vector2Int cell)
        {
            return new Vector3(
                (cell.x - (board.Width - 1) * 0.5f) * board.CellSize,
                0f,
                (cell.y - (board.Height - 1) * 0.5f) * board.CellSize);
        }
    }
}
