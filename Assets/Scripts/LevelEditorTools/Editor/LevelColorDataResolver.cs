using UnityEditor;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.LevelEditorTools
{
    public sealed partial class LevelEditorTool
    {
        private const string ColorDataFolder = "Assets/Data/ColorData";

        private ColorDataSO ResolveColorData(LevelDataSO level)
        {
            if (level.ColorData != null)
                return level.ColorData;

            string[] guids = AssetDatabase.FindAssets("t:ColorDataSO", new[] { ColorDataFolder });
            if (guids.Length != 1)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            ColorDataSO colors = AssetDatabase.LoadAssetAtPath<ColorDataSO>(path);
            // Persist the default palette so runtime uses the same data as the editor.
            if (colors != null)
                writer.SaveColorData(colors);
            return colors;
        }
    }
}
