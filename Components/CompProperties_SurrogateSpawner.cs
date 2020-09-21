using System;
using Verse;

namespace AndroidTiers
{
    public class CompProperties_SurrogateSpawner : CompProperties
    {
        public CompProperties_SurrogateSpawner()
        {
            this.compClass = typeof(CompSurrogateSpawner);
        }

        public PawnKindDef Pawnkind;
        public int gender;
    }
}
