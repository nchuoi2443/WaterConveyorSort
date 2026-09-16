using System;
using UnityEngine;
using WaterConveyorSort.InputHandling;

namespace WaterConveyorSort.BoardSystem.Buoys
{
    public sealed class BuoyStackVisual : MonoBehaviour, IInputReceiver
    {
        [SerializeField] private Transform buoyRoot;
        [SerializeField] private Vector3 firstBuoyOffset = new Vector3(0f, 0.2f, 0f);
        [SerializeField, Min(0.01f)] private float buoySpacing = 0.2f;

        [SerializeField] private Collider[] inputColliders;
        private InputSystem inputSystem;
        private BuoyStack owner;
        public bool CanReceiveInput => !released && isActiveAndEnabled && owner != null && owner.CanReceiveInput;

        public void BindInput(BuoyStack stack, InputSystem system)
        {
            if (stack == null) throw new ArgumentNullException(nameof(stack));
            if (system == null) throw new ArgumentNullException(nameof(system));
            UnregisterInput();
            owner = stack;
            inputSystem = system;
            if (inputColliders == null || inputColliders.Length == 0)
                inputColliders = GetComponents<Collider>();
            if (inputColliders.Length == 0)
                throw new InvalidOperationException($"Stack visual '{name}' requires an input collider.");
            if (isActiveAndEnabled) RegisterInput();
        }

        public void OnClick()
        {
            if (CanReceiveInput) owner.OnClick();
        }

        private void OnEnable() => RegisterInput();
        private void OnDisable() => UnregisterInput();

        private void RegisterInput()
        {
            if (inputSystem == null || owner == null || released) return;
            foreach (Collider inputCollider in inputColliders)
            {
                if (inputCollider != null) inputSystem.Register(inputCollider, this);
            }
        }

        private void UnregisterInput()
        {
            if (inputSystem == null || inputColliders == null) return;
            foreach (Collider inputCollider in inputColliders)
                inputSystem.Unregister(inputCollider, this);
        }

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
            UnregisterInput();
            owner = null;
            inputSystem = null;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
