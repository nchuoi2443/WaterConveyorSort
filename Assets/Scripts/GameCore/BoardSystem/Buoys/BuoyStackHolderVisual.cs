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
