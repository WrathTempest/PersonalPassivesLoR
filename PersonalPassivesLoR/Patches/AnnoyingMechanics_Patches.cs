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