using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.LevelEditorTools
{
    // SerializedProperty supplies Unity Undo and dirty tracking for committed changes.
    internal sealed class LevelDataWriter
    {
        private readonly SerializedObject serialized;
        public LevelDataWriter(SerializedObject serialized) { this.serialized = serialized; }

        public void SaveBoard(int width, int height, float cellSize)
        {
            serialized.Update();
            SerializedProperty board = serialized.FindProperty("board");
            board.FindPropertyRelative("width").intValue = width;
            board.FindPropertyRelative("height").intValue = height;
            board.FindPropertyRelative("cellSize").floatValue = cellSize;
            Commit("Edit Level Board");
        }

        public void SavePath(IReadOnlyList<Vector2Int> cells, bool isClosed)
        {
            serialized.Update();
            SerializedProperty path = serialized.FindProperty("path");
            path.FindPropertyRelative("isClosed").boolValue = isClosed;
            SerializedProperty points = path.FindPropertyRelative("cells");
            points.arraySize = cells.Count;
            for (int i = 0; i < cells.Count; i++)
                points.GetArrayElementAtIndex(i).vector2IntValue = cells[i];
            Commit("Save Level Path");
        }

        private void Commit(string undoName)
        {
            Undo.SetCurrentGroupName(undoName);
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(serialized.targetObject);
        }
    }
}
