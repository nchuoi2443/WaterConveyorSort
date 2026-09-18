using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaterConveyorSort.LevelData
{
    [CreateAssetMenu(fileName = "ColorData", menuName = "Water Conveyor Sort/Color Data")]
    public sealed class ColorDataSO : ScriptableObject
    {
        [SerializeField] private List<ColorEntryData> colors = new List<ColorEntryData>();

        public IReadOnlyList<ColorEntryData> Colors => colors;

        public bool TryGetMaterial(int code, out Material material)
        {
            foreach (ColorEntryData entry in colors)
            {
                if (entry.Code != code) continue;
                material = entry.Material;
                return material != null;
            }

            material = null;
            return false;
        }

        public bool TryGetColor(int code, out Color color)
        {
            for (int i = 0; i < colors.Count; i++)
            {
                if (colors[i].Code != code)
                    continue;

                color = colors[i].Color;
                return true;
            }

            color = Color.white;
            return false;
        }
    }

    [Serializable]
    public sealed class ColorEntryData
    {
        [SerializeField] private int code;
        [SerializeField] private string displayName = "New Color";
        [SerializeField] private Color color = Color.white;
        [Tooltip("Optional buoy material. Leave empty to tint the prefab material with this color.")]
        [SerializeField] private Material material;

        public int Code => code;
        public string DisplayName => displayName;
        public Color Color => color;
        public Material Material => material;
    }
}
