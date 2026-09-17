using System;
using System.Collections.Generic;
using UnityEngine;
using WaterConveyorSort.BoardSystem.Conveyor;

namespace WaterConveyorSort.BoardSystem.Buoys
{
    public sealed class BuoyTransferController
    {
        private readonly List<Transfer> transfers = new List<Transfer>();
        private readonly ConveyorController conveyor;
        private int generation;
        public BuoyTransferController(ConveyorController conveyor) { this.conveyor = conveyor; }
        public void Begin(BuoyStack stack, Vector2Int outlet, Action completed)
        {
            List<Buoy> selected = stack.GetTopGroup();
            if (selected.Count == 0) { completed(); return; }
            int requestGeneration = generation;
            conveyor.RequestEntry(outlet, stack.Visual.Step, group =>
            {
                if (generation != requestGeneration) return;
                transfers.Add(new Transfer { Stack = stack, Selected = selected, Group = group, Completed = completed });
            });
        }
        public void Tick(float deltaTime, float speed, float interval)
        {
            for (int t = transfers.Count - 1; t >= 0; t--)
            {
                Transfer transfer = transfers[t];
                transfer.Timer -= deltaTime;
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
                    if (!flight.Buoy.Visual.MoveTowards(target, Mathf.Max(speed, conveyor.MoveSpeed + 0.5f) * deltaTime)) continue;
                    transfer.Group.Receive(flight.Buoy, flight.Slot);
                    flight.Arrived = true;
                    transfer.Arrived++;
                }
                if (transfer.Arrived != transfer.Selected.Count) continue;
                transfers.RemoveAt(t);
                transfer.Completed();
            }
        }
        public void Clear()
        {
            generation++;
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
