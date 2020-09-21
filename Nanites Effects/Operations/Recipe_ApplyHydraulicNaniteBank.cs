﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AndroidTiers
{
    public class Recipe_ApplyHydraulicNaniteBank : Recipe_SurgeryAndroids
    {

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            CompUseEffect_HealHydraulicSystem.Apply(pawn);
        }

    }
}
