using UnityEngine;

namespace BrothersBlock
{
    public enum Element { Air, Water, Earth, Fire, Warrior }
    public enum JourneyMode { Story, Adventure, Survival, Duel }

    public static class BendingRules
    {
        public static readonly string[] Names = { "Custom traveller", "Aang", "Katara", "Toph", "Zuko", "Azula", "Sokka", "Suki", "Kyoshi Warrior" };
        public static readonly Element[] PresetElements = { Element.Air, Element.Air, Element.Water, Element.Earth, Element.Fire, Element.Fire, Element.Warrior, Element.Warrior, Element.Warrior };
        public static readonly string[] ModeNames = { "STORY", "ADVENTURE", "CO-OP WAVES", "1 vs 1" };
        public static readonly string[] ModeDescriptions = {
            "The stolen seals: an original short chapter. Meet the village guide, protect the valley, and face the Ash Commander.",
            "Explore three elemental shrines. Defeat their guards and collect every lost scroll. Explore alone or together.",
            "Stand together against growing waves of raiders. Both players earn XP. Survive as long as you can.",
            "Challenge your brother. First to three knockouts wins. Friendly damage is enabled only in this mode."
        };
        public static readonly Color[] Colours = {
            new Color(.79f,.88f,.73f), new Color(.26f,.71f,.91f), new Color(.56f,.74f,.36f), new Color(1f,.43f,.24f), new Color(.80f,.73f,.52f)
        };
        public static readonly Color[] Cloth = {
            new Color(.88f,.49f,.19f), new Color(.18f,.42f,.69f), new Color(.25f,.48f,.27f), new Color(.64f,.18f,.14f), new Color(.45f,.30f,.60f), new Color(.83f,.80f,.68f)
        };
        public static readonly Color[] Skin = {
            new Color(.95f,.76f,.59f), new Color(.80f,.57f,.38f), new Color(.62f,.39f,.24f), new Color(.40f,.24f,.16f), new Color(.88f,.65f,.46f)
        };
        private static readonly int[] thresholds = { 0, 60, 150, 270, 420, 600, 810, 1050 };
        public static readonly int[] UnlockLevels = { 1, 2, 4, 6 };
        public static readonly float[] Cooldowns = { .7f, 9f, 6f, 14f };
        private static readonly string[,] abilities = {
            { "Air gust", "Air dash", "Air burst", "Air glide" },
            { "Water bolt", "Healing tide", "Ice surge", "Blood bind" },
            { "Rock shot", "Earth armour", "Landslide", "Metal snare" },
            { "Fire blast", "Flame guard", "Fire ring", "Lightning" },
            { "Weapon throw", "Parry", "Sweeping strike", "Chi block" }
        };
        public static int Level(int xp) { int level = 1; for (int i = 1; i < thresholds.Length; i++) if (xp >= thresholds[i]) level = i + 1; return level; }
        public static int NextXP(int xp) { return Level(xp) == 8 ? 1050 : thresholds[Level(xp)]; }
        public static bool Unlocked(int xp, int slot) { return slot >= 0 && slot < 4 && Level(xp) >= UnlockLevels[slot]; }
        public static string Ability(Element element, int slot, int preset = 0)
        {
            if (slot < 0 || slot > 3) return "";
            if (element == Element.Warrior && slot == 0) return preset == 6 ? "Boomerang" : preset >= 7 ? "War fan" : "Weapon throw";
            return abilities[Mathf.Clamp((int)element, 0, 4), slot];
        }
        public static string Specialty(Element element)
        {
            switch (element) {
                case Element.Air: return "Fast movement, gusts and a brief airborne glide.";
                case Element.Water: return "Heal your team, freeze enemies, unlock a non-graphic blood-bending stun.";
                case Element.Earth: return "Armour, heavy strikes and a metal-binding attack.";
                case Element.Fire: return "Powerful ranged attacks and a lightning finisher.";
                default: return "Weapons, defensive parries and chi-blocking. No elemental powers.";
            }
        }
    }

    public static class JourneyProfile
    {
        public static bool PersistenceEnabled = true;
        private const string Prefix = "elemental-journey-v1-";
        public static int Preset { get { return Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "preset", 1), 0, 8); } set { PlayerPrefs.SetInt(Prefix + "preset", value); } }
        public static Element Kind { get { return (Element)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "element", 0), 0, 4); } set { PlayerPrefs.SetInt(Prefix + "element", (int)value); } }
        public static int Outfit { get { return Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "outfit", 0), 0, 5); } set { PlayerPrefs.SetInt(Prefix + "outfit", value); } }
        public static int Skin { get { return Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "skin", 0), 0, 4); } set { PlayerPrefs.SetInt(Prefix + "skin", value); } }
        public static int Hair { get { return Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "hair", 2), 0, 2); } set { PlayerPrefs.SetInt(Prefix + "hair", value); } }
        public static JourneyMode Mode { get { return (JourneyMode)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "mode", 0), 0, 3); } set { PlayerPrefs.SetInt(Prefix + "mode", (int)value); } }
        public static int XP(Element kind) { return Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "xp-" + (int)kind, 0), 0, 100000); }
        public static void SaveXP(Element kind, int value) { if (!PersistenceEnabled) return; PlayerPrefs.SetInt(Prefix + "xp-" + (int)kind, Mathf.Clamp(value, 0, 100000)); PlayerPrefs.Save(); }
        public static void Select(int preset)
        {
            Preset = Mathf.Clamp(preset, 0, 8); Kind = BendingRules.PresetElements[Preset];
            Outfit = Kind == Element.Air ? 0 : Kind == Element.Water || Preset == 6 ? 1 : Kind == Element.Fire ? 3 : 2;
            Skin = Preset == 2 || Preset == 6 ? 2 : 0; Hair = Preset == 1 ? 2 : Preset == 5 || Preset == 6 ? 1 : 0;
            PlayerPrefs.Save();
        }
    }
}
