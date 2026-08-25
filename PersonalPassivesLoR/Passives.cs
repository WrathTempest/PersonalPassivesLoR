//using CustomInvitation;
using LOR_DiceSystem;
using Sound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI;

namespace PersonalPassivesLoR
{
    public class PassiveAbility_HeavenlyDemon1 : PassiveAbilityBase
    {
        public static string Name = "Essence of Destruction";
        public static string Desc = "Increases speed die by 2, increases it by another 2 at emotion level 3. Recover 5% HP and apply debuffs to all enemies at the start of rounds. Grant various debuff immunities. At emotion level 3, deal increased damage and take decreased damage.";
        private int speedDice = 2;
        private int speedDiceEmotion = 2;
        private int dicePowerBonus = 2;
        private int perHPRecover = 5;
        private int minHandSize = 6;
        private int _elapsedRound = 0;
        private float SecondPhaseTrigger => this.owner.MaxHp * 0.5f;
        private bool canTriggerSecondPhase = true;
        private bool secondPhaseReady = false;
        private bool inSecondPhase = false;
        private int _dmgReduction = 0;
        private List<KeywordBuf> debuffImmune = new List<KeywordBuf>() { KeywordBuf.Stun, KeywordBuf.Seal, KeywordBuf.Decay, KeywordBuf.Disarm, KeywordBuf.Binding, KeywordBuf.Shock,
            KeywordBuf.Burn, KeywordBuf.Paralysis, KeywordBuf.Bleeding};

        private void DrawPages()
        {
            int count = (owner.savedCardDetail ?? owner.allyCardDetail).GetHand().Count;
            int num = minHandSize - count;
            if (num > 0)
            {
                owner.allyCardDetail.DrawCards(num);
            }
        }

        private void SetResistances()
        {
            if (!inSecondPhase)
            {
                this.owner.Book.SetResistHP(BehaviourDetail.Slash, AtkResist.Normal);
                this.owner.Book.SetResistHP(BehaviourDetail.Penetrate, AtkResist.Normal);
                this.owner.Book.SetResistHP(BehaviourDetail.Hit, AtkResist.Normal);
                this.owner.Book.SetResistBP(BehaviourDetail.Slash, AtkResist.Normal);
                this.owner.Book.SetResistBP(BehaviourDetail.Penetrate, AtkResist.Normal);
                this.owner.Book.SetResistBP(BehaviourDetail.Hit, AtkResist.Normal);
            }
            else
            {
                this.owner.Book.SetResistHP(BehaviourDetail.Slash, AtkResist.Endure);
                this.owner.Book.SetResistHP(BehaviourDetail.Penetrate, AtkResist.Endure);
                this.owner.Book.SetResistHP(BehaviourDetail.Hit, AtkResist.Endure);
                this.owner.Book.SetResistBP(BehaviourDetail.Slash, AtkResist.Endure);
                this.owner.Book.SetResistBP(BehaviourDetail.Penetrate, AtkResist.Endure);
                this.owner.Book.SetResistBP(BehaviourDetail.Hit, AtkResist.Endure);
            }
            
        }

        private void AddAura()
        {
            if (owner.bufListDetail.GetActivatedBufList().Find((BattleUnitBuf x) => x is BattleUnitBuf_KeterFinal_LibrarianAura) == null)
            {
                BattleUnitBuf_KeterFinal_LibrarianAura battleUnitBuf_KeterFinal_LibrarianAura = new BattleUnitBuf_KeterFinal_LibrarianAura();
                owner.bufListDetail.AddBuf(battleUnitBuf_KeterFinal_LibrarianAura);
            }
        }
        private void TrySecondPhase()
        {
            if (!secondPhaseReady) return;
            if (!canTriggerSecondPhase) return;
            secondPhaseReady = false;
            canTriggerSecondPhase = false;
            inSecondPhase = true;
            this.owner.RecoverBreakLife(1, false);
            this.owner.ResetBreakGauge();
            this.owner.breakDetail.nextTurnBreak = false;           
            SetResistances();
            this.owner.bufListDetail.AddBufWithoutDuplication(new BattleUnitBuf_HDBuff());
        }

