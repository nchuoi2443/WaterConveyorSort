using UnityEngine;

namespace WaterConveyorSort.BoardSystem.Buoys
{
    public sealed class BuoyVisual : MonoBehaviour
    {
        [Tooltip("Only renderers which should receive the buoy color.")]
        [SerializeField] private Renderer[] colorRenderers;
        [SerializeField] private BuoyColorConfig colorConfig;
        private MaterialPropertyBlock propertyBlock;
        private static readonly int BaseColor = Shader.PropertyToID("_Color");

        public void Refresh(int colorId, Color color)
        {
            if (colorConfig != null)
            {
                Material material = colorConfig.GetMaterial(colorId);
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
            foreach (Renderer target in colorRenderers)
            {
                if (target == null) continue;
                target.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColor, color);
                target.SetPropertyBlock(propertyBlock);
            }
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
        private bool released;

        public void Release()
        {
            if (released) return;
            released = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
