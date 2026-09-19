using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BrothersBlock
{
    public sealed class BenderCombat : NetworkBehaviour
    {
        public readonly NetworkVariable<int> Kind = new NetworkVariable<int>();
        public readonly NetworkVariable<int> Preset = new NetworkVariable<int>();
        public readonly NetworkVariable<int> Outfit = new NetworkVariable<int>();
        public readonly NetworkVariable<int> Skin = new NetworkVariable<int>();
        public readonly NetworkVariable<int> Hair = new NetworkVariable<int>();
        public readonly NetworkVariable<int> XP = new NetworkVariable<int>();
        public readonly NetworkVariable<int> HP = new NetworkVariable<int>(100);
        public readonly NetworkVariable<int> Shield = new NetworkVariable<int>();
        public readonly NetworkVariable<int> Score = new NetworkVariable<int>();
        public readonly NetworkVariable<float> StunUntil = new NetworkVariable<float>();
        public readonly NetworkVariable<float> BoostUntil = new NetworkVariable<float>();
        public static BenderCombat Local { get { return PlayerMotor.Local == null ? null : PlayerMotor.Local.GetComponent<BenderCombat>(); } }
        public Element Element { get { return (Element)Kind.Value; } }
        public bool Down { get { return HP.Value <= 0; } }
        public float Now { get { return NetworkManager == null ? Time.time : (float)NetworkManager.ServerTime.Time; } }
        public bool Stunned { get { return Now < StunUntil.Value; } }
        public bool Boosted { get { return Now < BoostUntil.Value; } }
        private readonly float[] localReady = new float[4];
        private readonly float[] serverReady = new float[4];
        private bool configured;
        private float respawnAt;
        private float immuneUntil;
        private AvatarLook look;

        public override void OnNetworkSpawn()
        {
            look = gameObject.AddComponent<AvatarLook>();
            if (IsOwner) {
                ConfigureServerRpc((int)JourneyProfile.Kind, JourneyProfile.Preset, JourneyProfile.Outfit, JourneyProfile.Skin, JourneyProfile.Hair, JourneyProfile.XP(JourneyProfile.Kind));
                XP.OnValueChanged += SaveProgress;
            }
            if (IsServer) immuneUntil = Now + 3;
        }
        private void SaveProgress(int oldXP, int newXP)
        {
            if (!IsOwner) return;
            JourneyProfile.SaveXP(Element, newXP);
            if (BendingRules.Level(newXP) > BendingRules.Level(oldXP) && GameHud.Instance != null)
                GameHud.Instance.ShowToast("LEVEL " + BendingRules.Level(newXP) + "! Check your new abilities.");
        }
        [ServerRpc]
        private void ConfigureServerRpc(int kind, int preset, int outfit, int skin, int hair, int xp)
        {
            if (configured) return;
            configured = true; Preset.Value = Mathf.Clamp(preset, 0, 8);
            Kind.Value = Preset.Value == 0 ? Mathf.Clamp(kind, 0, 4) : (int)BendingRules.PresetElements[Preset.Value];
            Outfit.Value = Mathf.Clamp(outfit, 0, 5); Skin.Value = Mathf.Clamp(skin, 0, 4); Hair.Value = Mathf.Clamp(hair, 0, 2);
            XP.Value = Mathf.Clamp(xp, 0, 100000);
        }
        private void Update()
        {
            if (!IsSpawned) return;
            if (IsServer && Down && Now >= respawnAt) Respawn(100);
            if (!IsOwner) return;
            if (Input.GetKeyDown(KeyCode.F) || (!Application.isMobilePlatform && Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))) TryCast(0);
            if (Input.GetKeyDown(KeyCode.Alpha1)) TryCast(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TryCast(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TryCast(3);
            if (Input.GetKeyDown(KeyCode.E)) Interact();
        }
        public float Cooldown(int slot) { return Mathf.Max(0, localReady[slot] - Time.unscaledTime); }
        public void TryCast(int slot, Vector3? direction = null)
        {
            if (!IsOwner || Down || Stunned || slot < 0 || slot > 3) return;
            if (!BendingRules.Unlocked(XP.Value, slot)) { GameHud.Instance.ShowToast("Unlocks at level " + BendingRules.UnlockLevels[slot]); return; }
            if (Cooldown(slot) > 0) return;
            localReady[slot] = Time.unscaledTime + BendingRules.Cooldowns[slot];
            Vector3 aim = direction ?? (Quaternion.Euler(0, FollowCamera.Instance == null ? transform.eulerAngles.y : FollowCamera.Instance.Yaw, 0) * Vector3.forward);
            CastServerRpc(slot, aim);
        }
        [ServerRpc]
        private void CastServerRpc(int slot, Vector3 aim)
        {
            if (slot < 0 || slot > 3 || Down || Stunned || !BendingRules.Unlocked(XP.Value, slot) || Now < serverReady[slot]) return;
            if (float.IsNaN(aim.x) || float.IsNaN(aim.z) || float.IsInfinity(aim.x) || float.IsInfinity(aim.z)) return;
            aim.y = 0; if (aim.sqrMagnitude < .1f) return; aim.Normalize();
            if (AdventureDirector.Instance == null || !AdventureDirector.Instance.CanFight) return;
            serverReady[slot] = Now + BendingRules.Cooldowns[slot];
            AdventureDirector.Instance.Cast(this, slot, aim);
        }
        public void Interact() { if (IsOwner && !Down) InteractServerRpc(); }
        [ServerRpc] private void InteractServerRpc() { if (!Down && AdventureDirector.Instance != null) AdventureDirector.Instance.Interact(this); }
        public void AddXP(int amount) { if (IsServer) XP.Value = Mathf.Clamp(XP.Value + amount, 0, 100000); }
        public void Heal(int amount) { if (IsServer && !Down) HP.Value = Mathf.Min(100, HP.Value + amount); }
        public void Hurt(int damage, float stun, BenderCombat attacker = null)
        {
            if (!IsServer || Down || Now < immuneUntil) return;
            int absorbed = Mathf.Min(Shield.Value, damage); Shield.Value -= absorbed;
            HP.Value = Mathf.Max(0, HP.Value - damage + absorbed);
            if (stun > 0) StunUntil.Value = Mathf.Max(StunUntil.Value, Now + stun);
            if (Down) {
                respawnAt = Now + 8;
                if (AdventureDirector.Instance != null) AdventureDirector.Instance.PlayerDown(this, attacker);
            }
        }
        public void Revive() { if (!IsServer || !Down) return; HP.Value = 45; StunUntil.Value = 0; immuneUntil = Now + 3; }
        public void Respawn(int health)
        {
            if (!IsServer) return;
            HP.Value = health; Shield.Value = 0; StunUntil.Value = 0; BoostUntil.Value = 0; immuneUntil = Now + 3;
            TeleportClientRpc(new Vector3(OwnerClientId == 0 ? -3 : 3, 1, -10));
        }
        [ClientRpc] private void TeleportClientRpc(Vector3 position) { if (IsOwner) GetComponent<PlayerMotor>().Teleport(position); }
        [ClientRpc] public void EffectClientRpc(Vector3 from, Vector3 to, int kind, int slot) { BendingEffect.Play(from, to, (Element)kind, slot); }
        [ClientRpc] public void WorldClientRpc(string json) { if (!IsServer && AdventureDirector.Instance != null) AdventureDirector.Instance.Receive(json); }
        [ClientRpc] public void NoticeClientRpc(string message) { if (GameHud.Instance != null) GameHud.Instance.ShowToast(message); }
        public override void OnNetworkDespawn()
        {
            if (IsOwner) { JourneyProfile.SaveXP(Element, XP.Value); XP.OnValueChanged -= SaveProgress; }
            if (look != null) Destroy(look);
        }
    }
}
