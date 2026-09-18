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
        [Header("Consume Animation")]
        [SerializeField, Min(0.01f)] private float consumeCollapseDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float consumePunchDuration = 0.12f;
        [SerializeField, Min(0.01f)] private float consumeShrinkDuration = 0.18f;
        [SerializeField, Min(1f)] private float consumePunchScale = 1.3f;
        public float ConsumeCollapseDuration => Mathf.Max(0.01f, consumeCollapseDuration);
        public float ConsumePunchDuration => Mathf.Max(0.01f, consumePunchDuration);
        public float ConsumeShrinkDuration => Mathf.Max(0.01f, consumeShrinkDuration);
        public float ConsumePunchScale => Mathf.Max(1f, consumePunchScale);

        [Header("Incoming Group")]
        [SerializeField, Min(0f)] private float receiveHeightTweenDuration = 0.2f;
        [SerializeField, Min(0.01f)] private float receiveDescentDuration = 0.25f;
        [SerializeField, Min(0f)] private float receiveTopClearance = 0.2f;
        public float ReceiveDescentDuration => Mathf.Max(0.01f, receiveDescentDuration);
        public float ReceiveTopClearance => Mathf.Max(0f, receiveTopClearance);
        private int reservedCount = -1;
        private float displayedHeight, tweenStartHeight, heightElapsed;
        private float HeightForCount(int value) => value > 0 ? buoyHeight * value + buoySpacing * (value - 1) : Mathf.Max(0f, emptyStackHeight);
        public void BeginReceiveHeight(int expectedCount)
        {
            if (fixedHeight) return;
            reservedCount = expectedCount;
            tweenStartHeight = displayedHeight;
            heightElapsed = 0f;
            if (receiveHeightTweenDuration <= 0f) ApplyHeight(HeightForCount(reservedCount));
        }
        public void TickReceiveHeight(float deltaTime)
        {
            if (reservedCount < 0 || fixedHeight) return;
            heightElapsed += Mathf.Max(0f, deltaTime);
            float progress = receiveHeightTweenDuration > 0f ? Mathf.Clamp01(heightElapsed / receiveHeightTweenDuration) : 1f;
            ApplyHeight(Mathf.Lerp(tweenStartHeight, HeightForCount(reservedCount), Mathf.SmoothStep(0f, 1f, progress)));
        }
        public void EndReceiveHeight()
        {
            reservedCount = -1;
            RefreshHeight(count);
        }
        private bool fixedHeight;
        private float fixedStackHeight;
        public void SetFixedHeight(float height)
        {
            fixedHeight = true;
            fixedStackHeight = Mathf.Max(0.01f, height);
            RefreshHeight(count);
        }
        public Vector3 GetTopPosition()
        {
            if (poleCached)
            {
                Vector3 top = poleTransform.TransformPoint(new Vector3(poleBounds.center.x, poleBounds.max.y, poleBounds.center.z));
                if (reservedCount >= 0)
                    top += transform.TransformVector(Vector3.up) * Mathf.Max(0f, HeightForCount(reservedCount) - displayedHeight);
                return top;
            }
            return transform.TransformPoint(firstBuoyOffset + Vector3.up * ((fixedHeight ? fixedStackHeight : HeightForCount(reservedCount >= 0 ? reservedCount : count)) - buoyHeight * 0.5f));
        }

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
            if (reservedCount >= 0 && !fixedHeight) { SetInputEnabled(inputEnabled); return; }
            ApplyHeight(fixedHeight ? fixedStackHeight : HeightForCount(count));
        }
        private void ApplyHeight(float height)
        {
            displayedHeight = height;
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
