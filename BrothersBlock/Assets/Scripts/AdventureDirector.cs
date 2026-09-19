using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrothersBlock
{
    [Serializable] public sealed class RaiderState
    {
        public int id, hp, maxHP, camp;
        public bool elite;
        public Vector3 position;
        public float stunnedUntil;
        [NonSerialized] public float nextAttack;
    }
    [Serializable] public sealed class JourneyState
    {
        public int mode, step, wave, scrolls;
        public bool complete;
        public string objective = "Choose a journey and open your room.";
        public Vector3 marker;
        public bool showMarker;
        public List<RaiderState> enemies = new List<RaiderState>();
    }

    public sealed class AdventureDirector : MonoBehaviour
    {
        public static AdventureDirector Instance { get; private set; }
        public JourneyState State { get; private set; } = new JourneyState();
        public JourneyMode Mode { get { return (JourneyMode)State.mode; } }
        public bool CanFight { get { return !State.complete && (Mode != JourneyMode.Duel || Players().Length == 2); } }
        public static readonly Vector3 Guide = new Vector3(0, 0, 0);
        public static readonly Vector3[] Shrines = { new Vector3(-42,0,38), new Vector3(-42,0,-42), new Vector3(42,0,38) };
        private readonly Dictionary<int, RaiderView> views = new Dictionary<int, RaiderView>();
        private bool started;
        private int nextID;
        private float sendAt, nextWaveAt;
        private GameObject beacon;
        private Material beaconMaterial;
        public static BenderCombat[] Players() { return FindObjectsByType<BenderCombat>(); }
        private void Awake() { Instance = this; }
        private BenderCombat Speaker()
        {
            foreach (BenderCombat player in Players()) if (player.IsSpawned && player.OwnerClientId == 0) return player;
            return null;
        }
        private void Update()
        {
            LanSession session = LanSession.Instance;
            if (session == null || !session.Playing) { if (started) Clear(); return; }
            if (!started) {
                started = true;
                if (session.Manager.IsHost) Begin(JourneyProfile.Mode);
            }
            if (session.Manager.IsHost) {
                TickEnemies(); TickObjectives();
                if (Time.unscaledTime >= sendAt && Speaker() != null) { sendAt = Time.unscaledTime + .12f; Speaker().WorldClientRpc(JsonUtility.ToJson(State)); }
            }
            DrawWorld();
        }
        public void Begin(JourneyMode mode)
        {
            if (LanSession.Instance == null || !LanSession.Instance.Manager.IsHost) return;
            State = new JourneyState { mode = (int)mode, wave = 1 };
            foreach (BenderCombat player in Players()) if (player.IsSpawned) { player.Score.Value = 0; player.Respawn(100); }
            if (mode == JourneyMode.Survival) SpawnWave();
            if (mode == JourneyMode.Adventure) for (int c = 0; c < 3; c++) SpawnCamp(Shrines[c], 3, c + 1, c == 2);
            Notice(mode == JourneyMode.Story ? "The valley's seals have been stolen. Speak to the guide by the blue lantern." : BendingRules.ModeDescriptions[(int)mode]);
            TickObjectives();
        }
        public void Receive(string json)
        {
            try { JourneyState incoming = JsonUtility.FromJson<JourneyState>(json); if (incoming != null && incoming.enemies != null) State = incoming; }
            catch (ArgumentException) { /* A later snapshot will replace a malformed packet. */ }
        }
        private void SpawnCamp(Vector3 centre, int count, int camp, bool boss)
        {
            for (int i = 0; i < count; i++) {
                float angle = (i * 360f / count + 35) * Mathf.Deg2Rad;
                bool elite = boss && i == 0;
                int health = elite ? 160 : 55 + (Mode == JourneyMode.Survival ? Mathf.Min(State.wave * 4, 40) : 0);
                State.enemies.Add(new RaiderState { id = ++nextID, hp = health, maxHP = health, camp = camp, elite = elite,
                    position = centre + new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle)) * 6f, nextAttack = Time.time + 2 });
            }
        }
        private void SpawnWave()
        {
            SpawnCamp(new Vector3(0,0,8), Mathf.Min(2 + State.wave, 10), 0, State.wave % 3 == 0);
            Notice("Wave " + State.wave + ": stand together!"); nextWaveAt = 0;
        }
        private void TickEnemies()
        {
            if (State.complete) return;
            BenderCombat[] players = Players();
            foreach (RaiderState enemy in State.enemies) {
                if (Time.time < enemy.stunnedUntil) continue;
                BenderCombat target = null; float nearest = Mode == JourneyMode.Survival || (Mode == JourneyMode.Story && State.step == 1) ? 150 : 26;
                foreach (BenderCombat player in players) {
                    if (!player.IsSpawned || player.Down) continue;
                    float distance = Vector3.Distance(enemy.position, player.transform.position);
                    if (distance < nearest) { nearest = distance; target = player; }
                }
                if (target == null) continue;
                Vector3 direction = target.transform.position - enemy.position; direction.y = 0; direction.Normalize();
                float attackRange = enemy.elite ? 14 : 2.2f;
                if (nearest > attackRange || !Visible(enemy.position, target.transform.position)) {
                    Vector3 delta = direction * ((enemy.elite ? 1.8f : 2.6f) * Mathf.Min(Time.deltaTime, .1f));
                    if (Physics.Raycast(enemy.position + Vector3.up * .7f, direction, 1.2f, ~(1 << 2)))
                        delta = Quaternion.Euler(0, enemy.id % 2 == 0 ? 80 : -80, 0) * delta;
                    if (!Physics.Raycast(enemy.position + Vector3.up * .7f, delta.normalized, .8f, ~(1 << 2))) enemy.position += delta;
                    enemy.position.x = Mathf.Clamp(enemy.position.x, -82, 82); enemy.position.z = Mathf.Clamp(enemy.position.z, -82, 82);
                } else if (Time.time >= enemy.nextAttack) {
                    enemy.nextAttack = Time.time + (enemy.elite ? 2 : 1.3f);
                    target.Hurt(enemy.elite ? 16 : 8, 0);
                    if (Speaker() != null) Speaker().EffectClientRpc(enemy.position + Vector3.up, target.transform.position + Vector3.up, (int)Element.Fire, 0);
                }
            }
        }
        public static bool Visible(Vector3 a, Vector3 b) { return !Physics.Linecast(a + Vector3.up, b + Vector3.up, ~(1 << 2), QueryTriggerInteraction.Ignore); }
        private void TickObjectives()
        {
            State.showMarker = false;
            if (Mode == JourneyMode.Duel) {
                if (!State.complete) State.objective = Players().Length < 2 ? "DUEL | Waiting for your brother to join." : "DUEL | First to 3 knockouts wins. Face your brother and attack.";
                return;
            }
            if (Mode == JourneyMode.Survival) {
                State.objective = "WAVE " + State.wave + " | " + State.enemies.Count + " raiders remaining. Revive a fallen brother with INTERACT.";
                if (State.enemies.Count == 0) {
                    if (nextWaveAt == 0) { nextWaveAt = Time.time + 7; Award(45); foreach (BenderCombat p in Players()) p.Heal(35); }
                    State.objective = "Wave cleared! Next wave in " + Mathf.CeilToInt(Mathf.Max(0, nextWaveAt - Time.time)) + "s.";
                    if (Time.time >= nextWaveAt) { State.wave++; SpawnWave(); }
                }
                return;
            }
            if (Mode == JourneyMode.Adventure) {
                int collected = ((State.scrolls & 1) != 0 ? 1 : 0) + ((State.scrolls & 2) != 0 ? 1 : 0) + ((State.scrolls & 4) != 0 ? 1 : 0);
                State.complete = collected == 3;
                State.objective = State.complete ? "EXPEDITION COMPLETE | All 3 scrolls found. Return to the guide to start another expedition." : "LOST SCROLLS " + collected + "/3 | Clear each shrine, then INTERACT beside its scroll.";
                State.showMarker = true; State.marker = Guide;
                for (int c = 0; c < 3; c++) if ((State.scrolls & (1 << c)) == 0) { State.marker = Shrines[c]; break; }
                return;
            }
            State.showMarker = true;
            if (State.step == 0) { State.marker = Guide; State.objective = "THE STOLEN SEALS | Speak to the village guide. Follow the blue light, then INTERACT."; }
            if (State.step == 1) {
                State.showMarker = false; State.objective = "PROTECT THE VILLAGE | Defeat " + State.enemies.Count + " raiders.";
                if (State.enemies.Count == 0) { State.step = 2; SpawnCamp(Shrines[0], 3, 1, false); Award(60); Notice("Guide: They took the first seal to the Tide Shrine. Recover it before the commander escapes!"); }
            }
            if (State.step == 2) { State.marker = Shrines[0]; State.objective = "THE TIDE SEAL | Defeat the shrine guards, then INTERACT at the blue shrine."; }
            if (State.step == 3) { State.marker = Shrines[2]; State.objective = "THE ASH COMMANDER | Defeat the commander and guards, then INTERACT at the ember shrine."; }
            if (State.step == 4) { State.marker = Guide; State.objective = "RETURN HOME | Bring the recovered seals to the village guide."; }
            if (State.step == 5) { State.marker = Guide; State.complete = true; State.objective = "CHAPTER COMPLETE | The valley is safe. Host: INTERACT at the guide to replay."; }
        }
        public void Interact(BenderCombat player)
        {
            if (Mode != JourneyMode.Duel) foreach (BenderCombat other in Players()) {
                if (other != player && other.Down && Vector3.Distance(other.transform.position, player.transform.position) < 5) {
                    other.Revive(); player.AddXP(10); Notice("Back on your feet! Your brother revived you."); return;
                }
            }
            bool nearGuide = Vector3.Distance(player.transform.position, Guide) < 6;
            if (State.complete && player.OwnerClientId == 0 && (Mode == JourneyMode.Duel || nearGuide)) { Begin(Mode); return; }
            if (Mode == JourneyMode.Story) {
                if (State.step == 0 && nearGuide) { State.step = 1; SpawnCamp(new Vector3(0,0,8), 3, 0, false); Notice("Guide: Raiders have crossed the bridge! Protect our village together."); }
                else if (State.step == 2 && State.enemies.Count == 0 && Vector3.Distance(player.transform.position, Shrines[0]) < 6) {
                    State.step = 3; Award(100); SpawnCamp(Shrines[2], 4, 3, true); Notice("The Tide Seal is yours. The Ash Commander waits at the ember shrine.");
                } else if (State.step == 3 && State.enemies.Count == 0 && Vector3.Distance(player.transform.position, Shrines[2]) < 6) {
                    State.step = 4; Award(120); Notice("The commander is defeated. Return both seals to the guide.");
                } else if (State.step == 4 && nearGuide) { State.step = 5; Award(150); Notice("Guide: Balance returns to our valley. You did this together. Chapter complete!"); }
                else Notice("Follow the objective marker. Clear its guards before collecting the seal.");
            } else if (Mode == JourneyMode.Adventure) {
                for (int camp = 0; camp < 3; camp++) if (Vector3.Distance(player.transform.position, Shrines[camp]) < 6) {
                    if ((State.scrolls & (1 << camp)) != 0) { Notice("This scroll has already been collected."); return; }
                    if (State.enemies.Exists(e => e.camp == camp + 1)) { Notice("Defeat this shrine's guards first."); return; }
                    State.scrolls |= 1 << camp; Award(100); Notice("Lost scroll recovered! Both players gained 100 XP."); return;
                }
                Notice("Find a scroll at the Tide, Stone or Ember Shrine. Move close and INTERACT.");
            }
        }
        public void PlayerDown(BenderCombat victim, BenderCombat attacker)
        {
            if (Mode == JourneyMode.Duel && attacker != null) {
                attacker.Score.Value++; attacker.AddXP(35);
                if (attacker.Score.Value >= 3) { State.complete = true; State.objective = BendingRules.Names[attacker.Preset.Value] + " WINS | Host: INTERACT for a rematch."; Notice(State.objective); }
                else Notice("Knockout! " + BendingRules.Names[attacker.Preset.Value] + " scores. First to three wins.");
            } else Notice("A brother is down! Move close and INTERACT to revive, or wait 8 seconds to respawn.");
        }
        public void Cast(BenderCombat caster, int slot, Vector3 aim)
        {
            Vector3 from = caster.transform.position;
            if (slot == 1 || (slot == 3 && caster.Element == Element.Air)) {
                if (caster.Element == Element.Water) { foreach (BenderCombat p in Players()) if (Vector3.Distance(from,p.transform.position) < 10 && (Mode != JourneyMode.Duel || p == caster)) p.Heal(35); }
                else if (caster.Element == Element.Air) caster.BoostUntil.Value = caster.Now + (slot == 3 ? 7 : 4);
                else caster.Shield.Value = Mathf.Min(65, caster.Shield.Value + 35);
                caster.EffectClientRpc(from + Vector3.up, from + Vector3.up, caster.Kind.Value, slot); return;
            }
            float range = slot == 2 ? 12 : 24;
            Vector3 point = from + aim * range;
            float best = float.MaxValue;
            if (Mode == JourneyMode.Duel) foreach (BenderCombat other in Players()) {
                if (other == caster || other.Down) continue;
                float score = AimScore(from, aim, other.transform.position, range);
                if (score < best) { best = score; point = other.transform.position; }
            } else foreach (RaiderState enemy in State.enemies) {
                float score = AimScore(from, aim, enemy.position, range);
                if (score < best) { best = score; point = enemy.position; }
            }
            caster.EffectClientRpc(from + Vector3.up * 1.2f, point + Vector3.up, caster.Kind.Value, slot);
            int damage = slot == 0 ? 20 : slot == 2 ? 34 : 48;
            float stun = slot == 3 && caster.Element != Element.Fire ? 3f : slot == 2 && caster.Element == Element.Water ? 1.5f : 0;
            if (slot == 3 && (caster.Element == Element.Water || caster.Element == Element.Warrior)) damage = 18;
            float radius = slot == 2 ? 5.5f : 1.2f;
            if (best == float.MaxValue) return;
            if (Mode == JourneyMode.Duel) {
                foreach (BenderCombat target in Players()) if (target != caster && Vector3.Distance(target.transform.position,point) < radius && Visible(from,target.transform.position)) target.Hurt(damage,stun,caster);
            } else {
                for (int i = State.enemies.Count - 1; i >= 0; i--) {
                    RaiderState enemy = State.enemies[i]; if (Vector3.Distance(enemy.position,point) > radius || !Visible(from,enemy.position)) continue;
                    enemy.hp -= damage; enemy.stunnedUntil = Time.time + stun;
                    if (enemy.hp <= 0) { State.enemies.RemoveAt(i); Award(enemy.elite ? 100 : 30); }
                }
            }
        }
        private static float AimScore(Vector3 origin, Vector3 aim, Vector3 target, float range)
        {
            Vector3 delta = target - origin; delta.y = 0;
            if (delta.magnitude > range || Vector3.Dot(delta.normalized,aim) < .55f || !Visible(origin,target)) return float.MaxValue;
            return delta.magnitude + (1 - Vector3.Dot(delta.normalized,aim)) * 15;
        }
        private void Award(int xp) { foreach (BenderCombat player in Players()) if (player.IsSpawned) player.AddXP(xp); }
        private void Notice(string message) { BenderCombat speaker = Speaker(); if (speaker != null) speaker.NoticeClientRpc(message); }
        private void DrawWorld()
        {
            var live = new HashSet<int>();
            foreach (RaiderState enemy in State.enemies) {
                live.Add(enemy.id);
                if (!views.TryGetValue(enemy.id, out RaiderView view)) { var item = new GameObject("Raider " + enemy.id); view = item.AddComponent<RaiderView>(); view.Build(enemy.elite); item.transform.position = enemy.position; views.Add(enemy.id,view); }
                view.Draw(enemy);
            }
            var gone = new List<int>(); foreach (var entry in views) if (!live.Contains(entry.Key)) { Destroy(entry.Value.gameObject); gone.Add(entry.Key); }
            foreach (int id in gone) views.Remove(id);
            if (beacon == null) {
                beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere); beacon.name = "Objective light"; beacon.layer = 2;
                Destroy(beacon.GetComponent<Collider>()); beaconMaterial = new Material(Shader.Find("Sprites/Default")); beaconMaterial.color = new Color(.35f,.85f,1f,.7f); beacon.GetComponent<Renderer>().sharedMaterial = beaconMaterial;
            }
            beacon.SetActive(State.showMarker);
            if (State.showMarker) { beacon.transform.position = State.marker + Vector3.up * (3 + Mathf.Sin(Time.time * 2) * .3f); beacon.transform.localScale = new Vector3(.6f,1.4f,.6f); }
        }
        private void Clear()
        {
            foreach (var view in views.Values) if (view != null) Destroy(view.gameObject); views.Clear();
            if (beacon != null) Destroy(beacon); if (beaconMaterial != null) Destroy(beaconMaterial);
            started = false; State = new JourneyState(); nextWaveAt = 0;
        }
        private void OnDestroy() { Clear(); if (Instance == this) Instance = null; }
    }
}
