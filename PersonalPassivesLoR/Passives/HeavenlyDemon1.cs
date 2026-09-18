//using CustomInvitation;
using Battle.CreatureEffect;
using DestinyofImmortal.Utils;
using ExtendedLoader;
using LOR_DiceSystem;
using LorIdExtensions;
using Sound;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UI;
using UnityEngine;
using PersonalPassivesLoR.MiscHelpers;
using static EmotionCardAbility_blackswan1;
using static PersonalPassivesLoR.Passives.PassiveAbility_HeavenlyDemon5;
using static UnityEngine.UI.CanvasScaler;

namespace PersonalPassivesLoR.Passives
{
    public class PassiveAbility_HeavenlyDemon1 : PassiveAbilityBase
    {
        private int _threerounds = 0;
        public bool forcedDeath = false;
        private int speedDice = 2;
        
        private int perHPRecover = 5;
        private int minHandSize = 9;
        private int _elapsedRound = 0;
        private string packageId = PersonalPassivesLoRInitializer.packageId;
        private float SecondPhaseTrigger => 1f;
        private bool canTriggerSecondPhase = true;
        private bool secondPhaseReady = false;
        public bool inSecondPhase = false;
        public static bool secondPhaseGlobal = false;
        private int _dmgReduction = 0;
        private int modCardnum = 7;
        private int emotionLevel => owner.emotionDetail?.EmotionLevel ?? 0;
        private int speedDiceEmotion => emotionLevel >= 3 ? 1 : 0;
        private List<BattleUnitModel> enemies => BattleObjectManager.instance.GetAliveList((owner.faction == Faction.Player) ? Faction.Enemy : Faction.Player);

        private readonly List<string> battleDialogues = new List<string>()
    {
        "Fools know when to give up. You, unfortunately, are no fool.",
        "You have come this far, only to kneel before me.",
        "Every step you take brings you closer to your end.",
        "Do you truly believe you can overcome me?",
        "Your struggle is admirable... and utterly futile.",
        "I have buried warriors greater than you.",
        "The longer this battle lasts, the more certain your death becomes.",
        "Show me what you are willing to sacrifice for victory.",
        "You mistake my patience for weakness.",
        "Enough games. Let us see how long you can endure."
    };

        private readonly List<string> battleDialogues2 = new List<string>()
    {
        "The heavens themselves have abandoned you.",
        "No prayer will reach you here.",
        "There is no salvation left for you.",
        "Even death will not spare you from what comes next.",
        "Your fate was decided long before this battle began.",
        "Struggle, rage, despair... none of it will change your ending.",
        "Let your final moments be filled with the knowledge that you were powerless.",
        "I will extinguish every last ember of hope within you.",
        "I will carve your name into the grave of the forgotten.",
        "This is the end. Not merely for you, but for everything you hoped to protect."
    };

        private readonly List<KeywordBuf> debuffImmune = new List<KeywordBuf>()
    {
        KeywordBuf.Binding, KeywordBuf.Shock, KeywordBuf.Burn, KeywordBuf.Paralysis, KeywordBuf.Bleeding
    };

        private readonly Dictionary<string, GameObject> _spawnedAuraObjects = new Dictionary<string, GameObject>();

        public override bool IsImmune(KeywordBuf buf)
        {
            return debuffImmune.Contains(buf);
        }

        private string _PREFAB_PATH = "Battle/DiceAttackEffects/New/FX/Mon/Xiao/FX_Mon_Xiao_Shout";

