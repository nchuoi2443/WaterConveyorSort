using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.LevelEditorTools
{
    // Draws the grid and converts pointer input into board coordinates.
    public sealed partial class LevelBoardView
    {
        private Vector2 scroll;
        private int capturedControl;
        private const float CellPixels = 32f;

        public void ReleaseInput()
        {
            if (capturedControl != 0 && GUIUtility.hotControl == capturedControl)
                GUIUtility.hotControl = 0;
            capturedControl = 0;
        }

        public bool Draw(BoardData board, IReadOnlyList<Vector2Int> cells, bool closed,
            IReadOnlyList<BuoyNodeData> nodes, int selectedNodeIndex, bool editable, out Vector2Int selectedCell)
        {
            selectedCell = default;
            bool selected = false;
            float width = Mathf.Max(1, board.Width) * CellPixels;
            float height = Mathf.Max(1, board.Height) * CellPixels;
            float viewHeight = Mathf.Min(height + 20f, 420f);
            Rect viewport = GUILayoutUtility.GetRect(0f, viewHeight, GUILayout.ExpandWidth(true));
            scroll = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, width, height));
            Rect boardRect = new Rect(0, 0, width, height);
            int control = GUIUtility.GetControlID(FocusType.Passive);
            Event evt = Event.current;

            if (evt.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(boardRect, new Color(0.16f, 0.16f, 0.16f));
                int minX = Mathf.Max(0, Mathf.FloorToInt(scroll.x / CellPixels));
                int maxX = Mathf.Min(board.Width, Mathf.CeilToInt((scroll.x + viewport.width) / CellPixels));
                int minY = Mathf.Max(0, Mathf.FloorToInt(scroll.y / CellPixels));
                int maxY = Mathf.Min(board.Height, Mathf.CeilToInt((scroll.y + viewport.height) / CellPixels));
                for (int x = minX; x <= maxX; x++)
                    EditorGUI.DrawRect(new Rect(x * CellPixels, scroll.y, 1, viewport.height), Color.gray);
                for (int y = minY; y <= maxY; y++)
                    EditorGUI.DrawRect(new Rect(scroll.x, y * CellPixels, viewport.width, 1), Color.gray);

                for (int i = 0; i < cells.Count; i++)
                {
                    Rect rect = CellRect(cells[i], board.Height);
                    if (rect.xMax < scroll.x || rect.xMin > scroll.x + viewport.width ||
                        rect.yMax < scroll.y || rect.yMin > scroll.y + viewport.height) continue;
                    Color color = i == 0 ? new Color(0.2f, 0.65f, 0.35f) : new Color(0.15f, 0.45f, 0.7f);
                    if (i == cells.Count - 1 && i != 0) color = new Color(0.8f, 0.5f, 0.15f);
                    EditorGUI.DrawRect(rect, color);
                    GUI.Label(rect, (i + 1).ToString(), EditorStyles.centeredGreyMiniLabel);
                }

                for (int i = 0; i < nodes.Count; i++)
                {
                    Rect rect = CellRect(nodes[i].GridPosition, board.Height);
                    if (rect.xMax < scroll.x || rect.xMin > scroll.x + viewport.width ||
                        rect.yMax < scroll.y || rect.yMin > scroll.y + viewport.height) continue;
                    Color color = i == selectedNodeIndex ? new Color(1f, 0.85f, 0.25f) : new Color(0.6f, 0.3f, 0.9f);
                    EditorGUI.DrawRect(rect, color);
                    GUI.Label(rect, $"N{i + 1}", EditorStyles.centeredGreyMiniLabel);
                }
            }

            bool inside = boardRect.Contains(evt.mousePosition) &&
                new Rect(scroll.x, scroll.y, viewport.width - 16f, viewport.height - 16f).Contains(evt.mousePosition);
            if (editable && evt.type == EventType.MouseDown && evt.button == 0 && inside)
            {
                GUIUtility.hotControl = control;
                capturedControl = control;
                selected = true;
            }
            else if (editable && evt.type == EventType.MouseDrag && GUIUtility.hotControl == control && inside)
                selected = true;
            else if (evt.type == EventType.MouseUp && GUIUtility.hotControl == control)
            {
                ReleaseInput();
                evt.Use();
            }

            if (selected)
            {
                selectedCell = new Vector2Int(Mathf.FloorToInt(evt.mousePosition.x / CellPixels),
                    board.Height - 1 - Mathf.FloorToInt(evt.mousePosition.y / CellPixels));
                evt.Use();
            }
            GUI.EndScrollView();
            EditorGUILayout.LabelField(closed ? "Đường kín • Xanh lá: bắt đầu • Cam: kết thúc"
                : "Đường hở • Xanh lá: bắt đầu • Cam: kết thúc", EditorStyles.miniLabel);
            return selected;
        }

        private Rect CellRect(Vector2Int cell, int height)
        {
            return new Rect(cell.x * CellPixels + 2, (height - 1 - cell.y) * CellPixels + 2,
                CellPixels - 3, CellPixels - 3);
        }
    }
}
