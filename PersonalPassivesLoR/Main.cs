using HarmonyLib;
using LOR_DiceSystem;
using Sound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PersonalPassivesLoR.MiscHelpers;

namespace PersonalPassivesLoR
{
   public class PersonalPassivesLoRInitializer : ModInitializer
    {
        public static string packageId = "PersonalKeyPage";
        private static readonly Harmony Harmony = new Harmony(packageId);

        public override void OnInitializeMod()
        {
            Harmony.PatchAll();
            _ = AudioHelper.LoadAllBGMAsync();
        }
    }
}
