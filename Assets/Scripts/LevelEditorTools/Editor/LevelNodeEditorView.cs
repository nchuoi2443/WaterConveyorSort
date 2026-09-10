using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.LevelEditorTools
{
    public sealed partial class LevelNodeEditorView
    {
        private const float PaletteNameWidth = 100f;
        private const float PaletteColorWidth = 72f;
        private const float PaletteColorHeight = 24f;
        private int selectedColumnIndex;
        private int selectedColorCode;
        private GUIStyle colorCellLabelStyle;
        private GUIStyle colorCellOutlineStyle;

        public void Draw(LevelDataSO level, ColorDataSO colorData, string colorDataFolder,
            int selectedNodeIndex, LevelDataWriter writer)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Node Editor", EditorStyles.boldLabel);

            if (colorData == null)
            {
                EditorGUILayout.HelpBox($"Không tìm thấy ColorDataSO trong {colorDataFolder}.", MessageType.Warning);
                return;
            }

            if (selectedNodeIndex < 0 || selectedNodeIndex >= level.BuoyNodes.Count)
            {
                EditorGUILayout.HelpBox("Chọn một node trên board để edit màu.", MessageType.Info);
                return;
            }

            BuoyNodeData node = level.BuoyNodes[selectedNodeIndex];
            EditorGUILayout.LabelField("Selected Node", $"Node {selectedNodeIndex + 1} - {node.GridPosition}");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Thêm cột"))
                {
                    writer.AddColumn(selectedNodeIndex);
                    return;
                }
            }

            if (node.Columns.Count == 0)
                return;

            EnsureSelectedColor(colorData);
            selectedColumnIndex = Mathf.Clamp(selectedColumnIndex, 0, node.Columns.Count - 1);
            selectedColumnIndex = EditorGUILayout.Popup("Column", selectedColumnIndex, BuildColumnLabels(node.Columns.Count));

            BuoyColumnData column = node.Columns[selectedColumnIndex];
            EditorGUI.BeginChangeCheck();
            int buoyCount = EditorGUILayout.DelayedIntField("Số phao", column.BuoyCount);
            if (EditorGUI.EndChangeCheck())
            {
                writer.SetBuoyCount(selectedNodeIndex, selectedColumnIndex, Mathf.Max(0, buoyCount));
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawColorPalette(colorData);
                DrawBuoyGrid(colorData, column, selectedNodeIndex, writer);
            }
        }

        private void DrawColorPalette(ColorDataSO colorData)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(210f)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.Width(PaletteNameWidth));
                    EditorGUILayout.LabelField("Color", EditorStyles.boldLabel, GUILayout.Width(PaletteColorWidth));
                }

                foreach (ColorEntryData entry in colorData.Colors)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(entry.DisplayName, GUILayout.Width(PaletteNameWidth),
                            GUILayout.Height(PaletteColorHeight));
                        Rect colorRect = GUILayoutUtility.GetRect(PaletteColorWidth, PaletteColorHeight,
                            GUILayout.Width(PaletteColorWidth), GUILayout.Height(PaletteColorHeight));
                        if (DrawColorCell(colorRect, entry.Color, entry.Code == selectedColorCode))
                            selectedColorCode = entry.Code;
                    }
                }
            }
        }

        private void DrawBuoyGrid(ColorDataSO colorData, BuoyColumnData column, int selectedNodeIndex,
            LevelDataWriter writer)
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField($"Column {selectedColumnIndex + 1}", EditorStyles.boldLabel);
                for (int i = column.Buoys.Count - 1; i >= 0; i--)
                {
                    BuoyData buoy = column.Buoys[i];
                    Color color = colorData.TryGetColor(buoy.ColorCode, out Color found) ? found : Color.clear;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"Ô {i + 1}", GUILayout.Width(36f));
                        Rect rect = GUILayoutUtility.GetRect(96f, 28f, GUILayout.Width(96f), GUILayout.Height(28f));
                        if (DrawColorCell(rect, color, false, GetColorName(colorData, buoy.ColorCode)))
                            writer.SaveBuoyColor(selectedNodeIndex, selectedColumnIndex, i, selectedColorCode);
                    }
                }
            }
        }

        private bool DrawColorCell(Rect rect, Color color, bool selected, string label = null)
        {
            color.a = 1f;
            Color border = selected ? Color.white : new Color(0.18f, 0.18f, 0.18f);
            float borderSize = selected ? 3f : 1f;
            EditorGUI.DrawRect(rect, border);
            EditorGUI.DrawRect(new Rect(rect.x + borderSize, rect.y + borderSize,
                rect.width - borderSize * 2f, rect.height - borderSize * 2f), color);

            if (!string.IsNullOrEmpty(label))
                DrawOutlinedLabel(rect, label);

            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private void DrawOutlinedLabel(Rect rect, string label)
        {
            colorCellLabelStyle ??= new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                normal = { textColor = Color.white }
            };
            colorCellOutlineStyle ??= new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                normal = { textColor = Color.black }
            };

            GUI.Label(new Rect(rect.x - 1f, rect.y, rect.width, rect.height), label, colorCellOutlineStyle);
            GUI.Label(new Rect(rect.x + 1f, rect.y, rect.width, rect.height), label, colorCellOutlineStyle);
            GUI.Label(new Rect(rect.x, rect.y - 1f, rect.width, rect.height), label, colorCellOutlineStyle);
            GUI.Label(new Rect(rect.x, rect.y + 1f, rect.width, rect.height), label, colorCellOutlineStyle);
            GUI.Label(rect, label, colorCellLabelStyle);
        }

        private void EnsureSelectedColor(ColorDataSO colorData)
        {
            if (colorData.Colors.Count == 0)
            {
                selectedColorCode = 0;
                return;
            }

            for (int i = 0; i < colorData.Colors.Count; i++)
            {
                if (colorData.Colors[i].Code == selectedColorCode)
                    return;
            }

            selectedColorCode = colorData.Colors[0].Code;
        }

        private string GetColorName(ColorDataSO colorData, int code)
        {
            foreach (ColorEntryData entry in colorData.Colors)
            {
                if (entry.Code == code)
                    return entry.DisplayName;
            }

            return code.ToString();
        }

        private string[] BuildColumnLabels(int count)
        {
            var labels = new string[count];
            for (int i = 0; i < count; i++)
                labels[i] = $"Column {i + 1}";
            return labels;
        }

    }
}