        private void EarthQuake()
        {
            GameObject gameObject = SingletonBehavior<BattleCamManager>.Instance.EffectCam.gameObject;
            if (gameObject != null)
            {
                GameObject gameObject2 = Util.LoadPrefab(_PREFAB_PATH);
                if (gameObject2 != null)
                {
                    gameObject2.transform.parent = owner.view.transform;
                    gameObject2.transform.localPosition = Vector3.zero;
                    gameObject2.transform.localRotation = Quaternion.identity;
                    UnityEngine.Object.Destroy(gameObject2, 2f);
                }
                CameraFilterPack_Distortion_ShockWave shockWave = gameObject.AddComponent<CameraFilterPack_Distortion_ShockWave>();
                Vector3 position = owner.view.charAppearance.atkEffectRoot.position;
                Vector3 vector = SingletonBehavior<BattleCamManager>.Instance.EffectCam.WorldToViewportPoint(position);
                shockWave.PosX = vector.x;
                shockWave.PosY = vector.y;
                shockWave.Speed = 2f;
                shockWave.Size = 1.5f;

                AutoScriptDestruct autoScriptDestruct = SingletonBehavior<BattleCamManager>.Instance?.EffectCam.gameObject.AddComponent<AutoScriptDestruct>();
                if (autoScriptDestruct != null)
                {
                    autoScriptDestruct.targetScript = shockWave;
                    autoScriptDestruct.time = 1f;
                }
            }
            CameraFilterUtil.EarthQuake(0.08f, 0.02f, 50f, 0.3f);
            SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Xiao_Roar");
        }

        private void ShowDialog()
        {
            List<string> targetList = inSecondPhase ? battleDialogues2 : battleDialogues;
            int index = (Singleton<StageController>.Instance.RoundTurn - 1) % targetList.Count;
            ShowDialog(targetList[index]);
        }

        private void ShowDialog(string dialog, Color? textColor = null, Color? glowColor = null)
        {
            if (inSecondPhase)
            {
                textColor = textColor ?? new Color(0.5f, 0.5f, 0.5f);
                glowColor = glowColor ?? new Color(1f, 0f, 0f);
            }
            Helpers.DisplayCustomAbnormalityDlg(owner.view.dialogUI, dialog, textColor, glowColor, 10f);
        }

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

            owner.Book.SetResistHP(BehaviourDetail.Slash, resist);
            owner.Book.SetResistHP(BehaviourDetail.Penetrate, resist);
            owner.Book.SetResistHP(BehaviourDetail.Hit, resist);
            owner.Book.SetResistBP(BehaviourDetail.Slash, resist);
            owner.Book.SetResistBP(BehaviourDetail.Penetrate, resist);
            owner.Book.SetResistBP(BehaviourDetail.Hit, resist);
        }

