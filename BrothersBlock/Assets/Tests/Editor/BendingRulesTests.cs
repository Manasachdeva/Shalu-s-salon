using BrothersBlock;
using NUnit.Framework;

public class BendingRulesTests
{
    [TestCase(0,1)] [TestCase(59,1)] [TestCase(60,2)] [TestCase(149,2)] [TestCase(150,3)]
    [TestCase(270,4)] [TestCase(420,5)] [TestCase(600,6)] [TestCase(810,7)] [TestCase(1050,8)] [TestCase(99999,8)]
    public void LevelsAdvanceAtTheSavedXPThresholds(int xp,int level) { Assert.AreEqual(level,BendingRules.Level(xp)); }
    [TestCase(0,0,true)] [TestCase(59,1,false)] [TestCase(60,1,true)] [TestCase(269,2,false)]
    [TestCase(270,2,true)] [TestCase(599,3,false)] [TestCase(600,3,true)] [TestCase(600,4,false)] [TestCase(600,-1,false)]
    public void SkillsStayLockedUntilTheirLevel(int xp,int slot,bool expected) { Assert.AreEqual(expected,BendingRules.Unlocked(xp,slot)); }
    [Test]
    public void EveryDisciplineHasFourDistinctAbilities()
    {
        for(int element=0;element<5;element++) {
            var names=new System.Collections.Generic.HashSet<string>();
            for(int slot=0;slot<4;slot++) Assert.IsTrue(names.Add(BendingRules.Ability((Element)element,slot)));
        }
        Assert.AreEqual("Lightning",BendingRules.Ability(Element.Fire,3));
        Assert.AreEqual("Blood bind",BendingRules.Ability(Element.Water,3));
    }
    [Test]
    public void PresetNonBendersKeepTheirWeaponIdentity()
    {
        Assert.AreEqual(Element.Warrior,BendingRules.PresetElements[6]);
        Assert.AreEqual(Element.Warrior,BendingRules.PresetElements[7]);
        Assert.AreEqual(Element.Warrior,BendingRules.PresetElements[8]);
        Assert.AreEqual("Boomerang",BendingRules.Ability(Element.Warrior,0,6));
        Assert.AreEqual("War fan",BendingRules.Ability(Element.Warrior,0,7));
    }
}
