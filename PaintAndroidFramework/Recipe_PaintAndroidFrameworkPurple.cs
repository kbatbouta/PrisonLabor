using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AndroidTiers
{
    public class Recipe_PaintAndroidFrameworkPurple : Recipe_SurgeryAndroids
    {

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            CompAndroidState cas = pawn.ATCompState;

            if (cas == null)
                return;

            cas.customColor = (int)AndroidPaintColor.Purple;
            this.applyFrameworkColor(pawn);
        }

    }
}
