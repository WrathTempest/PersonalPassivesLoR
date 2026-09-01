using DestinyofImmortal.Utils;
using HarmonyLib;
using LOR_DiceSystem;
using System.Collections.Generic;
using UnityEngine;

namespace PersonalPassivesLoR.Patches
{
    [HarmonyPatch(typeof(CharacterSound), nameof(CharacterSound.PlaySound))]
    public static class CharacterSound_PlaySound_Patch
    {
        private static readonly string[] FallbackSoundNames = new string[] { "Sword_Stab", "Sword_Vert", "Sword_Hori" };

        [HarmonyPrefix]
        public static bool Prefix(CharacterSound __instance, MotionDetail motion, bool win)
        {
            Dictionary<MotionDetail, CharacterSound.Sound> dic = Helpers.GetPrivateField<Dictionary<MotionDetail, CharacterSound.Sound>>(__instance, "_dic");
            List<CharacterSound.Sound> motionSounds = Helpers.GetPrivateField<List<CharacterSound.Sound>>(__instance, "_motionSounds");

            CharacterSound.Sound sound = null;

            if (dic != null && dic.ContainsKey(motion))
            {
                sound = dic[motion];
            }
            else if (motionSounds != null)
            {
                sound = motionSounds.Find(x => x.motion == motion);
            }

            // Determine if a valid AudioClip exists for this win/lose state
            AudioClip targetClip = win ? sound?.winSound : sound?.loseSound;

            if (targetClip != null)
            {
                if (win)
                {
                    Debug.Log($"[Sound Log] Motion: {motion} | Playing Win Sound: {targetClip.name}");
                }
                return true; // Let original PlaySound run normally
            }

            // Suitable sound not found -> Play random fallback sound and skip original method
            PlayFallbackSound(__instance);
            return false;
        }

        private static void PlayFallbackSound(CharacterSound instance)
        {
            string chosenSoundName = FallbackSoundNames[Random.Range(0, FallbackSoundNames.Length)];
            AudioClip clip = GetAudioClip(chosenSoundName);

            if (clip == null)
            {
                Debug.LogWarning($"[Sound Log] Could not load fallback sound clip: {chosenSoundName}");
                return;
            }

            BattleEffectSound soundPrefab = Helpers.GetPrivateField<BattleEffectSound>(instance, "_soundPrefab");
            if (soundPrefab == null || SingletonBehavior<BattleSoundManager>.Instance == null)
            {
                return;
            }

            BattleEffectSound battleEffectSound = Object.Instantiate<BattleEffectSound>(soundPrefab, SingletonBehavior<BattleSoundManager>.Instance.transform);
            battleEffectSound.Init(clip, SingletonBehavior<BattleSoundManager>.Instance.VolumeFX, false);

            Debug.Log($"[Sound Log] No suitable sound found for motion. Played fallback sound: {chosenSoundName}");
        }

        private static AudioClip GetAudioClip(string soundName)
        {
            // Try getting clip from cached static resources first
            var resources = Traverse.Create(typeof(CharacterSound)).Field("_motionSoundResources").GetValue<Dictionary<string, AudioClip>>();
            if (resources != null && resources.TryGetValue(soundName, out AudioClip clip) && clip != null)
            {
                return clip;
            }

            // Fallback to loading directly from Resources
            return Resources.Load<AudioClip>($"Sounds/MotionSound/{soundName}");
        }
    }
}

