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
        public BuoyTransferController(ConveyorController conveyor) { this.conveyor = conveyor; }
        internal void BeginReceive(BuoyStack stack, ConveyorBuoyGroup group, Action completed)
        {
            group.Moving = false;
            receives.Add(new ReceiveTransfer { Stack = stack, Group = group, Completed = completed });
        }
        public void Begin(BuoyStack stack, Vector2Int outlet, Action completed)
        {
            List<Buoy> selected = stack.GetTopGroup();
            if (selected.Count == 0) { completed(); return; }
            int requestGeneration = generation;
            conveyor.RequestEntry(outlet, stack.Visual.Step,
                target => Vector3.Distance(selected[0].Visual.transform.position, target) /
                    Mathf.Max(flightSpeed, conveyor.MoveSpeed + 0.5f), group =>
            {
                if (generation != requestGeneration) return;
                transfers.Add(new Transfer { Stack = stack, Selected = selected, Group = group, Completed = completed,
                    ExpectedArrival = Vector3.Distance(selected[0].Visual.transform.position, group.GetSlotPosition(0)) /
                        Mathf.Max(flightSpeed, conveyor.MoveSpeed + 0.5f) });
            });
        }
        public void Tick(float deltaTime, float speed, float interval)
        {
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
                    // Keep later buoys on their source until the first buoy has safely joined.
                    if (flight.Slot > 0 && !transfer.Group.Moving) continue;
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
            TickReceives(deltaTime, speed, interval);
        }
        private void TickReceives(float deltaTime, float speed, float interval)
        {
            for (int i = receives.Count - 1; i >= 0; i--)
            {
                ReceiveTransfer transfer = receives[i];
                transfer.Timer -= deltaTime;
                // Land in order so each buoy targets the next free position on the stack.
                if (transfer.InFlight == null && transfer.Group.Buoys.Count > 0 && transfer.Timer <= 0f)
                {
                    transfer.InFlight = transfer.Group.TakeTop();
                    transfer.Timer = Mathf.Max(0f, interval);
                }
                if (transfer.InFlight != null)
                {
                    Vector3 target = transfer.Stack.Visual.GetBuoyPosition(transfer.Stack.Buoys.Count);
                    if (!transfer.InFlight.Visual.MoveTowards(target, Mathf.Max(0.01f, speed) * deltaTime)) continue;
                    transfer.Stack.AddBuoy(transfer.InFlight);
                    transfer.InFlight = null;
                }
                if (transfer.Group.Buoys.Count > 0) continue;
                receives.RemoveAt(i);
                conveyor.RemoveGroup(transfer.Group);
                transfer.Completed();
            }
        }
        public void Clear()
        {
            generation++;
            foreach (ReceiveTransfer transfer in receives)
                transfer.InFlight?.Clear();
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
            public Buoy InFlight;
            public float Timer;
        }
        private sealed class Flight
        {
            public Buoy Buoy;
            public int Slot;
            public bool Arrived;
        }
    }
}
