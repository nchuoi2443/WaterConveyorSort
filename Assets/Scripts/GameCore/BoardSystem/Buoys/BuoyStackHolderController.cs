using System;
using System.Collections.Generic;
using UnityEngine;
using WaterConveyorSort.LevelData;
using Object = UnityEngine.Object;

namespace WaterConveyorSort.BoardSystem.Buoys
{
    public sealed class BuoyStackHolderController
    {
        private readonly List<BuoyStackHolder> holders = new List<BuoyStackHolder>();
        public IReadOnlyList<BuoyStackHolder> Holders { get; }

        public BuoyStackHolderController()
        {
            Holders = holders.AsReadOnly();
        }

        public void InitHolders(BoardData board, IReadOnlyList<BuoyNodeData> nodes,
            ColorDataSO colors, Transform boardRoot,
            BuoyStackHolderVisual holderPrefab, BuoyStackVisual stackPrefab, BuoyVisual buoyPrefab)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (boardRoot == null) throw new ArgumentNullException(nameof(boardRoot));
            if (nodes.Count > 0 && (holderPrefab == null || stackPrefab == null || buoyPrefab == null))
                throw new InvalidOperationException("Assign holder, stack and buoy visual prefabs.");
            foreach (BuoyNodeData node in nodes)
            {
                if (node == null || node.GridPosition.x < 0 || node.GridPosition.x >= board.Width ||
                    node.GridPosition.y < 0 || node.GridPosition.y >= board.Height)
                    throw new ArgumentException("A buoy node is null or outside the board.", nameof(nodes));
            }

            Clear();
            try
            {
                foreach (BuoyNodeData node in nodes)
                {
                    BuoyStackHolderVisual visual = Object.Instantiate(holderPrefab, boardRoot);
                    try
                    {
                        visual.name = $"Holder_{node.GridPosition.x}_{node.GridPosition.y}";
                        visual.transform.localPosition = BoardCoordinates.CellToLocal(board, node.GridPosition);
                        visual.transform.localRotation = Quaternion.identity;
                        var holder = new BuoyStackHolder(node, visual);
                        holders.Add(holder);
                        // Spawn every column for now; queue advancement will be implemented separately.
                        for (int i = 0; i < node.Columns.Count; i++)
                            CreateStack(holder, visual.transform, node.Columns[i], i, colors, stackPrefab, buoyPrefab);
                    }
                    catch { visual.Release(); throw; }
                }
            }
            catch { Clear(); throw; }
        }

        private static void CreateStack(BuoyStackHolder holder, Transform parent, BuoyColumnData source,
            int index, ColorDataSO colors, BuoyStackVisual stackPrefab, BuoyVisual buoyPrefab)
        {
            BuoyStackVisual visual = Object.Instantiate(stackPrefab, parent);
            try
            {
                visual.name = $"Stack_{index}";
                var stack = new BuoyStack(source, visual);
                holder.AddStack(stack, visual);
                for (int i = 0; i < source.Buoys.Count; i++)
                {
                    BuoyVisual buoyVisual = Object.Instantiate(buoyPrefab, visual.transform);
                    try
                    {
                        buoyVisual.name = $"Buoy_{i}";
                        var buoy = new Buoy(source.Buoys[i], colors, buoyVisual);
                        stack.AddBuoy(buoy);
                    }
                    catch { buoyVisual.Release(); throw; }
                }
            }
            catch { visual.Release(); throw; }
        }

        public void Clear()
        {
            foreach (BuoyStackHolder holder in holders) holder.Clear();
            holders.Clear();
        }
    }
}
