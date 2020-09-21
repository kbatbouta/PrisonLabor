using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AndroidTiers
{
    public class CompAndroidState : ThingComp
    {
        public Pawn pawn = null;

        public override void PostPostMake()
        {
            base.PostPostMake();
            base.parent.ATCompState = this;
            this.pawn = parent as Pawn;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.autoPaintStarted, "ATPP_autoPaintStarted", false);
            Scribe_Values.Look<bool>(ref this.connectedLWPNActive, "ATPP_connectedLWPNActive", false);
            Scribe_Values.Look<bool>(ref this.isBlankAndroid, "ATPP_isBlankAndroid", false);
            Scribe_Values.Look<bool>(ref this.showUploadProgress, "ATPP_showUploadProgress", false);
            Scribe_Values.Look<bool>(ref this.useBattery, "ATPP_useBattery", (Settings.defaultGeneratorMode == 1) ? false : true, true);
            Scribe_Values.Look<int>(ref this.uploadEndingGT, "ATPP_uploadEndingGT", -1);
            Scribe_Values.Look<int>(ref this.uploadStartGT, "ATPP_uploadStartGT", 0);
            Scribe_Values.Look<bool>(ref this.isSurrogate, "ATPP_isSurrogate", false);
            Scribe_Values.Look<string>(ref this.savedName, "ATPP_savedName", "");
            Scribe_Values.Look<int>(ref this.frameworkNaniteEffectGTEnd, "ATPP_frameworkNaniteEffectGTEnd", -1);
            Scribe_Values.Look<int>(ref this.frameworkNaniteEffectGTStart, "ATPP_frameworkNaniteEffectGTStart", -1);
            Scribe_Values.Look<int>(ref paintingRustGT, "ATPP_paintingRustGT", -2);
            Scribe_Values.Look<bool>(ref this.paintingIsRusted, "ATPP_paintingIsRusted", false);
            Scribe_Values.Look<int>(ref batteryExplosionEndingGT, "ATPP_batteryExplosionEndingGT", -1);
            Scribe_Values.Look<int>(ref customColor, "ATPP_customColor", (int)AndroidPaintColor.Default);

            Scribe_Deep.Look<Pawn>(ref this.externalController, "ATPP_externalController", new object[0]);

            Scribe_References.Look(ref surrogateController, "ATPP_surrogateController");
            Scribe_References.Look(ref lastController, "ATPP_lastController");
            Scribe_References.Look<Pawn>(ref this.uploadRecipient, "ATPP_uploadRecipient", false);
            Scribe_References.Look(ref this.connectedLWPN, "ATPP_connectedLWPN", false);

            Scribe_Defs.Look<HairDef>(ref this.hair, "ATPP_hair");
        }

        public override void PostDraw()
        {
            Material avatar = null;
            Vector3 vector;

            if(uploadEndingGT != -1 || showUploadProgress)
                avatar = Tex.UploadInProgress;
            else if(Find.DesignatorManager.SelectedDesignator is Designator_AndroidToControl && isSurrogate && surrogateController == null && (skyMind != null && skyMind.Infected == -1))
                avatar = Tex.SelectableSX;
            else if(Find.DesignatorManager.SelectedDesignator is Designator_SurrogateToHack && isSurrogate && parent.Faction != Faction.OfPlayer)
                avatar = Tex.SelectableSXToHack;
            else if(isSurrogate && surrogateController != null && !Settings.hideRemotelyControlledDeviceIcon)
                avatar = Tex.RemotelyControlledNode;

            if(avatar != null) {
                vector = this.parent.TrueCenter();
                vector.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays) + 0.28125f;
                vector.z += 1.4f;
                vector.x += this.parent.def.size.x / 2;

                Graphics.DrawMesh(MeshPool.plane08, vector, Quaternion.identity, avatar, 0);
            }
        }

        public override void PostDrawExtraSelectionOverlays()

        {
            base.PostDrawExtraSelectionOverlays();

            //Dessin liaison entre controlleur et SX
            if((surrogateController != null && isSurrogate && surrogateController.Map == parent.Map)) {
                GenDraw.DrawLineBetween(parent.TrueCenter(), surrogateController.TrueCenter(), SimpleColor.Blue);
            }

            if(surrogateController != null) {
                if(surrogateController.ATCompSurrogateOwner != null
                    && surrogateController.ATCompSurrogateOwner?.skyCloudHost != null
                    && surrogateController.ATCompSurrogateOwner?.skyCloudHost.Map == parent.Map) {
                    GenDraw.DrawLineBetween(parent.TrueCenter(), surrogateController.ATCompSurrogateOwner.skyCloudHost.TrueCenter(), SimpleColor.Red);
                }

                if((uploadEndingGT != -1 && uploadRecipient != null) || showUploadProgress) {
                    GenDraw.DrawLineBetween(parent.TrueCenter(), uploadRecipient.TrueCenter(), SimpleColor.Green);
                }
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            this.pawn = parent as Pawn;

            if(parent.Map == null || !parent.Spawned)
                return;

            if(!init) {
                checkTXWithSkinFacialTextureUpdate();
                init = true;
                //Reconexion auto au LWPN le cas echeant
                if(Utils.POWERPP_LOADED)
                    if(connectedLWPN != null && connectedLWPNActive) {
                        if(!Utils.GCATPP.pushLWPNAndroid(connectedLWPN, (Pawn)parent)) {
                            connectedLWPNActive = false;
                        }
                    }
            }

            if((externalController != null
                && surrogateController == null
                && skyMind != null && skyMind.hacked != 3
                && !parent.Map.gameConditionManager.ConditionIsActive(GameConditionDefOf.SolarFlare))) {

                Lord lordInvolved = null;
                if(pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction).Any((Pawn p) => p != pawn)) {
                    Pawn other = (Pawn)GenClosest.ClosestThing_Global(pawn.Position, pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction), 99999f, (Thing p) => p != pawn && ((Pawn)p).GetLord() != null, null);
                    lordInvolved = other.GetLord();
                }
                if(lordInvolved == null && !pawn.IsPrisoner) {
                    LordJob_DefendPoint lordJob = new LordJob_DefendPoint(pawn.Position);
                    lordInvolved = LordMaker.MakeNewLord(pawn.Faction, lordJob, Find.CurrentMap, null);
                }


                //Si controlleur non player du surrogate mort OU surrogate hacké avais un lors il existe tjr mais il n'est plus actif
                if(externalController.Dead || (skyMind != null && skyMind.hackOrigFaction.HostileTo(Faction.OfPlayer) && lordInvolved == null && !pawn.IsPrisoner)) {
                    //Rajout NoHost car comme en mode externalController on a pas remis le hediff pour eviter le bug bizard faisant que quand tentative integration ennemis hacké a un lord sa merdequand il a été down
                    addNoRemoteHostHediff();
                    externalController = null;
                }
                else {

                    //Tentative de reconnection automatique du surrogate a son controlleur externe
                    CompSurrogateOwner cso = externalController.ATCompSurrogateOwner;
                    cso.setControlledSurrogate((Pawn)parent, true);
                    pawn.mindState.Reset();
                    pawn.mindState.duty = null;
                    pawn.jobs.StopAll();
                    pawn.jobs.ClearQueuedJobs();
                    pawn.ClearAllReservations();
                    if(pawn.drafter != null)
                        pawn.drafter.Drafted = false;

                    if(lordInvolved != null)
                        lordInvolved.AddPawn(pawn);

                    if(lordInvolved != null && lordInvolved.CurLordToil is LordToil_Siege) {
                        LordToil_Siege st = (LordToil_Siege)lordInvolved.CurLordToil;

                        //Attribution job defender au pawn
                        Pawn p = (Pawn)parent;
                        //Traverse.Create( st ).Method("SetAsDefender").GetValue((Pawn)parent);
                        LordToilData_Siege data = (LordToilData_Siege)Traverse.Create(st).Property("Data").GetValue();
                        p.mindState.duty = new PawnDuty(DutyDefOf.Defend, data.siegeCenter, -1f);
                        p.mindState.duty.radius = data.baseRadius;
                        st.UpdateAllDuties();
                    }
                }
            }


            if(pawn == null) return;

            if(uploadEndingGT != -1) {
                checkInterruptedUpload();

                //Atteinte d'un chargement d'upload de conscience
                if(uploadRecipient != null && uploadEndingGT != -1 && uploadEndingGT < GenTicks.TicksGame) {
                    uploadEndingGT = -1;
                    uploadRecipient.ATCompState.uploadEndingGT = -1;

                    Utils.removeUploadHediff(pawn, uploadRecipient);

                    Find.LetterStack.ReceiveLetter("ATPP_LetterUploadOK".Translate(), "ATPP_LetterUploadOKDesc".Translate(pawn.LabelShortCap, uploadRecipient.LabelShortCap), LetterDefOf.PositiveEvent, parent);

                    if(pawn.def.defName == Utils.T1 && uploadRecipient.def.defName != Utils.T1)
                        Utils.removeSimpleMindedTrait(pawn);
                    else
                        Utils.addSimpleMindedTraitForT1(uploadRecipient);

                    //On realise effectivement la permutation puis le kill de la source
                    Utils.PermutePawn(pawn, uploadRecipient);

                    Utils.clearBlankAndroid(uploadRecipient);

                    //Report du blankAndroid pour le flagger dans la routine de kill
                    isBlankAndroid = true;

                    //Si destinataire de la duplication prisonnier Et emetteur pas prisonier on enleve la condition 
                    if(!pawn.IsPrisoner && uploadRecipient.IsPrisoner) {
                        if(uploadRecipient.Faction != Faction.OfPlayer) {
                            uploadRecipient.SetFaction(Faction.OfPlayer, null);
                        }

                        if(uploadRecipient.guest != null) {
                            uploadRecipient.guest.SetGuestStatus(null, false);
                        }
                    }

                    //SI destinataire de la duplication colon regular et emetteur prisonnier 
                    if(pawn.IsPrisoner && !uploadRecipient.IsPrisoner) {
                        if(uploadRecipient.Faction != pawn.Faction) {
                            uploadRecipient.SetFaction(pawn.Faction, null);
                        }

                        if(uploadRecipient.guest != null) {
                            uploadRecipient.guest.SetGuestStatus(Faction.OfPlayer, true);
                        }
                    }
                    if(!pawn.Dead)
                        pawn.Kill(null, null);
                    resetUploadStuff();
                }
            }

            if(batteryExplosionEndingGT != -1 && batteryExplosionEndingGT < GenTicks.TicksGame) {
                Utils.makeAndroidBatteryOverload(pawn);
                return;
            }

            //Atteinte fin application des nanites sur un androide
            if(frameworkNaniteEffectGTEnd != -1 && GenTicks.TicksGame >= frameworkNaniteEffectGTEnd && !pawn.Dead) {
                bool chance = false;
                int nb = 0;

                //Chance que nanite fail
                if(!Rand.Chance(Settings.percentageNanitesFail)) {
                    //Le cas echeant on enleve le rusting
                    clearRusted();

                    nb = pawn.health.hediffSet.hediffs.RemoveAll((Hediff h) => (Utils.AndroidOldAgeHediffFramework.Contains(h.def.defName)));
                    nb += pawn.health.hediffSet.hediffs.RemoveAll((Hediff h) => (h.def == HediffDefOf.MissingBodyPart || (Utils.ExceptionRepairableFrameworkHediff.Contains(h.def) && h.IsPermanent())));
                    if(nb > 0) {
                        Utils.refreshHediff(pawn);
                    }
                    chance = true;
                }

                if(nb == 0) {
                    if(chance)
                        Messages.Message("ATPP_NoBrokenStuffFound".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NegativeEvent, true);
                    else {
                        Messages.Message("ATPP_BrokenStuffRepairFailed".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NegativeEvent, true);
                    }
                }
                else {
                    Messages.Message("ATPP_BrokenFrameworkRepaired".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.PositiveEvent, true);
                }
                frameworkNaniteEffectGTEnd = -1;
                frameworkNaniteEffectGTStart = -1;
            }

            checkInfectionFix();
            checkTXWithSkinFacialTextureUpdate();

            if(skyMind != null && skyMind.Infected == -1) {

                //Recharge surrogate
                if(Utils.androidIsValidPodForCharging(pawn) && !isOrganic) {
                    //cpawn.needs.food.CurLevel = cpawn.needs.food.MaxLevel;
                    pawn.needs.food.CurLevelPercentage += Settings.percentageOfBatteryChargedEach6Sec;
                    Utils.throwChargingMote(pawn);
                }

                if(isSurrogate && surrogateController == null)
                    addNoRemoteHostHediff();
            }

            checkSolarFlareStuff();
            checkRusted();
            checkBlankAndroid();

            if(Utils.POWERPP_LOADED) {
                //Tentative de reco auto
                if(!connectedLWPNActive && connectedLWPN != null) {
                    if(Utils.GCATPP.pushLWPNAndroid(connectedLWPN, pawn))
                        connectedLWPNActive = true;
                }
            }
        }

        /*
         * Essentially usefull to fix visual bugged state of lite virused androids (correct cases where the patch is not executed at the end of the mental break and the state not cleared)
         */
        public void checkInfectionFix()
        {
            if(skyMind != null && skyMind.Infected == 4 && !pawn.InMentalState) {
                skyMind.Infected = -1;
                Hediff he = pawn.health.hediffSet.GetFirstHediffOfDef(Utils.hediffNoHost);
                if(he == null) {
                    pawn.health.AddHediff(Utils.hediffNoHost);
                }
            }
        }

        public void checkTXWithSkinFacialTextureUpdate()
        {

            if(isAndroidWithSkin) {
                Utils.lastResolveAllGraphicsHeadGraphicPath = null;

                //Changement tete
                if((!TXHurtedHeadSet && (pawn.health.summaryHealth.SummaryHealthPercent <= 0.85f && pawn.health.summaryHealth.SummaryHealthPercent > 0.45f)) || forcedDamageLevel == 1) {
                    TXHurtedHeadSet = true;

                    if(TXHurtedHeadSet2) {
                        TXHurtedHeadSet2 = false;
                        if(hair != null)
                            pawn.story.hairDef = hair;
                        hair = null;
                    }

                    Utils.changeTXBodyType(pawn, 1);
                    Utils.changeHARCrownType(pawn, "Average_Hurted");
                    pawn.Drawer.renderer.graphics.ResolveAllGraphics();
                    PortraitsCache.SetDirty(pawn);
                }
                else if((!TXHurtedHeadSet2 && (pawn.health.summaryHealth.SummaryHealthPercent <= 0.45f)) || forcedDamageLevel == 2) {
                    TXHurtedHeadSet = false;
                    TXHurtedHeadSet2 = true;
                    if(hair == null)
                        hair = pawn.story.hairDef;
                    pawn.story.hairDef = DefDatabase<HairDef>.GetNamed("Shaved", false);
                    Utils.changeTXBodyType(pawn, 2);
                    Utils.changeHARCrownType(pawn, "Average_Hurted2");
                    pawn.Drawer.renderer.graphics.ResolveAllGraphics();
                    PortraitsCache.SetDirty(pawn);
                }
                else {
                    if((TXHurtedHeadSet || !init) && pawn.health.summaryHealth.SummaryHealthPercent > 0.85f) {
                        TXHurtedHeadSet = false;
                        TXHurtedHeadSet2 = false;
                        if(hair != null) {
                            pawn.story.hairDef = hair;
                            hair = null;
                        }
                        Utils.changeTXBodyType(pawn, 0);
                        Utils.changeHARCrownType(pawn, "Average_Normal");
                        pawn.Drawer.renderer.graphics.ResolveAllGraphics();
                        PortraitsCache.SetDirty(pawn);
                    }
                    else if((TXHurtedHeadSet2 || !init) && pawn.health.summaryHealth.SummaryHealthPercent > 0.45f) {
                        TXHurtedHeadSet2 = false;
                        TXHurtedHeadSet = false;

                        if(pawn.health.summaryHealth.SummaryHealthPercent <= 0.85f)
                            TXHurtedHeadSet = true;

                        pawn.story.hairDef = hair;
                        hair = null;
                        if(pawn.health.summaryHealth.SummaryHealthPercent <= 0.85f) {
                            Utils.changeHARCrownType(pawn, "Average_Hurted");
                            Utils.changeTXBodyType(pawn, 1);
                        }
                        else {
                            Utils.changeHARCrownType(pawn, "Average_Normal");
                            Utils.changeTXBodyType(pawn, 0);
                        }
                        pawn.Drawer.renderer.graphics.ResolveAllGraphics();
                        PortraitsCache.SetDirty(pawn);
                    }
                    //(string)Traverse.Create(p1.story).Field("headGraphicPath").GetValue();
                }

                if(Utils.RIMMSQOL_LOADED && Utils.lastResolveAllGraphicsHeadGraphicPath != null) {
                    pawn.story.GetType().GetField("headGraphicPath", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pawn.story, Utils.lastResolveAllGraphicsHeadGraphicPath);
                    Utils.lastResolveAllGraphicsHeadGraphicPath = null;
                    /*cp.Drawer.renderer.graphics.ResolveAllGraphics();
                    PortraitsCache.SetDirty(cp);*/
                }
            }
        }

        public void checkBlankAndroid()
        {

            if(!pawn.Dead && isBlankAndroid) {
                Hediff he = pawn.health.hediffSet.GetFirstHediffOfDef(Utils.hediffBlankAndroid);
                if(he == null && pawn.health != null)
                    pawn.health.AddHediff(he);
            }
        }

        public void checkRusted()
        {

            //Entitées qui ne rust pas on degage et check avant de faire le menage des rust malplacés
            if(!isAndroidTIer || isAndroidWithSkin || dontRust) {
                Hediff he = pawn.health.hediffSet.GetFirstHediffOfDef(Utils.hediffRusted);
                if(he != null)
                    pawn.health.RemoveHediff(he);

                return;
            }


            if(!Settings.androidsCanRust) {
                if(paintingIsRusted) {
                    paintingIsRusted = false;
                    paintingRustGT = -3;
                }

                Hediff he = pawn.health.hediffSet.GetFirstHediffOfDef(Utils.hediffRusted);
                if(he != null)
                    pawn.health.RemoveHediff(he);

            }
            else {
                //Reprise de la rouille interrompue
                if(paintingRustGT == -3 && !paintingIsRusted) {
                    setRusted();
                }

                if(paintingRustGT != -1) {
                    paintingRustGT -= 300;
                    if(paintingRustGT < 0)
                        paintingRustGT = 0;
                }

                //Lancement paint auto 1jour avant la fin d'expiration du timeout
                if(Settings.allowAutoRepaint && (!pawn.IsPrisoner || Settings.allowAutoRepaintForPrisoners) && !autoPaintStarted && paintingRustGT <= 60000) {
                    //Déduction recipeDef
                    AndroidPaintColor color = (AndroidPaintColor)customColor;
                    string paintRecipeDefname = "";

                    switch(color) {
                        case AndroidPaintColor.Black:
                            paintRecipeDefname = "ATPP_PaintAndroidFrameworkBlack";
                            break;
                        case AndroidPaintColor.Blue:
                            paintRecipeDefname = "ATPP_PaintAndroidFrameworkBlue";
                            break;
                        case AndroidPaintColor.Cyan:
                            paintRecipeDefname = "ATPP_PaintAndroidFrameworkCyan";
                            break;
                        case AndroidPaintColor.None:
                        case AndroidPaintColor.Default:
                            paintRecipeDefname = "ATPP_PaintAndroidFrameworkDefault";
                            break;
                        case AndroidPaintColor.Gray:
                            paintRecipeDefname = "ATPP_PaintAndroidFrameworkGray";
                            break;
                        case AndroidPaintColor.Green:
                            paintRecipeDefname = "ATPP_PaintAndroidFrameworkGreen";
                            break;
                        case AndroidPaintColor.Khaki:
                            paintRecipeDefname = "ATPP_PaintAndroidFrameworkKhaki";
                            break;
                        case AndroidPaintColor.Orange:
                            paintRecipeDefname = "ATPP_PaintAndroidFrameworkOrange";
                            break;
                        case AndroidPaintColor.Pink:
                            paintRecipeDefname = "ATPP_PaintAndroidFrameworkPink";
                            break;
                        case AndroidPaintColor.Purple:
                            paintRecipeDefname = "ATPP_PaintAndroidFrameworkPurple";
                            break;
                        case AndroidPaintColor.Red:
                            paintRecipeDefname = "ATPP_PaintAndroidFrameworkRed";
                            break;
                        case AndroidPaintColor.White:
                            paintRecipeDefname = "ATPP_PaintAndroidFrameworkWhite";
                            break;
                        case AndroidPaintColor.Yellow:
                            paintRecipeDefname = "ATPP_PaintAndroidFrameworkYellow";
                            break;
                    }

                    RecipeDef recipe = DefDatabase<RecipeDef>.GetNamed(paintRecipeDefname, false);
                    if(recipe != null) {
                        //Renouvellement auto de la peinture (ajout operation auto)
                        pawn.health.surgeryBills.AddBill(new Bill_Medical(recipe));
                        autoPaintStarted = true;
                    }
                }

                //Rouille de la peinture ?
                if(paintingRustGT == 0 || (paintingRustGT == -1 && pawn.health.hediffSet.GetFirstHediffOfDef(Utils.hediffRusted) == null)) {
                    paintingIsRusted = true;
                    pawn.Drawer.renderer.graphics.ResolveAllGraphics();
                    PortraitsCache.SetDirty(pawn);
                    pawn.health.AddHediff(Utils.hediffRusted);

                    paintingRustGT = -1;
                }
                else {
                    if(!paintingIsRusted) {
                        //Cas aberrant (possede hediff de rusted alors que pas rusted)
                        Hediff cRusted = pawn.health.hediffSet.GetFirstHediffOfDef(Utils.hediffRusted);
                        if(cRusted != null) {
                            pawn.health.RemoveHediff(cRusted);
                        }
                    }
                }
            }
        }

        public void setRusted()
        {
            paintingIsRusted = true;
            paintingRustGT = -1;
            pawn.health.AddHediff(Utils.hediffRusted);
        }

        public void clearRusted()
        {
            paintingIsRusted = false;
            autoPaintStarted = false;
            paintingRustGT = (Rand.Range(Settings.minDaysAndroidPaintingCanRust, Settings.maxDaysAndroidPaintingCanRust) * 60000);

            pawn.Drawer.renderer.graphics.ResolveAllGraphics();
            if(Find.ColonistBar != null) {
                PortraitsCache.SetDirty(pawn);
            }

            //Retire du hediff de rouille
            Hediff he = pawn.health.hediffSet.GetFirstHediffOfDef(Utils.hediffRusted);
            if(he != null)
                pawn.health.RemoveHediff(he);
        }

        public void checkSolarFlareStuff()
        {
            //Androids avec une peau pas affectés par le solarflare
            if(pawn.def.defName == Utils.TX2 || pawn.def.defName == Utils.TX3)
                return;

            if(!isOrganic || pawn.VXAndVX0ChipPresent()) {
                bool solarFlareRunning = Utils.getRandomMapOfPlayer().gameConditionManager.ConditionIsActive(GameConditionDefOf.SolarFlare);

                if(Settings.disableSolarFlareEffect) {
                    //Retrait heddif si il avait été ajouté
                    if(solarFlareEffectApplied) {
                        Pawn cpawn = (Pawn)parent;
                        Hediff he = cpawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("ATPP_SolarFlareAndroidImpact"));
                        if(he != null)
                            cpawn.health.RemoveHediff(he);
                    }
                    solarFlareEffectApplied = false;
                    return;
                }

                //Application de l'effet
                if(solarFlareRunning && !solarFlareEffectApplied) {
                    Pawn cpawn = (Pawn)parent;
                    //Ajout heddif
                    cpawn.health.AddHediff(DefDatabase<HediffDef>.GetNamed("ATPP_SolarFlareAndroidImpact"));

                    solarFlareEffectApplied = true;
                }

                //Suppression de l'effet
                if(!solarFlareRunning && solarFlareEffectApplied) {
                    Pawn cpawn = (Pawn)parent;
                    //Ajout heddif
                    Hediff he = cpawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("ATPP_SolarFlareAndroidImpact"));
                    if(he != null)
                        cpawn.health.RemoveHediff(he);

                    //Suppression de l'heddif
                    solarFlareEffectApplied = false;
                }
            }
            else {
                Hediff he = pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("ATPP_SolarFlareAndroidImpact"));
                if(he != null)
                    pawn.health.RemoveHediff(he);
            }
        }

        public void addNoRemoteHostHediff()
        {
            Hediff he = pawn.health.hediffSet.GetFirstHediffOfDef(Utils.hediffNoHost);
            if(he == null) {
                pawn.health.AddHediff(Utils.hediffNoHost);
            }
        }


        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            base.parent.ATCompState = this;

            this.skyMind = parent.ATCompSkyMind;
            this.pawn = parent as Pawn;

            isAndroidWithSkin = Utils.ExceptionAndroidWithSkinList.Contains(pawn.def.defName);
            dontRust = Utils.ExceptionAndroidsDontRust.Contains(pawn.def.defName);

            bool isAndroidTier = pawn.IsAndroidTier();

            if(!isAndroidTier) {
                isOrganic = true;
            }

            if(isOrganic) {
                useBattery = false;
            }

            string MUID = parent.Map.GetUniqueLoadID();

            if(!respawningAfterLoad)
                Utils.GCATPP.pushSurrogateAndroidNotifyMapChanged((Pawn)this.parent, MUID);

            //Suppression traits blacklistés le cas echeant
            if(isAndroidTier && (!isSurrogate || (isSurrogate && surrogateController != null && surrogateController.IsAndroidTier())))
                Utils.removeMindBlacklistedTrait(pawn);

            this.isAndroidTIer = isAndroidTier;

            checkInfectionFix();

            if(isAndroidTier) {
                if(isAndroidWithSkin) {
                    if(pawn.gender == Gender.Male) {
                        BodyTypeDef bd = DefDatabase<BodyTypeDef>.GetNamed("Male", false);
                        if(bd != null)
                            pawn.story.bodyType = bd;
                    }
                    else {
                        BodyTypeDef bd = DefDatabase<BodyTypeDef>.GetNamed("Female", false);
                        if(bd != null)
                            pawn.story.bodyType = bd;
                    }
                }

                if(pawn.ownership != null && pawn.ownership.OwnedBed != null) {
                    if(pawn.ownership.OwnedBed.ForPrisoners != pawn.IsPrisoner) {
                        pawn.ownership.UnclaimBed();
                    }
                }
                //Starting du délais de rusting
                if(!dontRust) {
                    if(paintingRustGT == -2) {
                        paintingRustGT = (Rand.Range(Settings.minDaysAndroidPaintingCanRust, Settings.maxDaysAndroidPaintingCanRust) * 60000);
                    }

                    if(paintingRustGT == -1 && paintingIsRusted && pawn.health != null) {
                        Hediff he = pawn.health.hediffSet.GetFirstHediffOfDef(Utils.hediffRusted);
                        if(he == null) {
                            pawn.health.AddHediff(Utils.hediffRusted);
                        }
                    }
                }
            }
            else {
                if(pawn.health != null) {
                    paintingIsRusted = false;
                    Hediff he = pawn.health.hediffSet.GetFirstHediffOfDef(Utils.hediffRusted);
                    if(he != null)
                        pawn.health.RemoveHediff(he);
                }

                //Si VX0 dans une session en cours alors on chope le pawn permuté controleur
                if(surrogateController != null)
                    pawn = surrogateController;

                //Reset du child et adulthood si VX0 organic
                if(isSurrogate && isOrganic && pawn.story != null && pawn.story.adulthood != null) {
                    if(pawn.story.childhood != null) {
                        Backstory bs = null;

                        BackstoryDatabase.TryGetWithIdentifier("MercenaryRecruit", out bs);
                        if(bs != null)
                            pawn.story.childhood = bs;
                    }

                    pawn.story.adulthood = null;
                    //Reset incapable of
                    Utils.ResetCachedIncapableOf(pawn);
                }

            }
        }


        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);

            if(Utils.POWERPP_LOADED && connectedLWPN != null) {
                if(connectedLWPNActive)
                    Utils.GCATPP.popLWPNAndroid(connectedLWPN, (Pawn)parent);

                connectedLWPN = null;
                connectedLWPNActive = false;
            }

            //Si surrogate on notifis le changement de map de ce dernier pour qu'il soit correctement traqué
            if(isSurrogate) {
                string MUID = "caravan";
                if(map != null)
                    MUID = map.GetUniqueLoadID();

                Utils.GCATPP.pushSurrogateAndroidNotifyMapChanged((Pawn)this.parent, MUID);
            }
        }





        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            Utils.removeUploadHediff((Pawn)parent, uploadRecipient);
            if(uploadEndingGT != -1)
                checkInterruptedUpload();
            if(isSurrogate && previousMap == null) {
                string MUID = "caravan";
                Utils.GCATPP.pushSurrogateAndroidNotifyMapChanged((Pawn)this.parent, MUID);
            }
        }

        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);
            switch(signal) {
                case "SkyMindNetworkUserConnected":
                    break;
                case "SkyMindNetworkUserDisconnected":
                    //On va  invoquer le checkInterruption pour les duplicate et permutation 
                    checkInterruptedUpload();
                    break;
            }
        }

        private bool isRegularM7()
        {
            //Les M7Mech standard ne sont pas controlables
            return (!isSurrogate && isM7());
        }

        private bool isM7()
        {
            //Les M7Mech standard ne sont pas controlables
            return parent.def.defName == "M7Mech";
        }


        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Pawn pawn = (Pawn)parent;
            bool isPrisoner = pawn.IsPrisoner;
            bool transfertAllowed = Utils.mindTransfertsAllowed((Pawn)parent);

            //Si androide virusé (hacking) ajout boutton permettant de le shutdown
            if(skyMind != null && skyMind.Hacked == 1) {
                yield return new Command_Action {
                    icon = Tex.StopVirused,
                    defaultLabel = "ATPP_StopVirused".Translate(),
                    defaultDesc = "ATPP_StopVirusedDesc".Translate(),
                    action = delegate () {
                        pawn.Kill(null, null);
                    }
                };

                yield break;
            }

            if(!isOrganic && pawn.Faction == Faction.OfPlayer) {
                //Ajout possibilité de lancer l'explosion d'un androide a distance

                if(Utils.ResearchAndroidBatteryOverload.IsFinished) {
                    Texture2D tex = Tex.ForceAndroidToExplode;

                    if(batteryExplosionEndingGT != -1)
                        tex = Tex.ForceAndroidToExplodeDisabled;

                    yield return new Command_Action {
                        icon = tex,
                        defaultLabel = "ATPP_OverloadAndroid".Translate(),
                        defaultDesc = "ATPP_OverloadAndroidDesc".Translate(),
                        action = delegate () {
                            if(batteryExplosionEndingGT != -1)
                                return;
                            Find.WindowStack.Add(new Dialog_Msg("ATPP_UploadMakeAndroidBatteryOverloadConfirm".Translate(), "ATPP_UploadMakeAndroidBatteryOverloadConfirmDesc".Translate(), delegate {
                                batteryExplosionStartingGT = Find.TickManager.TicksGame;
                                batteryExplosionEndingGT = batteryExplosionStartingGT + 930;
                            }, false));
                        }
                    };
                }

                //Si POWER++ chargé ajout possibilité de rattaché android à un LWPN
                if(Utils.POWERPP_LOADED && useBattery) {
                    Texture2D tex = Tex.LWPNConnected;

                    if(connectedLWPN == null || !connectedLWPNActive || connectedLWPN.Destroyed || !connectedLWPN.TryGetComp<CompPowerTrader>().PowerOn)
                        tex = Tex.LWPNNotConnected;

                    yield return new Command_Action {
                        icon = tex,
                        defaultLabel = "ARKPPP_LWPSel".Translate(),
                        defaultDesc = "",
                        action = delegate () {
                            List<FloatMenuOption> opts = new List<FloatMenuOption>();
                            FloatMenu floatMenuMap;

                            foreach(var build in parent.Map.listerBuildings.allBuildingsColonist) {
                                if((build.def.defName == "ARKPPP_LocalWirelessPowerEmitter" || build.def.defName == "ARKPPP_LocalWirelessPortablePowerEmitter")
                                    && !build.IsBrokenDown()
                                    && build.TryGetComp<CompPowerTrader>().PowerOn) {
                                    ThingComp compLWPNEmitter = Utils.TryGetCompByTypeName(build, "CompLocalWirelessPowerEmitter", "Power++");
                                    if(compLWPNEmitter != null) {
                                        string lib = getConnectedLWPNLabel(build);

                                        opts.Add(new FloatMenuOption("ARKPPP_WPNListRow".Translate(lib, ((int)Utils.getCurrentAvailableEnergy(build.PowerComp.PowerNet)).ToString()), delegate {
                                            if(connectedLWPN != null) {
                                                Utils.GCATPP.popLWPNAndroid(connectedLWPN, pawn);
                                                connectedLWPNActive = false;
                                                connectedLWPN = null;
                                            }

                                            if(Utils.GCATPP.pushLWPNAndroid(build, pawn)) {
                                                connectedLWPN = build;
                                                connectedLWPNActive = true;
                                            }
                                            else {
                                                Messages.Message("ATPP_MessageLWPNNoSlotAvailable".Translate(), MessageTypeDefOf.NegativeEvent);
                                            }

                                        }, MenuOptionPriority.Default, null, null, 0f, null, null));
                                    }
                                }
                            }

                            //SI pas choix affichage de la raison 
                            if(opts.Count == 0)
                                opts.Add(new FloatMenuOption("ATPP_NoAvailableLWPN".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null));

                            //Si le recepteur est configuré pour se connecter a un LWPN définis on ajoute une option de deconnexion
                            if(connectedLWPN != null) {
                                opts.Add(new FloatMenuOption("ARKPPP_ClearCurrentWPNConnection".Translate(), delegate {
                                    if(connectedLWPN != null)
                                        Utils.GCATPP.popLWPNAndroid(connectedLWPN, (Pawn)parent);

                                    connectedLWPN = null;
                                    connectedLWPNActive = false;

                                }, MenuOptionPriority.Default, null, null, 0f, null, null));
                            }

                            floatMenuMap = new FloatMenu(opts, "ARKPPP_LocalWirelessPowerSelectorListTitle".Translate());
                            Find.WindowStack.Add(floatMenuMap);
                        }
                    };
                }
            }

            if(!isM7()) {

                if(!isOrganic) {
                    yield return new Command_Toggle {
                        icon = Tex.Battery,
                        defaultLabel = "ATPP_UseBattery".Translate(),
                        defaultDesc = "ATPP_UseBatteryDesc".Translate(),
                        isActive = (() => useBattery),
                        toggleAction = delegate () {
                            useBattery = !useBattery;
                            if(!useBattery && connectedLWPNActive) {
                                Utils.GCATPP.popLWPNAndroid(connectedLWPN, (Pawn)parent);
                            }
                        }
                    };
                }


                if(!Utils.GCATPP.isConnectedToSkyMind(pawn) || isPrisoner)
                    yield break;

                bool uploadInProgress = showUploadProgress || uploadEndingGT != -1;

                if(!isOrganic && !isSurrogate && Utils.anySkyMindNetResearched()) {
                    Texture2D selTex = Tex.UploadConsciousness;


                    if(!transfertAllowed)
                        selTex = Tex.UploadConsciousnessDisabled;

                    yield return new Command_Action {
                        icon = selTex,
                        defaultLabel = "ATPP_UploadConsciousness".Translate(),
                        defaultDesc = "ATPP_UploadConsciousnessDesc".Translate(),
                        action = delegate () {
                            if(!transfertAllowed)
                                return;
                            Utils.ShowFloatMenuAndroidCandidate((Pawn)parent, delegate (Pawn target) {
                                Find.WindowStack.Add(new Dialog_Msg("ATPP_UploadConsciousnessConfirm".Translate(parent.LabelShortCap, target.LabelShortCap), "ATPP_UploadConsciousnessConfirmDesc".Translate(parent.LabelShortCap, target.LabelShortCap) + "\n" + ("ATPP_WarningSkyMindDisconnectionRisk").Translate(), delegate {
                                    OnPermuteConfirmed((Pawn)parent, target);
                                }, false));
                            });
                        }
                    };
                }
            }

            //Permet de deconnecter l'utilisateur connecté sur le robot
            if(isSurrogate) {
                if(surrogateController != null) {
                    yield return new Command_Action {
                        icon = Tex.AndroidToControlTargetDisconnect,
                        defaultLabel = "ATPP_AndroidToControlTargetDisconnect".Translate(),
                        defaultDesc = "ATPP_AndroidToControlTargetDisconnectDesc".Translate(),
                        action = delegate () {
                            CompSurrogateOwner cso = surrogateController.ATCompSurrogateOwner;
                            if(cso != null)
                                cso.disconnectControlledSurrogate((Pawn)parent);
                        }
                    };
                }
                else {
                    if(lastController != null) {
                        //Permet au surrogate de se relier au dernier controller
                        yield return new Command_Action {
                            icon = Tex.AndroidSurrogateReconnectToLastController,
                            defaultLabel = "ATPP_AndroidSurrogateReconnectToLastController".Translate(),
                            defaultDesc = "ATPP_AndroidSurrogateReconnectToLastControllerDesc".Translate(),
                            action = delegate () {
                                if(lastController == null || lastController.Dead) {
                                    Messages.Message("ATPP_CannotReconnectToLastSurrogateController".Translate(), MessageTypeDefOf.NegativeEvent);
                                    return;
                                }

                                bool VX3Owner = lastController.VX3ChipPresent();
                                CompSurrogateOwner cso = lastController.ATCompSurrogateOwner;
                                if(cso != null) {
                                    //Check so lastController est un mind dans ce cas check qu'il ne fait pas deja autre chose
                                    if(cso.skyCloudHost != null) {
                                        CompSkyCloudCore csc = cso.skyCloudHost.TryGetComp<CompSkyCloudCore>();
                                        if(csc == null || csc.mindIsBusy(lastController)) {
                                            Messages.Message("ATPP_CannotReconnectToLastSurrogateController".Translate(), MessageTypeDefOf.NegativeEvent);
                                            return;
                                        }
                                    }

                                    //Si controller deconnecté tenttive reconnection au SkyMind
                                    bool isConnected = true;
                                    if(!Utils.GCATPP.isConnectedToSkyMind(lastController)) {
                                        if(!Utils.GCATPP.connectUser(lastController))
                                            isConnected = false;
                                    }

                                    //Deja en session le lastUser on jerte
                                    if(!isConnected || ((!VX3Owner && cso.isThereSX()) || (VX3Owner && cso.availableSX.Count + 1 > Settings.VX3MaxSurrogateControllableAtOnce)) || !cso.controlMode) {
                                        Messages.Message("ATPP_CannotReconnectToLastSurrogateController".Translate(), MessageTypeDefOf.NegativeEvent);
                                        return;
                                    }

                                    cso.setControlledSurrogate((Pawn)parent);
                                }
                            }
                        };
                    }
                }
            }
            else {
                //Si pas un surrogate

                if(Utils.GCATPP.isConnectedToSkyMind(parent) && !isBlankAndroid) {
                    CompSurrogateOwner cso = parent.ATCompSurrogateOwner;

                    //Pas d'organique ou de controlleur de surrogate en corus de session peuvent faire l'operation d'augmentation de points
                    if(!isOrganic && (cso == null || !cso.isThereSX())) {
                        yield return new Command_Action {
                            icon = Tex.SkillUp,
                            defaultLabel = "ATPP_Skills".Translate(),
                            defaultDesc = "ATPP_SkillsDesc".Translate(),
                            action = delegate () {
                                Find.WindowStack.Add(new Dialog_SkillUp((Pawn)parent));
                            }
                        };
                    }
                }

            }


            yield break;
        }

        public override string CompInspectStringExtra()
        {
            string ret = "";

            if(parent.Map == null)
                return base.CompInspectStringExtra();

            int level = 0;

            if(pawn.needs.food != null)
                level = (int)(pawn.needs.food.CurLevelPercentage * 100);

            if(!isOrganic) {
                ret += "ATPP_BatteryLevel".Translate(level) + "\n";

                if(Utils.POWERPP_LOADED && connectedLWPN != null) {
                    if(connectedLWPNActive)
                        ret += "ATPP_LWPNConnected".Translate(getConnectedLWPNLabel(connectedLWPN)) + "\n";
                    else
                        ret += "ATPP_LWPNDisconnected".Translate(getConnectedLWPNLabel(connectedLWPN)) + "\n";
                }

                if(batteryExplosionEndingGT != -1) {
                    float p;
                    p = Math.Min(1.0f, (float)(Find.TickManager.TicksGame - batteryExplosionStartingGT) / (float)(batteryExplosionEndingGT - batteryExplosionStartingGT));
                    ret += "ATPP_BatteryExplodeInProgress".Translate(((int)(p * (float)100)).ToString()) + "\n";
                }

                if(uploadEndingGT != -1 || showUploadProgress) {
                    //Calcul pourcentage de transfert
                    float p;
                    string action;

                    if(uploadEndingGT == -1) {
                        CompAndroidState cab = uploadRecipient.ATCompState;
                        p = Math.Min(1.0f, (float)(Find.TickManager.TicksGame - cab.uploadStartGT) / (float)(cab.uploadEndingGT - cab.uploadStartGT));
                        action = "ATPP_DownloadPercentage";
                    }
                    else {
                        p = Math.Min(1.0f, (float)(Find.TickManager.TicksGame - uploadStartGT) / (float)(uploadEndingGT - uploadStartGT));
                        action = "ATPP_UploadPercentage";
                    }


                    ret += action.Translate(((int)(p * (float)100)).ToString()) + "\n";
                }

                if(frameworkNaniteEffectGTEnd != -1) {
                    float p;
                    p = Math.Min(1.0f, (float)(Find.TickManager.TicksGame - frameworkNaniteEffectGTStart) / (float)(frameworkNaniteEffectGTEnd - frameworkNaniteEffectGTStart));
                    ret += "ATPP_NaniteFrameworkRepairingInProgress".Translate(((int)(p * (float)100)).ToString()) + "\n";
                }

                if(paintingIsRusted) {
                    ret += "ATPP_Rusted".Translate() + "\n";
                }
            }

            if(isSurrogate) {
                if(surrogateController != null)
                    ret += "ATPP_RemotelyControlledBy".Translate(((Pawn)parent).LabelShortCap) + "\n";

                if(lastController != null && externalController == null)
                    ret += "ATPP_PreviousSurrogateControllerIs".Translate(lastController.LabelShortCap) + "\n";


                if(surrogateController != null) {
                    CompSurrogateOwner cso = surrogateController.ATCompSurrogateOwner;
                    if(cso != null && surrogateController.VX3ChipPresent()) {
                        if(cso.SX == parent) {
                            ret += "ATPP_VX3SurrogateTypePrimary".Translate() + "\n";
                        }
                        else {
                            ret += "ATPP_VX3SurrogateTypeSecondary".Translate() + "\n";
                        }
                    }
                }
            }

            return ret.TrimEnd('\r', '\n') + base.CompInspectStringExtra();
        }


        public string getConnectedLWPNLabel(Building LWPNEmitter)
        {
            if(LWPNEmitter == null)
                return "";

            string ret = "";
            GameComponent GC_PPP = Utils.TryGetGameCompByTypeName("GC_PPP");
            Traverse GCPPP = Traverse.Create(GC_PPP);
            ThingComp compLWPNEmitter = Utils.TryGetCompByTypeName(LWPNEmitter, "CompLocalWirelessPowerEmitter", "Power++");
            if(compLWPNEmitter != null) {
                string LWPNID = (string)Traverse.Create(compLWPNEmitter).Field("LWPNID").GetValue();
                ret = (string)GCPPP.Method("getLWPNLabel", new object[] { LWPNID, false }).GetValue();
            }

            return ret;
        }

        /*
         * Detecte un cas d'interruption est le cas echeant tue les protagoniste de l'upload tous en affichant un message d'erreur
         */
        public void checkInterruptedUpload()
        {
            bool killSelf = false;
            Pawn cpawn = (Pawn)parent;

            bool recipientDeadOrNull = uploadRecipient == null || uploadRecipient.Dead;
            bool recipientConnected = false;
            bool emitterConnected = false;
            if(uploadRecipient != null && Utils.GCATPP.isConnectedToSkyMind(uploadRecipient, true))
                recipientConnected = true;

            if(Utils.GCATPP.isConnectedToSkyMind(cpawn))
                emitterConnected = true;

            if(isSurrogate && surrogateController != null) {
                CompSurrogateOwner cso = surrogateController.ATCompSurrogateOwner;
                if(cso == null)
                    return;

                //Surrogate en cours on check si clones toujours connecté
                if(cso.isThereSX() && cso.availableSX != null) {
                    bool hostBadConn = false;

                    //Si surrogateController stoclé dans le skyCloud
                    if(cso.skyCloudHost != null)
                        hostBadConn = !cso.skyCloudHost.TryGetComp<CompSkyCloudCore>().isRunning();
                    else
                        hostBadConn = !Utils.GCATPP.isConnectedToSkyMind(surrogateController, true);

                    bool surrogateBadConn = !Utils.GCATPP.isConnectedToSkyMind(cpawn, true);

                    if(hostBadConn || surrogateBadConn) {
                        //Log.Message("DDDDD==>"+ (!Utils.GCATPP.isConnectedToSkyMind(cpawn))+" "+ (!Utils.GCATPP.isConnectedToSkyMind(SX)));

                        Pawn disconnectedPawn = cpawn;
                        Pawn invertedPawn = surrogateController;
                        if(hostBadConn) {
                            disconnectedPawn = surrogateController;
                            invertedPawn = cpawn;
                        }

                        //Notification de la deconnexion accidentelle
                        if(disconnectedPawn != null && invertedPawn != null && disconnectedPawn.Faction == Faction.OfPlayer)
                            Messages.Message("ATPP_SurrogateUnexpectedDisconnection".Translate(invertedPawn.LabelShortCap), disconnectedPawn, MessageTypeDefOf.NegativeEvent);

                        //un ou les deux des composantes sont déconnectés ===> on lance la deconnection du SX
                        cso.stopControlledSurrogate(cpawn);
                    }
                }
            }

            //Si hote plus valide alors on arrete le processus et on kill les deux androids
            if(uploadEndingGT != -1 && (recipientDeadOrNull || cpawn.Dead || !emitterConnected || !recipientConnected)) {
                string reason = "";
                if(recipientDeadOrNull) {
                    reason = "ATPP_LetterInterruptedUploadDescCompHostDead".Translate();
                    killSelf = true;
                }

                if(cpawn.Dead) {
                    reason = "ATPP_LetterInterruptedUploadDescCompSourceDead".Translate();
                    if(uploadRecipient != null && !uploadRecipient.Dead) {
                        uploadRecipient.Kill(null, null);
                    }
                }

                if(reason == "") {
                    if(!recipientConnected || !emitterConnected) {
                        reason = "ATPP_LetterInterruptedUploadDescCompDiconnectionError".Translate();

                        killSelf = true;
                        if(uploadRecipient != null && !uploadRecipient.Dead) {
                            uploadRecipient.Kill(null, null);
                        }

                    }
                }

                resetUploadStuff();

                if(killSelf) {
                    if(!cpawn.Dead)
                        cpawn.Kill(null, null);
                }

                Utils.showFailedLetterMindUpload(reason);
            }
        }

        public void initAsSurrogate()
        {
            // on va lui ajouter un hediff afin de le downer en permanence(pas d'hote)
            Pawn cpawn = (Pawn)parent;

            isSurrogate = true;
            addNoRemoteHostHediff();

        }

        public void resetInternalState()
        {
            resetUploadStuff();
        }

        private void resetUploadStuff()
        {
            if(uploadRecipient != null) {
                CompAndroidState cab = uploadRecipient.ATCompState;
                cab.showUploadProgress = false;
                cab.uploadRecipient = null;
            }

            uploadStartGT = 0;
            uploadEndingGT = -1;
            uploadRecipient = null;
        }

        private void OnPermuteConfirmed(Pawn source, Pawn dest)
        {
            //Ajout hediff de transfert aux deux androids
            source.health.AddHediff(DefDatabase<HediffDef>.GetNamed("ATPP_ConsciousnessUpload"));
            dest.health.AddHediff(DefDatabase<HediffDef>.GetNamed("ATPP_ConsciousnessUpload"));

            int CGT = Find.TickManager.TicksGame;
            uploadRecipient = dest;
            uploadStartGT = CGT;
            uploadEndingGT = CGT + Settings.mindUploadHour * 2500;

            CompAndroidState cab = dest.ATCompState;
            cab.showUploadProgress = true;
            cab.uploadRecipient = (Pawn)parent;

            Messages.Message("ATPP_StartUpload".Translate(source.LabelShortCap, dest.LabelShortCap), parent, MessageTypeDefOf.PositiveEvent);
        }

        public bool UseBattery {
            get {
                return useBattery;
            }
        }

        //Stock le signal indiquant si le pawn à été attribué par le systeme de job pour faire du guarding ou non
        public bool useBattery = false;
        public int uploadEndingGT = -1;
        public int uploadStartGT = 0;
        public Pawn uploadRecipient;
        public bool isSurrogate = false;
        public Pawn surrogateController;

        //Sert a identifier les surrogates biologiques
        public bool isOrganic = false;
        public bool isAndroidTIer = false;
        public bool isAndroidWithSkin = false;

        public bool solarFlareEffectApplied = false;

        public bool showUploadProgress = false;

        //Stocke le pawn externe (n'appartenant pas au joueur) controllant le surrogate (le cas des groupes de factions alliés/neutre/ennemis)
        public Pawn externalController;

        public Pawn lastController;

        private CompSkyMind skyMind;

        public string savedName = "";

        public int frameworkNaniteEffectGTEnd = -1;
        public int frameworkNaniteEffectGTStart = -1;

        //AndroidPaintColor
        public int customColor = (int)AndroidPaintColor.Default;

        public int paintingRustGT = -2;
        public bool paintingIsRusted = false;

        public int batteryExplosionEndingGT = -1;
        public int batteryExplosionStartingGT = -1;

        public bool isBlankAndroid = false;

        public Building connectedLWPN;
        public bool connectedLWPNActive = false;
        public bool autoPaintStarted = false;

        public bool TXHurtedHeadSet = false;
        public bool TXHurtedHeadSet2 = false;

        public HairDef hair;

        public bool init = false;

        public bool dontRust = false;
        public int forcedDamageLevel = -1;
    }
}