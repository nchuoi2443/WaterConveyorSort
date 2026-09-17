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
        private readonly List<Buoy> consumedBuoys = new List<Buoy>();
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
                for (int i = 0; i < groupSize; i++)
                {
                    Buoy buoy = buoys[buoys.Count - 1];
                    buoys.RemoveAt(buoys.Count - 1);
                    buoy.Consume();
                    consumedBuoys.Add(buoy);
                }
                consumed += groupSize;
            }
            if (consumed > 0) visual.RefreshHeight(buoys.Count);
            return consumed;
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
            foreach (Buoy buoy in consumedBuoys) buoy.Clear();
            consumedBuoys.Clear();
            if (visual != null) visual.Release();
        }
    }
}
