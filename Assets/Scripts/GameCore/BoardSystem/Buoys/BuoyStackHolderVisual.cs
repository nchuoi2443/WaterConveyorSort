using System;
using System.Collections;
using UnityEngine;
using WaterConveyorSort.BoardSystem.Conveyor;

namespace WaterConveyorSort.BoardSystem.Buoys
{
    public sealed class BuoyStackHolderVisual : MonoBehaviour
    {
        [Tooltip("Optional decorative objects, separate from the stack root.")]
        [SerializeField] private GameObject singleStackVisual;
        [SerializeField] private GameObject multipleStackVisual;
        [SerializeField] private Transform stackRoot;
        [SerializeField] private Vector3 firstStackOffset;
        [SerializeField] private Vector3 stackStep = new Vector3(0f, 0f, -0.8f);

        public void SetOutletDirection(Vector2Int direction)
        {
            // Grid Y maps to board-local Z. The visual root is a direct child of boardRoot.
            // Keep unconfigured nodes at the default orientation for editor previews.
            transform.localRotation = direction == Vector2Int.zero
                ? Quaternion.identity
                : Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y), Vector3.up);
        }

        [SerializeField] private Collider receiveCollider;
        [SerializeField, Min(0f)] private float advanceDuration = 0.3f;
        private BuoyStackHolder owner;
        private Coroutine advanceTween;

        public void Initialize(BuoyStackHolder holder, int visibleCapacity)
        {
            owner = holder;
            if (singleStackVisual != null) singleStackVisual.SetActive(visibleCapacity == 1);
            if (multipleStackVisual != null) multipleStackVisual.SetActive(visibleCapacity == 2);
            if (receiveCollider == null) receiveCollider = GetComponent<Collider>();
            if (receiveCollider == null || !receiveCollider.isTrigger || receiveCollider.gameObject != gameObject)
                throw new InvalidOperationException("Holder requires a trigger collider on its root.");
        }

        internal bool ContainsGroup(ConveyorBuoyGroup group) => receiveCollider != null &&
            receiveCollider.enabled && (receiveCollider.ClosestPoint(group.transform.position) - group.transform.position)
            .sqrMagnitude <= group.DetectionRadius * group.DetectionRadius;

        private void OnTriggerEnter(Collider other) => DetectGroup(other);
        private void OnTriggerStay(Collider other) => DetectGroup(other);
        private void DetectGroup(Collider other)
        {
            if (released || owner == null) return;
            ConveyorBuoyGroup group = other.GetComponent<ConveyorBuoyGroup>();
            if (group != null) owner.TryReceive(group);
        }

        public void TweenToFront(Transform stack, Action completed)
        {
            if (advanceTween != null) StopCoroutine(advanceTween);
            if (advanceDuration <= 0f)
            {
                stack.localPosition = firstStackOffset;
                completed();
                return;
            }
            advanceTween = StartCoroutine(AdvanceStack(stack, completed));
        }

        private IEnumerator AdvanceStack(Transform stack, Action completed)
        {
            Vector3 start = stack.localPosition;
            float elapsed = 0f;
            while (elapsed < advanceDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / advanceDuration));
                stack.localPosition = Vector3.LerpUnclamped(start, firstStackOffset, t);
                yield return null;
            }
            stack.localPosition = firstStackOffset;
            advanceTween = null;
            completed();
        }

        public void PlaceStack(Transform stack, int index)
        {
            stack.SetParent(stackRoot != null ? stackRoot : transform, false);
            stack.localPosition = firstStackOffset + stackStep * index;
            stack.localRotation = Quaternion.identity;
        }
        private bool released;

        public void Release()
        {
            if (released) return;
            released = true;
            owner = null;
            if (advanceTween != null) StopCoroutine(advanceTween);
            advanceTween = null;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
