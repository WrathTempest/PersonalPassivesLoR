using Battle.CreatureEffect;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PersonalPassivesLoR.Passives
{
    public static class AuraHelper
    {
        public static GameObject AttachGiftAura(BattleUnitModel owner, Dictionary<string, GameObject> tracker, string path, Vector3 scale)
        {
            string giftPath = "Prefabs/Gifts/Gifts_NeedRename/Gift_" + path;
            if (tracker != null && tracker.TryGetValue(giftPath, out GameObject existing) && existing != null)
            {
                return existing;
            }

            CharacterAppearance charApp = owner?.view?.charAppearance;
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

            if (tracker != null) tracker[giftPath] = instance.gameObject;
            return instance.gameObject;
        }

        public static GameObject AttachGiftAura(BattleUnitModel owner, Dictionary<string, GameObject> tracker, string giftPath, float scale = 1.0f)
        {
            return AttachGiftAura(owner, tracker, giftPath, Vector3.one * scale);
        }

        public static GameObject AttachBodyAura(BattleUnitModel owner, Dictionary<string, GameObject> tracker, string prefabPath, Vector3 scale)
        {
            if (tracker != null && tracker.TryGetValue(prefabPath, out GameObject existing) && existing != null)
            {
                return existing;
            }

            CharacterAppearance charApp = owner?.view?.charAppearance;
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
                    bodyAura.SetAppearance(owner);
                }

                if (tracker != null) tracker[prefabPath] = auraObj;
            }
            return auraObj;
        }

        public static GameObject AttachBodyAura(BattleUnitModel owner, Dictionary<string, GameObject> tracker, string prefabPath, float scale = 1.0f)
        {
            return AttachBodyAura(owner, tracker, prefabPath, Vector3.one * scale);
        }

        public static GameObject AttachFXAura(BattleUnitModel owner, Dictionary<string, GameObject> tracker, string fxPath, float scale = 1.0f)
        {
            if (tracker != null && tracker.TryGetValue(fxPath, out GameObject existing) && existing != null)
            {
                return existing;
            }

            if (owner?.view == null) return null;

            CreatureEffect effect = SingletonBehavior<DiceEffectManager>.Instance.CreateNewFXCreatureEffect(
                fxPath, scale, owner.view, owner.view, -1f);

            if (effect != null && effect.gameObject != null)
            {
                if (tracker != null) tracker[fxPath] = effect.gameObject;
                return effect.gameObject;
            }
            return null;
        }

        public static GameObject AttachParticle(BattleUnitModel owner, Dictionary<string, GameObject> tracker, string particlePath, float scale = 1.5f)
        {
            if (tracker != null && tracker.TryGetValue(particlePath, out GameObject existing) && existing != null)
            {
                return existing;
            }

            CharacterAppearance charApp = owner?.view?.charAppearance;
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

                    if (tracker != null) tracker[particlePath] = particleObj;
                    return particleObj;
                }
            }
            return null;
        }

        public static void DestroyAuras(Dictionary<string, GameObject> tracker)
        {
            if (tracker == null) return;
            foreach (KeyValuePair<string, GameObject> kvp in tracker)
            {
                if (kvp.Value != null)
                {
                    UnityEngine.Object.Destroy(kvp.Value);
                }
            }
            tracker.Clear();
        }
    }
}

