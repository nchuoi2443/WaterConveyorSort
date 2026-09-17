using System;
using UnityEngine;
using WaterConveyorSort.InputHandling;

namespace WaterConveyorSort.BoardSystem.Buoys
{
    public sealed class BuoyStackVisual : MonoBehaviour, IInputReceiver
    {
        [SerializeField] private Transform buoyRoot;
        [SerializeField] private Vector3 firstBuoyOffset = new Vector3(0f, 0.2f, 0f);
        [SerializeField, Min(0f)] private float buoySpacing = 0f;
        [SerializeField, Min(0.01f)] private float buoyHeight = 0.2f;
        [Tooltip("Pole height in stack-local units when there are no buoys. Zero hides the empty pole.")]
        [SerializeField, Min(0f)] private float emptyStackHeight = 0.2f;
        [SerializeField] private Transform poleTransform;
        public float Step => buoyHeight + buoySpacing;
        private Vector3 poleScale, polePosition;
        private Bounds poleBounds;
        private bool poleCached;
        private bool inputEnabled;
        private int count;

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (inputColliders == null) return;
            foreach (Collider item in inputColliders)
                if (item != null) item.enabled = enabled && count > 0;
        }

        public void RefreshHeight(int buoyCount)
        {
            count = buoyCount;
            float height = count > 0 ? buoyHeight * count + buoySpacing * (count - 1) : Mathf.Max(0f, emptyStackHeight);
            if (poleTransform != null)
            {
                if (!poleCached)
                {
                    var mesh = poleTransform.GetComponent<MeshFilter>();
                    if (mesh != null && mesh.sharedMesh != null)
                    {
                        poleBounds = mesh.sharedMesh.bounds;
                        poleScale = poleTransform.localScale;
                        polePosition = poleTransform.localPosition;
                        poleCached = true;
                    }
                }
                poleTransform.gameObject.SetActive(height > 0f);
                if (poleCached && height > 0f && poleBounds.size.y > 0f)
                {
                    Vector3 scale = poleScale;
                    float parentScale = poleTransform.parent.TransformVector(Vector3.up).magnitude;
                    scale.y = height * transform.TransformVector(Vector3.up).magnitude / (poleBounds.size.y * parentScale);
                    poleTransform.localScale = scale;
                    poleTransform.localPosition = polePosition + Vector3.up * (poleBounds.min.y * (poleScale.y - scale.y));
                }
            }
            if (inputColliders != null)
                foreach (Collider item in inputColliders)
                    if (item is CapsuleCollider capsule)
                    {
                        capsule.height = Mathf.Max(height, capsule.radius * 2f);
                        Vector3 center = capsule.center;
                        center.y = firstBuoyOffset.y + (height - buoyHeight) * 0.5f;
                        capsule.center = center;
                    }
            SetInputEnabled(inputEnabled);
        }

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
            SetInputEnabled(owner.CanReceiveInput);
        }

        public void OnClick()
        {
            if (CanReceiveInput) owner.OnClick();
        }

        private void OnEnable() => RegisterInput();
        private void OnDisable() => UnregisterInput();
        private void OnValidate()
        {
            if (Application.isPlaying && !released) RefreshHeight(count);
        }

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

        public Vector3 GetBuoyPosition(int index) => (buoyRoot != null ? buoyRoot : transform)
            .TransformPoint(firstBuoyOffset + Vector3.up * (index * Step));

        public void PlaceBuoy(Transform buoy, int index)
        {
            // Source order is bottom to top for the initial layout.
            buoy.SetParent(buoyRoot != null ? buoyRoot : transform, false);
            buoy.localPosition = firstBuoyOffset + Vector3.up * (index * Step);
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
