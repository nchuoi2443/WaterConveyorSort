using System;
using UnityEngine;
using WaterConveyorSort.BoardSystem.Conveyor;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.BoardSystem
{
    public sealed class BoardManager : MonoBehaviour
    {
        [Tooltip("Board center and orientation. Keep its world scale at one for CellSize in world units.")]
        [SerializeField] private Transform boardRoot;
        [SerializeField] private ConveyorController conveyorController;

        public void InitBoard(BoardData boardData, PathData pathData)
        {
            if (boardRoot == null)
                throw new InvalidOperationException("BoardManager requires a board root.");
            if (conveyorController == null)
                throw new InvalidOperationException("BoardManager requires a ConveyorController.");

            conveyorController.InitConveyor(boardData, pathData, boardRoot);
        }
    }
}
