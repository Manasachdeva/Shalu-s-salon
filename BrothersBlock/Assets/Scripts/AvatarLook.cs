using System.Collections.Generic;
using UnityEngine;

namespace BrothersBlock
{
    public sealed class VisualPalette
    {
        private readonly Dictionary<Color,Material> materials = new Dictionary<Color,Material>();
        public Material Get(Color colour) {
            if (!materials.TryGetValue(colour, out Material material)) { material = new Material(Shader.Find("Standard")); material.color = colour; material.SetFloat("_Glossiness", .05f); materials.Add(colour, material); }
            return material;
        }
        public GameObject Part(Transform parent, string name, Vector3 position, Vector3 size, Color colour, bool solid = false, bool sphere = false)
        {
            GameObject part = GameObject.CreatePrimitive(sphere ? PrimitiveType.Sphere : PrimitiveType.Cube);
            part.name = name; part.layer = solid ? 0 : 2; part.transform.SetParent(parent,false); part.transform.localPosition = position; part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = Get(colour);
            Collider collider = part.GetComponent<Collider>(); collider.enabled = solid; if (!solid) Object.Destroy(collider);
            return part;
        }
        public TextMesh Label(Transform parent, string text, Vector3 position, float size, Color colour)
        {
            var go = new GameObject("World label"); go.layer = 2; go.transform.SetParent(parent,false); go.transform.localPosition = position;
            TextMesh label = go.AddComponent<TextMesh>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); go.GetComponent<Renderer>().sharedMaterial = label.font.material;
            label.text = text; label.fontSize = 48; label.characterSize = size; label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = colour;
            return label;
        }
        public void Dispose() { foreach (Material material in materials.Values) if (material != null) Object.Destroy(material); materials.Clear(); }
    }
    public sealed class AvatarLook : MonoBehaviour
    {
        private readonly VisualPalette palette = new VisualPalette();
        private BenderCombat combat;
        private PlayerMotor motor;
        private GameObject accessories;
        private int signature = -1;
        private void Start() { combat = GetComponent<BenderCombat>(); motor = GetComponent<PlayerMotor>(); }
        private void Update()
        {
            if (combat == null || !combat.IsSpawned) return;
            int value = combat.Preset.Value + 10 * combat.Outfit.Value + 100 * combat.Skin.Value + 1000 * combat.Hair.Value;
            if (signature != value) { signature = value; Dress(); }
            if (motor.NameLabel != null) {
                motor.NameLabel.characterSize = .029f;
                motor.NameLabel.text = BendingRules.Names[combat.Preset.Value] + (combat.IsOwner ? " (YOU)" : "") + "\n" + (combat.Down ? "DOWN - REVIVE" : "Lv " + BendingRules.Level(combat.XP.Value) + "  |  " + combat.HP.Value + " HP");
                motor.NameLabel.color = combat.Down ? new Color(1,.45f,.35f) : Color.white;
            }
        }
        private void Paint(string path, Color colour)
        {
            Transform item = transform.Find(path); if (item != null && item.TryGetComponent<Renderer>(out Renderer renderer)) renderer.sharedMaterial = palette.Get(colour);
        }
        private void Dress()
        {
            if (accessories != null) Destroy(accessories);
            accessories = new GameObject("Character details"); accessories.transform.SetParent(transform,false);
            Transform root = accessories.transform;
            Color cloth = BendingRules.Cloth[combat.Outfit.Value], skin = BendingRules.Skin[combat.Skin.Value], dark = new Color(.12f,.13f,.14f), trim = new Color(.86f,.77f,.52f);
            Paint("Shirt",cloth); Paint("Head",skin); Paint("Nose",skin); Paint("Left arm/Limb",skin); Paint("Right arm/Limb",skin);
            Paint("Left leg/Limb",cloth * .6f); Paint("Right leg/Limb",cloth * .6f);
            Transform hair = transform.Find("Hair"); if (hair != null) hair.gameObject.SetActive(combat.Hair.Value != 2);
            Paint("Hair",dark);
            palette.Part(root,"Belt",new Vector3(0,.88f,.02f),new Vector3(.69f,.13f,.40f),trim);
            palette.Part(root,"Robe",new Vector3(0,.72f,0),new Vector3(.67f,.26f,.38f),cloth);
            if (combat.Hair.Value == 1) palette.Part(root,"Topknot",new Vector3(0,2.13f,-.05f),new Vector3(.22f,.25f,.22f),dark,false,true);
            int p = combat.Preset.Value;
            if (p == 1) {
                Color arrow = new Color(.27f,.69f,.89f);
                palette.Part(root,"Arrow",new Vector3(0,1.87f,.231f),new Vector3(.06f,.24f,.022f),arrow);
                for (int s = -1; s <= 1; s += 2) { GameObject tip = palette.Part(root,"Arrow tip",new Vector3(s*.055f,1.80f,.235f),new Vector3(.045f,.14f,.025f),arrow); tip.transform.localRotation = Quaternion.Euler(0,0,s*-45); }
                palette.Part(root,"Monk sash",new Vector3(.13f,1.22f,.21f),new Vector3(.26f,.64f,.055f),new Color(.87f,.28f,.12f));
            }
            if (p == 2) for (int s = -1; s <= 1; s += 2) palette.Part(root,"Braid",new Vector3(s*.27f,1.57f,.04f),new Vector3(.11f,.65f,.15f),dark);
            if (p == 3) {
                palette.Part(root,"Headband",new Vector3(0,1.95f,.22f),new Vector3(.51f,.09f,.04f),trim);
                for (int s=-1;s<=1;s+=2) palette.Part(root,"Hair bun",new Vector3(s*.30f,1.84f,-.05f),new Vector3(.25f,.27f,.27f),dark,false,true);
            }
            if (p == 4) palette.Part(root,"Scar mark",new Vector3(-.13f,1.79f,.238f),new Vector3(.19f,.17f,.022f),new Color(.52f,.22f,.20f));
            if (p == 5) palette.Part(root,"Hair pin",new Vector3(0,2.14f,.015f),new Vector3(.29f,.045f,.07f),trim);
            if (p >= 7) {
                palette.Part(root,"Warrior face paint",new Vector3(0,1.75f,.23f),new Vector3(.45f,.39f,.018f),new Color(.93f,.92f,.83f));
                palette.Part(root,"Eye paint",new Vector3(0,1.80f,.248f),new Vector3(.40f,.06f,.018f),new Color(.57f,.14f,.14f));
                palette.Part(root,"Headdress",new Vector3(0,2.06f,0),new Vector3(.75f,.15f,.32f),trim);
                for (int s=-1;s<=1;s+=2) for (int f=-2;f<=2;f++) {
                    GameObject fan = palette.Part(root,"War fan",new Vector3(s*.61f,.99f,.15f),new Vector3(.07f,.46f,.07f),trim);
                    fan.transform.localRotation = Quaternion.Euler(0,0,s*(f*18+30));
                }
            } else if (combat.Element == Element.Warrior) {
                for (int s=-1;s<=1;s+=2) { GameObject blade = palette.Part(root,"Boomerang",new Vector3(.53f+s*.11f,.95f,.26f),new Vector3(.09f,.35f,.07f),new Color(.69f,.79f,.83f)); blade.transform.localRotation = Quaternion.Euler(0,0,s*40); }
            }
        }
        private void OnDestroy() { if (accessories != null) Destroy(accessories); palette.Dispose(); }
    }
    public sealed class RaiderView : MonoBehaviour
    {
        private readonly VisualPalette palette = new VisualPalette();
        private TextMesh label;
        public void Build(bool elite)
        {
            Color robe = elite ? new Color(.40f,.10f,.13f) : new Color(.28f,.29f,.33f);
            palette.Part(transform,"Robe",new Vector3(0,.8f,0),new Vector3(.75f,1.35f,.45f),robe);
            palette.Part(transform,"Head",new Vector3(0,1.68f,0),new Vector3(.46f,.46f,.43f),new Color(.70f,.52f,.35f));
            palette.Part(transform,"Mask",new Vector3(0,1.68f,-.23f),new Vector3(.5f,.23f,.06f),new Color(.64f,.17f,.13f));
            for (int s=-1;s<=1;s+=2) {
                palette.Part(transform,"Arm",new Vector3(s*.5f,.95f,0),new Vector3(.23f,.65f,.27f),robe);
                palette.Part(transform,"Boot",new Vector3(s*.24f,.16f,-.08f),new Vector3(.28f,.30f,.47f),Color.gray*.5f);
            }
            if (elite) palette.Part(transform,"Commander crest",new Vector3(0,2.03f,0),new Vector3(.16f,.38f,.4f),new Color(.95f,.49f,.17f));
            label = palette.Label(transform, "", Vector3.up*2.55f,.029f,Color.white);
        }
        public void Draw(RaiderState state)
        {
            Vector3 delta = state.position - transform.position;
            transform.position = Vector3.Lerp(transform.position,state.position,1-Mathf.Exp(-18*Time.deltaTime));
            if (delta.sqrMagnitude > .003f) transform.rotation = Quaternion.LookRotation(-delta.normalized);
            label.text = (state.elite ? "ASH COMMANDER" : "RAIDER") + "\n" + state.hp + " / " + state.maxHP;
            label.color = state.stunnedUntil > Time.time ? new Color(.45f,.83f,1) : new Color(1,.73f,.58f);
            if (Camera.main != null) label.transform.rotation = Camera.main.transform.rotation;
        }
        private void OnDestroy() { palette.Dispose(); }
    }
}
