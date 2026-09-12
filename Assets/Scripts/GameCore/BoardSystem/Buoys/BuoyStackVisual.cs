using UnityEngine;

namespace WaterConveyorSort.BoardSystem.Buoys
{
    public sealed class BuoyStackVisual : MonoBehaviour
    {
        [SerializeField] private Transform buoyRoot;
        [SerializeField] private Vector3 firstBuoyOffset = new Vector3(0f, 0.2f, 0f);
        [SerializeField, Min(0.01f)] private float buoySpacing = 0.2f;

        public void PlaceBuoy(Transform buoy, int index)
        {
            // Source order is bottom to top for the initial layout.
            buoy.SetParent(buoyRoot != null ? buoyRoot : transform, false);
            buoy.localPosition = firstBuoyOffset + Vector3.up * (index * buoySpacing);
            buoy.localRotation = Quaternion.identity;
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
