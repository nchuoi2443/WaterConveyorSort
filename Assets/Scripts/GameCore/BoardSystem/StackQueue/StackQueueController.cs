using System;
using System.Collections.Generic;
using WaterConveyorSort.BoardSystem.Buoys;
using WaterConveyorSort.BoardSystem.Conveyor;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.BoardSystem.StackQueue
{
    public sealed class StackQueueController
    {
        private readonly List<BuoyStack> stacks = new List<BuoyStack>();
        private readonly List<StackReservation> reservations = new List<StackReservation>();
        private readonly ConveyorController conveyor;
        private readonly BuoyTransferController transfers;
        private readonly StackQueueVisual visual;
        private bool accepting;
        private bool failed;
        public IReadOnlyList<BuoyStack> Stacks { get; }
        public event Action Full;

        public StackQueueController(ConveyorController conveyor, BuoyTransferController transfers, StackQueueVisual visual)
        {
            this.conveyor = conveyor ?? throw new ArgumentNullException(nameof(conveyor));
            this.transfers = transfers ?? throw new ArgumentNullException(nameof(transfers));
            this.visual = visual != null ? visual : throw new ArgumentNullException(nameof(visual));
            Stacks = stacks.AsReadOnly();
        }

        public void Initialize(int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            visual.ValidateSetup();
            Clear();
            try
            {
                for (int i = 0; i < count; i++)
                {
                    BuoyStackVisual stackVisual = visual.SpawnStack(i, count);
                    try
                    {
                        var stack = new BuoyStack(BuoyColumnData.CreateEmpty(), stackVisual);
                        stacks.Add(stack);
                        reservations.Add(new StackReservation());
                    }
                    catch { stackVisual.Release(); throw; }
                }
                failed = false;
                accepting = true;
            }
            catch { Clear(); throw; }
        }

        public void SetPaused(bool paused) => accepting = !paused && !failed && stacks.Count > 0;

        public bool TryReceive(ConveyorBuoyGroup group)
        {
            if (!accepting || transfers.IsPaused || group == null || !group.IsLoaded || group.IsReceiving || group.Buoys.Count == 0)
                return false;
            int color = group.Buoys[0].ColorCode;
            if (!group.HasColor(color)) throw new InvalidOperationException("A conveyor group must contain one color.");
            int index = FindStack(color);
            if (index < 0)
            {
                failed = true;
                accepting = false;
                Full?.Invoke();
                return false;
            }
            StackReservation reservation = reservations[index];
            int baseIndex = reservation.Count;
            // Reserve the color and all destination indices before the first flight starts.
            reservation.ColorCode = color;
            reservation.Count += group.Buoys.Count;
            conveyor.DetachGroup(group);
            transfers.BeginReceive(stacks[index], group, baseIndex, null, visual.ReceiveFlightDuration, visual.ReceiveLaunchDelay);
            return true;
        }

        private int FindStack(int color)
        {
            for (int i = 0; i < stacks.Count; i++)
            {
                StackReservation reservation = reservations[i];
                if (reservation.Count == 0 || reservation.ColorCode == color) return i;
            }
            return -1;
        }

        public void Clear()
        {
            accepting = false;
            foreach (BuoyStack stack in stacks) stack.Clear();
            stacks.Clear();
            reservations.Clear();
        }

        private sealed class StackReservation
        {
            public int Count;
            public int ColorCode;
        }
    }
}
