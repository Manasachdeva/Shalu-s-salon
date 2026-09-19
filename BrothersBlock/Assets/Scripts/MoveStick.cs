using UnityEngine;
using UnityEngine.EventSystems;

namespace BrothersBlock
{
    public sealed class MoveStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Knob;
        public Vector2 Value { get; private set; }
        private int pointer = int.MinValue;

        public void OnPointerDown(PointerEventData data)
        {
            if (pointer != int.MinValue) return;
            pointer = data.pointerId;
            OnDrag(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if (data.pointerId != pointer) return;
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, data.position, data.pressEventCamera, out position);
            Value = Vector2.ClampMagnitude(position / 60f, 1f);
            Knob.anchoredPosition = Value * 60;
        }
        public void OnPointerUp(PointerEventData data) { if (data.pointerId == pointer) ResetControl(); }
        private void ResetControl() { pointer = int.MinValue; Value = Vector2.zero; if (Knob != null) Knob.anchoredPosition = Vector2.zero; }
        private void OnDisable() { ResetControl(); }
    }
}
