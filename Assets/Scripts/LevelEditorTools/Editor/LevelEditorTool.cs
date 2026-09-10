using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.LevelEditorTools
{
    [CustomEditor(typeof(LevelDataSO))]
    public sealed class LevelEditorTool : Editor
    {
        private readonly LevelBoardView boardView = new LevelBoardView();
        private readonly LevelPathDraft draft = new LevelPathDraft();
        private LevelDataWriter writer;
        private string message;

        private void OnEnable()
        {
            writer = new LevelDataWriter(serializedObject);
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            boardView.ReleaseInput();
        }

        private void OnUndoRedo()
        {
            draft.Cancel();
            boardView.ReleaseInput();
            message = null;
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var level = (LevelDataSO)target;
            DrawBoardSettings(level);
            EditorGUILayout.Space();
            DrawToolbar(level);

            IReadOnlyList<Vector2Int> cells = draft.IsEditing ? draft.Cells : level.Path.Cells;
            bool closed = draft.IsEditing ? draft.IsClosed : level.Path.IsClosed;
            EditorGUILayout.LabelField("Board", $"{cells.Count} Spline points");
            EditorGUILayout.HelpBox(draft.IsEditing
                ? "Click hoặc kéo chuột để nối ô. Đường nối theo cạnh, ưu tiên ngang rồi dọc. " +
                  "Click ô đã có để cắt phần đường phía sau. Gốc (0, 0) ở góc dưới trái."
                : "Chọn Vẽ spline để chỉnh dữ liệu đường trên board.", MessageType.Info);

            if (boardView.Draw(level.Board, cells, closed, draft.IsEditing, out Vector2Int cell))
            {
                draft.Visit(cell);
                message = null;
                Repaint();
            }

            if (!string.IsNullOrEmpty(message))
                EditorGUILayout.HelpBox(message, MessageType.Warning);
        }

        private void DrawBoardSettings(LevelDataSO level)
        {
            EditorGUILayout.LabelField("Board Settings", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(draft.IsEditing))
            {
                EditorGUI.BeginChangeCheck();
                int width = EditorGUILayout.DelayedIntField("Width", level.Board.Width);
                int height = EditorGUILayout.DelayedIntField("Height", level.Board.Height);
                float size = EditorGUILayout.DelayedFloatField("Cell Size", level.Board.CellSize);
                if (EditorGUI.EndChangeCheck())
                {
                    width = Mathf.Max(1, width);
                    height = Mathf.Max(1, height);
                    size = float.IsNaN(size) || float.IsInfinity(size) ? 1f : Mathf.Max(0.01f, size);
                    if (LevelPathValidator.FitsBoard(level, width, height))
                    {
                        writer.SaveBoard(width, height, size);
                        message = null;
                    }
                    else
                        message = "Không thể thu nhỏ board vì đường hoặc node đang nằm ngoài kích thước mới.";
                }
            }
        }

        private void DrawToolbar(LevelDataSO level)
        {
            if (!draft.IsEditing)
            {
                if (GUILayout.Button("Vẽ spline"))
                {
                    draft.Begin(level.Path);
                    message = null;
                }
                return;
            }

            draft.IsClosed = EditorGUILayout.Toggle("Is Closed Line", draft.IsClosed);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Lưu spline"))
                {
                    message = LevelPathValidator.Validate(draft.Cells, draft.IsClosed, level);
                    if (message == null)
                    {
                        writer.SavePath(draft.Cells, draft.IsClosed);
                        draft.Cancel();
                        boardView.ReleaseInput();
                    }
                }
                if (GUILayout.Button("Hủy vẽ spline"))
                {
                    draft.Cancel();
                    boardView.ReleaseInput();
                    message = null;
                }
            }

            if (draft.IsEditing)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Bỏ ô cuối")) draft.RemoveLast();
                    if (GUILayout.Button("Xóa bản nháp")) draft.Clear();
                }
            }
        }
    }
}
