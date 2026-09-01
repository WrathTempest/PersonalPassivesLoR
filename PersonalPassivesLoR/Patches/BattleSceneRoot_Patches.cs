using LOR_DiceSystem;
using DestinyofImmortal.Utils;
using PersonalPassivesLoR.Passives;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UI;
using UnityEngine.UI;

namespace PersonalPassivesLoR.Patches
{
    [HarmonyPatch]
    internal class BattleSceneRoot_Patches
    {
        private static bool _isCreatureStorage = false;
        [HarmonyPatch(typeof(BattleSceneRoot), nameof(BattleSceneRoot.ChangeToSpecialMap))]
        [HarmonyPrefix]
        public static void SpecialMapPatch(BattleSceneRoot __instance, string mapName, bool playEffect, bool scaleChange)
        {
            if (__instance.currentMapObject.isCreature)
            {
                _isCreatureStorage = true;
                __instance.currentMapObject.isCreature = false;
            }

        }

        [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.Die))]
        [HarmonyPrefix]
        public static bool BypassDeath(BattleUnitModel __instance)
        {
            var passive = Helpers.GetPassive<PassiveAbility_HeavenlyDemon1>(__instance.passiveDetail);
            if (passive != null)
            {
                if (!passive.forcedDeath)
                {
                    passive.forcedDeath = true;
                    passive.ForceSecondPhase();
                    return false;
                }
            }
            return true;

        }

        [HarmonyPatch(typeof(BattleSceneRoot), nameof(BattleSceneRoot.ChangeToSpecialMap))]
        [HarmonyPostfix]
        public static void SpecialMapPatchPostfix(BattleSceneRoot __instance, string mapName, bool playEffect, bool scaleChange)
        {
            if (_isCreatureStorage)
            {
                _isCreatureStorage = false;
                __instance.currentMapObject.isCreature = true;
            }

        }

        [HarmonyPatch(typeof(BattleSoundManager), nameof(BattleSoundManager.ChangeEnemyTheme))]
        [HarmonyPrefix]
        [HarmonyPriority(0)]
        public static bool EnemyTheme(BattleSoundManager __instance, ref int idx)
        {
            UnityEngine.Debug.Log($"Enemy Theme called! idx: {idx}");
            if (idx == 0 || idx == 1)
            {
                return false;
            }
            if (PassiveAbility_HeavenlyDemon1.secondPhaseGlobal)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(BattleSoundManager), nameof(BattleSoundManager.ChangeAllyTheme))]
        [HarmonyPrefix]
        [HarmonyPriority(0)]
        public static bool AllyTheme(BattleSoundManager __instance, ref int idx)
        {
            UnityEngine.Debug.Log($"Ally Theme called! idx: {idx}");
            if (idx == 0 || idx == 1)
            {
                return false;
            }
            if (PassiveAbility_HeavenlyDemon1.secondPhaseGlobal)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(DiceCardXmlInfo), nameof(DiceCardXmlInfo.IsOnlyPage))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void RemoveOnlyPage(DiceCardXmlInfo __instance, ref bool __result)
        {
            List<CardOption> optionList = Helpers.GetPrivateField<List<CardOption>>(__instance, "optionList");
            optionList.Remove(CardOption.OnlyPage);
            Helpers.SetPrivateField<List<CardOption>>(__instance, "optionList", optionList);
            __result = false;
        }

        //[HarmonyPatch(typeof(BookModel), nameof(BookModel.GetOnlyCards))]
        //[HarmonyPostfix]
        //public static void RemoveOnlyCards(BookModel __instance, ref List<DiceCardXmlInfo> __result)
        //{
        //    __result = new List<DiceCardXmlInfo>();
        //    Helpers.SetPrivateField<List<DiceCardXmlInfo>>(__instance, "_onlyCards", __result);
        //}
    }
}