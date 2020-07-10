﻿using System;
using System.Collections.Generic;
using HarmonyLib;
using HugsLib;
using PrisonLabor.Core.AI.WorkGivers;
using PrisonLabor.Core.Other;
using PrisonLabor.Core.Trackers;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Noise;

namespace PrisonLabor.Core.Components
{
    public partial class PrisonerComp
    {
        private bool isHungry = false;
        private bool isStarving = false;

        public bool IsHungry => isHungry;
        public bool IsStarving => isStarving;

        private void TickLongPrisoner()
        {
            this.isHungry = pawn.needs.food.CurLevelPercentage < 0.15f;
            this.isStarving = pawn.needs.food.CurLevelPercentage <= 0.001f;
        }

        private void TickLongWarden()
        {
            if (pawn.CurJobDef.defName != "PrisonLabor_SupervisePrisonLabor") { return; }

            var room = pawn.GetRoom();
            if (room != null)
            {
                foreach (int id in Tracked.Prisoners[room.ID])
                {
                    var comp = (PrisonerComp)Tracked.pawnComps[id];
                    var prisoner = comp.pawn;

                    if (prisoner.IsFighting() && prisoner.CurrentBed() != null)
                    {
                        Job nJob = JobMaker.MakeJob(new JobDef
                        {
                            driverClass = typeof(JobDriver_TakeToBed),
                            defName = "PrisonLabor_StopFight",
                            label = "Policing",
                            reportString = "Stoping the Fight",
                            description = "Arresting prisoner for fighting.",
                            playerInterruptible = true,
                            checkOverrideOnDamage = CheckJobOverrideOnDamageMode.Never,
                            alwaysShowWeapon = true,
                            casualInterruptible = true
                        }, new LocalTargetInfo(prisoner), new LocalTargetInfo(prisoner.CurrentBed()));

                        pawn.jobs.StartJob(nJob, resumeCurJobAfterwards: true); return;
                    }

                    if (!pawn.CanReserve(prisoner, 1, -1, null, false))
                        continue;

                    if (comp.IsHungry == true)
                    {
                        if (!PrisonFoodUtility.FoodAvailableInRoomFor(room, prisoner))
                        {
                            if (FoodUtility.TryFindBestFoodSourceFor(pawn, prisoner, false, out Thing foodSource, out ThingDef thingDef))
                            {
                                Log.Message("dude");
                                float nutrition = FoodUtility.GetNutrition(foodSource, thingDef);
                                Job nJob = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("PrisonLabor_PrisonerDeliverFoodSupervise"), foodSource, prisoner);

                                nJob.count = FoodUtility.WillIngestStackCountOf(prisoner, thingDef, nutrition);
                                if (!room.isPrisonCell)
                                    nJob.targetC = RCellFinder.SpotToChewStandingNear(prisoner, foodSource);
                                else
                                    nJob.targetC = room.Cells.RandomElement();

                                pawn.jobs.StartJob(nJob, resumeCurJobAfterwards: true); return;
                            }
                        }
                    }
                }
            }
        }
    }
}