using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalPassivesLoR.Passives
{
    public class PassiveAbility_HeavenlyDemon5 : PassiveAbilityBase
    {
        private string Name = "Traces of Divinity";

        public override void OnRoundStart()
        {
            Faction targetFaction = (owner.faction == Faction.Player) ? Faction.Enemy : Faction.Player;
            foreach (BattleUnitModel enemy in BattleObjectManager.instance.GetAliveList(targetFaction))
            {
                if (!enemy.bufListDetail.HasBuf<BattleUnitBuf_HDDisadvantage>())
                {
                    enemy.bufListDetail.AddBufWithoutDuplication(new BattleUnitBuf_HDDisadvantage());
                }
                
            }
        }
        public override void ChangeDiceResult(BattleDiceBehavior behavior, ref int diceResult)
        {
            int level = owner.emotionDetail?.EmotionLevel ?? 0;

            int diceMin = behavior.GetDiceMin();
            int diceMax = behavior.GetDiceMax();

            int originalResult = diceResult;
            int advantageRoll = DiceStatCalculator.MakeDiceResult(diceMin, diceMax, 0);

            if (advantageRoll > originalResult)
            {
                diceResult = advantageRoll;

                behavior.owner.battleCardResultLog?.SetVanillaDiceValue(originalResult);
                behavior.owner.battleCardResultLog?.SetPassiveAbility(this);
            }
            
            diceResult += level;
            //UnityEngine.Debug.Log($"Reroll triggered! Original result: {originalResult}, advantageRoll: {advantageRoll}, final diceResult: {diceResult}");
        }
        public class BattleUnitBuf_HDDisadvantage : BattleUnitBuf
        {
            public override bool Hide => true;
            public override void ChangeDiceResult(BattleDiceBehavior behavior, ref int diceResult)
            {

                int diceMin = behavior.GetDiceMin();
                int diceMax = behavior.GetDiceMax();

                int originalResult = diceResult;
                int disadvantageRoll = DiceStatCalculator.MakeDiceResult(diceMin, diceMax, 0);

                if (disadvantageRoll < originalResult)
                {
                    diceResult = disadvantageRoll;

                    behavior.owner.battleCardResultLog?.SetVanillaDiceValue(originalResult);
                }
                UnityEngine.Debug.Log($"Disadvantage triggered! Original result: {originalResult}, disadvantageRoll: {disadvantageRoll}, final diceResult: {diceResult}");
            }
        }
    }
}
