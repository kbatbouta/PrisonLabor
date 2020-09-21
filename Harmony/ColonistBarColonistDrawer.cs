using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AndroidTiers
{
    //[HarmonyPatch(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.DrawColonist))]
    //public class ColonistBarColonistDrawer_DrawColonist
    //{
    //    public static bool Prefix(Pawn colonist)
    //    {            
    //        if(!colonist.IsAndroid())
    //            return true;
    //        if(colonist.IsBlankAndroid())
    //            return false;
    //        return true;
    //    }
    //}

    //[HarmonyPatch(typeof(RottableUtility), nameof(RottableUtility.IsDessicated))]
    //public class RottableUtility_IsDessicated
    //{
    //    public static bool Prefix(Thing t, ref bool __result)
    //    {
    //        if(t?.ATCompState?.isBlankAndroid ?? false) {
    //            __result = true; return false;
    //        }
    //        return true;
    //    }
    //}
}
