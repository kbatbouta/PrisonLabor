using System;
using Verse;
using RimWorld;

namespace AndroidTiers
{
    public class TimeinbedformedicalreasonsAndroid : RecordWorker
    {
        public override bool ShouldMeasureTimeNow(Pawn pawn)
        {
            return pawn.InBed() && (HealthAIUtility.ShouldSeekMedicalRestUrgent(pawn) || (HealthAIUtility.ShouldSeekMedicalRest(pawn) || pawn.CurJob.restUntilHealed));
        }
    }
}