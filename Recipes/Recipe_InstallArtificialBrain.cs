using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AndroidTiers
{
    public class Recipe_InstallArtificialBrain : Recipe_InstallImplant
    {

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            CompAndroidState cas = pawn.ATCompState;

            if (cas == null)
                return;

            //Le cas echeant on deconnecte le controlleur s'il y en a un 
            if(cas.surrogateController != null)
            {
                CompSurrogateOwner cso = cas.surrogateController.ATCompSurrogateOwner;
                if(cso != null)
                {
                    cso.disconnectControlledSurrogate(pawn);
                }
            }

            //On définis le fait qu'il ne sagit plus d'un surrogate mais d'un blank neural net andorid
            cas.isSurrogate = false;
            Hediff he = pawn.health.hediffSet.GetFirstHediffOfDef(Utils.hediffNoHost);
            if (he != null)
                pawn.health.RemoveHediff(he);

            cas.isBlankAndroid = true;
            pawn.health.AddHediff(Utils.hediffBlankAndroid);
        }

    }
}
