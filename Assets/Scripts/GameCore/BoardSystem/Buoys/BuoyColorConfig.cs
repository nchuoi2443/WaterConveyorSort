using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaterConveyorSort.BoardSystem.Buoys
{
    [CreateAssetMenu(fileName = "BuoyColorConfig", menuName = "Water Conveyor Sort/Buoy Color Config")]
    public sealed class BuoyColorConfig : ScriptableObject
    {
        // Unity serializes the entries; the dictionary is built for runtime lookup.
        [SerializeField] private List<ColorMaterialEntry> colors = new List<ColorMaterialEntry>();
        private Dictionary<int, Material> materials;

        public Material GetMaterial(int colorId)
        {
            if (materials == null) BuildLookup();
            if (!materials.TryGetValue(colorId, out Material material) || material == null)
                throw new InvalidOperationException($"BuoyColorConfig '{name}' has no material for color ID {colorId}.");
            return material;
        }

        private void OnEnable() => materials = null;
        private void OnValidate() => materials = null;

        private void BuildLookup()
        {
            var lookup = new Dictionary<int, Material>();
            foreach (ColorMaterialEntry entry in colors)
            {
                if (entry == null) continue;
                if (lookup.ContainsKey(entry.ColorId))
                    throw new InvalidOperationException($"BuoyColorConfig '{name}' contains duplicate color ID {entry.ColorId}.");
                lookup.Add(entry.ColorId, entry.Material);
            }
            materials = lookup;
        }

        [Serializable]
        private sealed class ColorMaterialEntry
        {
            [SerializeField] private int colorId;
            [SerializeField] private Material material;
            public int ColorId => colorId;
            public Material Material => material;
        }
    }
}
