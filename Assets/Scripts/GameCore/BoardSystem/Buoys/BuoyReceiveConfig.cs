using UnityEngine;

namespace WaterConveyorSort.BoardSystem.Buoys
{
    [CreateAssetMenu(fileName = "BuoyReceiveConfig", menuName = "Water Conveyor Sort/Buoy Receive Config")]
    public sealed class BuoyReceiveConfig : ScriptableObject
    {
        [Tooltip("Flight duration of each buoy from the conveyor to the stack, in seconds.")]
        [SerializeField, Min(0.01f)] private float flightDuration = 0.4f;
        [Tooltip("Delay between buoy launches. Zero launches the entire group together.")]
        [SerializeField, Min(0f)] private float launchDelay = 0.12f;

        public float FlightDuration => Mathf.Max(0.01f, flightDuration);
        public float LaunchDelay => Mathf.Max(0f, launchDelay);
    }
}
