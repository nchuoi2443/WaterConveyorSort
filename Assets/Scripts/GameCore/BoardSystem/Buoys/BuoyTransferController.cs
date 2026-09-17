using System;
using System.Collections.Generic;
using UnityEngine;
using WaterConveyorSort.BoardSystem.Conveyor;

namespace WaterConveyorSort.BoardSystem.Buoys
{
    public sealed class BuoyTransferController
    {
        private readonly List<Transfer> transfers = new List<Transfer>();
        private readonly List<ReceiveTransfer> receives = new List<ReceiveTransfer>();
        private readonly ConveyorController conveyor;
        private int generation;
        private float flightSpeed = 4f;
        private float receiveFlightDuration = 0.4f;
        private float receiveLaunchDelay = 0.12f;
        public bool IsPaused { get; private set; }
        public void SetPaused(bool paused) => IsPaused = paused;
        public void SetReceiveSettings(float duration, float delay)
        {
            receiveFlightDuration = Mathf.Max(0.01f, duration);
            receiveLaunchDelay = Mathf.Max(0f, delay);
        }
        public BuoyTransferController(ConveyorController conveyor) { this.conveyor = conveyor; }
        internal void BeginReceive(BuoyStack stack, ConveyorBuoyGroup group, Action completed)
        {
            BeginReceive(stack, group, stack.Buoys.Count, completed);
        }
        internal void BeginReceive(BuoyStack stack, ConveyorBuoyGroup group, int baseIndex,
            Action completed, float? duration = null, float? delay = null)
        {
            if (IsPaused || group.IsReceiving) throw new InvalidOperationException("The group cannot start another receive transfer.");
            group.IsReceiving = true;
            var transfer = new ReceiveTransfer { Stack = stack, Group = group, Completed = completed,
                BaseIndex = baseIndex, Count = group.Buoys.Count,
                Duration = Mathf.Max(0.01f, duration ?? receiveFlightDuration),
                Delay = Mathf.Max(0f, delay ?? receiveLaunchDelay) };
            receives.Add(transfer);
            LaunchReceiveFlights(transfer);
        }
        public void Begin(BuoyStack stack, Vector2Int outlet, BuoyStackHolder sourceHolder, Action completed)
        {
            BeginExport(stack, sourceHolder, completed, (spacing, estimate, accepted) =>
                conveyor.RequestEntry(outlet, spacing, estimate, accepted));
        }
        internal void BeginFromQueue(BuoyStack stack, Action completed, Action<ConveyorBuoyGroup> departing)
        {
            BeginExport(stack, null, completed, conveyor.RequestQueueEntry, departing);
        }
        private void BeginExport(BuoyStack stack, BuoyStackHolder sourceHolder, Action completed,
            Action<float, Func<Vector3, float>, Action<ConveyorBuoyGroup>> requestEntry,
            Action<ConveyorBuoyGroup> departing = null)
        {
            List<Buoy> selected = stack.GetTopGroup();
            if (selected.Count == 0) { completed(); return; }
            int requestGeneration = generation;
            requestEntry(stack.Visual.Step,
                target => Vector3.Distance(selected[0].Visual.transform.position, target) /
                    Mathf.Max(flightSpeed, conveyor.MoveSpeed + 0.5f), group =>
            {
                if (generation != requestGeneration) return;
                group.DepartureHolder = sourceHolder;
                departing?.Invoke(group);
                transfers.Add(new Transfer { Stack = stack, Selected = selected, Group = group, Completed = completed,
                    ExpectedArrival = Vector3.Distance(selected[0].Visual.transform.position, group.GetSlotPosition(0)) /
                        Mathf.Max(flightSpeed, conveyor.MoveSpeed + 0.5f) });
            });
        }
        public void Tick(float deltaTime, float speed, float interval)
        {
            if (IsPaused) return;
            flightSpeed = speed;
            for (int t = transfers.Count - 1; t >= 0; t--)
            {
                Transfer transfer = transfers[t];
                transfer.Timer -= deltaTime;
                transfer.Elapsed += deltaTime;
                if (transfer.Next < transfer.Selected.Count && transfer.Timer <= 0f)
                {
                    Buoy buoy = transfer.Selected[transfer.Next];
                    transfer.Stack.RemoveTop(buoy);
                    transfer.Flights.Add(new Flight { Buoy = buoy, Slot = transfer.Next++ });
                    transfer.Timer = Mathf.Max(0f, interval);
                }
                foreach (Flight flight in transfer.Flights)
                {
                    if (flight.Arrived) continue;
                    Vector3 target = transfer.Group.GetSlotPosition(flight.Slot);
                    bool canJoin = flight.Slot > 0 || conveyor.CanReceiveFirst(transfer.Group);
                    if (!canJoin && transfer.Elapsed >= transfer.ExpectedArrival)
                    {
                        // If the forecast changes, wait above the entry instead of overlapping traffic.
                        Vector3 holding = target + conveyor.GetEntryHoldingOffset();
                        flight.Buoy.Visual.MoveTowards(holding, Mathf.Max(speed, conveyor.MoveSpeed + 0.5f) * deltaTime);
                        continue;
                    }
                    if (!flight.Buoy.Visual.MoveTowards(target, Mathf.Max(speed, conveyor.MoveSpeed + 0.5f) * deltaTime)) continue;
                    if (!canJoin) continue;
                    transfer.Group.Receive(flight.Buoy, flight.Slot);
                    flight.Arrived = true;
                    transfer.Arrived++;
                }
                if (transfer.Arrived != transfer.Selected.Count) continue;
                transfers.RemoveAt(t);
                transfer.Group.IsLoaded = true;
                transfer.Completed();
            }
            TickReceives(deltaTime);
        }
        private static void LaunchReceiveFlights(ReceiveTransfer transfer)
        {
            // Use absolute launch times to preserve timing through slow frames and zero delay.
            while (transfer.Next < transfer.Count && transfer.Next * transfer.Delay <= transfer.Elapsed)
            {
                Buoy buoy = transfer.Group.TakeTop();
                transfer.Flights.Add(new ReceiveFlight { Buoy = buoy, Slot = transfer.Next,
                    StartPosition = buoy.Visual.transform.position, LaunchTime = transfer.Next * transfer.Delay });
                transfer.Next++;
            }
        }
        private void TickReceives(float deltaTime)
        {
            for (int i = receives.Count - 1; i >= 0; i--)
            {
                ReceiveTransfer transfer = receives[i];
                transfer.Elapsed += Mathf.Max(0f, deltaTime);
                LaunchReceiveFlights(transfer);
                foreach (ReceiveFlight flight in transfer.Flights)
                {
                    if (flight.Arrived) continue;
                    Vector3 target = transfer.Stack.Visual.GetBuoyPosition(transfer.BaseIndex + flight.Slot);
                    float progress = Mathf.Clamp01((transfer.Elapsed - flight.LaunchTime) / transfer.Duration);
                    flight.Buoy.Visual.transform.position = Vector3.Lerp(flight.StartPosition, target, progress);
                    if (progress < 1f) continue;
                    // Commit landings in launch order, even if a later flight catches up.
                    if (flight.Slot != transfer.Arrived) continue;
                    if (transfer.Stack.Buoys.Count != transfer.BaseIndex + flight.Slot) continue;
                    transfer.Stack.AddBuoy(flight.Buoy);
                    flight.Arrived = true;
                    transfer.Arrived++;
                }
                if (transfer.Arrived != transfer.Count) continue;
                receives.RemoveAt(i);
                conveyor.RemoveGroup(transfer.Group);
                transfer.Completed?.Invoke();
            }
        }
        public void Clear()
        {
            generation++;
            foreach (ReceiveTransfer transfer in receives)
                foreach (ReceiveFlight flight in transfer.Flights)
                    if (!flight.Arrived) flight.Buoy.Clear();
            receives.Clear();
            foreach (Transfer transfer in transfers)
                foreach (Flight flight in transfer.Flights)
                    if (!flight.Arrived) flight.Buoy.Clear();
            transfers.Clear();
        }
        private sealed class Transfer
        {
            public BuoyStack Stack;
            public List<Buoy> Selected;
            public ConveyorBuoyGroup Group;
            public Action Completed;
            public readonly List<Flight> Flights = new List<Flight>();
            public int Next, Arrived;
            public float Timer, Elapsed, ExpectedArrival;
        }
        private sealed class ReceiveTransfer
        {
            public BuoyStack Stack;
            public ConveyorBuoyGroup Group;
            public Action Completed;
            public readonly List<ReceiveFlight> Flights = new List<ReceiveFlight>();
            public int BaseIndex, Count, Next, Arrived;
            public float Elapsed, Duration, Delay;
        }
        private sealed class ReceiveFlight
        {
            public Buoy Buoy;
            public int Slot;
            public Vector3 StartPosition;
            public float LaunchTime;
            public bool Arrived;
        }
        private sealed class Flight
        {
            public Buoy Buoy;
            public int Slot;
            public bool Arrived;
        }
    }
}
