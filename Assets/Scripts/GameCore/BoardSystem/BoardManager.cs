using System;
using System.Collections.Generic;
using UnityEngine;
using WaterConveyorSort.BoardSystem.Conveyor;
using WaterConveyorSort.BoardSystem.Buoys;
using WaterConveyorSort.LevelData;
using WaterConveyorSort.InputHandling;
using WaterConveyorSort.BoardSystem.StackQueue;

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
        private StackQueueVisual stackQueueVisual;
        private StackQueueExit stackQueueExit;
        private StackQueueController stackQueue;
        public StackQueueController StackQueue => stackQueue;
        public event Action StackQueueFull;
        public void ConfigureStackQueue(StackQueueVisual visual, StackQueueExit exit)
        {
            stackQueueVisual = visual;
            stackQueueExit = exit;
        }
        public void SetPaused(bool paused)
        {
            transfers?.SetPaused(paused);
            stackQueue?.SetPaused(paused);
            if (conveyorController != null) conveyorController.SetPaused(paused);
            if (inputSystem != null) inputSystem.enabled = !paused;
        }
        private void OnQueueFull()
        {
            SetPaused(true);
            StackQueueFull?.Invoke();
        }
        private void OnConveyorEnd(ConveyorBuoyGroup group) => stackQueue?.TryReceive(group);

        private float transferSpeed = 4f;
        private float launchInterval = 0.12f;
        private float receiveFlightDuration = 0.4f;
        private float receiveLaunchDelay = 0.12f;
        public void SetReceiveSettings(float duration, float delay)
        {
            receiveFlightDuration = Mathf.Max(0.01f, duration);
            receiveLaunchDelay = Mathf.Max(0f, delay);
            transfers?.SetReceiveSettings(receiveFlightDuration, receiveLaunchDelay);
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
            IReadOnlyList<BuoyNodeData> nodes, ColorDataSO colors, int maxStackInStackQueue = 3)
        {
            if (boardRoot == null)
                throw new InvalidOperationException("BoardManager requires a board root.");
            if (conveyorController == null)
                throw new InvalidOperationException("BoardManager requires a ConveyorController.");
            if (stackQueueVisual == null || stackQueueExit == null)
                throw new InvalidOperationException("Assign Stack Queue Visual and Stack Queue Exit on LevelManager.");
            stackQueueVisual.ValidateSetup();
            if (nodes != null && nodes.Count > 0 &&
                (buoyPrefab == null || buoyStackPrefab == null || buoyStackHolderPrefab == null))
                throw new InvalidOperationException("Assign all three buoy prefabs on LevelManager.");

            if (inputSystem == null) inputSystem = GetComponent<InputSystem>();
            if (inputSystem == null) inputSystem = gameObject.AddComponent<InputSystem>();

            transfers?.Clear();
            stackQueueExit.Clear();
            stackQueue?.Clear();
            buoyStackHolderController.Clear();
            conveyorController.InitConveyor(boardData, pathData, boardRoot);
            buoyStackHolderController.InitHolders(boardData, nodes, colors, boardRoot,
                buoyStackHolderPrefab, buoyStackPrefab, buoyPrefab, inputSystem);
            transfers = new BuoyTransferController(conveyorController);
            transfers.SetReceiveSettings(receiveFlightDuration, receiveLaunchDelay);
            stackQueue = new StackQueueController(conveyorController, transfers, stackQueueVisual);
            stackQueue.Initialize(maxStackInStackQueue);
            stackQueue.Full += OnQueueFull;
            stackQueueExit.Initialize(stackQueue);
            conveyorController.ReachedEnd -= OnConveyorEnd;
            conveyorController.ReachedEnd += OnConveyorEnd;
            foreach (BuoyStackHolder holder in buoyStackHolderController.Holders)
                holder.InitializeTransfers(transfers);
            SetPaused(false);
        }

        private void OnDestroy()
        {
            transfers?.Clear();
            if (stackQueueExit != null) stackQueueExit.Clear();
            stackQueue?.Clear();
            if (conveyorController != null) conveyorController.ReachedEnd -= OnConveyorEnd;
            if (conveyorController != null) conveyorController.ClearGroups();
            buoyStackHolderController.Clear();
        }
    }
}
