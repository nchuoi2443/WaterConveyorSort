using System;
using System.Collections.Generic;
using UnityEngine;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.BoardSystem.Buoys
{
    public sealed class BuoyStack
    {
        private readonly BuoyStackVisual visual;
        private readonly List<Buoy> buoys = new List<Buoy>();
        private readonly List<ConsumeAnimation> consumeAnimations = new List<ConsumeAnimation>();
        internal bool IsConsuming => consumeAnimations.Count > 0;
        private bool cleared;
        internal BuoyStackVisual Visual => visual;
        private bool canReceiveInput;
        public bool CanReceiveInput
        {
            get => !cleared && canReceiveInput;
            set { canReceiveInput = value; visual.SetInputEnabled(value && !cleared); }
        }
        public List<Buoy> GetTopGroup()
        {
            var result = new List<Buoy>();
            if (buoys.Count == 0) return result;
            int color = buoys[buoys.Count - 1].ColorCode;
            for (int i = buoys.Count - 1; i >= 0 && buoys[i].ColorCode == color; i--) result.Add(buoys[i]);
            return result;
        }
        internal void RemoveTop(Buoy buoy)
        {
            if (buoys.Count == 0 || buoys[buoys.Count - 1] != buoy)
                throw new InvalidOperationException("Only the top buoy can leave a stack.");
            buoys.RemoveAt(buoys.Count - 1);
            visual.RefreshHeight(buoys.Count);
        }
        internal int ConsumeTopGroups()
        {
            const int groupSize = 5;
            int consumed = 0;
            while (!cleared && buoys.Count >= groupSize)
            {
                int top = buoys.Count - 1;
                int color = buoys[top].ColorCode;
                bool matches = true;
                for (int i = 1; i < groupSize; i++)
                    if (buoys[top - i].ColorCode != color) { matches = false; break; }
                if (!matches) break;
                var group = new Buoy[groupSize];
                for (int i = 0; i < groupSize; i++)
                {
                    Buoy buoy = buoys[buoys.Count - 1];
                    buoys.RemoveAt(buoys.Count - 1);
                    group[i] = buoy;
                }
                consumeAnimations.Add(new ConsumeAnimation(group));
                consumed += groupSize;
            }
            if (consumed > 0) visual.RefreshHeight(buoys.Count);
            return consumed;
        }

        internal void TickConsume(float deltaTime)
        {
            for (int i = consumeAnimations.Count - 1; i >= 0; i--)
            {
                ConsumeAnimation animation = consumeAnimations[i];
                if (!animation.Tick(deltaTime, visual)) continue;
                animation.Clear();
                consumeAnimations.RemoveAt(i);
            }
        }

        private sealed class ConsumeAnimation
        {
            private readonly Buoy[] group;
            private readonly Vector3[] positions;
            private readonly Vector3[] scales;
            private float elapsed;

            public ConsumeAnimation(Buoy[] group)
            {
                this.group = group;
                positions = new Vector3[group.Length];
                scales = new Vector3[group.Length];
                for (int i = 0; i < group.Length; i++)
                {
                    positions[i] = group[i].Visual.transform.localPosition;
                    scales[i] = group[i].Visual.transform.localScale;
                }
            }

            public bool Tick(float deltaTime, BuoyStackVisual settings)
            {
                elapsed += Mathf.Max(0f, deltaTime);
                int bottom = group.Length - 1;
                float upperPhaseDuration = settings.ConsumeCollapseDuration;
                float upperScale = 1f - Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / upperPhaseDuration));
                // Collapse the upper four around the bottom buoy without punching.
                for (int i = 0; i < bottom; i++)
                {
                    Transform target = group[i].Visual.transform;
                    target.localPosition = positions[bottom] + (positions[i] - positions[bottom]) * upperScale;
                    target.localScale = scales[i] * upperScale;
                }
                float bottomPhaseTime = elapsed - upperPhaseDuration;
                if (bottomPhaseTime < 0f) return false;
                float bottomScale = EvaluatePunchShrink(bottomPhaseTime, settings.ConsumePunchDuration,
                    settings.ConsumeShrinkDuration, settings.ConsumePunchScale);
                group[bottom].Visual.transform.localScale = scales[bottom] * bottomScale;
                return bottomPhaseTime >= settings.ConsumePunchDuration + settings.ConsumeShrinkDuration;
            }

            private static float EvaluatePunchShrink(float time, float punchDuration, float shrinkDuration, float punchScale)
            {
                if (time < punchDuration)
                    return Mathf.Lerp(1f, punchScale, Mathf.Sin(Mathf.Clamp01(time / punchDuration) * Mathf.PI * 0.5f));
                return Mathf.Lerp(punchScale, 0f,
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((time - punchDuration) / shrinkDuration)));
            }

            public void Clear()
            {
                foreach (Buoy buoy in group) buoy.Clear();
            }
        }

        public event Action<BuoyStack> Clicked;

        public IReadOnlyList<Buoy> Buoys { get; }
        public ColumnElementType Type { get; }
        public int TypeCount { get; }
        public IReadOnlyList<string> Elements { get; }

        public BuoyStack(BuoyColumnData source, BuoyStackVisual visual)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (visual == null) throw new ArgumentNullException(nameof(visual));
            this.visual = visual;
            Buoys = buoys.AsReadOnly();
            Type = source.ColumnType;
            TypeCount = source.ColumnTypeCount;
            var elements = new List<string>();
            foreach (var element in source.Elements) elements.Add(element.ElementId);
            Elements = elements.AsReadOnly();
        }

        internal void AddBuoy(Buoy buoy)
        {
            if (buoy == null) throw new ArgumentNullException(nameof(buoy));
            buoys.Add(buoy);
            visual.PlaceBuoy(buoy.Visual.transform, buoys.Count - 1);
            visual.RefreshHeight(buoys.Count);
        }

        public void OnClick()
        {
            if (cleared || !CanReceiveInput) return;
            Debug.Log($"Stack clicked: BuoyCount={buoys.Count}, Type={Type}, TypeCount={TypeCount}", visual);
            Clicked?.Invoke(this);
        }

        public void Clear()
        {
            if (cleared) return;
            cleared = true;
            CanReceiveInput = false;
            Clicked = null;
            foreach (Buoy buoy in buoys) buoy.Clear();
            buoys.Clear();
            foreach (ConsumeAnimation animation in consumeAnimations) animation.Clear();
            consumeAnimations.Clear();
            if (visual != null) visual.Release();
        }
    }
}
