using System;
using System.Collections.Generic;
using UnityEngine;
using WaterConveyorSort.BoardSystem.Conveyor;
using WaterConveyorSort.BoardSystem.Buoys;
using WaterConveyorSort.LevelData;
using WaterConveyorSort.InputHandling;

namespace WaterConveyorSort.BoardSystem
{
    public sealed class BoardManager : MonoBehaviour
    {
        [Tooltip("Board center and orientation. Keep its world scale at one for CellSize in world units.")]
        private Transform boardRoot;
        private ConveyorController conveyorController;
        private BuoyVisual buoyPrefab;
        private BuoyStackVisual buoyStackPrefab;
        private BuoyStackHolderVisual buoyStackHolderPrefab;

        private InputSystem inputSystem;

        private float transferSpeed = 4f;
        private float launchInterval = 0.12f;
        private BuoyReceiveConfig receiveConfig;
        public void SetReceiveConfig(BuoyReceiveConfig config)
        {
            receiveConfig = config;
            transfers?.SetReceiveConfig(config);
        }
        public void Configure(Transform root, ConveyorController conveyor, InputSystem input,
            BuoyVisual buoy, BuoyStackVisual stack, BuoyStackHolderVisual holder)
        {
            boardRoot = root;
            conveyorController = conveyor;
            inputSystem = input;
            buoyPrefab = buoy;
            buoyStackPrefab = stack;
            buoyStackHolderPrefab = holder;
        }
        public void SetMotionSettings(float speed, float interval)
        {
            transferSpeed = Mathf.Max(0.01f, speed);
            launchInterval = Mathf.Max(0f, interval);
        }
        private BuoyTransferController transfers;
        private void LateUpdate() => transfers?.Tick(Time.deltaTime, transferSpeed, launchInterval);

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
                throw new InvalidOperationException("Assign all three buoy prefabs on LevelManager.");

            if (inputSystem == null) inputSystem = GetComponent<InputSystem>();
            if (inputSystem == null) inputSystem = gameObject.AddComponent<InputSystem>();

            transfers?.Clear();
            buoyStackHolderController.Clear();
            conveyorController.InitConveyor(boardData, pathData, boardRoot);
            buoyStackHolderController.InitHolders(boardData, nodes, colors, boardRoot,
                buoyStackHolderPrefab, buoyStackPrefab, buoyPrefab, inputSystem);
            transfers = new BuoyTransferController(conveyorController);
            transfers.SetReceiveConfig(receiveConfig);
            foreach (BuoyStackHolder holder in buoyStackHolderController.Holders)
                holder.InitializeTransfers(transfers);
        }

        private void OnDestroy()
        {
            transfers?.Clear();
            if (conveyorController != null) conveyorController.ClearGroups();
            buoyStackHolderController.Clear();
        }
    }
}
