using System;
using System.Collections.Generic;
using UnityEngine;
using WaterConveyorSort.LevelData;
using WaterConveyorSort.BoardSystem.Conveyor;

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

        private BuoyTransferController transfer;
        private bool busy;
        internal bool CanReceive(ConveyorBuoyGroup group) => !busy && transfer != null &&
            ActiveStack != null && ActiveStack.Buoys.Count > 0 && group.IsLoaded &&
            group.HasColor(ActiveStack.Buoys[ActiveStack.Buoys.Count - 1].ColorCode);

        internal bool TryReceive(ConveyorBuoyGroup group)
        {
            if (!CanReceive(group)) return false;
            busy = true;
            ActiveStack.CanReceiveInput = false;
            transfer.BeginReceive(ActiveStack, group, FinishTransfer);
            return true;
        }
        public void InitializeTransfers(BuoyTransferController controller)
        {
            transfer = controller;
            foreach (BuoyStack stack in stacks) stack.Clicked += OnStackClicked;
            AdvanceQueue();
        }
        private void OnStackClicked(BuoyStack stack)
        {
            if (busy || stack != ActiveStack || transfer == null) return;
            busy = true;
            stack.CanReceiveInput = false;
            try { transfer.Begin(stack, OutletCell, FinishTransfer); }
            catch { busy = false; stack.CanReceiveInput = true; throw; }
        }
        private void FinishTransfer()
        {
            busy = false;
            AdvanceQueue();
        }
        private void AdvanceQueue()
        {
            while (ActiveStack != null && ActiveStack.Buoys.Count == 0)
            {
                ActiveStack.Clicked -= OnStackClicked;
                ActiveStack.Clear();
                stacks.RemoveAt(0);
            }
            for (int i = 0; i < stacks.Count; i++)
            {
                visual.PlaceStack(stacks[i].Visual.transform, i);
                stacks[i].CanReceiveInput = i == 0 && !busy;
            }
            visual.Refresh(stacks.Count);
        }

        public void Clear()
        {
            foreach (BuoyStack stack in stacks) stack.Clear();
            stacks.Clear();
            if (visual != null) visual.Release();
        }
    }
}