        private void ResetFlags()
        {
            if (owner.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_HDBuff) == null)
            {
                canTriggerSecondPhase = true;
                secondPhaseReady = false;
                inSecondPhase = false;
                _dmgReduction = 0;
            }
        }
        private void InitBgm()
        {
            if (this.owner.faction == Faction.Player)
            {
                AudioClip[] array = new AudioClip[3];
                AudioHelper.AudioStorage.TryGetValue("Persona5/Awakening", out AudioClip audioClip);
                if (audioClip != null)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = audioClip;
                    }
                    SingletonBehavior<BattleSoundManager>.Instance.SetAllyTheme(array);
                    SingletonBehavior<BattleSoundManager>.Instance.SetEnemyTheme(array);
                    SingletonBehavior<BattleSoundManager>.Instance.ChangeEnemyTheme(0);
                    return;
                }
                else
                {
                    Debug.LogError("Bgm Not found: Custom BGM Loader");
                }               
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

        public void ForceSecondPhase()
        {
            secondPhaseReady = true;
            canTriggerSecondPhase = true;
            owner.view.EnableView(true);
            owner.Extinct(false);
            owner.moveDetail.ReturnToFormationByBlink(false);
            TrySecondPhase();
        }

        private void TrySecondPhase()
        {
            if (!secondPhaseReady || !canTriggerSecondPhase) return;

            PlayChangingEffect();
            secondPhaseReady = false;
            canTriggerSecondPhase = false;
            inSecondPhase = true;
            secondPhaseGlobal = true;

            owner.RecoverBreakLife(1, false);
            owner.ResetBreakGauge();
            owner.SetKnockoutInsteadOfDeath(false);
            owner.breakDetail.nextTurnBreak = false;
            owner.bufListDetail.RemoveBufAll(BufPositiveType.Negative);
            SetResistances();
            EarthQuake();
            owner.UnitData.unitData.SetCustomName("Saint of Twin Blazes");
            owner.bufListDetail.AddBufWithoutDuplication(new BattleUnitBuf_HDBuff
            {
                breakGageAdder = (int)(owner.breakDetail.breakGauge * 0.5)
            });

            owner.RecoverHP(owner.MaxHp);
            owner.breakDetail.breakGauge = owner.breakDetail.GetDefaultBreakGauge();
        }

        private void PlayChangingEffect()
        {
            owner.view.charAppearance.ChangeMotion(ActionDetail.Default);

            MapManagerHelper.ChangeToInvitationMapWithDialogue("Yan", battleDialogues);
            AudioHelper.ChangeBGM("Persona5/Hymn of the Soul");
            
            Util.LoadPrefab("Battle/DiceAttackEffects/CreatureBattle/EGO_Freischutz_6thBullet")
                .GetComponent<FarAreaEffect_EGO_Freischutz_6thBullet>()
                .Init(owner, Array.Empty<object>());

            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList(Faction.Enemy);
            if (aliveList.Count > 0)
            {
                Util.LoadPrefab("Battle/DiceAttackEffects/CreatureBattle/EGO_Freischutz_6thBullet")
                    .GetComponent<FarAreaEffect_EGO_Freischutz_6thBullet>()
                    .Init(aliveList[0], Array.Empty<object>());
            }

            ReplaceSkin();
            AuraHelper.AttachParticle(owner, _spawnedAuraObjects, "Prefabs/Battle/SpecialEffect/RedMistRelease_ActivateParticle", 1.5f);
            SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Kali_Change", false, 1f, null);

            int emotionTotalCoinNumber = Singleton<StageController>.Instance.GetCurrentStageFloorModel().team.emotionTotalCoinNumber;
            Singleton<StageController>.Instance.GetCurrentWaveModel().team.emotionTotalBonus = emotionTotalCoinNumber + 1;
        }

        private void InitializeBuff()
        {
            int hp = 0;
            int breakLife = 0;
            foreach (BattleUnitModel enemy in enemies)
            {
                hp += enemy.MaxHp;
                breakLife += enemy.breakDetail.GetDefaultBreakGauge();
            }
            if (owner.bufListDetail.GetActivatedBufList().Find(x => x is HDBuffInitialBoost) == null)
            {
                owner.bufListDetail.AddBuf(new HDBuffInitialBoost
                {
                    hpAdder = hp,
                    breakGageAdder = breakLife / 2
                });
                owner.RecoverHP(owner.MaxHp);
                owner.breakDetail.breakGauge = owner.breakDetail.GetDefaultBreakGauge();
            }
        }

        private void InitializeAuras()
        {
            //AuraHelper.AttachBodyAura(owner, _spawnedAuraObjects, "Battle/DiceAttackEffects/New/FX/PC/Librarian/FX_PC_Librarian_Light", 3.5f);
            AuraHelper.AttachGiftAura(owner, _spawnedAuraObjects, "BloodNight2", 1.5f);
            
        }

        private void ReplaceSkin()
        {
            AuraHelper.DestroyAuras(_spawnedAuraObjects);
            owner.view.ChangeSkin(new LorName(packageId, "Sazantos"));
            owner.view.ChangeHeight(450);
            Helpers.ChangeSkinSoundFromSkinName(owner.view.charAppearance, "TheHead");
            InitializeAuras();
            AuraHelper.AttachGiftAura(owner, _spawnedAuraObjects, "BlueReverberation2", 1.5f);
            AuraHelper.AttachGiftAura(owner, _spawnedAuraObjects, "GearGod2", 1f);
            //AuraHelper.AttachGiftAura(owner, _spawnedAuraObjects, "Promise2", 1.5f);
        }

        private void ReduceCostDeck()
        {
            foreach (BattleDiceCardModel card in owner.allyCardDetail.GetAllDeck())
            {
                card.SetCostToZero();
                card.exhaust = false;
            }
        }

        public override void OnUnitCreated()
        {
            ResetFlags();
            SetResistances();
            
        }

        public override void OnDie()
        {
            base.OnDie();
            AuraHelper.DestroyAuras(_spawnedAuraObjects);
        }

        public override void OnRoundEndTheLast()
        {
            TrySecondPhase();
            
        }

        public override void OnDrawCard()
        {
            base.OnDrawCard();
            ReduceCostDeck();
            //int stack = Math.Min((int)Math.Ceiling((double)(Singleton<StageController>.Instance.RoundTurn / 2)), 10);
            //owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, stack, null);
        }

        public override void OnRollSpeedDice()
        {
            EveryThreeRounds();
        }

        private void AddEgoCards()
        {
            for (int i = 1; i <= modCardnum; i++)
            {
                owner.personalEgoDetail.AddCard(new LorId(packageId, i));
            }
        }

        public override void OnWaveStart()
        {
            if (!inSecondPhase)
            {
                InitializeBuff();
                InitializeAuras();
                forcedDeath = false;
                Helpers.ChangeSkinSoundFromSkinName(owner.view.charAppearance, "BlackSilence3");
                owner.Book.SetMaxPlayPoint(4);
            }
        }

        private void EveryThreeRounds()
        {
            if (_elapsedRound % 3 != 0) return;
            foreach (SpeedDice item in owner.speedDiceResult)
            {
                item.value = 999;
            }
            
        }

        public override void OnRoundStart()
        {
            _elapsedRound++;
            if (_elapsedRound == 1)
            {
                //MapManagerHelper.ChangeToInvitationMapWithDialogue("Yan", battleDialogues2);
            }
            
            if (!inSecondPhase && emotionLevel >= 5)
            {
                AudioHelper.ChangeBGM("Persona5/Awakening", 0.85f, 1f);
            }

            if ((_elapsedRound) % 3 == 0)
            {
                _threerounds++;
                if (owner.IsBreakLifeZero())
                {
                    owner.RecoverBreakLife(1, false);
                    owner.ResetBreakGauge();
                    owner.breakDetail.nextTurnBreak = false;
                    owner.turnState = BattleUnitTurnState.WAIT_CARD;
                    owner.breakDetail.RecoverBreak(owner.breakDetail.GetDefaultBreakGauge());
                    owner.moveDetail.ReturnToFormationByBlink(true);
                    owner.view.charAppearance.ChangeMotion(ActionDetail.Standing);
                    
                }
                foreach (BattleUnitModel enemy in enemies)
                {
                    enemy.bufListDetail.AddBuf(new BattleUnitBuf_DarkFlame());
                    //enemy.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Burn, 3, null);
                }
                if (_threerounds > 0 && !inSecondPhase)
                {
                    owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, _threerounds, null);
                    owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Protection, _threerounds, null);
                }
                EarthQuake();
            }
            
            ShowDialog();
            owner.ShowPassiveTypo(this);
            if (!inSecondPhase)
            {
                TrySecondPhase();
            }
           
            if (owner.bufListDetail.GetActivatedBuf(KeywordBuf.KeterFinal_DoubleEmotion) == null)
            {
                //owner.bufListDetail.AddBuf(new BattleUnitBuf_KeterFinal_DoubleEmotion());
            }
            owner.cardSlotDetail.RecoverPlayPoint(owner.MaxPlayPoint);
            AddEgoCards();
            DrawPages();

            if (inSecondPhase)
            {
                foreach (BattleUnitModel unit in BattleObjectManager.instance.GetAliveList(false))
                {
                    unit.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.NullifyPower, 1, null);
                }
                int recovery = owner.MaxHp * perHPRecover / 100;
                owner.RecoverHP(recovery);
                ReduceEnemyHP(0.05f);
            }
        }

        

        public override bool BeforeTakeDamage(BattleUnitModel attacker, int dmg)
        {
            _dmgReduction = 0;
            if (canTriggerSecondPhase && owner.hp - dmg <= SecondPhaseTrigger)
            {
                _dmgReduction = (int)(SecondPhaseTrigger - (owner.hp - dmg));
                secondPhaseReady = true;
            }
            return base.BeforeTakeDamage(attacker, dmg);
        }

        public override int GetDamageReductionAll()
        {
            if (canTriggerSecondPhase && owner.hp <= SecondPhaseTrigger)
            {
                secondPhaseReady = true;
                return 9999;
            }
            return _dmgReduction;
        }

        public override bool DontChangeResistByBreak() => true;

        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            if (inSecondPhase)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus { max = _threerounds * 2 });
            }
            behavior.ApplyDiceStatBonus(new DiceStatBonus { dmg = Math.Min(_elapsedRound, 10) });
        }

        public override int SpeedDiceNumAdder()
        {
            return inSecondPhase ? speedDice + 2 + speedDiceEmotion : speedDice + speedDiceEmotion;
        }

        public class BattleUnitBuf_DarkFlame : BattleUnitBuf
        {
            public override bool Hide => true;

            public override AtkResist GetResistHP(AtkResist origin, BehaviourDetail detail)
            {
                AtkResist baseResist = base.GetResistHP(origin, detail);
                if (baseResist == AtkResist.Endure) return AtkResist.Vulnerable;
                if (baseResist == AtkResist.Resist) return AtkResist.Weak;
                return baseResist;
            }

            public override AtkResist GetResistBP(AtkResist origin, BehaviourDetail detail)
            {
                AtkResist baseResist = base.GetResistBP(origin, detail);
                if (baseResist == AtkResist.Endure) return AtkResist.Vulnerable;
                if (baseResist == AtkResist.Resist) return AtkResist.Weak;
                return baseResist;
            }

            public override void OnRoundEnd() => Destroy();
        }

        public class HDBuffInitialBoost : BattleUnitBuf
        {
            public override bool Hide => true;
            public int hpAdder;
            public int breakGageAdder;

            private readonly List<KeywordBuf> debuffImmune = new List<KeywordBuf>()
            {
            KeywordBuf.Stun, KeywordBuf.Seal, KeywordBuf.Decay, KeywordBuf.Disarm, KeywordBuf.Binding, KeywordBuf.Weak, KeywordBuf.Seal
            };

            private readonly List<Type> immuneBuff = new List<Type>()
            {
                typeof(BattleUnitBuf_Butterfly_Seal),
                typeof(BattleUnitBuf_sealTemp),
                typeof(BattleUnitBuf_KingOfGreed_DestroySpeedDice),
                typeof(BattleUnitBuf_Oswald_Daze),
                typeof(BattleUnitBuf_Oswald_Distract),
                typeof(BattleUnitBuf_Pluto_Contracted_1),
                typeof(BattleUnitBuf_Pluto_Contracted_2),
                typeof(BattleUnitBuf_Pluto_Contracted_3),
                typeof(BattleUnitBuf_Pluto_Contracted_4),
                typeof(BattleUnitBuf_Pluto_Contracted_5),
                typeof(BattleUnitBuf_HDDisadvantage)
            };

            public override bool IsImmune(BattleUnitBuf buf)
            {
                return immuneBuff.Contains(buf.GetType());
            }

            public override bool IsImmune(KeywordBuf buf) => debuffImmune.Contains(buf);

            public HDBuffInitialBoost() => stack = 0;

            public override StatBonus GetStatBonus()
            {
                return new StatBonus
                {
                    hpAdder = hpAdder,
                    breakGageAdder = breakGageAdder
                };
            }
        }

        public class BattleUnitBuf_HDBuff : BattleUnitBuf
        {
            public override bool Hide => true;
            public int hpAdder;
            public int breakGageAdder;

            public override StatBonus GetStatBonus()
            {
                return new StatBonus
                {
                    hpAdder = hpAdder,
                    breakGageAdder = breakGageAdder
                };
            }

            public override int GetDamageReductionRate() => 0;
            public override int GetBreakDamageReductionRate() => 0;

            public override void BeforeGiveDamage(BattleDiceBehavior behavior)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    dmgRate = 5,
                    breakRate = 5
                });
            }
        }
    }



}
