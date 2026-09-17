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

        public void ConfigurePathSlots(float spacing, float minimumGap)
        {
            slotSpacing = Mathf.Max(0.01f, spacing);
            groupGap = Mathf.Max(0.01f, minimumGap);
        }

        public void RequestEntry(Vector2Int outletCell, float spacing, Action<ConveyorBuoyGroup> accepted)
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
            waiting.Add(new EnterRequest { Distance = closed ? Mathf.Repeat(_pathMoveSlots[nearest].Distance, length) : _pathMoveSlots[nearest].Distance, Spacing = spacing, Accepted = accepted });
        }

        private void ProcessEntries()
        {
            // Independent entry points can proceed; overlapping requests retain arrival order.
            for (int index = 0; index < waiting.Count;)
            {
                EnterRequest request = waiting[index];
                bool blocked = false;
                foreach (GroupPosition position in positions)
                {
                    if (EntryDistance(position.Distance, request.Distance) < groupGap)
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
                var visual = new GameObject("ConveyorBuoyGroup").AddComponent<ConveyorBuoyGroupVisual>();
                visual.transform.SetParent(root, false);
                visual.transform.rotation = root.rotation;
                var group = new ConveyorBuoyGroup(visual, PercentAt(request.Distance), request.Spacing);
                groups.Add(group);
                positions.Add(new GroupPosition { Group = group, Distance = request.Distance });
                PlaceGroup(positions[positions.Count - 1]);
                request.Accepted(group);
            }
        }

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
            position.Group.Visual.transform.position = splineComputer.Evaluate(position.Group.Percent).position + root.up * rootYOffset;
        }

        private void Update()
        {
            if (_pathMoveSlots.Count < 2) return;
            positions.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            float step = MoveSpeed * Time.deltaTime;
            foreach (GroupPosition position in positions)
                position.Step = position.Group.Moving ? (closed ? step : Mathf.Min(step, length - position.Distance)) : 0f;
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
        }
        private sealed class EnterRequest
        {
            public float Distance, Spacing;
            public Action<ConveyorBuoyGroup> Accepted;
        }

        public void ClearGroups()
        {
            foreach (ConveyorBuoyGroup group in groups) group.Clear();
            groups.Clear();
            positions.Clear();
            waiting.Clear();
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
