using UnityEngine;

namespace BrothersBlock
{
    public sealed class FollowCamera : MonoBehaviour
    {
        public static FollowCamera Instance { get; private set; }
        public float Yaw { get; private set; } = 0;
        private float pitch = 24f;
        private bool following;

        private void Awake() { Instance = this; }

        private void LateUpdate()
        {
            PlayerMotor player = PlayerMotor.Local;
            if (player == null)
            {
                following = false;
                float angle = Time.unscaledTime * .018f;
                transform.position = new Vector3(Mathf.Sin(angle) * 62f, 52, -Mathf.Cos(angle) * 62f);
                transform.LookAt(new Vector3(0, 0, 4));
                return;
            }
            Vector2 look = GameHud.Instance == null ? Vector2.zero : GameHud.Instance.ConsumeLook();
            if (!Application.isMobilePlatform && Input.GetMouseButton(1))
                look += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 15f;
            Yaw += look.x * .13f;
            pitch = Mathf.Clamp(pitch - look.y * .11f, 8f, 65f);
            Vector3 focus = player.transform.position + Vector3.up * 1.55f;
            Quaternion rotation = Quaternion.Euler(pitch, Yaw, 0);
            Vector3 offset = rotation * new Vector3(0, 0, -6.5f);
            RaycastHit hit;
            float distance = Physics.SphereCast(focus, .22f, offset.normalized, out hit, offset.magnitude, ~(1 << 2), QueryTriggerInteraction.Ignore) ? Mathf.Max(.5f, hit.distance - .15f) : offset.magnitude;
            Vector3 desired = focus + offset.normalized * distance;
            // Snap inward around obstacles; smooth outward motion to avoid camera clipping.
            transform.position = !following || distance < 6.3f ? desired : Vector3.Lerp(transform.position, desired, 1 - Mathf.Exp(-15f * Time.deltaTime));
            transform.rotation = rotation;
            following = true;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
