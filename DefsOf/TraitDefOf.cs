using System;
using RimWorld;

namespace AndroidTiers
{
        [DefOf]
        public static class TraitDefOf
        {
            static TraitDefOf()
            {
                DefOfHelper.EnsureInitializedInCtor(typeof(TraitDefOf));
            }

            public static TraitDef FeelingsTowardHumanity;

            public static TraitDef Transhumanist;
    }
}
