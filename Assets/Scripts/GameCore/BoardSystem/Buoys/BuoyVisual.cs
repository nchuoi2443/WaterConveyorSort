using UnityEngine;

namespace WaterConveyorSort.BoardSystem.Buoys
{
    public sealed class BuoyVisual : MonoBehaviour
    {
        [Tooltip("Only renderers which should receive the buoy color.")]
        [SerializeField] private Renderer[] colorRenderers;
        private MaterialPropertyBlock propertyBlock;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public void Refresh(Color color)
        {
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
