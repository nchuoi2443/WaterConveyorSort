using System.Collections.Generic;
using UnityEngine;
using WaterConveyorSort.BoardSystem.Buoys;

namespace WaterConveyorSort.BoardSystem.Conveyor
{
    public sealed class ConveyorBuoyGroup
    {
        private readonly List<Buoy> buoys = new List<Buoy>();
        public IReadOnlyList<Buoy> Buoys => buoys;
        internal readonly ConveyorBuoyGroupVisual Visual;
        internal double Percent;
        internal bool Moving;
        private readonly float spacing;
        public ConveyorBuoyGroup(ConveyorBuoyGroupVisual visual, double percent, float spacing)
        { Visual = visual; Percent = percent; this.spacing = spacing; }
        public Vector3 GetSlotPosition(int slot) => Visual.transform.TransformPoint(Vector3.up * (slot * spacing));
        public void Receive(Buoy buoy, int slot)
        {
            buoy.Visual.transform.SetParent(Visual.transform, true);
            buoy.Visual.transform.localPosition = Vector3.up * (slot * spacing);
            buoy.Visual.transform.localRotation = Quaternion.identity;
            buoys.Add(buoy);
            if (slot == 0) Moving = true;
        }
        public void Clear()
        {
            foreach (Buoy buoy in buoys) buoy.Clear();
            buoys.Clear();
            if (Visual != null) Object.Destroy(Visual.gameObject);
        }
    }
}
