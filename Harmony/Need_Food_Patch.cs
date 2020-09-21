using Verse;
using Verse.AI;
using Verse.AI.Group;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System;

namespace AndroidTiers
{
    internal class Need_Food_Patch

    {

        //Eviter que l'android se décharge pendant qu'il se recharge
        [HarmonyPatch(typeof(Need_Food), "get_FoodFallPerTick")]
        public class FoodFallPerTick_Patch
        {
            [HarmonyPostfix]
            public static void Listener(Pawn ___pawn, ref float __result)
            {
                if(!___pawn.IsAndroidTier())
                    return;

                if(Utils.androidIsValidPodForCharging(___pawn) || Utils.androidReloadingAtChargingStation(___pawn)) {
                    __result = 0f;
                }
            }
        }
    }
}