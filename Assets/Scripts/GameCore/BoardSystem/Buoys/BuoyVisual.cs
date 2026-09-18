using System.Collections.Generic;
using UnityEngine;
using WaterConveyorSort.LevelData;

namespace WaterConveyorSort.BoardSystem.Buoys
{
    public sealed class BuoyVisual : MonoBehaviour
    {
        [Tooltip("Only renderers which should receive the buoy color.")]
        [SerializeField] private Renderer[] colorRenderers;
        private MaterialPropertyBlock propertyBlock;
        private static readonly int BaseColor = Shader.PropertyToID("_Color");

        public void Refresh(int colorId, Color color, ColorDataSO colors)
        {
            CacheDefaultMaterials();
            if (colors.TryGetMaterial(colorId, out Material material))
            {
                if (colorRenderers == null) return;
                foreach (Renderer target in colorRenderers)
                {
                    if (target == null) continue;
                    target.SetPropertyBlock(null);
                    target.sharedMaterial = material;
                }
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            if (colorRenderers == null) return;
            for (int i = 0; i < colorRenderers.Length; i++)
            {
                Renderer target = colorRenderers[i];
                if (target == null) continue;
                target.sharedMaterials = defaultMaterials[i];
                target.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColor, color);
                target.SetPropertyBlock(propertyBlock);
            }
        }
        private Material[][] defaultMaterials;

        private void CacheDefaultMaterials()
        {
            if (defaultMaterials != null || colorRenderers == null) return;
            defaultMaterials = new Material[colorRenderers.Length][];
            for (int i = 0; i < colorRenderers.Length; i++)
                if (colorRenderers[i] != null) defaultMaterials[i] = colorRenderers[i].sharedMaterials;
        }

        public bool MoveTowards(Vector3 target, float distance)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, distance);
            return (transform.position - target).sqrMagnitude <= 0.000001f;
        }

        [Tooltip("Optional model child to spin independently of the movement root.")]
        [SerializeField] private Transform spinRoot;
        [Tooltip("Head visual hidden during flight and shown again on landing.")]
        [SerializeField] private GameObject head;
        [SerializeField, Min(0f)] private float flightArcHeight = 1f;
        public float FlightArcHeight => Mathf.Max(0f, flightArcHeight);
        private Transform spinningTransform;
        private Quaternion flightRotation;
        private Quaternion headFacingOffset;
        private void Awake()
        {
            if (head != null) headFacingOffset = Quaternion.Inverse(transform.rotation) * head.transform.rotation;
        }
        public void SetHeadDirection(Vector3 direction, Vector3 up)
        {
            if (head == null || direction.sqrMagnitude < 0.000001f) return;
            head.transform.rotation = Quaternion.LookRotation(direction, up) * headFacingOffset;
        }
        public void BeginFlight()
        {
            if (head != null) head.SetActive(false);
            spinningTransform = spinRoot != null ? spinRoot : transform;
            flightRotation = spinningTransform.localRotation;
        }
        public void SetFlightProgress(float progress)
        {
            if (spinningTransform != null)
                spinningTransform.localRotation = flightRotation * Quaternion.AngleAxis(180f * Mathf.Clamp01(progress), Vector3.right);
        }
        public void EndFlight()
        {
            spinningTransform = null;
            if (head != null) head.SetActive(true);
        }
        private static readonly Dictionary<BuoyVisual, Stack<BuoyVisual>> pools = new Dictionary<BuoyVisual, Stack<BuoyVisual>>();
        private BuoyVisual poolPrefab;
        private bool released;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPools() => pools.Clear();

        public static BuoyVisual Rent(BuoyVisual prefab, Transform parent)
        {
            if (!pools.TryGetValue(prefab, out Stack<BuoyVisual> pool))
            {
                pool = new Stack<BuoyVisual>();
                pools.Add(prefab, pool);
            }
            BuoyVisual instance = null;
            while (pool.Count > 0 && instance == null) instance = pool.Pop();
            if (instance == null) instance = Instantiate(prefab, parent);
            instance.poolPrefab = prefab;
            instance.released = false;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = prefab.transform.localPosition;
            instance.transform.localRotation = prefab.transform.localRotation;
            instance.transform.localScale = prefab.transform.localScale;
            if (instance.spinRoot != null && prefab.spinRoot != null)
                instance.spinRoot.localRotation = prefab.spinRoot.localRotation;
            if (instance.head != null && prefab.head != null)
                instance.head.transform.localRotation = prefab.head.transform.localRotation;
            instance.EndFlight();
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Release()
        {
            if (released) return;
            released = true;
            gameObject.SetActive(false);
            if (poolPrefab != null && pools.TryGetValue(poolPrefab, out Stack<BuoyVisual> pool))
            {
                transform.SetParent(null, false);
                pool.Push(this);
            }
            else Destroy(gameObject);
        }
    }
}
