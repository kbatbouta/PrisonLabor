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
    internal class PawnInventoryGenerator_Patch

    {
        [HarmonyPatch(typeof(PawnInventoryGenerator), "GiveRandomFood")]
        public class GiveRandomFood_PatchPostfix
        {
            [HarmonyPostfix]
            public static void Listener(Pawn p)
            {
                if(Utils.PawnInventoryGeneratorCanHackInvNutritionValue) {
                    //restauration du coef de nutrition
                    if(Utils.ExceptionAndroidList.Contains(p.def.defName)) {
                        p.kindDef.invNutrition = Utils.PawnInventoryGeneratorLastInvNutritionValue;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PawnInventoryGenerator), "GiveRandomFood")]
        public class GiveRandomFood_PatchPrefix
        {
            [HarmonyPrefix]
            public static bool Listener(Pawn p)
            {
                if(Utils.PawnInventoryGeneratorCanHackInvNutritionValue) {
                    //mise en place coef de nutrition fake
                    if(Utils.ExceptionAndroidList.Contains(p.def.defName)) {
                        Utils.PawnInventoryGeneratorLastInvNutritionValue = p.kindDef.invNutrition;
                        p.kindDef.invNutrition = 1.0f;
                    }
                }
                return true;
            }
        }
    }
}