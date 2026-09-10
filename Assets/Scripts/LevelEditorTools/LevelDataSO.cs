using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaterConveyorSort.LevelData
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "Water Conveyor Sort/Level Data")]
    public sealed class LevelDataSO : ScriptableObject
    {
        [SerializeField] private BoardData board = new BoardData();
        [SerializeField] private PathData path = new PathData();
        [SerializeField] private List<BuoyNodeData> buoyNodes = new List<BuoyNodeData>();

        public BoardData Board => board;
        public PathData Path => path;
        public IReadOnlyList<BuoyNodeData> BuoyNodes => buoyNodes;
    }

    [Serializable]
    public sealed class BoardData
    {
        [SerializeField, Min(1)] private int width = 10;
        [SerializeField, Min(1)] private int height = 10;
        [SerializeField, Min(0.01f)] private float cellSize = 1f;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
    }

    [Serializable]
    public sealed class PathData
    {
        [SerializeField] private bool isClosed;

        // Cells are ordered from the start of the path to its end.
        // A closed path connects the last cell to the first without duplicating it.
        [SerializeField] private List<Vector2Int> cells = new List<Vector2Int>();

        public bool IsClosed => isClosed;
        public IReadOnlyList<Vector2Int> Cells => cells;
    }

    [Serializable]
    public sealed class BuoyNodeData
    {
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private List<BuoyColumnData> columns = new List<BuoyColumnData>
        {
            new BuoyColumnData()
        };

        public Vector2Int GridPosition => gridPosition;
        public IReadOnlyList<BuoyColumnData> Columns => columns;
    }

    [Serializable]
    public sealed class BuoyColumnData
    {
        public const int DefaultBuoyCount = 5;

        // An empty list means the column has no elements.
        [SerializeField] private List<ColumnElementData> elements = new List<ColumnElementData>();
        [SerializeField] private List<BuoyData> buoys = CreateDefaultBuoys();

        public IReadOnlyList<ColumnElementData> Elements => elements;
        public IReadOnlyList<BuoyData> Buoys => buoys;
        public int BuoyCount => buoys.Count;

        private static List<BuoyData> CreateDefaultBuoys()
        {
            var result = new List<BuoyData>(DefaultBuoyCount);
            for (int i = 0; i < DefaultBuoyCount; i++)
            {
                result.Add(new BuoyData());
            }

            return result;
        }
    }

    [Serializable]
    public sealed class BuoyData
    {
        // The editor will assign an ID from the game's shared color palette.
        [SerializeField] private string colorId = string.Empty;
        [SerializeField] private List<BuoyElementData> elements = new List<BuoyElementData>();

        public string ColorId => colorId;
        public IReadOnlyList<BuoyElementData> Elements => elements;
    }

    [Serializable]
    public sealed class ColumnElementData
    {
        // Identifies a column element, such as ice or box.
        [SerializeField] private string elementId = string.Empty;

        public string ElementId => elementId;
    }

    [Serializable]
    public sealed class BuoyElementData
    {
        // Identifies a buoy element, such as hidden.
        [SerializeField] private string elementId = string.Empty;

        public string ElementId => elementId;
    }
}
