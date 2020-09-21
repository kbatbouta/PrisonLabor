using Verse;
using Verse.AI;
using Verse.AI.Group;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using System.Reflection.Emit;

namespace AndroidTiers
{
    internal class Thing_Patch

    {
        /*
         * PostFix évitant d'attribuer de need comfort et outdoor aux T1 et T2 et l'hygiene a l'ensemble des robots
         */
        [HarmonyPatch(typeof(Thing), "Ingested")]
        public class Ingested_Patch
        {
            [HarmonyPostfix]
            public static void Listener(Pawn ingester, float nutritionWanted, ref float __result)
            {
                try {
                    if((Utils.ExceptionAndroidList.Contains(ingester.def.defName)))
                        if(Settings.androidNeedToEatMore)
                            __result *= 0.5f;
                }
                catch(Exception e) {
                    Log.Message("[ATPP] Thing.Ingested : " + e.Message + " - " + e.StackTrace);
                }
            }
        }

        /*
         * Pawn inside SkyCloud are not interpreted and suspended 
         */
        [HarmonyPatch(typeof(Thing), "get_Suspended")]
        public class Suspended_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                yield return new CodeInstruction(OpCodes.Ldarg_0) { labels = codes[0].labels };
                yield return new CodeInstruction(OpCodes.Call, typeof(Suspended_Patch).GetMethod("Suspended"));
                yield return new CodeInstruction(OpCodes.Ret);
            }

            public static bool Suspended(Thing thing)
            {
                if(thing.ATCompSurrogateOwner != null && thing.ATCompSurrogateOwner.skyCloudHost != null)
                    return true;

                if(thing.Spawned)
                    return false;

                if(thing.ParentHolder != null)
                    return ThingOwnerUtility.ContentsSuspended(thing.ParentHolder);

                return false;
            }
        }
    }
}