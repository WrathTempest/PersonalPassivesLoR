using HarmonyLib;
using UnityEngine.UI;
using UI;
using DestinyofImmortal.Utils;

namespace PersonalPassivesLoR.Patches
{
    [HarmonyPatch]
    internal class UISizeSliderBar_Patch
    {
        [HarmonyPatch(typeof(UISizeSliderBar), nameof(UISizeSliderBar.InitBarValue))]
        [HarmonyPostfix]
        public static void SetValues(UISizeSliderBar __instance)
        {
            Slider slider = Helpers.GetPrivateField<Slider>(__instance, "slider");
            slider.maxValue = 999f;
            Helpers.SetPrivateField<Slider>(__instance, "slider", slider);
            Helpers.SetPrivateField<float>(__instance, "maxValue", 999f);
        }
       
    }
}