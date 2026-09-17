using System.Collections.Generic;
using UnityEngine;
using WaterConveyorSort.BoardSystem.Buoys;

namespace WaterConveyorSort.BoardSystem.Conveyor
{
    public sealed class ConveyorBuoyGroup : MonoBehaviour
    {
        private readonly List<Buoy> buoys = new List<Buoy>();
        public IReadOnlyList<Buoy> Buoys => buoys;
        internal double Percent;
        internal bool Moving;
        internal bool IsLoaded { get; set; }
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
        public void Initialize(double percent, float spacing)
        { Percent = percent; this.spacing = spacing; }
        public Vector3 GetSlotPosition(int slot) => transform.TransformPoint(Vector3.up * (slot * spacing));
        public void Receive(Buoy buoy, int slot)
        {
            buoy.Visual.transform.SetParent(transform, true);
            buoy.Visual.transform.localPosition = Vector3.up * (slot * spacing);
            buoy.Visual.transform.localRotation = Quaternion.identity;
            buoys.Add(buoy);
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
