using Unity.Netcode;
using UnityEngine;

namespace BrothersBlock
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : NetworkBehaviour
    {
        public static PlayerMotor Local { get; private set; }
        public Transform LeftArm;
        public Transform RightArm;
        public Transform LeftLeg;
        public Transform RightLeg;
        public Renderer Shirt;
        public TextMesh NameLabel;
        private CharacterController controller;
        private Vector3 previousPosition;
        private float verticalSpeed;
        private float gait;
        private BenderCombat combat;

        public override void OnNetworkSpawn()
        {
            controller = GetComponent<CharacterController>();
            combat = GetComponent<BenderCombat>();
            controller.enabled = IsOwner;
            if (IsOwner) Local = this;
            previousPosition = transform.position;
            if (Shirt != null) Shirt.material.color = OwnerClientId == Unity.Netcode.NetworkManager.ServerClientId ? new Color(.15f, .69f, .68f) : new Color(1f, .54f, .31f);
            if (NameLabel != null) NameLabel.text = IsOwner ? "YOU" : "YOUR BROTHER";
        }

        private void Update()
        {
            if (!IsSpawned) return;
            if (IsOwner) Move();
            Animate();
        }

        private void Move()
        {
            if (combat != null && (combat.Down || combat.Stunned)) return;
            GameHud hud = GameHud.Instance;
            Vector2 movement = hud == null ? Vector2.zero : hud.Movement;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) movement.y += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) movement.y -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) movement.x += 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) movement.x -= 1;
            movement = Vector2.ClampMagnitude(movement, 1f);
            float yaw = FollowCamera.Instance == null ? 0 : FollowCamera.Instance.Yaw;
            Vector3 direction = Quaternion.Euler(0, yaw, 0) * new Vector3(movement.x, 0, movement.y);
            bool running = Input.GetKey(KeyCode.LeftShift) || (hud != null && hud.Running);
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2f;
            bool jump = Input.GetKeyDown(KeyCode.Space) || (hud != null && hud.ConsumeJump());
            bool boosted = combat != null && combat.Boosted;
            if (jump && controller.isGrounded) verticalSpeed = boosted ? 11f : 7f;
            verticalSpeed = Mathf.Max(verticalSpeed - (boosted ? 7f : 20f) * Time.deltaTime, -30f);
            controller.Move((direction * (running ? 8f : 4.8f) * (boosted ? 1.6f : 1f) + Vector3.up * verticalSpeed) * Time.deltaTime);
            if (direction.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 14f * Time.deltaTime);
            if (transform.position.y < -8f || Mathf.Abs(transform.position.x) > 93 || Mathf.Abs(transform.position.z) > 93)
            {
                controller.enabled = false;
                transform.position = new Vector3(IsHost ? -3 : 3, 1, -10);
                controller.enabled = true;
                verticalSpeed = 0;
            }
        }

        private void Animate()
        {
            Vector3 delta = transform.position - previousPosition;
            delta.y = 0;
            float speed = delta.magnitude / Mathf.Max(Time.deltaTime, .001f);
            previousPosition = transform.position;
            gait += Time.deltaTime * Mathf.Min(speed, 8f) * 2.2f;
            float angle = speed > .15f ? Mathf.Sin(gait) * Mathf.Min(speed * 5f, 32f) : 0;
            if (LeftArm != null) LeftArm.localRotation = Quaternion.Euler(angle, 0, 0);
            if (RightArm != null) RightArm.localRotation = Quaternion.Euler(-angle, 0, 0);
            if (LeftLeg != null) LeftLeg.localRotation = Quaternion.Euler(-angle, 0, 0);
            if (RightLeg != null) RightLeg.localRotation = Quaternion.Euler(angle, 0, 0);
            if (NameLabel != null && FollowCamera.Instance != null) NameLabel.transform.rotation = FollowCamera.Instance.transform.rotation;
        }

        public override void OnNetworkDespawn()
        {
            if (Local == this) Local = null;
        }
        public void Teleport(Vector3 position)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            controller.enabled = false; transform.position = position; verticalSpeed = 0; controller.enabled = IsOwner;
        }
    }
}
