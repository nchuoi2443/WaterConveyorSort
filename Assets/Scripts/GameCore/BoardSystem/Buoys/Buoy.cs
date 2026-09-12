using System;
using System.Collections.Generic;
using UnityEngine;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.BoardSystem.Buoys
{
    public sealed class Buoy
    {
        private readonly BuoyVisual visual;
        public int ColorCode { get; }
        public BuoyElementType Type { get; }
        public int TypeCount { get; }
        public IReadOnlyList<string> Elements { get; }
        internal BuoyVisual Visual => visual;

        public Buoy(BuoyData source, ColorDataSO colors, BuoyVisual visual)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (visual == null) throw new ArgumentNullException(nameof(visual));
            if (colors == null) throw new ArgumentNullException(nameof(colors), "Buoy requires a ColorDataSO palette.");
            if (!colors.TryGetColor(source.ColorCode, out Color color))
                throw new InvalidOperationException($"Palette '{colors.name}' has no color with code {source.ColorCode}.");

            this.visual = visual;
            ColorCode = source.ColorCode;
            Type = source.BuoyType;
            TypeCount = source.BuoyTypeCount;
            var elements = new List<string>();
            foreach (var element in source.Elements) elements.Add(element.ElementId);
            Elements = elements.AsReadOnly();
            visual.Refresh(color);
        }

        public void Clear()
        {
            if (visual != null) visual.Release();
        }
    }
}