        private void SetSize()
        {
            UnitDataModel dataModel = owner.UnitData.unitData;
            if (dataModel.customizeData != null)
            {
                SingletonBehavior<UICharacterRenderer>.Instance.SetTextureSize(dataModel, (int)(dataModel.customizeData.height * 1.5));
            }
        }
        public override void OnRoundEndTheLast()
        {
            this.TrySecondPhase();
        }
        public override void OnUnitCreated()
        {
            SetResistances();
            //SetSize();
            AddAura();
        }
        public override void OnDrawCard()
        {
            base.OnDrawCard();
            this._elapsedRound++;
            if (this._elapsedRound >= 3)
            {
                this._elapsedRound = 0;
                if (this.owner.allyCardDetail.GetHand().Count >= this.owner.allyCardDetail.maxHandCount)
                {
                    this.owner.allyCardDetail.DiscardInHand(1);
                }
                this.owner.allyCardDetail.AddNewCard(616007, false);
            }
        }
        public override void OnRoundStart()
        {
            this.owner.ShowPassiveTypo(this);
            TrySecondPhase();

            //get all enemy units that are still alive
            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList((this.owner.faction == Faction.Player) ? Faction.Enemy : Faction.Player);
            //loop through each unit
            foreach (BattleUnitModel battleUnitModel in aliveList)
            {
                //apply debuffs
                battleUnitModel.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Burn, 3, null);
            }
            if (inSecondPhase)
            {
                int recovery = this.owner.MaxHp * perHPRecover / 100;
                this.owner.RecoverHP(recovery);
            }
                     
