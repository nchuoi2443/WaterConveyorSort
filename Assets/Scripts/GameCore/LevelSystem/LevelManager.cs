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

            boardManager.InitBoard(levelData.Board, levelData.Path);
        }
    }
}
