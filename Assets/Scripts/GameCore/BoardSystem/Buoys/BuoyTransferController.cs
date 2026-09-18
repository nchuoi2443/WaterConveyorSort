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
        private readonly List<ReceiveTransfer> awaitingConsumeCompletion = new List<ReceiveTransfer>();
        private readonly List<BuoyStack> consumingStacks = new List<BuoyStack>();
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
            BeginReceive(stack, group, stack.Buoys.Count, completed, descentDuration: stack.Visual.ReceiveDescentDuration,
                topClearance: stack.Visual.ReceiveTopClearance);
        }
        internal void BeginReceive(BuoyStack stack, ConveyorBuoyGroup group, int baseIndex,
            Action completed, float? duration = null, float? delay = null, Action<int> consumed = null, float descentDuration = 0f, float topClearance = 0f)
        {
            if (IsPaused || group.IsReceiving) throw new InvalidOperationException("The group cannot start another receive transfer.");
            group.IsReceiving = true;
            var transfer = new ReceiveTransfer { Stack = stack, Group = group, Completed = completed, Consumed = consumed,
                BaseIndex = baseIndex, Count = group.Buoys.Count,
                Duration = Mathf.Max(0.01f, duration ?? receiveFlightDuration),
                Delay = Mathf.Max(0f, delay ?? receiveLaunchDelay),
                DescentDuration = Mathf.Max(0f, descentDuration), TopClearance = Mathf.Max(0f, topClearance) };
            stack.Visual.BeginReceiveHeight(baseIndex + transfer.Count);
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
            for (int i = consumingStacks.Count - 1; i >= 0; i--)
            {
                consumingStacks[i].Visual.TickConsume(deltaTime);
                if (!consumingStacks[i].IsConsuming) consumingStacks.RemoveAt(i);
            }
            int tickGeneration = generation;
            for (int i = 0; i < awaitingConsumeCompletion.Count;)
            {
                ReceiveTransfer completed = awaitingConsumeCompletion[i];
                if (completed.Stack.IsConsuming) { i++; continue; }
                awaitingConsumeCompletion.RemoveAt(i);
                completed.Completed?.Invoke();
                if (generation != tickGeneration || IsPaused) return;
            }
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
                    buoy.Visual.BeginFlight();
                    Vector3 start = buoy.Visual.transform.position;
                    transfer.Flights.Add(new Flight { Buoy = buoy, Slot = transfer.Next++, StartPosition = start,
                        Up = transfer.Stack.Visual.transform.up,
                        Duration = Mathf.Max(0.01f, Vector3.Distance(start, transfer.Group.GetSlotPosition(transfer.Next - 1)) /
                            Mathf.Max(speed, conveyor.MoveSpeed + 0.5f)) });
                    transfer.Timer = Mathf.Max(0f, interval);
                }
                foreach (Flight flight in transfer.Flights)
                {
                    if (flight.Arrived) continue;
                    Vector3 target = transfer.Group.GetSlotPosition(flight.Slot);
                    bool canJoin = flight.Slot > 0 || conveyor.CanReceiveFirst(transfer.Group);
                    flight.Elapsed += Mathf.Max(0f, deltaTime);
                    float progress = Mathf.Clamp01(flight.Elapsed / flight.Duration);
                    if (!canJoin)
                    {
                        // Hold above occupied traffic until the reserved entry is safe.
                        target += conveyor.GetEntryHoldingOffset();
                        progress = Mathf.Min(progress, 0.95f);
                    }
                    flight.Buoy.Visual.transform.position = EvaluateArc(flight.StartPosition, target,
                        flight.Up, progress, flight.Buoy.Visual.FlightArcHeight);
                    flight.Buoy.Visual.SetFlightProgress(progress);
                    if (progress < 1f || !canJoin || flight.Slot != transfer.Arrived) continue;
                    flight.Buoy.Visual.EndFlight();
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
                buoy.Visual.BeginFlight();
                Vector3 target = transfer.Stack.Visual.GetBuoyPosition(transfer.BaseIndex + transfer.Next);
                Vector3 top = transfer.Stack.Visual.GetTopPosition();
                Vector3 up = transfer.Stack.Visual.transform.up;
                float rise = Mathf.Max(0f, Vector3.Dot(top - target, up)) + transfer.TopClearance;
                transfer.Flights.Add(new ReceiveFlight { Buoy = buoy, Slot = transfer.Next,
                    StartPosition = buoy.Visual.transform.position, ApexPosition = target + up * rise, LaunchTime = transfer.Next * transfer.Delay });
                transfer.Next++;
            }
        }
        private void TickReceives(float deltaTime)
        {
            for (int i = receives.Count - 1; i >= 0; i--)
            {
                ReceiveTransfer transfer = receives[i];
                transfer.Elapsed += Mathf.Max(0f, deltaTime);
                transfer.Stack.Visual.TickReceiveHeight(deltaTime);
                LaunchReceiveFlights(transfer);
                foreach (ReceiveFlight flight in transfer.Flights)
                {
                    if (flight.Arrived) continue;
                    Vector3 target = transfer.Stack.Visual.GetBuoyPosition(transfer.BaseIndex + flight.Slot);
                    float progress = Mathf.Clamp01((transfer.Elapsed - flight.LaunchTime) / transfer.Duration);
                    flight.Buoy.Visual.SetFlightProgress(progress);
                    if (transfer.DescentDuration > 0f)
                    {
                        if (progress < 1f)
                        {
                            flight.Buoy.Visual.transform.position = EvaluateArc(flight.StartPosition, flight.ApexPosition, transfer.Stack.Visual.transform.up, progress, flight.Buoy.Visual.FlightArcHeight);
                            continue;
                        }
                        float descent = Mathf.Clamp01((transfer.Elapsed - flight.LaunchTime - transfer.Duration) / transfer.DescentDuration);
                        flight.Buoy.Visual.transform.position = Vector3.Lerp(flight.ApexPosition, target, Mathf.SmoothStep(0f, 1f, descent));
                        if (descent < 1f) continue;
                    }
                    else
                    {
                        flight.Buoy.Visual.transform.position = EvaluateArc(flight.StartPosition, target, transfer.Stack.Visual.transform.up, progress, flight.Buoy.Visual.FlightArcHeight);
                        if (progress < 1f) continue;
                    }
                    // Commit landings in launch order, even if a later flight catches up.
                    if (flight.Slot != transfer.Arrived) continue;
                    if (transfer.Stack.Buoys.Count != transfer.BaseIndex + flight.Slot) continue;
                    flight.Buoy.Visual.EndFlight();
                    transfer.Stack.AddBuoy(flight.Buoy);
                    flight.Arrived = true;
                    transfer.Arrived++;
                }
                if (transfer.Arrived != transfer.Count) continue;
                receives.RemoveAt(i);
                conveyor.RemoveGroup(transfer.Group);
                transfer.Stack.Visual.EndReceiveHeight();
                int consumed = transfer.Stack.ConsumeTopGroups();
                if (consumed > 0)
                {
                    if (!consumingStacks.Contains(transfer.Stack)) consumingStacks.Add(transfer.Stack);
                    // Keep later reserved destinations aligned after removing top buoys.
                    foreach (ReceiveTransfer pending in receives)
                        if (pending.Stack == transfer.Stack) pending.BaseIndex -= consumed;
                    transfer.Consumed?.Invoke(consumed);
                }
                // A holder may clear its empty stack in this callback, so keep it busy
                // until every consume animation on that stack has released its buoys.
                if (transfer.Stack.IsConsuming) awaitingConsumeCompletion.Add(transfer);
                else transfer.Completed?.Invoke();
            }
        }
        private static Vector3 EvaluateArc(Vector3 start, Vector3 target, Vector3 up, float progress, float height)
        {
            // Account for different endpoint heights so the buoy rises before descending.
            float arcHeight = Mathf.Max(height, Mathf.Abs(Vector3.Dot(target - start, up)) * 0.5f + 0.1f);
            return Vector3.Lerp(start, target, progress) + up * (4f * arcHeight * progress * (1f - progress));
        }
        public void Clear()
        {
            generation++;
            consumingStacks.Clear();
            awaitingConsumeCompletion.Clear();
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
            public Action<int> Consumed;
            public BuoyStack Stack;
            public ConveyorBuoyGroup Group;
            public Action Completed;
            public readonly List<ReceiveFlight> Flights = new List<ReceiveFlight>();
            public int BaseIndex, Count, Next, Arrived;
            public float Elapsed, Duration, Delay, DescentDuration, TopClearance;
        }
        private sealed class ReceiveFlight
        {
            public Buoy Buoy;
            public int Slot;
            public Vector3 StartPosition, ApexPosition;
            public float LaunchTime;
            public bool Arrived;
        }
        private sealed class Flight
        {
            public Vector3 StartPosition, Up;
            public float Elapsed, Duration;
            public Buoy Buoy;
            public int Slot;
            public bool Arrived;
        }
    }
}
