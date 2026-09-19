using UnityEngine;
using UnityEngine.EventSystems;

namespace BrothersBlock
{
    public sealed class LookPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private int pointer = int.MinValue;
        private Vector2 pending;
        public void OnPointerDown(PointerEventData data) { if (pointer == int.MinValue) pointer = data.pointerId; }
        public void OnDrag(PointerEventData data)
        {
            if (data.pointerId == pointer) pending += data.delta / GetComponentInParent<Canvas>().scaleFactor;
        }
        public void OnPointerUp(PointerEventData data) { if (data.pointerId == pointer) pointer = int.MinValue; }
        public Vector2 Consume() { Vector2 value = pending; pending = Vector2.zero; return value; }
        private void OnDisable() { pending = Vector2.zero; pointer = int.MinValue; }
    }
}
