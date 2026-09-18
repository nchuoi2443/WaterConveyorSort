using System;
using TMPro;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using WaterConveyorSort.BoardSystem.Conveyor;

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

        public void SetOutletDirection(Vector2Int direction)
        {
            // Grid Y maps to board-local Z. The visual root is a direct child of boardRoot.
            // Keep unconfigured nodes at the default orientation for editor previews.
            transform.localRotation = direction == Vector2Int.zero
                ? Quaternion.identity
                : Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y), Vector3.up);
        }

        [SerializeField] private Collider receiveCollider;
        [SerializeField, Min(0f)] private float advanceDuration = 0.3f;
        [Header("Queue Status")]
        [SerializeField] private TMP_Text remainingStackText;
        [SerializeField] private GameObject imgTick;
        [SerializeField] private Transform statusRoot;
        [SerializeField] private Camera statusCamera;
        public void SetQueueStatus(int capacity, int remaining)
        {
            bool show = capacity > 1;
            if (remainingStackText != null)
            {
                remainingStackText.text = remaining.ToString();
                remainingStackText.gameObject.SetActive(show && remaining > 0);
            }
            if (imgTick != null) imgTick.SetActive(show && remaining == 0);
        }
        private void LateUpdate()
        {
            if (statusRoot == null) return;
            Camera camera = statusCamera != null ? statusCamera : Camera.main;
            if (camera != null) statusRoot.rotation = camera.transform.rotation;
        }
        private BuoyStackHolder owner;
        private Coroutine advanceTween;

        public void Initialize(BuoyStackHolder holder, int visibleCapacity)
        {
            owner = holder;
            if (singleStackVisual != null) singleStackVisual.SetActive(visibleCapacity == 1);
            if (multipleStackVisual != null) multipleStackVisual.SetActive(visibleCapacity == 2);
            if (receiveCollider == null) receiveCollider = GetComponent<Collider>();
            if (receiveCollider == null || !receiveCollider.isTrigger || receiveCollider.gameObject != gameObject)
                throw new InvalidOperationException("Holder requires a trigger collider on its root.");
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        internal bool ContainsGroup(ConveyorBuoyGroup group) => gameObject.activeInHierarchy && receiveCollider != null &&
            receiveCollider.enabled && (receiveCollider.ClosestPoint(group.transform.position) - group.transform.position)
            .sqrMagnitude <= group.DetectionRadius * group.DetectionRadius;

        private void OnTriggerEnter(Collider other) => DetectGroup(other);
        private void OnTriggerStay(Collider other) => DetectGroup(other);
        private void DetectGroup(Collider other)
        {
            if (released || owner == null) return;
            ConveyorBuoyGroup group = other.GetComponent<ConveyorBuoyGroup>();
            if (group != null) owner.TryReceive(group);
        }

        public void TweenToFront(Transform stack, Action completed)
        {
            if (advanceTween != null) StopCoroutine(advanceTween);
            if (advanceDuration <= 0f)
            {
                stack.localPosition = firstStackOffset;
                completed();
                return;
            }
            advanceTween = StartCoroutine(AdvanceStack(stack, completed));
        }

        private IEnumerator AdvanceStack(Transform stack, Action completed)
        {
            Vector3 start = stack.localPosition;
            float elapsed = 0f;
            while (elapsed < advanceDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / advanceDuration));
                stack.localPosition = Vector3.LerpUnclamped(start, firstStackOffset, t);
                yield return null;
            }
            stack.localPosition = firstStackOffset;
            advanceTween = null;
            completed();
        }

        public void PlaceStack(Transform stack, int index)
        {
            stack.SetParent(stackRoot != null ? stackRoot : transform, false);
            stack.localPosition = firstStackOffset + stackStep * index;
            stack.localRotation = Quaternion.identity;
        }
        [Header("Disappear Animation")]
        [Tooltip("Time to shrink to zero after the punch.")]
        [SerializeField, Min(0.01f)] private float disappearDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float disappearPunchDuration = 0.12f;
        [SerializeField, Min(1f)] private float disappearPunchScale = 1.15f;
        private Coroutine disappearTween;
        private static readonly Dictionary<BuoyStackHolderVisual, Stack<BuoyStackHolderVisual>> pools = new Dictionary<BuoyStackHolderVisual, Stack<BuoyStackHolderVisual>>();
        private BuoyStackHolderVisual poolPrefab;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPools() => pools.Clear();

        public static BuoyStackHolderVisual Rent(BuoyStackHolderVisual prefab, Transform parent)
        {
            if (!pools.TryGetValue(prefab, out Stack<BuoyStackHolderVisual> pool))
            {
                pool = new Stack<BuoyStackHolderVisual>();
                pools.Add(prefab, pool);
            }
            BuoyStackHolderVisual instance = null;
            while (pool.Count > 0 && instance == null) instance = pool.Pop();
            if (instance == null) instance = Instantiate(prefab, parent);
            instance.poolPrefab = prefab;
            instance.released = false;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = prefab.transform.localPosition;
            instance.transform.localRotation = prefab.transform.localRotation;
            instance.transform.localScale = prefab.transform.localScale;
            if (instance.receiveCollider != null) instance.receiveCollider.enabled = true;
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void PlayDisappear(Action completed, Func<bool> isPaused)
        {
            if (released || disappearTween != null) return;
            if (receiveCollider != null) receiveCollider.enabled = false;
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
            if (disappearTween != null) StopCoroutine(disappearTween);
            disappearTween = null;
            owner = null;
            if (advanceTween != null) StopCoroutine(advanceTween);
            advanceTween = null;
            gameObject.SetActive(false);
            if (poolPrefab != null && pools.TryGetValue(poolPrefab, out Stack<BuoyStackHolderVisual> pool))
            {
                transform.SetParent(null, false);
                pool.Push(this);
            }
            else Destroy(gameObject);
        }
    }
}
