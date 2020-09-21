using System;
using Verse;

namespace AndroidTiers
{
    public class HediffCompProperties_RegenWoundsAdv : HediffCompProperties
    {
        public HediffCompProperties_RegenWoundsAdv()
        {
            this.compClass = typeof(HediffComp_RegenWoundsAdv);
        }

        public float HealingAmount;

        public int Delay;
    }
}