            this.owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, Singleton<StageController>.Instance.RoundTurn, null);
            this.owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Endurance, Singleton<StageController>.Instance.RoundTurn, null);
            if (owner.bufListDetail.GetActivatedBuf(KeywordBuf.KeterFinal_DoubleEmotion) == null)
            {
                owner.bufListDetail.AddBuf(new BattleUnitBuf_KeterFinal_DoubleEmotion());
            }

            DrawPages();
        }
        public override bool BeforeTakeDamage(BattleUnitModel attacker, int dmg)
        {
            this._dmgReduction = 0;

            if (canTriggerSecondPhase && this.owner.hp - dmg <= SecondPhaseTrigger)
            {
                this._dmgReduction = (int)(SecondPhaseTrigger - (this.owner.hp - dmg));
                this.secondPhaseReady = true;
            }

            return base.BeforeTakeDamage(attacker, dmg);
        }

        public override int GetDamageReductionAll()
        {
            if (canTriggerSecondPhase && this.owner.hp <= SecondPhaseTrigger)
            {
                this.secondPhaseReady = true;
                return 9999;
            }

            return this._dmgReduction;
        }
        public override bool IsImmune(KeywordBuf buf)
        {
            return debuffImmune.Contains(buf);
        }
        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                power = dicePowerBonus
            });
        }
        public override int SpeedDiceNumAdder()
        {
            return owner?.emotionDetail?.EmotionLevel >= 3 ? speedDice + speedDiceEmotion : speedDice;
        }

        public class BattleUnitBuf_HDBuff : BattleUnitBuf
        {
            // Token: 0x17000EEF RID: 3823
            // (get) Token: 0x06008F3F RID: 36671 RVA: 0x002A7BA1 File Offset: 0x002A5DA1
            public override KeywordBuf bufType
            {
                get
                {
                    return KeywordBuf.Maxim;
                }
            }

            // Token: 0x06008F40 RID: 36672 RVA: 0x001036F9 File Offset: 0x001018F9
            public override int GetDamageReductionRate()
            {
                return 30;
            }

            // Token: 0x06008F41 RID: 36673 RVA: 0x001036F9 File Offset: 0x001018F9
            public override int GetBreakDamageReductionRate()
            {
                return 30;
            }

            // Token: 0x06008F42 RID: 36674 RVA: 0x002A7BA5 File Offset: 0x002A5DA5
            public override void BeforeGiveDamage(BattleDiceBehavior behavior)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    dmgRate = 50,
                    breakRate = 50
                });
            }
        }
    }

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

        // Token: 0x06004DDC RID: 19932 RVA: 0x001A3583 File Offset: 0x001A1783
        public override void OnUnitCreated()
        {
            this.InitPurple();
            SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Purple_Change", false, 1f, null);
        }

        // Token: 0x06004DDD RID: 19933 RVA: 0x001A3634 File Offset: 0x001A1834
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
        private void RemoveAllStanceBuf()
        {
            this.owner.bufListDetail.RemoveBufAll(KeywordBuf.PurpleSlash);
            this.owner.bufListDetail.RemoveBufAll(KeywordBuf.PurplePenetrate);
            this.owner.bufListDetail.RemoveBufAll(KeywordBuf.PurpleHit);
            this.owner.bufListDetail.RemoveBufAll(KeywordBuf.PurpleDefense);
        }

        // Token: 0x06004DDF RID: 19935 RVA: 0x001A3728 File Offset: 0x001A1928
        public void ChangeStance_slash()
        {
            this.owner.UnitData.historyInWave.purpleTearForm_Sla++;
            this.RemoveAllStanceBuf();
            this.owner.bufListDetail.AddBuf(new BattleUnitBuf_purpleSlash());
            owner.view.SetAltSkin("TwistedArgalia");
            this.owner.view.StartEgoSkinChangeEffect("Character");
            
            this._currentStance = PurpleStance.Slash;
            if (this.owner.faction == Faction.Player)
            {
                this.owner.view.speedDiceSetterUI.DeselectAll();
                int count = (owner.savedCardDetail ?? owner.allyCardDetail).GetHand().Count;
                List<DiceCardXmlInfo> deckForBattle = this.owner.UnitData.unitData.GetDeckForBattle(0);
                this.owner.ChangeBaseDeck(deckForBattle, count);
            }
        }

        // Token: 0x06004DE0 RID: 19936 RVA: 0x001A3840 File Offset: 0x001A1A40
        public void ChangeStance_penetrate()
        {
            this.owner.UnitData.historyInWave.purpleTearForm_Pen++;
            this.RemoveAllStanceBuf();
            this.owner.bufListDetail.AddBuf(new BattleUnitBuf_purplePenetrate());
            owner.view.SetAltSkin("TheRedMist");
            this.owner.view.StartEgoSkinChangeEffect("Character");
            //SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Purple_Change", false, 1f, null);
            this._currentStance = PurpleStance.Penetrate;
            if (this.owner.faction == Faction.Player)
            {
                this.owner.view.speedDiceSetterUI.DeselectAll();
                int count = (owner.savedCardDetail ?? owner.allyCardDetail).GetHand().Count;
                List<DiceCardXmlInfo> deckForBattle = this.owner.UnitData.unitData.GetDeckForBattle(1);
                this.owner.ChangeBaseDeck(deckForBattle, count);
            }
        }

        // Token: 0x06004DE1 RID: 19937 RVA: 0x001A3958 File Offset: 0x001A1B58
        public void ChangeStance_hit()
        {
            this.owner.UnitData.historyInWave.purpleTearForm_Hit++;
            this.RemoveAllStanceBuf();
            this.owner.bufListDetail.AddBuf(new BattleUnitBuf_purpleHit());
            owner.view.SetAltSkin("TwistPhilip");
            this.owner.view.StartEgoSkinChangeEffect("Character");
            //SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Purple_Change", false, 1f, null);
            this._currentStance = PurpleStance.Hit;
            if (this.owner.faction == Faction.Player)
            {
                this.owner.view.speedDiceSetterUI.DeselectAll();
                int count = (owner.savedCardDetail ?? owner.allyCardDetail).GetHand().Count;
                List<DiceCardXmlInfo> deckForBattle = this.owner.UnitData.unitData.GetDeckForBattle(2);
                this.owner.ChangeBaseDeck(deckForBattle, count);
            }
        }

        // Token: 0x06004DE2 RID: 19938 RVA: 0x001A3A70 File Offset: 0x001A1C70
        public void ChangeStance_defense()
        {
            this.owner.UnitData.historyInWave.purpleTearForm_Def++;
            this.RemoveAllStanceBuf();
            this.owner.bufListDetail.AddBuf(new BattleUnitBuf_purpleDefense());
            owner.view.SetAltSkin("TwistEileen");
            this.owner.view.StartEgoSkinChangeEffect("Character");
            //SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Purple_Change", false, 1f, null);
            this._currentStance = PurpleStance.Defense;
            if (this.owner.faction == Faction.Player)
            {
                this.owner.view.speedDiceSetterUI.DeselectAll();
                int count = (owner.savedCardDetail ?? owner.allyCardDetail).GetHand().Count;
                List<DiceCardXmlInfo> deckForBattle = this.owner.UnitData.unitData.GetDeckForBattle(3);
                this.owner.ChangeBaseDeck(deckForBattle, count);
            }
        }

        // Token: 0x04003694 RID: 13972
        private PurpleStance _currentStance;
    }
}
