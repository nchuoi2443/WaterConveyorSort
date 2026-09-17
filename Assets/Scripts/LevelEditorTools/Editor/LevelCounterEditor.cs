using UnityEditor;
using UnityEngine;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.LevelEditorTools
{
    public sealed partial class LevelEditorTool
    {
        private bool isPlacingCounter;
        private void DrawCounterSettings(LevelDataSO level)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Conveyor Capacity", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            int maximum = EditorGUILayout.DelayedIntField("Max Buoy In Conveyor (Groups)", level.MaxBuoyInConveyor);
            if (EditorGUI.EndChangeCheck()) writer.SaveMaxBuoyInConveyor(maximum);
            MaxBuoyCounterTxtData data = level.MaxBuoyCounterTxt;
            EditorGUILayout.LabelField("MaxBuoyCounterTxt", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUILayout.Toggle("Enabled", data.Enabled);
            Vector2Int position = EditorGUILayout.Vector2IntField("Grid Position", data.GridPosition);
            if (EditorGUI.EndChangeCheck())
            {
                if (!enabled || CanPlaceCounter(level, position))
                {
                    writer.SaveMaxBuoyCounter(enabled, position);
                    if (!enabled) isPlacingCounter = false;
                    message = null;
                }
                else message = "Counter must occupy a free cell inside the board.";
            }
            using (new EditorGUI.DisabledScope(draft.IsEditing))
                if (GUILayout.Button(isPlacingCounter ? "Cancel Counter Placement" : "Place Counter On Board"))
                {
                    isPlacingCounter = !isPlacingCounter;
                    isEditingNodes = false;
                    isMovingNode = false;
                    boardView.ReleaseInput();
                    message = null;
                }
            EditorGUILayout.LabelField("Preview", $"0/{level.MaxBuoyInConveyor}");
        }
        private bool CanPlaceCounter(LevelDataSO level, Vector2Int position) =>
            Contains(position, level.Board.Width, level.Board.Height) &&
            !ContainsPathCell(level.Path.Cells, position) && FindNodeIndex(level, position) < 0;
        private void PlaceCounter(LevelDataSO level, Vector2Int position)
        {
            if (!CanPlaceCounter(level, position))
            {
                message = "Counter must occupy a free cell inside the board.";
                return;
            }
            writer.SaveMaxBuoyCounter(true, position);
            isPlacingCounter = false;
            boardView.ReleaseInput();
            message = null;
        }
    }
}
