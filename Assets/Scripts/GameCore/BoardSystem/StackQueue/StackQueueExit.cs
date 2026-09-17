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
