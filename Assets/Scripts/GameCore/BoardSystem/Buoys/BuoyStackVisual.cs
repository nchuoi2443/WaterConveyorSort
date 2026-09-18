using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
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
            // Keep the empty holder pole visible until its disappearance animation finishes.
            if (count == 0 && !fixedHeight && displayedHeight > 0f)
            {
                SetInputEnabled(inputEnabled);
                return;
            }
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

        private Transform headFacingRoot;

        internal void SetHeadFacingRoot(Transform holderRoot)
        {
            headFacingRoot = holderRoot;
        }

        internal void AlignBuoyHead(BuoyVisual buoy)
        {
            if (headFacingRoot != null)
                buoy.SetHeadDirection(headFacingRoot.forward, headFacingRoot.up);
        }

        public void PlaceBuoy(Transform buoy, int index)
        {
            // Source order is bottom to top for the initial layout.
            buoy.SetParent(buoyRoot != null ? buoyRoot : transform, false);
            buoy.localPosition = firstBuoyOffset + Vector3.up * (index * Step);
        }
        private readonly List<ConsumeAnimation> consumeAnimations = new List<ConsumeAnimation>();
        internal bool IsConsuming => consumeAnimations.Count > 0;

        internal void PlayConsume(Transform[] group, Action completed)
        {
            consumeAnimations.Add(new ConsumeAnimation(group, completed));
        }

        internal void TickConsume(float deltaTime)
        {
            for (int i = consumeAnimations.Count - 1; i >= 0; i--)
            {
                ConsumeAnimation animation = consumeAnimations[i];
                if (!animation.Tick(deltaTime, this)) continue;
                consumeAnimations.RemoveAt(i);
                animation.Completed?.Invoke();
            }
        }

        internal void CancelConsume() => consumeAnimations.Clear();

        private sealed class ConsumeAnimation
        {
            private readonly Transform[] group;
            public Action Completed { get; }
            private readonly Vector3[] positions;
            private readonly Vector3[] scales;
            private float elapsed;

            public ConsumeAnimation(Transform[] group, Action completed)
            {
                this.group = group;
                Completed = completed;
                positions = new Vector3[group.Length];
                scales = new Vector3[group.Length];
                for (int i = 0; i < group.Length; i++)
                {
                    positions[i] = group[i].localPosition;
                    scales[i] = group[i].localScale;
                }
            }

            public bool Tick(float deltaTime, BuoyStackVisual settings)
            {
                elapsed += Mathf.Max(0f, deltaTime);
                int bottom = group.Length - 1;
                float upperPhaseDuration = settings.ConsumeCollapseDuration;
                float upperScale = 1f - Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / upperPhaseDuration));
                // Collapse the upper four around the bottom buoy without punching.
                for (int i = 0; i < bottom; i++)
                {
                    Transform target = group[i];
                    target.localPosition = positions[bottom] + (positions[i] - positions[bottom]) * upperScale;
                    target.localScale = scales[i] * upperScale;
                }
                float bottomPhaseTime = elapsed - upperPhaseDuration;
                if (bottomPhaseTime < 0f) return false;
                float bottomScale = EvaluatePunchShrink(bottomPhaseTime, settings.ConsumePunchDuration,
                    settings.ConsumeShrinkDuration, settings.ConsumePunchScale);
                group[bottom].localScale = scales[bottom] * bottomScale;
                return bottomPhaseTime >= settings.ConsumePunchDuration + settings.ConsumeShrinkDuration;
            }

            private static float EvaluatePunchShrink(float time, float punchDuration, float shrinkDuration, float punchScale)
            {
                if (time < punchDuration)
                    return Mathf.Lerp(1f, punchScale, Mathf.Sin(Mathf.Clamp01(time / punchDuration) * Mathf.PI * 0.5f));
                return Mathf.Lerp(punchScale, 0f,
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((time - punchDuration) / shrinkDuration)));
            }

        }

        [Header("Disappear Animation")]
        [Tooltip("Time to shrink to zero after the punch.")]
        [SerializeField, Min(0.01f)] private float disappearDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float disappearPunchDuration = 0.12f;
        [SerializeField, Min(1f)] private float disappearPunchScale = 1.15f;
        private Coroutine disappearTween;
        private static readonly Dictionary<BuoyStackVisual, Stack<BuoyStackVisual>> pools = new Dictionary<BuoyStackVisual, Stack<BuoyStackVisual>>();
        private BuoyStackVisual poolPrefab;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPools() => pools.Clear();

        public static BuoyStackVisual Rent(BuoyStackVisual prefab, Transform parent)
        {
            if (!pools.TryGetValue(prefab, out Stack<BuoyStackVisual> pool))
            {
                pool = new Stack<BuoyStackVisual>();
                pools.Add(prefab, pool);
            }
            BuoyStackVisual instance = null;
            while (pool.Count > 0 && instance == null) instance = pool.Pop();
            if (instance == null) instance = Instantiate(prefab, parent);
            instance.poolPrefab = prefab;
            instance.released = false;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = prefab.transform.localPosition;
            instance.transform.localRotation = prefab.transform.localRotation;
            instance.transform.localScale = prefab.transform.localScale;
            instance.headFacingRoot = null;
            instance.fixedHeight = false;
            instance.reservedCount = -1;
            instance.count = 0;
            instance.displayedHeight = 0f;
            instance.RefreshHeight(0);
            instance.SetInputEnabled(false);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void PlayDisappear(Action completed, Func<bool> isPaused)
        {
            if (released || disappearTween != null) return;
            SetInputEnabled(false);
            disappearTween = StartCoroutine(Disappear(completed, isPaused));
        }

        private IEnumerator Disappear(Action completed, Func<bool> isPaused)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            float punchDuration = Mathf.Max(0.01f, disappearPunchDuration);
            float shrinkDuration = Mathf.Max(0.01f, disappearDuration);
            float punchScale = Mathf.Max(1f, disappearPunchScale);
            while (elapsed < punchDuration + shrinkDuration)
            {
                if (isPaused == null || !isPaused()) elapsed += Time.deltaTime;
                float scale = elapsed < punchDuration
                    ? Mathf.Lerp(1f, punchScale, Mathf.Sin(Mathf.Clamp01(elapsed / punchDuration) * Mathf.PI * 0.5f))
                    : Mathf.Lerp(punchScale, 0f, Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01((elapsed - punchDuration) / shrinkDuration)));
                transform.localScale = startScale * scale;
                yield return null;
            }
            transform.localScale = Vector3.zero;
            disappearTween = null;
            completed?.Invoke();
        }

        private bool released;

        public void Release()
        {
            if (released) return;
            released = true;
            CancelConsume();
            if (disappearTween != null) StopCoroutine(disappearTween);
            disappearTween = null;
            UnregisterInput();
            owner = null;
            inputSystem = null;
            headFacingRoot = null;
            gameObject.SetActive(false);
            if (poolPrefab != null && pools.TryGetValue(poolPrefab, out Stack<BuoyStackVisual> pool))
            {
                transform.SetParent(null, false);
                pool.Push(this);
            }
            else Destroy(gameObject);
        }
    }
}
