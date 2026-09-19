using System.Collections;
using System.Linq;
using BrothersBlock;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ElementalJourneyTests
{
    private BenderCombat player;
    private AdventureDirector director;
    [UnitySetUp]
    public IEnumerator StartRoom()
    {
        JourneyProfile.PersistenceEnabled=false;
        yield return SceneManager.LoadSceneAsync("Neighbourhood",LoadSceneMode.Single); yield return null;
        LanSession.Instance.Host(); float until=Time.realtimeSinceStartup+8;
        while(BenderCombat.Local==null&&Time.realtimeSinceStartup<until)yield return null;
        player=BenderCombat.Local; Assert.IsNotNull(player); director=AdventureDirector.Instance;
        director.Begin(JourneyMode.Story); player.Preset.Value=0; player.Kind.Value=(int)Element.Fire; player.XP.Value=0;
        yield return new WaitForSeconds(.2f);
    }
    [UnityTest]
    public IEnumerator NetworkCastRespectsLocksCooldownsAndDefence()
    {
        director.Begin(JourneyMode.Survival); RaiderState enemy=director.State.enemies[0];
        player.GetComponent<PlayerMotor>().Teleport(enemy.position+new Vector3(0,1,-5)); yield return null;
        int before=enemy.hp; player.TryCast(3,Vector3.forward); yield return new WaitForSeconds(.15f);
        Assert.AreEqual(before,enemy.hp,"A locked sub-bending skill must not deal damage.");
        player.XP.Value=600; player.TryCast(3,Vector3.forward); yield return new WaitForSeconds(.15f);
        Assert.Less(enemy.hp,before,"An unlocked lightning RPC should hit its target.");
        int after=enemy.hp; player.TryCast(3,Vector3.forward); yield return new WaitForSeconds(.15f);
        Assert.AreEqual(after,enemy.hp,"Cooldown should prevent a second immediate cast.");
        player.TryCast(1); yield return null; Assert.Greater(player.Shield.Value,0);
        player.Kind.Value=(int)Element.Water; player.HP.Value=30;
        director.Cast(player,1,Vector3.forward); Assert.AreEqual(65,player.HP.Value);
        player.Kind.Value=(int)Element.Air; director.Cast(player,3,Vector3.forward); Assert.IsTrue(player.Boosted);
        player.Kind.Value=(int)Element.Earth;
        enemy=director.State.enemies[1]; player.GetComponent<PlayerMotor>().Teleport(enemy.position+new Vector3(0,1,-5));
        director.Cast(player,3,Vector3.forward); Assert.Greater(enemy.stunnedUntil,Time.time);
    }
    private IEnumerator ClearGuards(int camp=-1)
    {
        int[] ids=director.State.enemies.Where(e=>camp<0||e.camp==camp).Select(e=>e.id).ToArray();
        foreach(int id in ids) {
            for(int attack=0;attack<12;attack++) {
                RaiderState enemy=director.State.enemies.Find(e=>e.id==id); if(enemy==null)break;
                player.GetComponent<PlayerMotor>().Teleport(enemy.position+new Vector3(0,1,-4));
                director.Cast(player,0,Vector3.forward); yield return null;
            }
            Assert.IsFalse(director.State.enemies.Exists(e=>e.id==id),"Guard should be defeated by repeated server combat hits.");
        }
        yield return null;
    }
    [UnityTest]
    public IEnumerator StoryHasACompletePlayableQuestChain()
    {
        player.GetComponent<PlayerMotor>().Teleport(AdventureDirector.Guide+new Vector3(0,1,-3));
        player.Interact(); yield return null; Assert.AreEqual(1,director.State.step);
        yield return ClearGuards(); Assert.AreEqual(2,director.State.step);
        yield return ClearGuards();
        player.GetComponent<PlayerMotor>().Teleport(AdventureDirector.Shrines[0]+new Vector3(0,1,-3)); player.Interact(); yield return null;
        Assert.AreEqual(3,director.State.step); yield return ClearGuards();
        player.GetComponent<PlayerMotor>().Teleport(AdventureDirector.Shrines[2]+new Vector3(0,1,-3)); player.Interact(); yield return null;
        Assert.AreEqual(4,director.State.step);
        player.GetComponent<PlayerMotor>().Teleport(AdventureDirector.Guide+new Vector3(0,1,-3)); player.Interact(); yield return null;
        Assert.IsTrue(director.State.complete); Assert.GreaterOrEqual(player.XP.Value,600,"The story should unlock a first sub-bending skill.");
    }
    [UnityTest]
    public IEnumerator AdventureScrollsRequireClearingTheirGuards()
    {
        director.Begin(JourneyMode.Adventure);
        player.GetComponent<PlayerMotor>().Teleport(AdventureDirector.Shrines[0]+new Vector3(0,1,-3)); player.Interact(); yield return null;
        Assert.AreEqual(0,director.State.scrolls);
        for(int camp=0;camp<3;camp++) {
            yield return ClearGuards(camp+1);
            player.GetComponent<PlayerMotor>().Teleport(AdventureDirector.Shrines[camp]+new Vector3(0,1,-3)); player.Interact(); yield return null;
            Assert.AreNotEqual(0,director.State.scrolls&(1<<camp));
        }
        Assert.IsTrue(director.State.complete); Assert.AreEqual(7,director.State.scrolls);
    }
    [UnityTearDown]
    public IEnumerator StopRoom()
    {
        if(LanSession.Instance!=null)LanSession.Instance.Leave();
        yield return null; yield return null; JourneyProfile.PersistenceEnabled=true;
    }
}
