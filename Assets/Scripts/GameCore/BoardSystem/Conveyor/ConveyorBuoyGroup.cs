using System.Collections.Generic;
using UnityEngine;
using WaterConveyorSort.BoardSystem.Buoys;
using WaterConveyorSort.BoardSystem.StackQueue;

namespace WaterConveyorSort.BoardSystem.Conveyor
{
    public sealed class ConveyorBuoyGroup : MonoBehaviour
    {
        private readonly List<Buoy> buoys = new List<Buoy>();
        public IReadOnlyList<Buoy> Buoys => buoys;
        internal double Percent;
        internal bool Moving;
        internal bool IsLoaded { get; set; }
        internal bool IsReceiving { get; set; }
        internal BuoyStackHolder DepartureHolder { get; set; }
        internal StackQueueExit DepartureExit { get; set; }
        public float DetectionRadius => detectionCollider != null ?
            detectionCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x),
                Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z)) : 0f;
        private SphereCollider detectionCollider;
        private Rigidbody body;

        private void Update()
        {
            if (DepartureHolder != null && !DepartureHolder.ContainsGroup(this)) DepartureHolder = null;
            if (DepartureExit != null && !DepartureExit.ContainsGroup(this)) DepartureExit = null;
        }
        public bool HasColor(int colorCode) => buoys.Count > 0 && buoys.TrueForAll(buoy => buoy.ColorCode == colorCode);
        internal Buoy TakeTop()
        {
            int index = buoys.Count - 1;
            Buoy buoy = buoys[index];
            buoys.RemoveAt(index);
            buoy.Visual.transform.SetParent(transform.parent, true);
            return buoy;
        }
        private float spacing;
        private Vector3 travelDirection;
        private Vector3 travelUp = Vector3.up;
        internal void SetTravelDirection(Vector3 direction, Vector3 up)
        {
            travelDirection = direction;
            travelUp = up;
            foreach (Buoy buoy in buoys) buoy.Visual.SetHeadDirection(direction, up);
        }
        public void Initialize(double percent, float spacing)
        {
            Percent = percent;
            this.spacing = spacing;
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            detectionCollider = gameObject.AddComponent<SphereCollider>();
            detectionCollider.radius = 0.12f;
            detectionCollider.isTrigger = true;
            body = gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
        }
        public Vector3 GetSlotPosition(int slot) => transform.TransformPoint(Vector3.up * (slot * spacing));
        public void Receive(Buoy buoy, int slot)
        {
            buoy.Visual.transform.SetParent(transform, true);
            buoy.Visual.transform.localPosition = Vector3.up * (slot * spacing);
            buoys.Add(buoy);
            buoy.Visual.SetHeadDirection(travelDirection, travelUp);
            if (slot == 0) Moving = true;
        }
        public void Clear()
        {
            foreach (Buoy buoy in buoys) buoy.Clear();
            buoys.Clear();
            Destroy(gameObject);
        }
        private void OnDestroy()
        {
            foreach (Buoy buoy in buoys) buoy.Clear();
            buoys.Clear();
        }
    }
}
