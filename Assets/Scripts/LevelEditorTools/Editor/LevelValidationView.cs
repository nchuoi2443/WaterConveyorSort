using System.Collections.Generic;
using UnityEditor;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.LevelEditorTools
{
    public sealed partial class LevelEditorTool
    {
        private void DrawValidationPanel(LevelDataSO level, ColorDataSO colorData)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Level Validation", EditorStyles.boldLabel);

            List<string> errors = ValidateLevel(level, colorData);
            if (errors.Count == 0)
            {
                EditorGUILayout.HelpBox("Level data hợp lệ với các rule hiện tại.", MessageType.Info);
                return;
            }

            for (int i = 0; i < errors.Count; i++)
                EditorGUILayout.HelpBox(errors[i], MessageType.Warning);
        }

        private List<string> ValidateLevel(LevelDataSO level, ColorDataSO colorData)
        {
            var errors = new List<string>();
            string pathError = ValidatePath(level.Path.Cells, level.Path.IsClosed, level);
            if (pathError != null)
                errors.Add(pathError);

            if (colorData == null)
                errors.Add("Không tìm thấy ColorDataSO để validate màu.");
            else
                ValidatePalette(colorData, errors);

            ValidateNodes(level, colorData, errors);
            return errors;
        }

        private void ValidatePalette(ColorDataSO colorData, List<string> errors)
        {
            var colorCodes = new HashSet<int>();
            foreach (ColorEntryData entry in colorData.Colors)
            {
                if (!colorCodes.Add(entry.Code))
                    errors.Add($"ColorData bị trùng mã màu: {entry.DisplayName}.");
            }
        }

        private void ValidateNodes(LevelDataSO level, ColorDataSO colorData, List<string> errors)
        {
            var colorCounts = new Dictionary<int, int>();
            var nodePositions = new HashSet<UnityEngine.Vector2Int>();
            for (int nodeIndex = 0; nodeIndex < level.BuoyNodes.Count; nodeIndex++)
            {
                BuoyNodeData node = level.BuoyNodes[nodeIndex];
                if (!Contains(node.GridPosition, level.Board.Width, level.Board.Height))
                    errors.Add($"Node {nodeIndex + 1} nằm ngoài board.");
                if (!nodePositions.Add(node.GridPosition))
                    errors.Add($"Node {nodeIndex + 1} bị trùng vị trí {node.GridPosition}.");

                if (node.Columns.Count == 0)
                    errors.Add($"Node {nodeIndex + 1} chưa có column.");

                for (int columnIndex = 0; columnIndex < node.Columns.Count; columnIndex++)
                {
                    BuoyColumnData column = node.Columns[columnIndex];
                    if (column.BuoyCount <= 0)
                        errors.Add($"Node {nodeIndex + 1}, Column {columnIndex + 1} chưa có phao.");

                    for (int buoyIndex = 0; buoyIndex < column.Buoys.Count; buoyIndex++)
                    {
                        int colorCode = column.Buoys[buoyIndex].ColorCode;
                        if (colorData != null && !colorData.TryGetColor(colorCode, out _))
                        {
                            errors.Add($"Node {nodeIndex + 1}, Column {columnIndex + 1}, Ô {buoyIndex + 1} dùng màu không tồn tại.");
                            continue;
                        }

                        colorCounts.TryGetValue(colorCode, out int count);
                        colorCounts[colorCode] = count + 1;
                    }
                }
            }

            foreach (KeyValuePair<int, int> entry in colorCounts)
            {
                int remainder = entry.Value % BuoyColumnData.DefaultBuoyCount;
                if (remainder == 0)
                    continue;

                int extra = remainder;
                int missing = BuoyColumnData.DefaultBuoyCount - remainder;
                string colorName = GetColorName(colorData, entry.Key);
                errors.Add($"Màu {colorName} có {entry.Value} phao, không chia hết cho {BuoyColumnData.DefaultBuoyCount}. " +
                    $"Thiếu {missing} phao để đủ nhóm tiếp theo, hoặc thừa {extra} phao so với nhóm trước.");
            }
        }

        private string GetColorName(ColorDataSO colorData, int colorCode)
        {
            if (colorData == null)
                return "Unknown";

            foreach (ColorEntryData entry in colorData.Colors)
            {
                if (entry.Code == colorCode)
                    return entry.DisplayName;
            }

            return "Unknown";
        }
    }
}
