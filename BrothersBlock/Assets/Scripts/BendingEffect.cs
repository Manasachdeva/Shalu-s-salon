using UnityEngine;
using UnityEngine.Rendering;

namespace BrothersBlock
{
    public sealed class BendingEffect : MonoBehaviour
    {
        private Vector3 from, to;
        private float born;
        private int slot;
        private Element element;
        private Material material;
        private GameObject orb;
        private LineRenderer line;
        public static void Play(Vector3 from, Vector3 to, Element element, int slot)
        {
            var go = new GameObject("Bending effect");
            var effect = go.AddComponent<BendingEffect>(); effect.from = from; effect.to = to; effect.element = element; effect.slot = slot; effect.born = Time.time;
            effect.material = new Material(Shader.Find("Sprites/Default"));
            Color colour = slot == 3 && element == Element.Fire ? new Color(.50f,.79f,1) : slot == 3 && element == Element.Water ? new Color(.72f,.27f,.48f) : BendingRules.Colours[(int)element];
            effect.material.color = colour;
            effect.orb = GameObject.CreatePrimitive(element == Element.Earth || element == Element.Warrior ? PrimitiveType.Cube : PrimitiveType.Sphere);
            effect.orb.transform.SetParent(go.transform,false); effect.orb.layer = 2;
            Collider collider = effect.orb.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            effect.orb.GetComponent<Renderer>().sharedMaterial = effect.material;
            effect.line = go.AddComponent<LineRenderer>(); effect.line.sharedMaterial = effect.material; effect.line.useWorldSpace = true;
            effect.line.startWidth = .10f; effect.line.endWidth = .03f; effect.line.shadowCastingMode = ShadowCastingMode.Off;
            effect.line.positionCount = 6;
            for (int i=0;i<6;i++) {
                Vector3 point = Vector3.Lerp(from,to,i/5f);
                if (slot == 3 && element == Element.Fire && i>0 && i<5) point += new Vector3(i%2==0?.45f:-.45f,.3f,0);
                effect.line.SetPosition(i,point);
            }
        }
        private void Update()
        {
            float age = (Time.time-born)/.45f;
            if (age>=1) { Destroy(gameObject); return; }
            bool ring = slot == 1 || slot == 2 || (slot == 3 && element == Element.Air);
            orb.transform.position = ring ? to : Vector3.Lerp(from,to,Mathf.Min(age*2,1));
            orb.transform.localScale = ring ? Vector3.one * Mathf.Lerp(.3f,slot == 2 ? 5 : 3,age) : Vector3.one * (.48f + .3f*(1-age));
            orb.transform.Rotate(70*Time.deltaTime,190*Time.deltaTime,50*Time.deltaTime);
            Color c = material.color; c.a = (1-age)*(ring?.26f:1); material.color = c;
            line.enabled = !ring;
        }
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
