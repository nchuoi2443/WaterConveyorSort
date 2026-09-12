using System;
using System.Collections.Generic;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.BoardSystem.Buoys
{
    public sealed class BuoyStack
    {
        private readonly BuoyStackVisual visual;
        private readonly List<Buoy> buoys = new List<Buoy>();
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
        }

        public void Clear()
        {
            foreach (Buoy buoy in buoys) buoy.Clear();
            buoys.Clear();
            if (visual != null) visual.Release();
        }
    }
}
