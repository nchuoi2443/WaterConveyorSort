using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.LevelEditorTools
{
    // SerializedProperty supplies Unity Undo and dirty tracking for committed changes.
    public sealed partial class LevelDataWriter
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

        public void SaveColorData(ColorDataSO colorData)
        {
            serialized.Update();
            serialized.FindProperty("colorData").objectReferenceValue = colorData;
            Commit("Assign Level Color Data");
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

        public int EnsureNodeAt(Vector2Int gridPosition)
        {
            serialized.Update();
            SerializedProperty nodes = serialized.FindProperty("buoyNodes");
            int existing = FindNodeIndex(nodes, gridPosition);
            if (existing >= 0)
                return existing;

            int index = nodes.arraySize;
            nodes.InsertArrayElementAtIndex(index);
            SerializedProperty node = nodes.GetArrayElementAtIndex(index);
            node.FindPropertyRelative("gridPosition").vector2IntValue = gridPosition;

            SerializedProperty columns = node.FindPropertyRelative("columns");
            columns.arraySize = 1;
            EnsureColumnDefaults(columns.GetArrayElementAtIndex(0));
            Commit("Add Buoy Node");
            return index;
        }

        public void AddColumn(int nodeIndex)
        {
            serialized.Update();
            SerializedProperty columns = FindColumns(nodeIndex);
            if (columns == null)
                return;

            int index = columns.arraySize;
            columns.InsertArrayElementAtIndex(index);
            EnsureColumnDefaults(columns.GetArrayElementAtIndex(index));
            Commit("Add Buoy Column");
        }

        public void SetBuoyCount(int nodeIndex, int columnIndex, int count)
        {
            serialized.Update();
            SerializedProperty columns = FindColumns(nodeIndex);
            if (columns == null || columnIndex < 0 || columnIndex >= columns.arraySize)
                return;

            SerializedProperty buoys = columns.GetArrayElementAtIndex(columnIndex).FindPropertyRelative("buoys");
            int oldCount = buoys.arraySize;
            buoys.arraySize = Mathf.Max(0, count);

            for (int i = oldCount; i < buoys.arraySize; i++)
            {
                SerializedProperty buoy = buoys.GetArrayElementAtIndex(i);
                buoy.FindPropertyRelative("colorCode").intValue = 0;
                buoy.FindPropertyRelative("elements").arraySize = 0;
            }

            Commit("Edit Buoy Count");
        }

        public void SaveBuoyColor(int nodeIndex, int columnIndex, int buoyIndex, int colorCode)
        {
            serialized.Update();
            SerializedProperty buoy = FindBuoy(nodeIndex, columnIndex, buoyIndex);
            if (buoy == null)
                return;

            buoy.FindPropertyRelative("colorCode").intValue = colorCode;
            Commit("Edit Buoy Color");
        }

        private void Commit(string undoName)
        {
            Undo.SetCurrentGroupName(undoName);
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(serialized.targetObject);
        }

        private SerializedProperty FindBuoy(int nodeIndex, int columnIndex, int buoyIndex)
        {
            SerializedProperty columns = FindColumns(nodeIndex);
            if (columns == null)
                return null;
            if (columnIndex < 0 || columnIndex >= columns.arraySize)
                return null;

            SerializedProperty buoys = columns.GetArrayElementAtIndex(columnIndex).FindPropertyRelative("buoys");
            if (buoyIndex < 0 || buoyIndex >= buoys.arraySize)
                return null;

            return buoys.GetArrayElementAtIndex(buoyIndex);
        }

        private SerializedProperty FindColumns(int nodeIndex)
        {
            SerializedProperty nodes = serialized.FindProperty("buoyNodes");
            if (nodeIndex < 0 || nodeIndex >= nodes.arraySize)
                return null;

            return nodes.GetArrayElementAtIndex(nodeIndex).FindPropertyRelative("columns");
        }

        private int FindNodeIndex(SerializedProperty nodes, Vector2Int gridPosition)
        {
            for (int i = 0; i < nodes.arraySize; i++)
            {
                if (nodes.GetArrayElementAtIndex(i).FindPropertyRelative("gridPosition").vector2IntValue == gridPosition)
                    return i;
            }

            return -1;
        }

        private void EnsureColumnDefaults(SerializedProperty column)
        {
            column.FindPropertyRelative("elements").arraySize = 0;
            SerializedProperty buoys = column.FindPropertyRelative("buoys");
            buoys.arraySize = BuoyColumnData.DefaultBuoyCount;
            for (int i = 0; i < buoys.arraySize; i++)
            {
                SerializedProperty buoy = buoys.GetArrayElementAtIndex(i);
                buoy.FindPropertyRelative("colorCode").intValue = 0;
                buoy.FindPropertyRelative("elements").arraySize = 0;
            }
        }
    }
}
