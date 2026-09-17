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
