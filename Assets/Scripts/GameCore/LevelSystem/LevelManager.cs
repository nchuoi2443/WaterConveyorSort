using System;
using UnityEngine;
using WaterConveyorSort.BoardSystem;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.LevelSystem
{
    public sealed class LevelManager : MonoBehaviour
    {
        [SerializeField] private LevelDataSO levelData;
        [SerializeField] private BoardManager boardManager;

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

            boardManager.InitBoard(levelData.Board, levelData.Path, levelData.BuoyNodes, levelData.ColorData);
        }
    }
}
