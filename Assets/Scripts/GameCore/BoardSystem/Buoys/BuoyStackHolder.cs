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
        private readonly Queue<BuoyColumnData> pending = new Queue<BuoyColumnData>();
        private Func<BuoyColumnData, BuoyStack> createStack;
        private BuoyTransferController transfer;
        private bool busy;
        private bool cleared;
        public IReadOnlyList<BuoyStack> Stacks { get; }
        public BuoyStack ActiveStack => stacks.Count > 0 ? stacks[0] : null;
        public int VisibleCapacity { get; }
        public Vector2Int GridPosition { get; }
        public Vector2Int OutletDirection { get; }
        public Vector2Int OutletCell => GridPosition + OutletDirection;

        public BuoyStackHolder(BuoyNodeData source, BuoyStackHolderVisual visual)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (visual == null) throw new ArgumentNullException(nameof(visual));
            if (source.Columns.Count == 0) throw new ArgumentException("A holder requires at least one column.", nameof(source));
            this.visual = visual;
            Stacks = stacks.AsReadOnly();
            VisibleCapacity = Mathf.Min(2, source.Columns.Count);
            GridPosition = source.GridPosition;
            OutletDirection = source.OutletDirection;
            foreach (BuoyColumnData column in source.Columns)
                if (column.Buoys.Count > 0) pending.Enqueue(column);
            visual.SetOutletDirection(OutletDirection);
            visual.Initialize(this, VisibleCapacity);
        }

        internal void InitializeStacks(Func<BuoyColumnData, BuoyStack> factory)
        {
            createStack = factory ?? throw new ArgumentNullException(nameof(factory));
            FillVisibleStacks();
            RefreshInput();
        }

        private void FillVisibleStacks()
        {
            while (stacks.Count < VisibleCapacity && pending.Count > 0)
            {
                BuoyStack stack = createStack(pending.Dequeue());
                stacks.Add(stack);
                stack.Clicked += OnStackClicked;
                visual.PlaceStack(stack.Visual.transform, stacks.Count - 1);
                stack.CanReceiveInput = false;
            }
        }

        internal bool ContainsGroup(ConveyorBuoyGroup group) => !cleared && visual != null && visual.ContainsGroup(group);
        internal bool CanReceive(ConveyorBuoyGroup group) => !cleared && !busy && transfer != null &&
            !transfer.IsPaused &&
            OutletDirection != Vector2Int.zero && ActiveStack != null && ActiveStack.Buoys.Count > 0 &&
            group != null && group.IsLoaded && !group.IsReceiving && group.DepartureHolder != this &&
            group.HasColor(ActiveStack.Buoys[ActiveStack.Buoys.Count - 1].ColorCode);

        internal bool TryReceive(ConveyorBuoyGroup group)
        {
            if (!CanReceive(group)) return false;
            busy = true;
            RefreshInput();
            transfer.BeginReceive(ActiveStack, group, FinishTransfer);
            return true;
        }

        public void InitializeTransfers(BuoyTransferController controller)
        {
            transfer = controller ?? throw new ArgumentNullException(nameof(controller));
            RefreshInput();
        }

        private void OnStackClicked(BuoyStack stack)
        {
            if (cleared || busy || stack != ActiveStack || transfer == null || transfer.IsPaused) return;
            busy = true;
            RefreshInput();
            try { transfer.Begin(stack, OutletCell, this, FinishTransfer); }
            catch { busy = false; RefreshInput(); throw; }
        }

        private void FinishTransfer()
        {
            if (cleared) return;
            if (ActiveStack == null || ActiveStack.Buoys.Count > 0)
            {
                busy = false;
                RefreshInput();
                return;
            }
            ActiveStack.Clicked -= OnStackClicked;
            ActiveStack.Clear();
            stacks.RemoveAt(0);
            Transform promoted = ActiveStack != null ? ActiveStack.Visual.transform : null;
            // The replacement appears in the rear slot while the previous rear stack advances.
            FillVisibleStacks();
            if (promoted != null) visual.TweenToFront(promoted, FinishQueueAdvance);
            else FinishQueueAdvance();
        }

        private void FinishQueueAdvance()
        {
            if (cleared) return;
            busy = false;
            RefreshInput();
        }

        private void RefreshInput()
        {
            for (int i = 0; i < stacks.Count; i++)
                stacks[i].CanReceiveInput = i == 0 && !busy && !cleared && transfer != null;
        }

        public void Clear()
        {
            if (cleared) return;
            cleared = true;
            busy = true;
            pending.Clear();
            createStack = null;
            transfer = null;
            foreach (BuoyStack stack in stacks) stack.Clear();
            stacks.Clear();
            if (visual != null) visual.Release();
        }
    }
}
