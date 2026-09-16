using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WaterConveyorSort.InputHandling
{
    [DisallowMultipleComponent]
    public sealed class InputSystem : MonoBehaviour
    {
        [SerializeField] private Camera inputCamera;
        [SerializeField] private LayerMask raycastMask = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0.01f)] private float maxDistance = 1000f;

        private readonly Dictionary<Collider, IInputReceiver> receivers = new Dictionary<Collider, IInputReceiver>();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

        public void Register(Collider inputCollider, IInputReceiver receiver)
        {
            if (inputCollider == null) throw new ArgumentNullException(nameof(inputCollider));
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));
            if (receivers.TryGetValue(inputCollider, out IInputReceiver existing) && !ReferenceEquals(existing, receiver))
                throw new InvalidOperationException($"Collider '{inputCollider.name}' already has an input receiver.");
            receivers[inputCollider] = receiver;
        }

        public void Unregister(Collider inputCollider, IInputReceiver receiver)
        {
            if (ReferenceEquals(inputCollider, null)) return;
            if (receivers.TryGetValue(inputCollider, out IInputReceiver existing) && ReferenceEquals(existing, receiver))
                receivers.Remove(inputCollider);
        }

        private void Update()
        {
            // Process only the primary touch and avoid the mouse event simulated by touch.
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began) ProcessPress(touch.position, touch.fingerId);
                return;
            }

            if (Input.GetMouseButtonDown(0)) ProcessPress(Input.mousePosition, -1);
        }

        private void ProcessPress(Vector2 screenPosition, int pointerId)
        {
            if (IsOverUI(screenPosition, pointerId)) return;
            if (inputCamera == null) inputCamera = Camera.main;
            if (inputCamera == null) return;

            Ray ray = inputCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, raycastMask, QueryTriggerInteraction.Ignore) &&
                receivers.TryGetValue(hit.collider, out IInputReceiver receiver) && receiver.CanReceiveInput)
                receiver.OnClick();
        }

        private bool IsOverUI(Vector2 screenPosition, int pointerId)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return false;
            var pointer = new PointerEventData(eventSystem) { position = screenPosition, pointerId = pointerId };
            uiHits.Clear();
            eventSystem.RaycastAll(pointer, uiHits);
            foreach (RaycastResult hit in uiHits)
            {
                if (hit.module is UnityEngine.UI.GraphicRaycaster) return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            receivers.Clear();
        }
    }
}
