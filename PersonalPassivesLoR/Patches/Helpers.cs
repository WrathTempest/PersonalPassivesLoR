using HarmonyLib;
using LOR_DiceSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace DestinyofImmortal.Utils
{
    /// <summary>
    /// Static utilities class for common functions and properties to be used within your mod code
    /// </summary>
    internal static class Helpers
    {
        public static void ChangeSkinSoundFromSkinName(
    CharacterAppearance owner,
    string skinName)
        {
            string path;

            GameObject gameObject =
                Singleton<AssetBundleManagerRemake>.Instance
                    .LoadCharacterPrefab(skinName, "", out path);

            if (gameObject == null)
            {
                UnityEngine.Debug.LogError($"Skin not found: {skinName}");
                return;
            }

            CharacterAppearance characterAppearance =
                gameObject.GetComponent<CharacterAppearance>();

            characterAppearance.Initialize(path);

            CharacterSound sourceSound = characterAppearance.soundInfo;
            CharacterSound targetSound = owner.soundInfo;

            List<CharacterSound.Sound> sourceSounds =
                Helpers.GetPrivateField<List<CharacterSound.Sound>>(
                    sourceSound,
                    "_motionSounds"
                );

            List<CharacterSound.Sound> targetSounds =
                Helpers.GetPrivateField<List<CharacterSound.Sound>>(
                    targetSound,
                    "_motionSounds"
                );

            Dictionary<LOR_DiceSystem.MotionDetail, CharacterSound.Sound> targetDictionary =
                Helpers.GetPrivateField<
                    Dictionary<LOR_DiceSystem.MotionDetail, CharacterSound.Sound>
                >(targetSound, "_dic");

            if (sourceSounds == null || targetSounds == null)
            {
                UnityEngine.Debug.LogError("Motion sound list was null.");
                return;
            }

            targetSounds.Clear();
            targetSounds.AddRange(sourceSounds);

            targetDictionary?.Clear();

            UnityEngine.Debug.Log(
                $"Changed motion sounds to skin {skinName}. " +
                $"Loaded {sourceSounds.Count} sounds."
            );
            foreach (var sound in targetSounds)
            {
                if (sound.motion == MotionDetail.J)
                {
                    //sound.winSound = GetMotionSound(sourceSound, MotionDetail.S5);
                }
                if (sound.motion == MotionDetail.H)
                {
                    //sound.winSound = GetMotionSound(sourceSound, MotionDetail.S);
                }
                if (sound.winSound != null)
                {
                    UnityEngine.Debug.Log(
                        $"Win sound: {sound.winSound.name} | Motion: {sound.motion}"
                    );
                }
            }
        }

        public static AudioClip GetMotionSound(
        CharacterSound characterSound,
        MotionDetail motion,
        bool win = true)
        {
            if (characterSound == null || motion == null)
                return null;

            List<CharacterSound.Sound> sounds =
                Helpers.GetPrivateField<List<CharacterSound.Sound>>(
                    characterSound,
                    "_motionSounds"
                );

            if (sounds == null)
                return null;

            CharacterSound.Sound sound =
                sounds.Find(x => x.motion == motion);

            if (sound == null)
                return null;

            return win ? sound.winSound : sound.loseSound;
        }
        public static T GetPassive<T>(BattleUnitPassiveDetail instance) where T : PassiveAbilityBase
        {
            List<PassiveAbilityBase> passiveList = GetPrivateField<List<PassiveAbilityBase>>(instance, "_passiveList");
            return passiveList?.Find(x => x is T) as T;
        }
        public static void DisplayCustomAbnormalityDlg(
        BattleDialogUI ui,
        string dialogue,
        Color? textColor = null,
        Color? glowColor = null,
        float duration = 10f)
        {
            if (ui == null) return;

            // 1. Fetch private fields via reflection
            TextMeshProUGUI txtDlg = GetPrivateField<TextMeshProUGUI>(ui, "_txtAbnormalityDlg");
            Canvas canvas = GetPrivateField<Canvas>(ui, "_canvas");
            Coroutine currentRoutine = GetPrivateField<Coroutine>(ui, "_routine");

            if (txtDlg == null || canvas == null) return;

            // 2. Fall back to BattleManagerUI's Negative state colors if not provided
            Color finalTextColor = textColor ?? SingletonBehavior<BattleManagerUI>.Instance.negativeTextColor;
            Color finalGlowColor = glowColor ?? SingletonBehavior<BattleManagerUI>.Instance.negativeCoinColor;

            // 3. Stop running routine if active
            if (currentRoutine != null)
            {
                ui.StopCoroutine(currentRoutine);
                SetPrivateField<Coroutine>(ui, "_routine", null);
                canvas.enabled = false;
            }

            // 4. Update canvas & CanvasGroup state
            CanvasGroup cg = ui.GetComponent<CanvasGroup>();
            canvas.enabled = true;

            if (cg != null)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

     

            
            UnityEngine.Debug.Log($"Setting color to {finalTextColor} glow to {finalGlowColor}, current color before/after: ...");
            txtDlg.text = dialogue;
            txtDlg.color = finalTextColor;
            UnityEngine.Debug.Log($"color after: {txtDlg.color}");
            Material mat = txtDlg.fontMaterial;
            mat.EnableKeyword("GLOW_ON");
            mat.SetColor("_GlowColor", finalGlowColor);
            txtDlg.fontMaterial = mat;
            txtDlg.SetMaterialDirty();
            txtDlg.ForceMeshUpdate();
            UnityEngine.Debug.Log($"GLOW_ON enabled: {txtDlg.fontMaterial.IsKeywordEnabled("GLOW_ON")}");
            UnityEngine.Debug.Log($"glow after: {txtDlg.fontMaterial.GetColor("_GlowColor")}");
            // 8. Start new coroutine and store reference
            AbnormalityDlgEffect effect = txtDlg.GetComponent<AbnormalityDlgEffect>();
            if (effect != null)
            {
                effect.Init();
            }
            Coroutine newRoutine = ui.StartCoroutine(CustomAbnormalityDlgRoutine(ui, canvas, cg, duration));
            SetPrivateField(ui, "_routine", newRoutine);
        }

        private static IEnumerator CustomAbnormalityDlgRoutine(
            BattleDialogUI ui,
            Canvas canvas,
            CanvasGroup cg,
            float duration)
        {
            float elapsed = 0f;

            // Fade In (0.5s)
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * 2f;
                if (cg != null) cg.alpha = elapsed;
                yield return null;
            }

            // Hold display
            yield return YieldCache.WaitForSeconds(duration);

            // Fade Out (0.5s)
            elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * 2f;
                if (cg != null) cg.alpha = 1f - elapsed;
                yield return null;
            }

            // Cleanup state
            canvas.enabled = false;
            if (cg != null)
            {
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            SetPrivateField<Coroutine>(ui, "_routine", null);
            yield break;
        }

        public static T GetPrivateField<T>(object instance, string fieldName)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));

            // Use AccessTools to get the field
            return AccessTools.FieldRefAccess<T>(instance.GetType(), fieldName)(instance);
        }
        public static void SetPrivateField<T>(object instance, string fieldName, T newValue)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));

            Type type = instance.GetType();
            FieldInfo field = null;

            while (type != null)
            {
                field = type.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (field != null)
                    break;

                type = type.BaseType;
            }

            if (field == null)
                throw new MissingFieldException(instance.GetType().FullName, fieldName);

            field.SetValue(instance, newValue);
        }

        public static T GetPrivateProperty<T>(object instance, string propertyName)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();

            // Try property first
            var prop = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (prop != null)
            {
                return (T)prop.GetValue(instance, null);
            }

            // Fallback: try getter method directly (get_PropertyName)
            var getter = type.GetMethod(
                "get_" + propertyName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (getter != null)
            {
                return (T)getter.Invoke(instance, null);
            }

            throw new MissingMemberException(type.FullName, propertyName);
        }

        public static void SetPrivateProperty<T>(object instance, string propertyName, T value)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();

            var prop = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (prop != null)
            {
                prop.SetValue(instance, value, null);
                return;
            }

            var setter = type.GetMethod(
                "set_" + propertyName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (setter != null)
            {
                setter.Invoke(instance, new object[] { value });
                return;
            }

            throw new MissingMemberException(type.FullName, propertyName);
        }

        public static string GetRealCaller(int skipFrames = 1)
        {
            var stackTrace = new StackTrace(skipFrames, true);
            foreach (var frame in stackTrace.GetFrames())
            {
                MethodBase method = frame.GetMethod();
                if (method == null) continue;

                // Skip Harmony-generated dynamic methods
                if (method.Name.Contains("DMD<")) continue;

                // Skip helper class itself
                if (method.DeclaringType == typeof(Helpers)) continue;

                return $"{method.DeclaringType.FullName}.{method.Name}";
            }

            return "UnknownCaller";
        }

        /// <summary>
        /// Logs a traced call for debugging.
        /// </summary>
        public static void LogCaller(string message = "", int skipFrames = 1)
        {
            string caller = GetRealCaller(skipFrames + 1); // +1 for hero.battleDataBehaviour.battleData method
            //Main.Log.LogInfo($"{message} Called by: {caller}");
        }

        public static object Call(
    object instance,
    string methodName,
    Type[] paramTypes,
    params object[] args)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            Type type = instance.GetType();

            MethodInfo method = AccessTools.Method(type, methodName, paramTypes);

            if (method == null)
                throw new MissingMethodException(type.FullName, methodName);

            return method.Invoke(instance, args);
        }

        public static object Call(
        object instance,
        string methodName,
        params object[] args)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            Type type = instance.GetType();

            MethodInfo method = AccessTools.Method(type, methodName);

            if (method == null)
                throw new MissingMethodException(type.FullName, methodName);

            return method.Invoke(instance, args);
        }

        public static object CallStatic(
            Type type,
            string methodName,
            params object[] args)
        {
            MethodInfo method = AccessTools.Method(type, methodName);

            if (method == null)
                throw new MissingMethodException(type.FullName, methodName);

            return method.Invoke(null, args);
        }
    }
}
