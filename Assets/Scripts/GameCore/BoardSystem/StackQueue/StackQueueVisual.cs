using System;
using UnityEngine;
using WaterConveyorSort.BoardSystem.Buoys;

namespace WaterConveyorSort.BoardSystem.StackQueue
{
    public sealed class StackQueueVisual : MonoBehaviour
    {
        [Tooltip("Separate spawn transform. Its position is the center of the stack row.")]
        [SerializeField] private Transform spawnRoot;
        [SerializeField] private BuoyStackVisual stackPrefab;
        [SerializeField, Min(0.01f)] private float stackSpacing = 1.2f;
        [SerializeField, Min(0.01f)] private float receiveFlightDuration = 0.4f;
        [SerializeField, Min(0f)] private float receiveLaunchDelay = 0.12f;
        [SerializeField, Min(0.01f)] private float fixedStackHeight = 1f;
        [SerializeField, Min(0.01f)] private float receiveDescentDuration = 0.25f;
        [SerializeField, Min(0f)] private float receiveTopClearance = 0.2f;
        public float ReceiveDescentDuration => Mathf.Max(0.01f, receiveDescentDuration);
        public float ReceiveTopClearance => Mathf.Max(0f, receiveTopClearance);
        public float ReceiveFlightDuration => Mathf.Max(0.01f, receiveFlightDuration);
        public float ReceiveLaunchDelay => Mathf.Max(0f, receiveLaunchDelay);

        public void ValidateSetup()
        {
            if (stackPrefab == null)
                throw new InvalidOperationException("Assign Stack Prefab on StackQueueVisual.");
        }

        public Vector3 GetStackOffset(int index, int count) =>
            Vector3.right * ((index - (count - 1) * 0.5f) * Mathf.Max(0.01f, stackSpacing));

        internal BuoyStackVisual SpawnStack(int index, int count)
        {
            BuoyStackVisual visual = Instantiate(stackPrefab, spawnRoot != null ? spawnRoot : transform);
            visual.name = $"QueueStack_{index}";
            visual.transform.localPosition = GetStackOffset(index, count);
            visual.transform.localRotation = Quaternion.identity;
            visual.SetInputEnabled(false);
            visual.SetFixedHeight(fixedStackHeight);
            return visual;
        }
    }
}
