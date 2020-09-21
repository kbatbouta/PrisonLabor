using Verse;
using Verse.AI;
using Verse.AI.Group;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection.Emit;

namespace AndroidTiers
{
    internal class Pawn_NeedsTracker_Patch
    {
        /*
         * PostFix évitant d'attribuer de need comfort et outdoor aux T1 et T2 et l'hygiene a l'ensemble des robots
         */
        [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
        public class ShouldHaveNeed_Patch
        {
            [HarmonyPostfix]
            public static void Listener(NeedDef nd, ref bool __result, Pawn ___pawn)
            {
                try {
                    bool isAndroid = Utils.ExceptionAndroidList.Contains(___pawn.def.defName);

                    //SI pas un androide on jerte
                    if(!isAndroid)
                        return;

                    bool advancedAndroids = Utils.ExceptionAndroidListAdvanced.Contains(___pawn.def.defName);

                    if((Utils.ExceptionAndroidListBasic.Contains(___pawn.def.defName)
                        && (nd.defName == "Outdoors"))
                        || (___pawn.def.defName == "Android1Tier" && nd.defName == "Beauty")
                        || (isAndroid && (nd.defName == "Hygiene" || nd.defName == "Bladder" || nd.defName == "DBHThirst"))
                        || (nd.defName == "Comfort" && (!advancedAndroids || (advancedAndroids && Settings.removeComfortNeedForT3T4)))) {
                        __result = false;
                    }

                    //Activation besoin de bouffe pour les M7 surrogates (SM7)
                    if(___pawn.def.defName == "M7Mech" && ___pawn.IsSurrogateAndroid() && nd.defName == "Food")
                        __result = true;
                }
                catch(Exception e) {
                    Log.Message("[ATPP] Pawn_StoryTracker.ShouldHaveNeed : " + e.Message + " - " + e.StackTrace);
                }
            }
        }


        [HarmonyPatch(typeof(Pawn_NeedsTracker), "NeedsTrackerTick")]
        public class NeedsTrackerTick_Patch
        {
            public static bool Skip(Pawn_NeedsTracker instance)
            {
                if(instance.pawn.ATCompSurrogateOwner?.skyCloudHost != null)
                    return false;
                if(instance.pawn.IsBlankAndroid())
                    return false;
                return true;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                var l1 = new Label();
                yield return new CodeInstruction(OpCodes.Ldarg_0) { labels = codes[0].labels };
                yield return new CodeInstruction(OpCodes.Call, typeof(NeedsTrackerTick_Patch).GetMethod("Skip"));
                yield return new CodeInstruction(OpCodes.Brfalse_S, l1);

                codes.Last().labels.Add(l1);
                for(int i = 0; i < codes.Count; i++)
                    yield return codes[i];
            }
        }
    }
}