using UnityEngine;

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

        public void Refresh(int stackCount)
        {
            if (singleStackVisual != null) singleStackVisual.SetActive(stackCount == 1);
            if (multipleStackVisual != null) multipleStackVisual.SetActive(stackCount > 1);
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
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
