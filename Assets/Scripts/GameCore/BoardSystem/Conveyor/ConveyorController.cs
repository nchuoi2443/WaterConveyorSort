using Dreamteck.Splines;
using System;
using System.Collections.Generic;
using UnityEngine;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.BoardSystem.Conveyor
{
    public sealed class ConveyorController : MonoBehaviour
    {
        private SplineComputer splineComputer;
        private SplineMesh splineMesh;

        private float moveSpeed = 1f;
        private float rootYOffset = 0.2f;
        private bool paused;
        public event Action<ConveyorBuoyGroup> ReachedEnd;
        private int maxBuoyInConveyor = 5;
        public int CurrentGroupCount => positions.Count;
        public int MaxBuoyInConveyor => maxBuoyInConveyor;
        public event Action<int, int> GroupCountChanged;
        public void SetGroupCapacity(int maximum)
        {
            maxBuoyInConveyor = Mathf.Max(1, maximum);
            NotifyGroupCount();
        }
        private void NotifyGroupCount() => GroupCountChanged?.Invoke(CurrentGroupCount, maxBuoyInConveyor);
        public void SetPaused(bool value) => paused = value;
        public void Configure(SplineComputer computer, SplineMesh mesh)
        {
            splineComputer = computer;
            splineMesh = mesh;
        }
        public void SetMotionSettings(float speed, float yOffset)
        {
            moveSpeed = Mathf.Max(0f, speed);
            rootYOffset = yOffset;
        }
        public float MoveSpeed => Mathf.Max(0f, moveSpeed);
        private readonly List<ConveyorBuoyGroup> groups = new List<ConveyorBuoyGroup>();
        private BoardData board;
        private Transform root;
        private bool closed;
        private float length;
        private ConveyorBuilder conveyorBuilder;

        private readonly List<PathMoveSlot> _pathMoveSlots = new List<PathMoveSlot>();
        private readonly List<EnterRequest> waiting = new List<EnterRequest>();
        private readonly List<GroupPosition> positions = new List<GroupPosition>();
        private float slotSpacing = 0.3f;
        private float groupGap = 0.6f;
        internal void DetachGroup(ConveyorBuoyGroup group)
        {
            int removed = positions.RemoveAll(position => position.Group == group);
            group.Moving = false;
            if (removed > 0) NotifyGroupCount();
        }
        internal void RemoveGroup(ConveyorBuoyGroup group)
        {
            int removed = positions.RemoveAll(position => position.Group == group);
            groups.Remove(group);
            group.Clear();
            if (removed > 0) NotifyGroupCount();
        }

        public void ConfigurePathSlots(float spacing, float minimumGap)
        {
            slotSpacing = Mathf.Max(0.01f, spacing);
            groupGap = Mathf.Max(0.01f, minimumGap);
        }

        public void RequestEntry(Vector2Int outletCell, float spacing, Func<Vector3, float> estimateArrival, Action<ConveyorBuoyGroup> accepted)
        {
            if (_pathMoveSlots.Count < 2 || length < groupGap)
                throw new InvalidOperationException("Conveyor path is too short for the configured group gap.");
            Vector3 entry = root.TransformPoint(BoardCoordinates.CellToLocal(board, outletCell));
            double percent = splineComputer.Project(entry).percent;
            float distance = splineComputer.CalculateLength(percent, 1.0);
            // Snap entry to a sampled movement slot, preserving the configured backward direction.
            int nearest = 0;
            float best = float.MaxValue;
            for (int i = 0; i < _pathMoveSlots.Count; i++)
            {
                float delta = Mathf.Abs(_pathMoveSlots[i].Distance - distance);
                if (closed) delta = Mathf.Min(delta, length - delta);
                if (delta < best) { best = delta; nearest = i; }
            }
            waiting.Add(new EnterRequest { Distance = closed ? Mathf.Repeat(_pathMoveSlots[nearest].Distance, length) : _pathMoveSlots[nearest].Distance, Spacing = spacing, EstimateArrival = estimateArrival, Accepted = accepted });
        }

        public void RequestQueueEntry(float spacing, Func<Vector3, float> estimateArrival, Action<ConveyorBuoyGroup> accepted)
        {
            if (_pathMoveSlots.Count < 2 || length < groupGap)
                throw new InvalidOperationException("Conveyor path is too short for the configured group gap.");
            // The last authored path node is the start of backward conveyor movement.
            waiting.Add(new EnterRequest { Distance = 0f, Spacing = spacing,
                EstimateArrival = estimateArrival, Accepted = accepted });
        }

        private void ProcessEntries()
        {
            // Independent entry points can proceed; overlapping requests retain arrival order.
            for (int index = 0; index < waiting.Count;)
            {
                if (CurrentGroupCount >= maxBuoyInConveyor) break;
                EnterRequest request = waiting[index];
                Vector3 entry = splineComputer.Evaluate(PercentAt(request.Distance)).position + root.up * rootYOffset;
                float leadTime = Mathf.Max(0f, request.EstimateArrival(entry));
                bool blocked = false;
                foreach (GroupPosition position in positions)
                {
                    if (EntryDistance(position.Distance, request.Distance) >= groupGap) continue;
                    float ahead = position.Distance - request.Distance;
                    if (closed) ahead = Mathf.Repeat(ahead, length);
                    // Only an outgoing, moving group can clear space during the first flight.
                    // Rear traffic and other loading reservations must already be separated.
                    float measuredSpeed = Time.deltaTime > 0f ? position.Step / Time.deltaTime : 0f;
                    float predictedTravel = Mathf.Min(MoveSpeed, measuredSpeed) * leadTime;
                    if (!closed) predictedTravel = Mathf.Min(predictedTravel, length - position.Distance);
                    if (!position.Group.Moving || ahead < 0f || ahead >= groupGap ||
                        ahead + predictedTravel < groupGap)
                    {
                        blocked = true;
                        break;
                    }
                }
                if (!blocked)
                    for (int older = 0; older < index; older++)
                        if (EntryDistance(waiting[older].Distance, request.Distance) < groupGap)
                        {
                            blocked = true;
                            break;
                        }
                if (blocked) { index++; continue; }
                // Reserve before the callback so another request cannot claim the same space.
                waiting.RemoveAt(index);
                var group = new GameObject("ConveyorBuoyGroup").AddComponent<ConveyorBuoyGroup>();
                group.transform.SetParent(root, false);
                group.transform.rotation = root.rotation;
                group.Initialize(PercentAt(request.Distance), request.Spacing);
                groups.Add(group);
                positions.Add(new GroupPosition { Group = group, Distance = request.Distance });
                PlaceGroup(positions[positions.Count - 1]);
                NotifyGroupCount();
                request.Accepted(group);
            }
        }

        public bool CanReceiveFirst(ConveyorBuoyGroup group)
        {
            GroupPosition reservation = positions.Find(item => item.Group == group);
            if (reservation == null) return false;
            foreach (GroupPosition other in positions)
                if (other != reservation && EntryDistance(other.Distance, reservation.Distance) < groupGap - 0.0001f)
                    return false;
            return true;
        }

        public Vector3 GetEntryHoldingOffset() => root.up * groupGap;

        private float EntryDistance(float a, float b)
        {
            float distance = Mathf.Abs(a - b);
            return closed ? Mathf.Min(distance, length - distance) : distance;
        }

        private double PercentAt(float distance)
        {
            int low = 0, high = _pathMoveSlots.Count - 1;
            while (high - low > 1)
            {
                int mid = (low + high) / 2;
                if (_pathMoveSlots[mid].Distance <= distance) low = mid; else high = mid;
            }
            PathMoveSlot a = _pathMoveSlots[low], b = _pathMoveSlots[high];
            float t = Mathf.InverseLerp(a.Distance, b.Distance, distance);
            return a.Percent + (b.Percent - a.Percent) * t;
        }

        private void PlaceGroup(GroupPosition position)
        {
            position.Group.Percent = PercentAt(position.Distance);
            position.Group.transform.position = splineComputer.Evaluate(position.Group.Percent).position + root.up * rootYOffset;
        }

        private void Update()
        {
            if (paused || _pathMoveSlots.Count < 2) return;
            positions.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            float step = MoveSpeed * Time.deltaTime;
            foreach (GroupPosition position in positions)
            {
                position.Step = position.Group.Moving ? (closed ? step : Mathf.Min(step, length - position.Distance)) : 0f;
            }
            // Propagate a stopped/loading group's constraint backwards through the queue.
            // Simultaneous steps let a full moving loop advance without slot deadlock.
            for (int pass = 0; pass < positions.Count; pass++)
            {
                bool changed = false;
                for (int i = positions.Count - 1; i >= 0; i--)
                {
                    if (!closed && i == positions.Count - 1 || positions.Count < 2) continue;
                    GroupPosition current = positions[i], next = positions[(i + 1) % positions.Count];
                    float gap = next.Distance - current.Distance;
                    if (i == positions.Count - 1) gap += length;
                    float allowed = Mathf.Max(0f, gap - groupGap + next.Step);
                    if (current.Step > allowed) { current.Step = allowed; changed = true; }
                }
                if (!changed) break;
            }
            foreach (GroupPosition position in positions)
            {
                position.Distance += position.Step;
                if (closed) position.Distance = Mathf.Repeat(position.Distance, length);
                PlaceGroup(position);
            }
            ProcessEntries();
            if (!closed)
                for (int i = positions.Count - 1; i >= 0 && !paused; i--)
                {
                    GroupPosition position = positions[i];
                    if (position.Distance < length || position.EndNotified ||
                        !position.Group.IsLoaded || position.Group.IsReceiving) continue;
                    position.EndNotified = true;
                    ReachedEnd?.Invoke(position.Group);
                }
        }

        private sealed class PathMoveSlot
        {
            public float Distance;
            public double Percent;
        }
        private sealed class GroupPosition
        {
            public ConveyorBuoyGroup Group;
            public float Distance, Step;
            public bool EndNotified;
        }
        private sealed class EnterRequest
        {
            public float Distance, Spacing;
            public Func<Vector3, float> EstimateArrival;
            public Action<ConveyorBuoyGroup> Accepted;
        }

        public void ClearGroups()
        {
            foreach (ConveyorBuoyGroup group in groups) group.Clear();
            groups.Clear();
            positions.Clear();
            waiting.Clear();
            NotifyGroupCount();
        }
        private void OnDestroy() => ClearGroups();

        public void InitConveyor(BoardData boardData, PathData pathData, Transform boardRoot)
        {
            ClearGroups();
            board = boardData;
            root = boardRoot;
            closed = pathData.IsClosed;
            conveyorBuilder = new ConveyorBuilder(splineComputer, splineMesh);
            conveyorBuilder.BuildConveyor(boardData, pathData, boardRoot);
            length = splineComputer.CalculateLength();
            _pathMoveSlots.Clear();
            int segments = Mathf.Max(1, Mathf.CeilToInt(length / slotSpacing));
            for (int i = 0; i <= segments; i++)
            {
                float distance = length * i / segments;
                _pathMoveSlots.Add(new PathMoveSlot { Distance = distance,
                    Percent = i == segments ? 0.0 : splineComputer.Travel(1.0, distance, Spline.Direction.Backward) });
            }
        }
    }
}
