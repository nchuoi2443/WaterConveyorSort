using System;
using System.Collections.Generic;
using UnityEngine;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.BoardSystem.Buoys
{
    public sealed class BuoyStackHolder
    {
        private readonly BuoyStackHolderVisual visual;
        private readonly List<BuoyStack> stacks = new List<BuoyStack>();
        public IReadOnlyList<BuoyStack> Stacks { get; }
        public BuoyStack ActiveStack => stacks.Count > 0 ? stacks[0] : null;
        public Vector2Int GridPosition { get; }
        public Vector2Int OutletDirection { get; }
        public Vector2Int OutletCell => GridPosition + OutletDirection;

        public BuoyStackHolder(BuoyNodeData source, BuoyStackHolderVisual visual)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (visual == null) throw new ArgumentNullException(nameof(visual));
            if (source.Columns.Count == 0)
                throw new ArgumentException("A holder requires at least one column.", nameof(source));
            this.visual = visual;
            Stacks = stacks.AsReadOnly();
            GridPosition = source.GridPosition;
            OutletDirection = source.OutletDirection;
            visual.SetOutletDirection(OutletDirection);
            visual.Refresh(source.Columns.Count);
        }

        internal void AddStack(BuoyStack stack, BuoyStackVisual stackVisual)
        {
            if (stack == null) throw new ArgumentNullException(nameof(stack));
            stacks.Add(stack);
            visual.PlaceStack(stackVisual.transform, stacks.Count - 1);
        }

        public void Clear()
        {
            foreach (BuoyStack stack in stacks) stack.Clear();
            stacks.Clear();
            if (visual != null) visual.Release();
        }
    }
}
