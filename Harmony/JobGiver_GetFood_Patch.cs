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
    internal class JobGiver_GetFood_Patch
    {
        [HarmonyPostfix]
        public static void Listener(Pawn pawn, ref Job __result)
        {
            if(!pawn.IsAndroidTier()) return;
            if(Utils.ExceptionAndroidCanReloadWithPowerList.Contains(pawn.def.defName)) {
                CompAndroidState ca = pawn.GetComp<CompAndroidState>();
                if(ca == null || !pawn.Spawned || !ca.UseBattery || pawn.Drafted)
                    return;

                if(Utils.POWERPP_LOADED && ca.connectedLWPNActive && ca.connectedLWPN != null) {
                    __result = null;
                    return;
                }

                Building rsb = Utils.GCATPP.getFreeReloadStation(pawn.Map, pawn);
                if(rsb == null) {
                    __result = null;
                    return;
                }

                CompReloadStation rs = rsb.TryGetComp<CompReloadStation>();

                if(rs == null) {
                    __result = null;
                    return;
                }

                __result = new Job(
                    DefDatabase<JobDef>.GetNamed("ATPP_GoReloadBattery"),
                    new LocalTargetInfo(rs.getFreeReloadPlacePos(pawn)),
                    new LocalTargetInfo(rsb));
            }
        }
    }
}