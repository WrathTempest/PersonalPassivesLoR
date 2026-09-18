using DestinyofImmortal.Utils;
using LOR_DiceSystem;
using Sound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PersonalPassivesLoR.Passives
{
    public class PassiveAbility_HeavenlyDemonStance : PassiveAbilityBase
    {
        // Token: 0x17000872 RID: 2162
        // (get) Token: 0x06004DD9 RID: 19929 RVA: 0x001A357B File Offset: 0x001A177B
        public PurpleStance CurrentStance
        {
            get
            {
                return this._currentStance;
            }
        }

        // Token: 0x06004DDA RID: 19930 RVA: 0x001A3583 File Offset: 0x001A1783
        public override void OnWaveStart()
        {
            this.InitPurple();
        }

        // Token: 0x06004DDB RID: 19931 RVA: 0x001A358C File Offset: 0x001A178C
        public override void OnRoundStart()
        {
            base.OnRoundStart();
            Helpers.ChangeSkinSoundFromSkinName(owner.view.charAppearance, "BlackSilence3");
            switch (this.CurrentStance)
            {
                case PurpleStance.Slash:
                    this.owner.UnitData.historyInWave.purpleTearForm_Sla++;
                    break;
                case PurpleStance.Penetrate:
                    this.owner.UnitData.historyInWave.purpleTearForm_Pen++;
                    break;
                case PurpleStance.Hit:
                    this.owner.UnitData.historyInWave.purpleTearForm_Hit++;
                    break;
                case PurpleStance.Defense:
                    this.owner.UnitData.historyInWave.purpleTearForm_Def++;
                    break;
                default:
                    break;
            }
            ChangeStances();
        }

        // Token: 0x06004DDC RID: 19932 RVA: 0x001A3583 File Offset: 0x001A1783
        public override void OnUnitCreated()
        {
            //this.InitPurple();
            SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Purple_Change", false, 1f, null);
            
        }

        // Token: 0x06004DDD RID: 19933 RVA: 0x001A3634 File Offset: 0x001A1834

        private void ChangeStances()
        {
            switch (Singleton<StageController>.Instance.RoundTurn % 4)
            {
                case 1:
                    this.ChangeStance_slash();
                    return;
                case 2:
                    this.ChangeStance_penetrate();
                    return;
                case 3:
                    this.ChangeStance_hit();
                    return;
                case 0:
                    this.ChangeStance_defense();
                    return;
                default:
                    return;
            }
        }
        private void InitPurple()
        {
            // to add custom cards for this
            //this.owner.personalEgoDetail.AddCard(609020);
            //this.owner.personalEgoDetail.AddCard(609021);
            //this.owner.personalEgoDetail.AddCard(609022);
            //this.owner.personalEgoDetail.AddCard(609023);
            switch (RandomUtil.Range(0, 3))
            {
                case 0:
                    this.ChangeStance_slash();
                    return;
                case 1:
                    this.ChangeStance_penetrate();
                    return;
                case 2:
                    this.ChangeStance_hit();
                    return;
                case 3:
                    this.ChangeStance_defense();
                    return;
                default:
                    return;
            }
        }

        // Token: 0x06004DDE RID: 19934 RVA: 0x001A36D0 File Offset: 0x001A18D0
        private void RemoveAllStanceBufandPassive()
        {
            var passivePen = Helpers.GetPassive<PassiveAbility_260005>(owner.passiveDetail);
            if (passivePen != null)
            {
                owner.passiveDetail.DestroyPassive(passivePen);
                owner.passiveDetail.RemovePassive();
            }
                      
        }

        // Token: 0x06004DDF RID: 19935 RVA: 0x001A3728 File Offset: 0x001A1928
        public void ChangeStance_slash()
        {
            this.owner.UnitData.historyInWave.purpleTearForm_Sla++;
            this.RemoveAllStanceBufandPassive();
            SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Purple_Change", false, 1f, null);
            this._currentStance = PurpleStance.Slash;
            if (this.owner.faction == Faction.Player)
            {
                //this.owner.view.speedDiceSetterUI.DeselectAll();
                int count = (owner.savedCardDetail ?? owner.allyCardDetail).GetHand().Count;
                List<DiceCardXmlInfo> deckForBattle = this.owner.UnitData.unitData.GetDeckForBattle(0);
                this.owner.ChangeBaseDeck(deckForBattle, count);
            }
        }

        // Token: 0x06004DE0 RID: 19936 RVA: 0x001A3840 File Offset: 0x001A1A40
        public void ChangeStance_penetrate()
        {
            this.owner.UnitData.historyInWave.purpleTearForm_Pen++;        
            this.RemoveAllStanceBufandPassive();
            owner.passiveDetail.AddPassive(new PassiveAbility_260005());
            SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Purple_Change", false, 1f, null);
            this._currentStance = PurpleStance.Penetrate;
            if (this.owner.faction == Faction.Player)
            {
                //this.owner.view.speedDiceSetterUI.DeselectAll();
                int count = (owner.savedCardDetail ?? owner.allyCardDetail).GetHand().Count;
                List<DiceCardXmlInfo> deckForBattle = this.owner.UnitData.unitData.GetDeckForBattle(1);
                this.owner.ChangeBaseDeck(deckForBattle, count);
            }
        }

        // Token: 0x06004DE1 RID: 19937 RVA: 0x001A3958 File Offset: 0x001A1B58
        public void ChangeStance_hit()
        {
            
            this.owner.UnitData.historyInWave.purpleTearForm_Hit++;
            this.RemoveAllStanceBufandPassive();
            this.owner.bufListDetail.AddBuf(new BattleUnitBuf_purpleHit());

            SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Purple_Change", false, 1f, null);
            this._currentStance = PurpleStance.Hit;
            if (this.owner.faction == Faction.Player)
            {
                //this.owner.view.speedDiceSetterUI.DeselectAll();
                int count = (owner.savedCardDetail ?? owner.allyCardDetail).GetHand().Count;
                List<DiceCardXmlInfo> deckForBattle = this.owner.UnitData.unitData.GetDeckForBattle(2);
                this.owner.ChangeBaseDeck(deckForBattle, count);
            }
        }

        // Token: 0x06004DE2 RID: 19938 RVA: 0x001A3A70 File Offset: 0x001A1C70
        public void ChangeStance_defense()
        {
            Helpers.ChangeSkinSoundFromSkinName(owner.view.charAppearance, "TheHead");
            this.owner.UnitData.historyInWave.purpleTearForm_Def++;
            this.RemoveAllStanceBufandPassive();
            this.owner.bufListDetail.AddBuf(new BattleUnitBuf_purpleDefense());
            SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Purple_Change", false, 1f, null);
            this._currentStance = PurpleStance.Defense;
            if (this.owner.faction == Faction.Player)
            {
                //this.owner.view.speedDiceSetterUI.DeselectAll();
                int count = (owner.savedCardDetail ?? owner.allyCardDetail).GetHand().Count;
                List<DiceCardXmlInfo> deckForBattle = this.owner.UnitData.unitData.GetDeckForBattle(3);
                this.owner.ChangeBaseDeck(deckForBattle, count);
            }
        }

        

        // Token: 0x04003694 RID: 13972
        private PurpleStance _currentStance;
    }
}
