using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrothersBlock
{
    public sealed class Neighbourhood : MonoBehaviour
    {
        private readonly VisualPalette palette = new VisualPalette();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private Color stone = new Color(.67f,.65f,.53f);
        private Color pale = new Color(.88f,.82f,.64f);
        private void Awake()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.38f,.42f,.46f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.63f,.77f,.81f); RenderSettings.fogStartDistance = 75; RenderSettings.fogEndDistance = 185;
            Box("Island",new Vector3(0,-1,0),new Vector3(176,2,176),new Color(.45f,.61f,.38f),true);
            Box("Sea",new Vector3(0,-1.3f,0),new Vector3(600,.3f,600),new Color(.22f,.48f,.60f));
            Box("Village plaza",new Vector3(0,.015f,0),new Vector3(35,.03f,38),pale);
            Box("North path",new Vector3(0,.02f,25),new Vector3(6,.04f,110),new Color(.69f,.59f,.39f));
            Box("Cross path",new Vector3(0,.025f,0),new Vector3(165,.05f,6),new Color(.69f,.59f,.39f));
            for (int s=-1;s<=1;s+=2) {
                Box("Stone boundary",new Vector3(s*86,.8f,0),new Vector3(2,1.6f,174),stone,true);
                Box("Stone boundary",new Vector3(0,.8f,s*86),new Vector3(174,1.6f,2),stone,true);
                Box("Shrine path",new Vector3(s*42,.03f,18),new Vector3(5,.06f,132),pale);
            }
            Shrine(AdventureDirector.Shrines[0],BendingRules.Colours[1],"TIDE SHRINE");
            Shrine(AdventureDirector.Shrines[1],BendingRules.Colours[2],"STONE SHRINE");
            Shrine(AdventureDirector.Shrines[2],BendingRules.Colours[3],"EMBER SHRINE");
            Shrine(new Vector3(42,0,-42),BendingRules.Colours[0],"AIR SANCTUARY");
            Box("Tide pool",new Vector3(-57,.04f,53),new Vector3(22,.08f,24),new Color(.27f,.60f,.72f));
            Box("Pool bridge",new Vector3(-57,.4f,53),new Vector3(4,.8f,26),stone,true);
            for (int i=0;i<8;i++) {
                float x= -73 + (i%4)*17, z= i<4 ? -72 : 72;
                Mountain(new Vector3(x,0,z),9+(i%3)*5);
            }
            for (int s=-1;s<=1;s+=2) for (int i=0;i<3;i++) House(new Vector3(s*26,0,-17+i*17),i==1?new Color(.66f,.44f,.29f):new Color(.65f,.59f,.41f));
            for (int i=0;i<14;i++) {
                int side=i%2==0?-1:1;
                Tree(new Vector3(side*(60+(i%3)*5),0,-60+i*9));
            }
            for (int s=-1;s<=1;s+=2) for (int z=-1;z<=1;z+=2) {
                Vector3 p=new Vector3(s*15,0,z*16);
                Box("Lantern post",p+Vector3.up*1.5f,new Vector3(.15f,3,.15f),new Color(.30f,.23f,.18f),true);
                Box("Paper lantern",p+Vector3.up*3,new Vector3(.7f,.9f,.7f),new Color(.95f,.65f,.30f));
            }
            Vector3 guide=AdventureDirector.Guide;
            Box("Guide robe",guide+Vector3.up*.85f,new Vector3(.7f,1.6f,.5f),new Color(.22f,.40f,.45f));
            Box("Guide face",guide+Vector3.up*1.9f,new Vector3(.5f,.5f,.45f),new Color(.72f,.51f,.33f));
            Box("Guide hair",guide+Vector3.up*2.17f,new Vector3(.53f,.14f,.49f),new Color(.68f,.67f,.61f));
            palette.Label(transform,"VILLAGE GUIDE",guide+Vector3.up*2.85f,.045f,Color.white);
            palette.Label(transform,"VALLEY OF FOUR WINDS",new Vector3(0,4,-29),.085f,pale);
            Combine();
        }
        private void Box(string name,Vector3 position,Vector3 size,Color colour,bool solid=false) { palette.Part(transform,name,position,size,colour,solid); }
        private void Shrine(Vector3 p,Color colour,string name)
        {
            Box("Shrine ground",p+Vector3.up*.025f,new Vector3(19,.05f,19),pale);
            for (int s=-1;s<=1;s+=2) {
                Box("Shrine column",p+new Vector3(s*6,2.2f,7),new Vector3(.9f,4.4f,.9f),stone,true);
                Box("Shrine column",p+new Vector3(s*6,2.2f,-7),new Vector3(.9f,4.4f,.9f),stone,true);
                Box("Shrine lintel",p+new Vector3(0,4.5f,s*7),new Vector3(14,.65f,1.2f),colour);
            }
            Box("Scroll pedestal",p+new Vector3(0,.5f,0),new Vector3(1.4f,1,1.4f),stone,true);
            Box("Ancient scroll",p+new Vector3(0,1.18f,0),new Vector3(.7f,.13f,.5f),new Color(.96f,.87f,.58f));
            palette.Label(transform,name,p+new Vector3(0,4.5f,-7.7f),.06f,Color.white);
            for (int i=0;i<3;i++) Box("Banner",p+new Vector3(-3+i*3,3.1f,7),new Vector3(1.4f,1.9f,.06f),colour);
        }
        private void House(Vector3 p,Color colour)
        {
            Box("Village house",p+Vector3.up*2,new Vector3(8,4,8),colour,true);
            Box("Roof",p+Vector3.up*4.3f,new Vector3(10,.6f,10),new Color(.23f,.33f,.34f));
            Box("Upper roof",p+Vector3.up*4.8f,new Vector3(7,.45f,7),new Color(.29f,.39f,.37f));
            Box("Door",p+new Vector3(0,1.2f,-4.03f),new Vector3(1.5f,2.4f,.1f),new Color(.25f,.20f,.14f));
            for (int s=-1;s<=1;s+=2) Box("Window",p+new Vector3(s*2.5f,2.4f,-4.05f),new Vector3(1.2f,1.2f,.08f),new Color(.91f,.72f,.39f));
        }
        private void Tree(Vector3 p)
        {
            Box("Tree trunk",p+Vector3.up*1.5f,new Vector3(.7f,3,.7f),new Color(.37f,.28f,.18f),true);
            palette.Part(transform,"Canopy",p+Vector3.up*4.2f,new Vector3(4.5f,4.5f,4.5f),new Color(.26f,.47f,.30f),false,true);
            palette.Part(transform,"Canopy",p+new Vector3(1,5,0),new Vector3(3,3,3),new Color(.44f,.60f,.32f),false,true);
        }
        private void Mountain(Vector3 p,float height)
        {
            GameObject rock=palette.Part(transform,"Cliff",p+Vector3.up*(height*.35f),new Vector3(13,height,11),new Color(.47f,.51f,.45f),true);
            rock.transform.localRotation=Quaternion.Euler(0,25,14);
            Box("Snow cap",p+Vector3.up*(height*.82f),new Vector3(6,1,7),new Color(.84f,.87f,.79f));
        }
        private void Combine()
        {
            var batches=new Dictionary<Material,List<CombineInstance>>();
            foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>()) {
                Renderer renderer=filter.GetComponent<Renderer>(); if (renderer==null||filter.sharedMesh==null) continue;
                Material material=renderer.sharedMaterial; if (!batches.ContainsKey(material)) batches[material]=new List<CombineInstance>();
                batches[material].Add(new CombineInstance { mesh=filter.sharedMesh, transform=transform.worldToLocalMatrix*filter.transform.localToWorldMatrix }); renderer.enabled=false;
            }
            foreach (var entry in batches) {
                var go=new GameObject("World geometry"); go.layer=2; go.transform.SetParent(transform,false);
                var mesh=new Mesh { indexFormat=IndexFormat.UInt32 }; mesh.CombineMeshes(entry.Value.ToArray()); meshes.Add(mesh);
                go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterial=entry.Key;
            }
        }
        private void OnDestroy() { palette.Dispose(); foreach (Mesh mesh in meshes) Destroy(mesh); }
    }
}
