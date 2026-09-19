using UnityEngine;
using UnityEngine.EventSystems;

namespace BrothersBlock
{
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool Held { get; private set; }
        private int pointer = int.MinValue;
        public void OnPointerDown(PointerEventData data) { if (pointer == int.MinValue) { pointer = data.pointerId; Held = true; } }
        public void OnPointerUp(PointerEventData data) { if (data.pointerId == pointer) { Held = false; pointer = int.MinValue; } }
        private void OnDisable() { Held = false; pointer = int.MinValue; }
    }
}
