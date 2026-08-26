//using CustomInvitation;
using Battle.CreatureEffect;
using LOR_DiceSystem;
using Sound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UI;
using UnityEngine;
using static EmotionCardAbility_blackswan1;

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

        private List<KeywordBuf> debuffImmune = new List<KeywordBuf>()
        {
        KeywordBuf.Stun, KeywordBuf.Seal, KeywordBuf.Decay, KeywordBuf.Disarm,
        KeywordBuf.Binding, KeywordBuf.Shock, KeywordBuf.Burn, KeywordBuf.Paralysis, KeywordBuf.Bleeding
        };

        private string giftAuraPath = "Prefabs/Gifts/Gifts_NeedRename/Gift_BlueReverberation2";

        // Track all spawned visual effects for clean destruction
        // Dictionary mapping path -> spawned GameObject to natively block duplicates
        private Dictionary<string, GameObject> _spawnedAuraObjects = new Dictionary<string, GameObject>();

        // ==========================================
        // AURA HELPER METHODS (WITH DUPLICATE CHECK)
        // ==========================================

        // 1. Gift Appearance Aura Helper
        private GameObject AttachGiftAura(string giftPath, float scale = 1.0f)
        {
            return AttachGiftAura(giftPath, Vector3.one * scale);
        }

        private GameObject AttachGiftAura(string giftPath, Vector3 scale)
        {
            if (_spawnedAuraObjects.TryGetValue(giftPath, out GameObject existing) && existing != null)
            {
                return existing; // Already spawned
            }

            CharacterAppearance charApp = this.owner?.view?.charAppearance;
            if (charApp == null) return null;

            GiftAppearance_Aura prefab = Resources.Load<GiftAppearance_Aura>(giftPath);
            if (prefab == null)
            {
                Debug.LogError($"GiftAppearance_Aura prefab not found at path: {giftPath}");
                return null;
            }

            GiftAppearance_Aura instance = UnityEngine.Object.Instantiate(prefab);
            FieldInfo auraField = typeof(GiftAppearance_Aura).GetField("_aura", BindingFlags.NonPublic | BindingFlags.Instance);

            if (auraField?.GetValue(instance) is BodyAura aura)
            {
                aura.gameObject.SetActive(true);
                aura.transform.parent = charApp.transform;
                aura.transform.localPosition = Vector3.zero;
                aura.transform.localRotation = Quaternion.identity;
                aura.transform.localScale = scale;
                aura.SetAppearance(charApp);
            }

            _spawnedAuraObjects[giftPath] = instance.gameObject;
            return instance.gameObject;
        }

        // 2. BodyAura / Prefab Helper
        private GameObject AttachBodyAura(string prefabPath, float scale = 1.0f)
        {
            return AttachBodyAura(prefabPath, Vector3.one * scale);
        }

        private GameObject AttachBodyAura(string prefabPath, Vector3 scale)
        {
            if (_spawnedAuraObjects.TryGetValue(prefabPath, out GameObject existing) && existing != null)
            {
                return existing; // Already spawned
            }

            CharacterAppearance charApp = this.owner?.view?.charAppearance;
            if (charApp == null) return null;

            GameObject auraObj = Util.LoadPrefab(prefabPath);
            if (auraObj != null)
            {
                auraObj.transform.parent = charApp.transform;
                auraObj.transform.localPosition = Vector3.zero;
                auraObj.transform.localRotation = Quaternion.identity;
                auraObj.transform.localScale = scale;

                BodyAura bodyAura = auraObj.GetComponent<BodyAura>();
                if (bodyAura != null)
                {
                    bodyAura.SetAppearance(this.owner);
                }

                _spawnedAuraObjects[prefabPath] = auraObj;
            }
            return auraObj;
        }

        // 3. FX / CreatureEffect Helper
        private GameObject AttachFXAura(string fxPath, float scale = 1.0f)
        {
            if (_spawnedAuraObjects.TryGetValue(fxPath, out GameObject existing) && existing != null)
            {
                return existing; // Already spawned
            }

            if (this.owner?.view == null) return null;

            CreatureEffect effect = SingletonBehavior<DiceEffectManager>.Instance.CreateNewFXCreatureEffect(
                fxPath, scale, this.owner.view, this.owner.view, -1f);

            if (effect != null && effect.gameObject != null)
            {
                _spawnedAuraObjects[fxPath] = effect.gameObject;
                return effect.gameObject;
            }
            return null;
        }

        // 4. Particle Helper
        private GameObject AttachParticle(string particlePath, float scale = 1.5f)
        {
            if (_spawnedAuraObjects.TryGetValue(particlePath, out GameObject existing) && existing != null)
            {
                return existing; // Already spawned
            }

            CharacterAppearance charApp = this.owner?.view?.charAppearance;
            if (charApp == null) return null;

            UnityEngine.Object particleResource = Resources.Load(particlePath);
            if (particleResource != null)
            {
                GameObject particleObj = UnityEngine.Object.Instantiate(particleResource) as GameObject;
                if (particleObj != null)
                {
                    particleObj.transform.parent = charApp.transform;
                    particleObj.transform.localPosition = Vector3.zero;
                    particleObj.transform.localRotation = Quaternion.identity;
                    particleObj.transform.localScale = Vector3.one * scale;

                    _spawnedAuraObjects[particlePath] = particleObj;
                    return particleObj;
                }
            }
            return null;
        }

        // Single master destruction method
        private void DestroyAuras()
        {
            foreach (KeyValuePair<string, GameObject> kvp in _spawnedAuraObjects)
            {
                if (kvp.Value != null)
                {
                    UnityEngine.Object.Destroy(kvp.Value);
                }
            }
            _spawnedAuraObjects.Clear();
        }

        // ==========================================
        // PASSIVE LOGIC & LIFECYCLE
        // ==========================================

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
            AtkResist resist = inSecondPhase ? AtkResist.Endure : AtkResist.Normal;

            this.owner.Book.SetResistHP(BehaviourDetail.Slash, resist);
            this.owner.Book.SetResistHP(BehaviourDetail.Penetrate, resist);
            this.owner.Book.SetResistHP(BehaviourDetail.Hit, resist);
            this.owner.Book.SetResistBP(BehaviourDetail.Slash, resist);
            this.owner.Book.SetResistBP(BehaviourDetail.Penetrate, resist);
            this.owner.Book.SetResistBP(BehaviourDetail.Hit, resist);
        }

        private void ResetFlags()
        {
            if (owner.bufListDetail.GetActivatedBufList().Find((BattleUnitBuf x) => x is PassiveAbility_160004.BattleUnitBuf_battle) == null)
            {
                canTriggerSecondPhase = true;
                secondPhaseReady = false;
                inSecondPhase = false;
                _dmgReduction = 0;
            }
        }

        private void ReduceEnemyHP(float percent)
        {
            foreach (BattleUnitModel enemy in BattleObjectManager.instance.GetAliveList(Faction.Enemy))
            {
                if (!enemy.IsExtinction())
                {
                    enemy.LoseHp((int)(enemy.MaxHp * percent));
                }
            }
        }

        private void TrySecondPhase()
        {
            if (!secondPhaseReady || !canTriggerSecondPhase) return;

            PlayChangingEffect();
            secondPhaseReady = false;
            canTriggerSecondPhase = false;
            inSecondPhase = true;

            this.owner.RecoverBreakLife(1, false);
            this.owner.ResetBreakGauge();
            this.owner.breakDetail.nextTurnBreak = false;
            SetResistances();

            owner.bufListDetail.AddBufWithoutDuplication(new BattleUnitBuf_HDBuff
            {
                hpAdder = (int)(owner.MaxHp * 0.3),
                breakGageAdder = (int)(owner.breakDetail.breakGauge * 0.5)
            });

            owner.RecoverHP(owner.MaxHp);
            owner.breakDetail.breakGauge = owner.breakDetail.GetDefaultBreakGauge();

            if (owner.bufListDetail.GetActivatedBufList().Find((BattleUnitBuf x) => x is BattleUnitBuf_KeterFinal_SilenceGirl_Gaze_Aura) == null)
            {
                owner.bufListDetail.AddBuf(new BattleUnitBuf_KeterFinal_SilenceGirl_Gaze_Aura());
            }
        }

        private void PlayChangingEffect()
        {
            this.owner.view.charAppearance.ChangeMotion(ActionDetail.Default);
            SingletonBehavior<BattleSceneRoot>.Instance.ChangeCreatureMap("RedHood", true);
            Util.LoadPrefab("Battle/DiceAttackEffects/CreatureBattle/EGO_Freischutz_6thBullet")
                .GetComponent<FarAreaEffect_EGO_Freischutz_6thBullet>()
                .Init(this.owner, Array.Empty<object>());

            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList(Faction.Enemy);
            if (aliveList.Count > 0)
            {
                Util.LoadPrefab("Battle/DiceAttackEffects/CreatureBattle/EGO_Freischutz_6thBullet")
                    .GetComponent<FarAreaEffect_EGO_Freischutz_6thBullet>()
                    .Init(aliveList[0], Array.Empty<object>());
            }

            // Refactored FX and Particle calls using helpers
            AttachFXAura("6_G/FX_IllusionCard_6_G_BloodAura", 1.25f);
            AttachParticle("Prefabs/Battle/SpecialEffect/RedMistRelease_ActivateParticle", 1.5f);

            SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Kali_Change", false, 1f, null);
            
            int emotionTotalCoinNumber = Singleton<StageController>.Instance.GetCurrentStageFloorModel().team.emotionTotalCoinNumber;
            Singleton<StageController>.Instance.GetCurrentWaveModel().team.emotionTotalBonus = emotionTotalCoinNumber + 1;
        }
        private void InitializeBuff()
        {
            if (owner.bufListDetail.GetActivatedBufList().Find((BattleUnitBuf x) => x is HDBuffInitialBoost) == null)
            {
                int maxHP = 0;
                int maxBreak = 0;
                List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList((this.owner.faction == Faction.Player) ? Faction.Enemy : Faction.Player);
                foreach (BattleUnitModel enemy in aliveList)
                {
                    if (enemy.MaxHp > maxHP) maxHP = enemy.MaxHp;
                    if (enemy.breakDetail.breakGauge > maxBreak) maxBreak = enemy.breakDetail.breakGauge;
                }
                owner.bufListDetail.AddBuf(new HDBuffInitialBoost
                {
                    hpAdder = owner.MaxHp * 3,
                    breakGageAdder = owner.breakDetail.GetDefaultBreakGauge(),
                });
                owner.RecoverHP(owner.MaxHp);
                owner.breakDetail.breakGauge = owner.breakDetail.GetDefaultBreakGauge();
                // Refactored Aura Initializations
                AttachBodyAura("Battle/DiceAttackEffects/New/FX/PC/Librarian/FX_PC_Librarian_Light", 3.5f);
                AttachGiftAura(giftAuraPath, 1.5f);
            }
            
        }

        private void ReduceCostDeck()
        {
            foreach (BattleDiceCardModel card in owner.allyCardDetail.GetDeck())
            {
                card.SetCostToZero();
            }
        }

        public override void OnUnitCreated()
        {
            ResetFlags();
            SetResistances();
            ReduceCostDeck();
        }

        public override void OnDie()
        {
            base.OnDie();
            // Single cleanup wipes all spawned BodyAuras, GiftAuras, FX, and Particles
            DestroyAuras();
        }

        public override void OnRoundEndTheLast()
        {
            this.TrySecondPhase();
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

        public override void OnWaveStart()
        {
            if (!inSecondPhase)
            {
                InitializeBuff();
            }
        }

        public override void OnRoundStart()
        {
            this.owner.ShowPassiveTypo(this);
            if (!inSecondPhase)
            {
                TrySecondPhase();
            }   
            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList((this.owner.faction == Faction.Player) ? Faction.Enemy : Faction.Player);
            foreach (BattleUnitModel enemy in aliveList)
            {
                enemy.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Burn, 3, null);
            }

            this.owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, Singleton<StageController>.Instance.RoundTurn, null);
            this.owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Endurance, Singleton<StageController>.Instance.RoundTurn, null);

            if (owner.bufListDetail.GetActivatedBuf(KeywordBuf.KeterFinal_DoubleEmotion) == null)
            {
                owner.bufListDetail.AddBuf(new BattleUnitBuf_KeterFinal_DoubleEmotion());
            }

            DrawPages();
            owner.cardSlotDetail.SetPlayPoint(this.owner.cardSlotDetail.GetMaxPlayPoint() + 1);
            this.owner.cardSlotDetail.RecoverPlayPoint(this.owner.cardSlotDetail.GetMaxPlayPoint());
            
            if (inSecondPhase)
            {
                int recovery = this.owner.MaxHp * perHPRecover / 100;
                this.owner.RecoverHP(recovery);
                ReduceEnemyHP(0.05f);
            }
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

        public class HDBuffInitialBoost : BattleUnitBuf
        {
            public override bool Hide
            {
                get
                {
                    return true;
                }
            }

            // Token: 0x06008F8D RID: 36749 RVA: 0x00103B28 File Offset: 0x00101D28
            public HDBuffInitialBoost()
            {
                this.stack = 0;
            }

            // Token: 0x06008F8E RID: 36750 RVA: 0x002A7DB1 File Offset: 0x002A5FB1
            public override StatBonus GetStatBonus()
            {
                return new StatBonus
                {
                    hpAdder = this.hpAdder,
                    breakGageAdder = this.breakGageAdder
                };
            }

            // Token: 0x04006DD9 RID: 28121
            public int hpAdder;

            // Token: 0x04006DDA RID: 28122
            public int breakGageAdder;
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

            public override StatBonus GetStatBonus()
            {
                return new StatBonus
                {
                    hpAdder = this.hpAdder,
                    breakGageAdder = this.breakGageAdder
                };
            }

            // Token: 0x04006DD9 RID: 28121
            public int hpAdder;

            // Token: 0x04006DDA RID: 28122
            public int breakGageAdder;

            // Token: 0x06008F40 RID: 36672 RVA: 0x001036F9 File Offset: 0x001018F9
            public override int GetDamageReductionRate()
            {
                return 0;
            }

            // Token: 0x06008F41 RID: 36673 RVA: 0x001036F9 File Offset: 0x001018F9
            public override int GetBreakDamageReductionRate()
            {
                return 0;
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
