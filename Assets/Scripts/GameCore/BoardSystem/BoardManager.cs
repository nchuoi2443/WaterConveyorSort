using System;
using System.Collections.Generic;
using UnityEngine;
using WaterConveyorSort.BoardSystem.Conveyor;
using WaterConveyorSort.BoardSystem.Buoys;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.BoardSystem
{
    public sealed class BoardManager : MonoBehaviour
    {
        [Tooltip("Board center and orientation. Keep its world scale at one for CellSize in world units.")]
        [SerializeField] private Transform boardRoot;
        [SerializeField] private ConveyorController conveyorController;
        [SerializeField] private BuoyVisual buoyPrefab;
        [SerializeField] private BuoyStackVisual buoyStackPrefab;
        [SerializeField] private BuoyStackHolderVisual buoyStackHolderPrefab;

        private readonly BuoyStackHolderController buoyStackHolderController = new BuoyStackHolderController();

        public void InitBoard(BoardData boardData, PathData pathData,
            IReadOnlyList<BuoyNodeData> nodes, ColorDataSO colors)
        {
            if (boardRoot == null)
                throw new InvalidOperationException("BoardManager requires a board root.");
            if (conveyorController == null)
                throw new InvalidOperationException("BoardManager requires a ConveyorController.");
            if (nodes != null && nodes.Count > 0 &&
                (buoyPrefab == null || buoyStackPrefab == null || buoyStackHolderPrefab == null))
                throw new InvalidOperationException("Assign all three buoy prefabs on BoardManager.");

            conveyorController.InitConveyor(boardData, pathData, boardRoot);
            buoyStackHolderController.InitHolders(boardData, nodes, colors, boardRoot,
                buoyStackHolderPrefab, buoyStackPrefab, buoyPrefab);
        }

        private void OnDestroy()
        {
            buoyStackHolderController.Clear();
        }
    }
}
