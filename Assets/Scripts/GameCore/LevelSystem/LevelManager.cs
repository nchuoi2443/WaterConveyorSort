using System;
using Dreamteck.Splines;
using WaterConveyorSort.BoardSystem.Buoys;
using WaterConveyorSort.BoardSystem.Conveyor;
using WaterConveyorSort.InputHandling;
using UnityEngine;
using WaterConveyorSort.BoardSystem;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.LevelSystem
{
    public sealed class LevelManager : MonoBehaviour
    {
        [SerializeField] private LevelDataSO levelData;
        [SerializeField] private BoardManager boardManager;

        [Header("Board Setup")]
        [SerializeField] private Transform boardRoot;
        [SerializeField] private ConveyorController conveyorController;
        [SerializeField] private InputSystem inputSystem;
        [SerializeField] private BuoyVisual buoyPrefab;
        [SerializeField] private BuoyStackVisual buoyStackPrefab;
        [SerializeField] private BuoyStackHolderVisual buoyStackHolderPrefab;
        [Header("Conveyor Setup")]
        [SerializeField] private SplineComputer splineComputer;
        [SerializeField] private SplineMesh splineMesh;
        [SerializeField, Min(0f)] private float moveSpeed = 1f;
        [Tooltip("Group root height above the spline, along the board's local up axis, in world units.")]
        [SerializeField] private float rootYOffset = 0.2f;
        [SerializeField, Min(0.01f)] private float pathMoveSlotSpacing = 0.3f;
        [SerializeField, Min(0.01f)] private float conveyorGroupGap = 0.6f;
        [Header("Transfer Setup")]
        [SerializeField, Min(0.01f)] private float transferSpeed = 4f;
        [SerializeField, Min(0f)] private float launchInterval = 0.12f;

        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            ApplyMotionSettings();
        }

        private void ApplyMotionSettings()
        {
            if (boardManager != null) boardManager.SetMotionSettings(transferSpeed, launchInterval);
            if (conveyorController != null) conveyorController.SetMotionSettings(moveSpeed, rootYOffset);
        }

        private void Start()
        {
            InitLevel();
        }

        public void InitLevel()
        {
            if (levelData == null)
                throw new InvalidOperationException("LevelManager requires level data.");
            if (boardManager == null)
                throw new InvalidOperationException("LevelManager requires a BoardManager.");
            if (levelData.BuoyNodes.Count > 0 && levelData.ColorData == null)
                throw new InvalidOperationException($"Level '{levelData.name}' has no ColorDataSO assigned. Assign Color Data on the level asset.");

            if (conveyorController == null || splineComputer == null || splineMesh == null || boardRoot == null || inputSystem == null)
                throw new InvalidOperationException("Assign board root, input system, conveyor and spline references on LevelManager.");
            conveyorController.Configure(splineComputer, splineMesh);
            conveyorController.ConfigurePathSlots(pathMoveSlotSpacing, conveyorGroupGap);
            boardManager.Configure(boardRoot, conveyorController, inputSystem, buoyPrefab, buoyStackPrefab, buoyStackHolderPrefab);
            ApplyMotionSettings();
            boardManager.InitBoard(levelData.Board, levelData.Path, levelData.BuoyNodes, levelData.ColorData);
        }
    }
}
