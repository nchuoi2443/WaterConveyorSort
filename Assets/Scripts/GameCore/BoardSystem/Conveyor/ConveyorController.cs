using Dreamteck.Splines;
using UnityEngine;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.BoardSystem.Conveyor
{
    public sealed class ConveyorController : MonoBehaviour
    {
        [SerializeField] private SplineComputer splineComputer;
        [SerializeField] private SplineMesh splineMesh;

        private ConveyorBuilder conveyorBuilder;

        public void InitConveyor(BoardData boardData, PathData pathData, Transform boardRoot)
        {
            conveyorBuilder = new ConveyorBuilder(splineComputer, splineMesh);
            conveyorBuilder.BuildConveyor(boardData, pathData, boardRoot);
        }
    }
}
