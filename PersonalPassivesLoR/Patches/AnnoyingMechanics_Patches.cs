using DestinyofImmortal.Utils;
using HarmonyLib;
using PersonalPassivesLoR.Passives;
using LOR_DiceSystem;
using Sound;
using System;
using System.Collections.Generic;
using UI;
using UnityEngine.UI;

namespace PersonalPassivesLoR.Patches
{
    [HarmonyPatch]
    internal class AnnoyingMechanics_Patches
    {
        [HarmonyPatch(typeof(BookModel), nameof(BookModel.IsLockByBluePrimary))]
        [HarmonyPostfix]
        public static void RemoveCardLock(ref bool __result)
        {
            __result = false;

        }

        [HarmonyPatch(typeof(PassiveAbility_1303012), nameof(PassiveAbility_1303012.OnUseCard))]
        [HarmonyPrefix]
        [HarmonyPriority(0)]
        public static bool BypassMeatPassive(PassiveAbility_1303012 __instance, BattlePlayingCardDataInUnitModel curCard)
        {
            BattleUnitModel owner = Helpers.GetPrivateField<BattleUnitModel>(__instance, "owner");
            UnityEngine.Debug.Log($"In BypassMeat Passive patch! Current target: {curCard.target.UnitData.unitData.name}");
            owner.Die();
            if (curCard.card.GetID() != 703319)
            {
                return true;
            }
            
            BattleUnitModel target = curCard.target;
            if (target.passiveDetail.HasPassive<PassiveAbility_HeavenlyDemon1>())
            {
                Helpers.SetPrivateField<BattleUnitModel>(__instance, "_meat", null);
                UnityEngine.Debug.Log($"Successfully bypassed");
                return false;
            }
            return true;

        }

        [HarmonyPatch(typeof(DiceCardSelfAbility_greta_catch), nameof(DiceCardSelfAbility_greta_catch.OnUseCard))]
        [HarmonyPrefix]
        [HarmonyPriority(0)]
        public static bool BypassMeat(DiceCardSelfAbility_greta_catch __instance)
        {
            BattleUnitModel target = __instance.card.target;
            if (target.passiveDetail.HasPassive<PassiveAbility_HeavenlyDemon1>())
            {
                return false;
            }
            return true;

        }

        [HarmonyPatch(typeof(PassiveAbility_605211), nameof(PassiveAbility_605211.OnRoundStartAfter))]
        [HarmonyPrefix]
        public static bool RemoveSwallow(PassiveAbility_605211 __instance)
        {
            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList(Faction.Player);
            BattleUnitModel battleUnitModel = aliveList.Find((BattleUnitModel x) => x.passiveDetail.HasPassive<PassiveAbility_HeavenlyDemon1>());
            if (battleUnitModel != null)
            {
                return false;
            }
            return true;

        }

        [HarmonyPatch(typeof(PassiveAbility_250227), nameof(PassiveAbility_250227.OnRoundStart))]
        [HarmonyPrefix]
        public static bool BypassPTTeleport(PassiveAbility_250227 __instance)
        {
            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList(Faction.Player);
            BattleUnitModel battleUnitModel = aliveList.Find((BattleUnitModel x) => x.passiveDetail.HasPassive<PassiveAbility_HeavenlyDemon1>());
            if (battleUnitModel == null) return true;

            BattleUnitModel owner = Helpers.GetPrivateField<BattleUnitModel>(__instance, "owner");
            bool _teleportReady = Helpers.GetPrivateField<bool>(__instance, "_teleportReady");
            int _teleportCondition = Helpers.GetPrivateField<int>(__instance, "_teleportCondition");
            if (owner.UnitData.floorBattleData.param2 <= 0 && (_teleportReady || owner.hp <= (float)_teleportCondition))
            {
                owner.UnitData.floorBattleData.param2 = 1;
                Singleton<StageController>.Instance.ChangeFloorForcely(Singleton<StageController>.Instance.CurrentFloor, owner);
            }
            return false;

        }

    }
}