using System;
using System.Collections.Generic;
using WaterConveyorSort.BoardSystem.Buoys;
using WaterConveyorSort.BoardSystem.Conveyor;
using WaterConveyorSort.LevelData;
using WaterConveyorSort.InputHandling;

namespace WaterConveyorSort.BoardSystem.StackQueue
{
    public sealed class StackQueueController
    {
        private readonly List<BuoyStack> stacks = new List<BuoyStack>();
        private readonly List<StackReservation> reservations = new List<StackReservation>();
        private readonly ConveyorController conveyor;
        private readonly BuoyTransferController transfers;
        private readonly StackQueueVisual visual;
        private readonly InputSystem inputSystem;
        private readonly StackQueueExit exit;
        private bool accepting;
        private bool failed;
        public IReadOnlyList<BuoyStack> Stacks { get; }
        public event Action Full;

        public StackQueueController(ConveyorController conveyor, BuoyTransferController transfers, StackQueueVisual visual,
            InputSystem inputSystem, StackQueueExit exit)
        {
            this.conveyor = conveyor ?? throw new ArgumentNullException(nameof(conveyor));
            this.transfers = transfers ?? throw new ArgumentNullException(nameof(transfers));
            this.visual = visual != null ? visual : throw new ArgumentNullException(nameof(visual));
            this.inputSystem = inputSystem ?? throw new ArgumentNullException(nameof(inputSystem));
            this.exit = exit;
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
                        stackVisual.BindInput(stack, inputSystem);
                        stack.Clicked += OnStackClicked;
                    }
                    catch { stackVisual.Release(); throw; }
                }
                failed = false;
                accepting = true;
            }
            catch { Clear(); throw; }
        }

        public void SetPaused(bool paused)
        {
            accepting = !paused && !failed && stacks.Count > 0;
            for (int i = 0; i < stacks.Count; i++) RefreshInput(i);
        }
        private void RefreshInput(int index)
        {
            StackReservation reservation = reservations[index];
            stacks[index].CanReceiveInput = accepting && !transfers.IsPaused && !reservation.Outgoing &&
                reservation.Incoming == 0 && stacks[index].Buoys.Count > 0;
        }
        private void OnStackClicked(BuoyStack stack)
        {
            int index = stacks.IndexOf(stack);
            if (index < 0 || !accepting || transfers.IsPaused || !stack.CanReceiveInput) return;
            StackReservation reservation = reservations[index];
            int count = stack.GetTopGroup().Count;
            if (count == 0) return;
            reservation.Outgoing = true;
            // Future incoming groups reserve slots after the departing top group.
            reservation.Count -= count;
            RefreshInput(index);
            transfers.BeginFromQueue(stack, () => FinishExport(index, reservation),
                group => group.DepartureExit = exit);
        }
        private void FinishExport(int index, StackReservation reservation)
        {
            reservation.Outgoing = false;
            foreach (PendingReceive pending in reservation.Pending)
            {
                pending.Group.IsReceiving = false;
                StartReceive(index, reservation, pending.Group, pending.BaseIndex);
            }
            reservation.Pending.Clear();
            RefreshInput(index);
        }
        private void StartReceive(int index, StackReservation reservation, ConveyorBuoyGroup group, int baseIndex)
        {
            transfers.BeginReceive(stacks[index], group, baseIndex, () =>
            {
                reservation.Incoming--;
                RefreshInput(index);
            }, visual.ReceiveFlightDuration, visual.ReceiveLaunchDelay);
        }

        public bool TryReceive(ConveyorBuoyGroup group)
        {
            if (!accepting || transfers.IsPaused || group == null || !group.IsLoaded || group.IsReceiving || group.Buoys.Count == 0 || (exit != null && group.DepartureExit == exit))
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
            reservation.Incoming++;
            RefreshInput(index);
            conveyor.DetachGroup(group);
            if (reservation.Outgoing)
            {
                group.IsReceiving = true;
                reservation.Pending.Add(new PendingReceive { Group = group, BaseIndex = baseIndex });
            }
            else StartReceive(index, reservation, group, baseIndex);
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
            foreach (BuoyStack stack in stacks)
            {
                stack.Clicked -= OnStackClicked;
                stack.Clear();
            }
            stacks.Clear();
            reservations.Clear();
        }

        private sealed class PendingReceive
        {
            public ConveyorBuoyGroup Group;
            public int BaseIndex;
        }
        private sealed class StackReservation
        {
            public bool Outgoing;
            public int Incoming;
            public readonly List<PendingReceive> Pending = new List<PendingReceive>();
            public int Count;
            public int ColorCode;
        }
    }
}
