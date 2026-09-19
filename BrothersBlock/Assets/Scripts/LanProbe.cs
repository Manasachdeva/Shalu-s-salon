#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace BrothersBlock
{
    // Runs only when explicit development-test arguments are supplied.
    public sealed class LanProbe : MonoBehaviour
    {
        private string role,folder,runtimeError;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            string[] args=Environment.GetCommandLineArgs();int r=Array.IndexOf(args,"--lan-probe"),o=Array.IndexOf(args,"--probe-dir");
            if(r<0||r+1>=args.Length||o<0||o+1>=args.Length)return;
            var probe=new GameObject("Automated multiplayer check").AddComponent<LanProbe>();probe.role=args[r+1];probe.folder=args[o+1];
        }
        private IEnumerator Start()
        {
            JourneyProfile.PersistenceEnabled=false; Application.logMessageReceived+=OnLog;
            yield return new WaitForSeconds(role=="host"?.7f:2f);
            LanSession session=LanSession.Instance;
            if(session==null){Finish(false,"Session missing");yield break;}
            if(role=="host"){CapturePreview("menu-preview.png");session.Host();}else session.Join("127.0.0.1");
            float deadline=Time.realtimeSinceStartup+30;BenderCombat remote=null;
            while(Time.realtimeSinceStartup<deadline){foreach(BenderCombat p in AdventureDirector.Players())if(p.IsSpawned&&!p.IsOwner)remote=p;if(session.Playing&&BenderCombat.Local!=null&&remote!=null)break;yield return null;}
            if(remote==null||BenderCombat.Local==null){Finish(false,"Both players did not spawn: "+session.Message);yield break;}
            BenderCombat local=BenderCombat.Local;AdventureDirector director=AdventureDirector.Instance;
            if(role=="host"){
                yield return new WaitForSeconds(.7f);director.Begin(JourneyMode.Duel);
                local.Kind.Value=(int)Element.Fire;local.Preset.Value=4;local.Outfit.Value=3;local.XP.Value=600;
                remote.Kind.Value=(int)Element.Water;remote.Preset.Value=2;remote.Outfit.Value=1;remote.XP.Value=600;
                local.Hair.Value=remote.Hair.Value=0;local.Skin.Value=0;remote.Skin.Value=2;
            }
            deadline=Time.realtimeSinceStartup+10;
            while((director.Mode!=JourneyMode.Duel||local.XP.Value!=600||remote.XP.Value!=600)&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return new WaitForSeconds(.5f);
            Vector3 remoteStart=remote.transform.position,localStart=local.transform.position;
            float until=Time.realtimeSinceStartup+3;
            while(Time.realtimeSinceStartup<until){local.GetComponent<CharacterController>().Move(Vector3.forward*(2*Time.deltaTime));yield return null;}
            float remoteDistance=Vector3.Distance(remoteStart,remote.transform.position),localDistance=Vector3.Distance(localStart,local.transform.position);
            local.GetComponent<PlayerMotor>().Teleport(role=="host"?new Vector3(0,1,-4):new Vector3(0,1,3));
            yield return new WaitForSeconds(1);
            local.TryCast(0,remote.transform.position-local.transform.position);
            yield return new WaitForSeconds(.8f);
            bool combat=local.HP.Value<=80&&remote.HP.Value<=80;
            bool presets=local.Kind.Value!=remote.Kind.Value&&local.Preset.Value!=remote.Preset.Value;
            if(role=="host")CapturePreview("game-preview.png");
            bool duel=false,friendly=true;
            if(role=="host"){
                for(int round=0;round<3;round++){
                    remote.HP.Value=20;
                    director.Cast(local,0,(remote.transform.position-local.transform.position).normalized);
                    yield return new WaitForSeconds(.3f);
                    if(round<2){remote.Respawn(100);yield return new WaitForSeconds(3.3f);}
                }
                duel=director.State.complete&&local.Score.Value==3;
                yield return new WaitForSeconds(1);
                // Wait beyond respawn immunity so this checks co-op friendly fire itself.
                director.Begin(JourneyMode.Survival);yield return new WaitForSeconds(3.3f);
                local.TryCast(0,remote.transform.position-local.transform.position);yield return new WaitForSeconds(.3f);friendly=remote.HP.Value==100;
                RaiderState enemy=director.State.enemies[0];local.GetComponent<PlayerMotor>().Teleport(enemy.position+new Vector3(0,1,-4));
                for(int hit=0;hit<3;hit++){director.Cast(local,0,Vector3.forward);yield return null;}
            }else{
                deadline=Time.realtimeSinceStartup+22;
                while(!director.State.complete&&Time.realtimeSinceStartup<deadline)yield return null;
                duel=director.State.complete;
            }
            deadline=Time.realtimeSinceStartup+10;
            while((director.Mode!=JourneyMode.Survival||director.State.enemies.Count!=2||local.XP.Value<=600||remote.XP.Value<=600)&&Time.realtimeSinceStartup<deadline)yield return null;
            bool coop=director.Mode==JourneyMode.Survival&&director.State.enemies.Count==2&&local.XP.Value>600&&remote.XP.Value>600;
            bool passed=remoteDistance>1&&localDistance>1&&combat&&presets&&duel&&coop&&friendly&&string.IsNullOrEmpty(runtimeError);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,role+"-result.json"),JsonUtility.ToJson(new Result{passed=passed,role=role,localDistance=localDistance,remoteDistance=remoteDistance,combatReplicated=combat,presetsReplicated=presets,duelCompleted=duel,cooperativeXP=coop,friendlyFirePrevented=friendly,message=runtimeError??session.Message},true));
            yield return new WaitForSeconds(3);session.Leave();yield return new WaitForSeconds(.3f);Application.Quit(passed?0:1);
        }
        private void OnLog(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)runtimeError=message;}
        private void CapturePreview(string filename)
        {
            Camera camera=Camera.main;Canvas canvas=FindAnyObjectByType<Canvas>();
            if(camera==null||canvas==null||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            var target=new RenderTexture(1600,900,24);RenderTexture previous=RenderTexture.active;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,filename),image.EncodeToPNG());
            camera.targetTexture=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;RenderTexture.active=previous;target.Release();Destroy(target);Destroy(image);
        }
        private void Finish(bool passed,string message){Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,role+"-result.json"),JsonUtility.ToJson(new Result{passed=passed,role=role,message=message},true));Application.Quit(passed?0:1);}
        private void OnDestroy(){Application.logMessageReceived-=OnLog;}
        [Serializable]private sealed class Result {public bool passed;public string role;public float localDistance,remoteDistance;public bool combatReplicated,presetsReplicated,duelCompleted,cooperativeXP,friendlyFirePrevented;public string message;}
    }
}
#endif
