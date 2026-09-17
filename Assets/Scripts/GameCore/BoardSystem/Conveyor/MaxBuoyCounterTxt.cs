using System;
using TMPro;
using UnityEngine;

namespace WaterConveyorSort.BoardSystem.Conveyor
{
    public sealed class MaxBuoyCounterTxt : MonoBehaviour
    {
        [SerializeField] private TMP_Text counterText;
        [SerializeField] private float heightOffset = 0.8f;
        public float HeightOffset => heightOffset;
        private ConveyorController conveyor;

        public void ValidateSetup()
        {
            if (counterText == null)
                throw new InvalidOperationException("Assign Counter Text on your MaxBuoyCounterTxt prefab.");
        }

        public void Bind(ConveyorController controller)
        {
            ValidateSetup();
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            Unbind();
            conveyor = controller;
            conveyor.GroupCountChanged += Refresh;
            Refresh(conveyor.CurrentGroupCount, conveyor.MaxBuoyInConveyor);
        }
        private void Refresh(int current, int maximum) => counterText.SetText("{0}/{1}", current, maximum);
        private void Unbind()
        {
            if (conveyor != null) conveyor.GroupCountChanged -= Refresh;
            conveyor = null;
        }
        public void Release()
        {
            Unbind();
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        private void OnDestroy() => Unbind();
    }
}
