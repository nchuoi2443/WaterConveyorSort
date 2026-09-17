using UnityEngine;
using WaterConveyorSort.BoardSystem.Conveyor;

namespace WaterConveyorSort.BoardSystem.StackQueue
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class StackQueueExit : MonoBehaviour
    {
        private StackQueueController queue;
        public void Initialize(StackQueueController controller) => queue = controller;
        public void Clear() => queue = null;
        private void Awake() => GetComponent<BoxCollider>().isTrigger = true;
        private void OnTriggerEnter(Collider other) => DetectGroup(other);
        private void OnTriggerStay(Collider other) => DetectGroup(other);
        private void DetectGroup(Collider other)
        {
            if (queue == null) return;
            ConveyorBuoyGroup group = other.GetComponent<ConveyorBuoyGroup>();
            if (group != null) queue.TryReceive(group);
        }
        internal bool ContainsGroup(ConveyorBuoyGroup group)
        {
            Vector3 position = group.transform.position;
            return (GetComponent<BoxCollider>().ClosestPoint(position) - position).sqrMagnitude <=
                group.DetectionRadius * group.DetectionRadius;
        }
        private void OnDrawGizmosSelected()
        {
            BoxCollider trigger = GetComponent<BoxCollider>();
            if (trigger == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(trigger.center, trigger.size);
        }
    }
}
