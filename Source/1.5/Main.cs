using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.Sound;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;


namespace Sleepys_MorePsycasts
{
    //Comp Properties
    public class CompProperties_SLP_AbilityGiveTwoHediff :
       CompProperties_AbilityGiveHediff
    {
        public HediffDef hediffDef2;
    }

    public class CompProperties_SLP_AbilityWordOfContempt : CompProperties_EffectWithDest
    {
        public CompProperties_SLP_AbilityWordOfContempt() => this.compClass = typeof(CompAbilityEffect_SLP_WordOfContempt);
    }

    public class CompProperties_SLP_AbilityWordOfFriendship : CompProperties_EffectWithDest
    {
        public CompProperties_SLP_AbilityWordOfFriendship() => this.compClass = typeof(CompAbilityEffect_SLP_WordOfFriendship);
    }

    public class CompProperties_SLP_AbilityTeleport : CompProperties_EffectWithDest
    {
        public IntRange stunTicks;
        public float maxBodySize = 3.5f;
    }

    public class CompProperties_SLP_AreaEffectLaunchProjectile : CompProperties_AbilityEffect
    {
        public ThingDef projectileDef;
        public float effectPreviewRadius = 1f;

        public CompProperties_SLP_AreaEffectLaunchProjectile() => this.compClass = typeof(CompAbilityEffect_SLP_AreaEffectLaunchProjectile);
    }

    public class CompProperties_SLP_AbilitySpawnOffCorpse : CompProperties_AbilityEffect
    {
        public ThingDef thingDef;
        public bool allowOnBuildings = true;
        public bool sendSkipSignal = true;
        public bool dropGear = false;

        public CompProperties_SLP_AbilitySpawnOffCorpse() => this.compClass = typeof(CompAbilityEffect_SLP_SpawnOffCorpse);
    }

    public class CompProperties_SLP_AbilitySpawnBrickFromChunk : CompProperties_AbilityEffect
    {
        public bool sendSkipSignal = true;

        public CompProperties_SLP_AbilitySpawnBrickFromChunk() => this.compClass = typeof(CompAbilityEffect_SLP_SpawnBrickFromChunk);
    }


    //Comp Ability Effect
    public class SLP_CompAbilityEffect_Ignite : CompAbilityEffect
    {
        public new CompProperties_AbilityEffect Props => (CompProperties_AbilityEffect)this.props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn casterPawn = this.parent.pawn;
            if (target == (LocalTargetInfo)(Thing)null)
                Log.Message("Tried to apply ignite to nothing.");
            else
                FireUtility.TryStartFireIn(target.Cell, this.parent.pawn.Map, 0.4f, casterPawn);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest) => (double)FireUtility.ChanceToStartFireIn(target.Cell, this.parent.pawn.Map) > 0.0;
    }

    public class CompAbilityEffect_SLP_GiveTwoHediff : CompAbilityEffect_WithDuration
    {
        public new CompProperties_SLP_AbilityGiveTwoHediff Props => (CompProperties_SLP_AbilityGiveTwoHediff)this.props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (!this.Props.onlyApplyToSelf && this.Props.applyToTarget)
                this.ApplyInner(target.Pawn, this.parent.pawn);
            if (!this.Props.applyToSelf && !this.Props.onlyApplyToSelf)
                return;
            this.ApplyInner(this.parent.pawn, target.Pawn);
        }

        protected void ApplyInner(Pawn target, Pawn other)
        {
            if (target == null)
                return;

            if (this.Props.replaceExisting)
            {
                Hediff firstHediffOfDef = target.health.hediffSet.GetFirstHediffOfDef(this.Props.hediffDef);
                if (firstHediffOfDef != null)
                    target.health.RemoveHediff(firstHediffOfDef);
            }
            if (this.Props.replaceExisting)
            {
                Hediff firstHediffOfDef2 = target.health.hediffSet.GetFirstHediffOfDef(this.Props.hediffDef2);
                if (firstHediffOfDef2 != null)
                    target.health.RemoveHediff(firstHediffOfDef2);
            }

            Hediff hediff = HediffMaker.MakeHediff(this.Props.hediffDef, target, this.Props.onlyBrain ? target.health.hediffSet.GetBrain() : (BodyPartRecord)null);
            HediffComp_Disappears comp1 = hediff.TryGetComp<HediffComp_Disappears>();
            if (comp1 != null)
                comp1.ticksToDisappear = this.GetDurationSeconds(target).SecondsToTicks();
            if ((double)this.Props.severity >= 0.0)
                hediff.Severity = this.Props.severity;
            HediffComp_Link comp2 = hediff.TryGetComp<HediffComp_Link>();
            if (comp2 != null)
            {
                comp2.other = (Thing)other;
                comp2.drawConnection = target == this.parent.pawn;
            }
            target.health.AddHediff(hediff);

            Hediff hediff2 = HediffMaker.MakeHediff(this.Props.hediffDef2, target, this.Props.onlyBrain ? target.health.hediffSet.GetBrain() : (BodyPartRecord)null);
            HediffComp_Disappears comp1a = hediff2.TryGetComp<HediffComp_Disappears>();
            if (comp1a != null)
                comp1a.ticksToDisappear = this.GetDurationSeconds(target).SecondsToTicks();
            if ((double)this.Props.severity >= 0.0)
                hediff2.Severity = this.Props.severity;
            HediffComp_Link comp2a = hediff2.TryGetComp<HediffComp_Link>();
            if (comp2a != null)
            {
                comp2a.other = (Thing)other;
                comp2a.drawConnection = target == this.parent.pawn;
            }
            target.health.AddHediff(hediff2);
        }
    }

    public class CompAbilityEffect_SLP_FertilitySkip : CompAbilityEffect
    {
        public static IEnumerable<IntVec3> AffectedCells(
        LocalTargetInfo target,
        Map map,
        Ability parent)
        {
            if (!target.Cell.Filled(parent.pawn.Map))
            {
                foreach (IntVec3 intVec3 in GenRadial.RadialCellsAround(target.Cell, parent.def.EffectRadius, true))
                {
                    IntVec3 item = intVec3;
                    if (item.InBounds(map) && GenSight.LineOfSightToEdges(target.Cell, item, map, true))
                        yield return item;
                    item = new IntVec3();
                }
            }
        }

        public new CompProperties_AbilityEffect Props => (CompProperties_AbilityEffect)this.props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = this.parent.pawn.Map;
            foreach (IntVec3 affectedCell in AffectedCells(target, map, this.parent))
            {
                TerrainDef terrain = map.terrainGrid.TerrainAt(affectedCell);
                if (!terrain.IsRiver)
                {
                    /*
                    if (terrain.IsWater)
                    {
                        TerrainDef named = DefDatabase<TerrainDef>.GetNamed("Mud");
                        if (named != null)
                            map.terrainGrid.SetTerrain(affectedCell, named);
                        else
                            map.terrainGrid.SetTerrain(affectedCell, TerrainDefOf.Sand);
                    }
                    else
                    */
                    if (!terrain.IsWater)
                    {
                        List<TerrainDef> terrainDefList = new List<TerrainDef>();
                        terrainDefList.Add(TerrainDefOf.Gravel);
                        terrainDefList.Add(TerrainDefOf.Soil);
                        foreach (TerrainThreshold terrainThreshold in map.Biome.terrainsByFertility)
                        {
                            if (!terrainDefList.Contains(terrainThreshold.terrain))
                                terrainDefList.Add(terrainThreshold.terrain);
                        }
                        foreach (TerrainPatchMaker terrainPatchMaker in map.Biome.terrainPatchMakers)
                        {
                            foreach (TerrainThreshold threshold in terrainPatchMaker.thresholds)
                            {
                                if (!terrainDefList.Contains(threshold.terrain))
                                    terrainDefList.Add(threshold.terrain);
                            }
                        }
                        IOrderedEnumerable<TerrainDef> source = terrainDefList.FindAll((Predicate<TerrainDef>)(e => (double)e.fertility > (double)terrain.fertility && (double)e.fertility <= 1.0)).OrderBy<TerrainDef, float>((Func<TerrainDef, float>)(e => e.fertility));
                        if (source.Count<TerrainDef>() != 0)
                        {
                            TerrainDef newTerr = source.First<TerrainDef>();
                            map.terrainGrid.SetTerrain(affectedCell, newTerr);
                        }
                    }
                }
            }
        }
    }

    public class SLP_CompAbilityEffect_Recondition : CompAbilityEffect
    {
        public new CompProperties_AbilityEffect Props => (CompProperties_AbilityEffect)this.props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = this.parent.pawn.Map;
            Thing thing = target.Thing == null ? target.Cell.GetThingList(map).RandomElement<Thing>() : target.Thing;

            int num = (((thing.MaxHitPoints - thing.HitPoints) / 2) + (thing.MaxHitPoints / 20));
            int CheckOverMaxHP = num + thing.HitPoints;

            if (CheckOverMaxHP > thing.MaxHitPoints)
            {
                thing.HitPoints = thing.MaxHitPoints;
            }
            else
            {
                thing.HitPoints += num;
            }

            SoundDefOf.Psycast_Skip_Exit.PlayOneShot((SoundInfo)new TargetInfo(target.Cell, map));
            SLP_Utilities.SpawnFleck(target, FleckDefOf.PsycastSkipInnerExit, map);
            SLP_Utilities.SpawnFleck(target, FleckDefOf.PsycastSkipOuterRingExit, map);
            SLP_Utilities.SpawnEffecter(target, EffecterDefOf.Skip_Exit, map, 60, this.parent);
        }
    }

    public class SLP_CompAbilityEffect_CleanSkip : CompAbilityEffect
    {
        public new CompProperties_AbilityEffect Props => (CompProperties_AbilityEffect)this.props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = this.parent.pawn.Map;
            foreach (IntVec3 affectedCell in SLP_Utilities.AffectedCells(target, map, this.parent))
            {
                List<Thing> thingList = affectedCell.GetThingList(map);
                for (int index = 0; index < thingList.Count; ++index)
                {
                    if (thingList[index] is Filth filth)
                    {
                        filth.Destroy();
                        SoundDefOf.Psycast_Skip_Exit.PlayOneShot((SoundInfo)new TargetInfo(affectedCell, map));
                        SLP_Utilities.SpawnFleck(new LocalTargetInfo(affectedCell), FleckDefOf.PsycastSkipInnerExit, map);
                        SLP_Utilities.SpawnFleck(new LocalTargetInfo(affectedCell), FleckDefOf.PsycastSkipOuterRingExit, map);
                        SLP_Utilities.SpawnEffecter(new LocalTargetInfo(affectedCell), EffecterDefOf.Skip_Exit, map, 60, this.parent);
                    }
                }
            }
        }
    }

    public class SLP_CompAbilityEffect_Resurrect : CompAbilityEffect
    {
        public new CompProperties_Resurrect Props => (CompProperties_Resurrect)this.props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn innerPawn = ((Corpse)target.Thing).InnerPawn;
            if (!SLP_ResurrectionUtility.TryResurrectWithSideEffects(innerPawn))
                return;
            Messages.Message((string)"MessagePawnResurrected".Translate((NamedArgument)(Thing)innerPawn), (LookTargets)(Thing)innerPawn, MessageTypeDefOf.PositiveEvent);
            MoteMaker.MakeAttachedOverlay((Thing)innerPawn, ThingDefOf.Mote_ResurrectFlash, Vector3.zero);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!target.HasThing || !(target.Thing is Corpse thing) || thing.GetRotStage() != RotStage.Dessicated)
                return base.Valid(target, throwMessages);
            if (throwMessages)
                Messages.Message((string)"MessageCannotResurrectDessicatedCorpse".Translate(), (LookTargets)(Thing)thing, MessageTypeDefOf.RejectInput, false);
            return false;
        }
    }

    public class CompAbilityEffect_SLP_WordOfContempt : CompAbilityEffect_WithDest
    {
        private const int MinAge = 3;

        public override bool HideTargetPawnTooltip => true;

        public override TargetingParameters targetParams => new TargetingParameters()
        {
            canTargetSelf = true,
            canTargetBuildings = false,
            canTargetAnimals = false,
            canTargetMechs = false
        };

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = target.Pawn;
            Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(SLP_HediffDefOf.SLP_PsychicContempt);
            if (firstHediffOfDef != null)
                pawn.health.RemoveHediff(firstHediffOfDef);
            SLP_Hediff_PsychicContempt hd = (SLP_Hediff_PsychicContempt)HediffMaker.MakeHediff(SLP_HediffDefOf.SLP_PsychicContempt, pawn, pawn.health.hediffSet.GetBrain());
            hd.target = dest.Thing;
            HediffComp_Disappears comp = hd.TryGetComp<HediffComp_Disappears>();
            if (comp != null)
            {
                float numSeconds = this.parent.def.EffectDuration(this.parent.pawn) * pawn.GetStatValue(StatDefOf.PsychicSensitivity);
                comp.ticksToDisappear = numSeconds.SecondsToTicks();
            }
            pawn.health.AddHediff((Hediff)hd);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest) => this.Valid(target, false);

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            Pawn pawn1 = this.selectedTarget.Pawn;
            Pawn pawn2 = target.Pawn;
            if (pawn1 == pawn2)
                return false;
            if ((double)pawn1.ageTracker.AgeBiologicalYearsFloat < MinAge)
            {
                Messages.Message((string)("CannotUseAbility".Translate((NamedArgument)this.parent.def.label) + ": " + "AbilityCantApplyTooYoung".Translate((NamedArgument)(Thing)pawn1)), (LookTargets)(Thing)pawn1, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if ((double)pawn2.ageTracker.AgeBiologicalYearsFloat < MinAge)
            {
                Messages.Message((string)("CannotUseAbility".Translate((NamedArgument)this.parent.def.label) + ": " + "AbilityCantApplyTooYoung".Translate((NamedArgument)(Thing)pawn2)), (LookTargets)(Thing)pawn2, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return base.ValidateTarget(target, true);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = target.Pawn;
            if (pawn != null)
            {
                if (!AbilityUtility.ValidateNoMentalState(pawn, throwMessages, this.parent))
                    return false;
            }
            return true;
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target) => this.selectedTarget.IsValid ? (string)"PsychicContemptFor".Translate() : (string)"PsychicContemptInduceIn".Translate();
    }

    public class CompAbilityEffect_SLP_WordOfFriendship : CompAbilityEffect_WithDest
    {
        private const int MinAge = 3;

        public override bool HideTargetPawnTooltip => true;

        public override TargetingParameters targetParams => new TargetingParameters()
        {
            canTargetSelf = true,
            canTargetBuildings = false,
            canTargetAnimals = false,
            canTargetMechs = false
        };

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = target.Pawn;
            Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(SLP_HediffDefOf.SLP_PsychicFriendship);
            if (firstHediffOfDef != null)
                pawn.health.RemoveHediff(firstHediffOfDef);
            SLP_Hediff_PsychicFriendship hd = (SLP_Hediff_PsychicFriendship)HediffMaker.MakeHediff(SLP_HediffDefOf.SLP_PsychicFriendship, pawn, pawn.health.hediffSet.GetBrain());
            hd.target = dest.Thing;
            HediffComp_Disappears comp = hd.TryGetComp<HediffComp_Disappears>();
            if (comp != null)
            {
                float numSeconds = this.parent.def.EffectDuration(this.parent.pawn) * pawn.GetStatValue(StatDefOf.PsychicSensitivity);
                comp.ticksToDisappear = numSeconds.SecondsToTicks();
            }
            pawn.health.AddHediff((Hediff)hd);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest) => this.Valid(target, false);

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            Pawn pawn1 = this.selectedTarget.Pawn;
            Pawn pawn2 = target.Pawn;
            if (pawn1 == pawn2)
                return false;
            if ((double)pawn1.ageTracker.AgeBiologicalYearsFloat < MinAge)
            {
                Messages.Message((string)("CannotUseAbility".Translate((NamedArgument)this.parent.def.label) + ": " + "AbilityCantApplyTooYoung".Translate((NamedArgument)(Thing)pawn1)), (LookTargets)(Thing)pawn1, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if ((double)pawn2.ageTracker.AgeBiologicalYearsFloat < MinAge)
            {
                Messages.Message((string)("CannotUseAbility".Translate((NamedArgument)this.parent.def.label) + ": " + "AbilityCantApplyTooYoung".Translate((NamedArgument)(Thing)pawn2)), (LookTargets)(Thing)pawn2, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return base.ValidateTarget(target, true);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = target.Pawn;
            if (pawn != null)
            {
                if (!AbilityUtility.ValidateNoMentalState(pawn, throwMessages, this.parent))
                    return false;
            }
            return true;
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target) => this.selectedTarget.IsValid ? (string)"PsychicFriendshipFor".Translate() : (string)"PsychicFriendshipInduceIn".Translate();
    }

    public class CompAbilityEffect_SLP_Flashstep : CompAbilityEffect_WithDest
    {
        //public Thing Caster => (Thing)this.parent.pawn; // New 1.0.3.1
        public Thing GetCaster()
        {
            return (Thing)this.parent.pawn; // New 1.0.3.1
        }

        public new CompProperties_SLP_AbilityTeleport Props
        {
            get
            {
                return (CompProperties_SLP_AbilityTeleport)this.props;
            }
        }

        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            yield return new PreCastAction
            {
                action = delegate (LocalTargetInfo t, LocalTargetInfo d)
                {
                    if (!this.parent.def.HasAreaOfEffect)
                    {
                        Pawn pawn = t.Pawn;
                        if (pawn != null)
                        {
                            FleckCreationData dataAttachedOverlay = FleckMaker.GetDataAttachedOverlay(pawn, FleckDefOf.PsycastSkipFlashEntry, new Vector3(-0.5f, 0f, -0.5f), 1f, -1f);
                            dataAttachedOverlay.link.detachAfterTicks = 5;
                            pawn.Map.flecks.CreateFleck(dataAttachedOverlay);
                        }
                        else
                        {
                            FleckMaker.Static(t.CenterVector3, this.parent.pawn.Map, FleckDefOf.PsycastSkipFlashEntry, 1f);
                        }
                        FleckMaker.Static(d.Cell, this.parent.pawn.Map, FleckDefOf.PsycastSkipInnerExit, 1f);
                    }
                    if (this.Props.destination != AbilityEffectDestination.RandomInRange)
                    {
                        FleckMaker.Static(d.Cell, this.parent.pawn.Map, FleckDefOf.PsycastSkipOuterRingExit, 1f);
                    }
                    if (!this.parent.def.HasAreaOfEffect)
                    {
                        SLP_SoundDefOf.SLP_Psycast_Shunpo.PlayOneShot(new TargetInfo(d.Cell, this.parent.pawn.Map, false));
                        SLP_SoundDefOf.SLP_Psycast_Shunpo2.PlayOneShot(new TargetInfo(t.Cell, this.parent.pawn.Map, false));
                    }
                },
                ticksAwayFromCast = 5
            };
            yield break;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (target.HasThing)
            {
                base.Apply(target, dest);
                LocalTargetInfo destination = base.GetDestination(dest.IsValid ? dest : target);
                if (destination.IsValid)
                {
                    Pawn pawn = this.parent.pawn;
                    if (!this.parent.def.HasAreaOfEffect)
                    {
                        this.parent.AddEffecterToMaintain(EffecterDefOf.Skip_Entry.Spawn(target.Thing, pawn.Map, 1f), target.Thing.Position, 60, null);
                    }
                    else
                    {
                        this.parent.AddEffecterToMaintain(EffecterDefOf.Skip_EntryNoDelay.Spawn(target.Thing, pawn.Map, 1f), target.Thing.Position, 60, null);
                    }
                    if (this.Props.destination == AbilityEffectDestination.Selected)
                    {
                        this.parent.AddEffecterToMaintain(EffecterDefOf.Skip_Exit.Spawn(destination.Cell, pawn.Map, 1f), destination.Cell, 60, null);
                    }
                    else
                    {
                        this.parent.AddEffecterToMaintain(EffecterDefOf.Skip_ExitNoDelay.Spawn(destination.Cell, pawn.Map, 1f), destination.Cell, 60, null);
                    }
                    CompCanBeDormant compCanBeDormant = target.Thing.TryGetComp<CompCanBeDormant>();
                    if (compCanBeDormant != null)
                    {
                        compCanBeDormant.WakeUp();
                    }
                    target.Thing.Position = destination.Cell;
                    Pawn pawn2 = target.Thing as Pawn;
                    if (pawn2 != null)
                    {
                        pawn2.stances.stunner.StunFor(this.Props.stunTicks.RandomInRange, this.parent.pawn, false, false);
                        pawn2.Notify_Teleported(true, true);
                        CompAbilityEffect_Teleport.SendSkipUsedSignal(pawn2.Position, pawn2);
                    }
                    if (this.Props.destClamorType != null)
                    {
                        GenClamor.DoClamor(pawn, target.Cell, (float)this.Props.destClamorRadius, this.Props.destClamorType);
                    }
                }
            }
        }

        public override bool CanHitTarget(LocalTargetInfo target)
        {
            return base.CanPlaceSelectedTargetAt(target) && base.CanHitTarget(target);
        }

        public override bool Valid(LocalTargetInfo target, bool showMessages = true)
        {
            AcceptanceReport report = this.CanSkipTarget(target);
            if (!report)
            {
                Pawn pawn;
                if (showMessages && !report.Reason.NullOrEmpty() && (pawn = (target.Thing as Pawn)) != null)
                {
                    Messages.Message("CannotSkipTarget".Translate(pawn.Named("PAWN")) + ": " + report.Reason, pawn, MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return base.Valid(target, showMessages);
        }

        private AcceptanceReport CanSkipTarget(LocalTargetInfo target)
        {
            Pawn pawn;
            if ((pawn = (target.Thing as Pawn)) != null)
            {
                if (pawn.BodySize > this.Props.maxBodySize)
                {
                    return "CannotSkipTargetTooLarge".Translate();
                }
                if (pawn.kindDef.skipResistant)
                {
                    return "CannotSkipTargetPsychicResistant".Translate();
                }
                // New 1.0.3.1
                if (GetCaster() != pawn)
                {
                    return "CannotSkipTargetIsntCaster".Translate();
                }
            }
            return true;
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            return this.CanSkipTarget(target).Reason;
        }

        public static void SendSkipUsedSignal(LocalTargetInfo target, Thing initiator)
        {
            Find.SignalManager.SendSignal(new Signal(CompAbilityEffect_Teleport.SkipUsedSignalTag, target.Named("POSITION"), initiator.Named("SUBJECT")));
        }

        public static string SkipUsedSignalTag = "CompAbilityEffect.SkipUsed";
    }

    public class CompAbilityEffect_SLP_Skipstep : CompAbilityEffect_WithDest
    {

        //public Thing Caster => (Thing)this.parent.pawn; // New 1.0.3.1
        public Thing GetCaster()
        {
            return (Thing)this.parent.pawn; // New 1.0.3.1
        }

        public new CompProperties_SLP_AbilityTeleport Props
        {
            get
            {
                return (CompProperties_SLP_AbilityTeleport)this.props;
            }
        }

        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            yield return new PreCastAction
            {
                action = delegate (LocalTargetInfo t, LocalTargetInfo d)
                {
                    if (!this.parent.def.HasAreaOfEffect)
                    {
                        Pawn pawn = t.Pawn;
                        if (pawn != null)
                        {
                            FleckCreationData dataAttachedOverlay = FleckMaker.GetDataAttachedOverlay(pawn, FleckDefOf.PsycastSkipFlashEntry, new Vector3(-0.5f, 0f, -0.5f), 1f, -1f);
                            dataAttachedOverlay.link.detachAfterTicks = 5;
                            pawn.Map.flecks.CreateFleck(dataAttachedOverlay);
                        }
                        else
                        {
                            FleckMaker.Static(t.CenterVector3, this.parent.pawn.Map, FleckDefOf.PsycastSkipFlashEntry, 1f);
                        }
                        FleckMaker.Static(d.Cell, this.parent.pawn.Map, FleckDefOf.PsycastSkipInnerExit, 1f);
                    }
                    if (this.Props.destination != AbilityEffectDestination.RandomInRange)
                    {
                        FleckMaker.Static(d.Cell, this.parent.pawn.Map, FleckDefOf.PsycastSkipOuterRingExit, 1f);
                    }
                    if (!this.parent.def.HasAreaOfEffect)
                    {
                        SLP_SoundDefOf.SLP_Psycast_Sonido.PlayOneShot(new TargetInfo(d.Cell, this.parent.pawn.Map, false));
                    }
                },
                ticksAwayFromCast = 5
            };
            yield break;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (target.HasThing)
            {
                base.Apply(target, dest);
                LocalTargetInfo destination = base.GetDestination(dest.IsValid ? dest : target);
                if (destination.IsValid)
                {
                    Pawn pawn = this.parent.pawn;
                    if (!this.parent.def.HasAreaOfEffect)
                    {
                        this.parent.AddEffecterToMaintain(EffecterDefOf.Skip_Entry.Spawn(target.Thing, pawn.Map, 1f), target.Thing.Position, 60, null);
                    }
                    else
                    {
                        this.parent.AddEffecterToMaintain(EffecterDefOf.Skip_EntryNoDelay.Spawn(target.Thing, pawn.Map, 1f), target.Thing.Position, 60, null);
                    }
                    if (this.Props.destination == AbilityEffectDestination.Selected)
                    {
                        this.parent.AddEffecterToMaintain(EffecterDefOf.Skip_Exit.Spawn(destination.Cell, pawn.Map, 1f), destination.Cell, 60, null);
                    }
                    else
                    {
                        this.parent.AddEffecterToMaintain(EffecterDefOf.Skip_ExitNoDelay.Spawn(destination.Cell, pawn.Map, 1f), destination.Cell, 60, null);
                    }
                    CompCanBeDormant compCanBeDormant = target.Thing.TryGetComp<CompCanBeDormant>();
                    if (compCanBeDormant != null)
                    {
                        compCanBeDormant.WakeUp();
                    }
                    target.Thing.Position = destination.Cell;
                    Pawn pawn2 = target.Thing as Pawn;
                    if (pawn2 != null)
                    {
                        pawn2.stances.stunner.StunFor(this.Props.stunTicks.RandomInRange, this.parent.pawn, false, false);
                        pawn2.Notify_Teleported(true, true);
                        CompAbilityEffect_Teleport.SendSkipUsedSignal(pawn2.Position, pawn2);
                    }
                    if (this.Props.destClamorType != null)
                    {
                        GenClamor.DoClamor(pawn, target.Cell, (float)this.Props.destClamorRadius, this.Props.destClamorType);
                    }
                }
            }
        }

        public override bool CanHitTarget(LocalTargetInfo target)
        {
            return base.CanPlaceSelectedTargetAt(target) && base.CanHitTarget(target);
        }

        public override bool Valid(LocalTargetInfo target, bool showMessages = true)
        {
            AcceptanceReport report = this.CanSkipTarget(target);
            if (!report)
            {
                Pawn pawn;
                if (showMessages && !report.Reason.NullOrEmpty() && (pawn = (target.Thing as Pawn)) != null)
                {
                    Messages.Message("CannotSkipTarget".Translate(pawn.Named("PAWN")) + ": " + report.Reason, pawn, MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return base.Valid(target, showMessages);
        }

        private AcceptanceReport CanSkipTarget(LocalTargetInfo target)
        {
            Pawn pawn;
            if ((pawn = (target.Thing as Pawn)) != null)
            {
                if (pawn.BodySize > this.Props.maxBodySize)
                {
                    return "CannotSkipTargetTooLarge".Translate();
                }
                if (pawn.kindDef.skipResistant)
                {
                    return "CannotSkipTargetPsychicResistant".Translate();
                }
                // New 1.0.3.1
                if (GetCaster() != pawn)
                {
                    return "CannotSkipTargetIsntCaster".Translate();
                }
            }
            return true;
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            return this.CanSkipTarget(target).Reason;
        }

        public static void SendSkipUsedSignal(LocalTargetInfo target, Thing initiator)
        {
            Find.SignalManager.SendSignal(new Signal(CompAbilityEffect_Teleport.SkipUsedSignalTag, target.Named("POSITION"), initiator.Named("SUBJECT")));
        }

        public static string SkipUsedSignalTag = "CompAbilityEffect.SkipUsed";
    }

    public class CompAbilityEffect_SLP_AreaEffectLaunchProjectile : CompAbilityEffect
    {
        public CompProperties_SLP_AreaEffectLaunchProjectile Props => (CompProperties_SLP_AreaEffectLaunchProjectile)this.props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            this.LaunchProjectile(target);
        }

        private void LaunchProjectile(LocalTargetInfo target)
        {
            if (this.Props.projectileDef == null)
                return;
            Pawn pawn = this.parent.pawn;
            ((Projectile)GenSpawn.Spawn(this.Props.projectileDef, pawn.Position, pawn.Map)).Launch((Thing)pawn, pawn.DrawPos, target, target, ProjectileHitFlags.IntendedTarget);
        }

        public override bool AICanTargetNow(LocalTargetInfo target) => target.Pawn != null;

        public override void DrawEffectPreview(LocalTargetInfo target) => GenDraw.DrawRadiusRing(target.Cell, this.Props.effectPreviewRadius);
    }

    public class CompAbilityEffect_SLP_SpawnOffCorpse : CompAbilityEffect
    {
        public CompProperties_SLP_AbilitySpawnOffCorpse Props => (CompProperties_SLP_AbilitySpawnOffCorpse)this.props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn innerPawn = ((Corpse)target.Thing).InnerPawn;
            Map map = this.parent.pawn.Map;
            Thing thing = target.Thing == null ? target.Cell.GetThingList(map).RandomElement<Thing>() : target.Thing;
            int healthFactor = thing.HitPoints;
            int meatQtyAmount = (int)thing.GetStatValue(StatDefOf.MeatAmount);
            float nutritionQtyAmount = thing.GetStatValue(StatDefOf.Nutrition);

            if (this.Props.dropGear)
                innerPawn.Strip(false);
            if (innerPawn.Corpse != null)
                innerPawn.Corpse.Destroy(DestroyMode.Vanish);
            else
                innerPawn.Destroy(DestroyMode.Vanish);

            int i = 0;
            float chemAmount = (healthFactor * ((meatQtyAmount / 10) + (nutritionQtyAmount * 6)) / 100) + 5;
            // float chemAmount = ((healthFactor * (meatQtyAmount / 3))/100) + 5;

            while (i < chemAmount)
            {
                GenSpawn.Spawn(this.Props.thingDef, target.Cell, this.parent.pawn.Map);
                i++;
            }
            
            if (!this.Props.sendSkipSignal)
                return;
            CompAbilityEffect_Teleport.SendSkipUsedSignal(target, (Thing)this.parent.pawn);
        }

        /*
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!target.HasThing || !(target.Thing is Corpse thing) || thing.GetRotStage() != RotStage.Dessicated)
                return base.Valid(target, throwMessages);
            if (throwMessages)
                Messages.Message((string)"SLPMessageCannotTurnIntoChemfuel".Translate(), (LookTargets)(Thing)thing, MessageTypeDefOf.RejectInput, false);
            return false;
        }
        */
    }

    public class CompAbilityEffect_SLP_SpawnBrickFromChunk : CompAbilityEffect
    {
        public CompProperties_SLP_AbilitySpawnBrickFromChunk Props => (CompProperties_SLP_AbilitySpawnBrickFromChunk)this.props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Thing thing1 = target.Thing;
            int i = 0;
            int spawnBricksQty = 15;

            if (thing1.def == SLP_ChunksDefOf.ChunkSandstone)
            {
                thing1.Destroy(DestroyMode.Vanish);
                while (i < spawnBricksQty)
                {
                    GenSpawn.Spawn(SLP_ChunksDefOf.BlocksSandstone, target.Cell, this.parent.pawn.Map);
                    i++;
                }

                if (!this.Props.sendSkipSignal)
                    return;
                CompAbilityEffect_Teleport.SendSkipUsedSignal(target, (Thing)this.parent.pawn);
            }

            if (thing1.def == SLP_ChunksDefOf.ChunkGranite)
            {
                thing1.Destroy(DestroyMode.Vanish);
                while (i < spawnBricksQty)
                {
                    GenSpawn.Spawn(SLP_ChunksDefOf.BlocksGranite, target.Cell, this.parent.pawn.Map);
                    i++;
                }

                if (!this.Props.sendSkipSignal)
                    return;
                CompAbilityEffect_Teleport.SendSkipUsedSignal(target, (Thing)this.parent.pawn);
            }

            if (thing1.def == SLP_ChunksDefOf.ChunkLimestone)
            {
                thing1.Destroy(DestroyMode.Vanish);
                while (i < spawnBricksQty)
                {
                    GenSpawn.Spawn(SLP_ChunksDefOf.BlocksLimestone, target.Cell, this.parent.pawn.Map);
                    i++;
                }

                if (!this.Props.sendSkipSignal)
                    return;
                CompAbilityEffect_Teleport.SendSkipUsedSignal(target, (Thing)this.parent.pawn);
            }

            if (thing1.def == SLP_ChunksDefOf.ChunkSlate)
            {
                thing1.Destroy(DestroyMode.Vanish);
                while (i < spawnBricksQty)
                {
                    GenSpawn.Spawn(SLP_ChunksDefOf.BlocksSlate, target.Cell, this.parent.pawn.Map);
                    i++;
                }

                if (!this.Props.sendSkipSignal)
                    return;
                CompAbilityEffect_Teleport.SendSkipUsedSignal(target, (Thing)this.parent.pawn);
            }

            if (thing1.def == SLP_ChunksDefOf.ChunkMarble)
            {
                thing1.Destroy(DestroyMode.Vanish);
                while (i < spawnBricksQty)
                {
                    GenSpawn.Spawn(SLP_ChunksDefOf.BlocksMarble, target.Cell, this.parent.pawn.Map);
                    i++;
                }

                if (!this.Props.sendSkipSignal)
                    return;
                CompAbilityEffect_Teleport.SendSkipUsedSignal(target, (Thing)this.parent.pawn);
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Thing thing = target.Thing;
            if (thing.def == SLP_ChunksDefOf.ChunkSandstone || thing.def == SLP_ChunksDefOf.ChunkGranite || thing.def == SLP_ChunksDefOf.ChunkLimestone || thing.def == SLP_ChunksDefOf.ChunkSlate || thing.def == SLP_ChunksDefOf.ChunkMarble)
                return base.Valid(target, throwMessages);
            if (throwMessages)
                Messages.Message((string)"SLPMessageCannotTurnIntoBricks".Translate(), (LookTargets)(Thing)thing, MessageTypeDefOf.RejectInput, false);
            return false;
        }
    }


    //Dart Psycast
    public class CompAbilityEffect_SLP_Dart : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            int probabilityChunks = UnityEngine.Random.Range(1, 11);
            int numOfChunks;
            if (probabilityChunks < 3) //20% Chance for 2 Chunks
            {
                numOfChunks = 2;
            }
            else
            {
                numOfChunks = 1; //80% Chance for 1 Chunk
            }

            int probabilityIntercept = UnityEngine.Random.Range(1, 11);
            int interceptType;
            if (probabilityIntercept < 4)
            {
                interceptType = 1; //30% Chance for Meteorite
            }
            else
            {
                interceptType = 2; //70% Chance for Ship Chunks
            }

            bool targetValid = Valid(target);
            if (targetValid == true)
            {
                if (interceptType == 2)
                {
                    SLP_SpawnShipChunks(target.Cell, this.parent.pawn.Map, numOfChunks);
                }
                if (interceptType == 1)
                {
                    SLP_SpawnMeteorite(target.Cell, this.parent.pawn.Map);
                }

            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.Cell.Roofed(this.parent.pawn.Map))
            {
                if (throwMessages)
                    Messages.Message((string)("CannotUseAbility".Translate((NamedArgument)this.parent.def.label) + ": " + "AbilityRoofed".Translate()), (LookTargets)target.ToTargetInfo(this.parent.pawn.Map), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            foreach (IntVec3 item in GenAdj.OccupiedRect(target.Cell, Rot4.North, new IntVec2(2, 2)))
            {
                RoofDef roof = item.GetRoof(this.parent.pawn.Map);
                if (roof != null && roof.isThickRoof)
                {
                    if (throwMessages)
                        Messages.Message((string)("CannotUseAbility".Translate((NamedArgument)this.parent.def.label) + ": " + "AbilityNotEnoughFreeSpace".Translate()), (LookTargets)target.ToTargetInfo(this.parent.pawn.Map), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
            }
            return true;
        }


        private void SLP_SpawnShipChunks(IntVec3 firstChunkPos, Map map, int count)
        {
            this.SpawnShipChunk(firstChunkPos, map);
            for (int index = 0; index < count - 1; ++index)
            {
                IntVec3 pos;
                if (this.TryFindShipChunkDropCell(firstChunkPos, map, 5, out pos))
                {
                    if (!pos.Roofed(this.parent.pawn.Map))
                        this.SpawnShipChunk(pos, map);
                }

            }
        }

        private void SLP_SpawnMeteorite(IntVec3 firstChunkPos, Map map)
        {
            List<Thing> contents = ThingSetMakerDefOf.Meteorite.root.Generate();
            this.SpawnMeteorite(firstChunkPos, map, contents);
        }

        private void SpawnMeteorite(IntVec3 pos, Map map, List<Thing> contents)
        {
            SkyfallerMaker.SpawnSkyfaller(ThingDefOf.MeteoriteIncoming, contents, pos, map);
        }

        private void SpawnShipChunk(IntVec3 pos, Map map)
        {
            SkyfallerMaker.SpawnSkyfaller(ThingDefOf.ShipChunkIncoming, ThingDefOf.ShipChunk, pos, map);
        }

        private bool TryFindShipChunkDropCell(IntVec3 nearLoc, Map map, int maxDist, out IntVec3 pos)
        {
            return CellFinderLoose.TryFindSkyfallerCell(ThingDefOf.ShipChunkIncoming, map, out pos, 10, nearLoc, maxDist, false);
        }
    }


    //Hediff
    public class HediffCompProperties_SLP_NeedOffset : HediffCompProperties
    {
        public NeedDef need;
        public float offset;

        public HediffCompProperties_SLP_NeedOffset() => this.compClass = typeof(SLP_HediffComp_AdjustNeed);
    }

    public class SLP_HediffComp_AdjustNeed : HediffComp
    {

        private Need needCached;
        public HediffCompProperties_SLP_NeedOffset Props => (HediffCompProperties_SLP_NeedOffset)this.props;

        private Need Need
        {
            get
            {
                if (this.needCached == null)
                    this.needCached = this.Pawn.needs.TryGetNeed(this.Props.need);
                return this.needCached;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (this.Need == null)
                return;
            this.Need.CurLevel += this.Props.offset;
        }
        /*
        private SLP_HediffCompProperties_NeedOffset Props => (SLP_HediffCompProperties_NeedOffset)this.props;

        public void SLP_AdjustNeed(Pawn pawn)
        {
            if (pawn.needs == null)
                return;
            Need need = pawn.needs.TryGetNeed(this.Props.need);
            if (need == null)
                return;
            float offset = this.Props.offset;
            need.CurLevel += offset;
        */
    }

    public class SLP_HediffComp_ScarsHealing : HediffComp
    {

        public override void CompPostTick(ref float severityAdjustment)
        {
            float healrate = 0.2f;
            base.CompPostTick(ref severityAdjustment);
            Hediff_Injury hediff_Injury = SLP_Utilities.SLP_FindPermanentInjury(base.Pawn);
            if (hediff_Injury == null)
            {
                severityAdjustment = -1;
            }

            if (base.Pawn.IsHashIntervalTick(600))
                hediff_Injury.Heal(healrate);
        }
    }

    public class SLP_Hediff_PsychicContempt : HediffWithTarget
    {
        public override string LabelBase => base.LabelBase + " " + this.def.targetPrefix + " " + this.target?.LabelShortCap;

        public override void Notify_RelationAdded(Pawn otherPawn, PawnRelationDef relationDef)
        {
            if (otherPawn != this.target || relationDef != PawnRelationDefOf.Lover && relationDef != PawnRelationDefOf.Fiance && relationDef != PawnRelationDefOf.Spouse)
                return;
            this.pawn.health.RemoveHediff((Hediff)this);
        }
    }

    public class SLP_Hediff_PsychicFriendship : HediffWithTarget
    {
        public override string LabelBase => base.LabelBase + " " + this.def.targetPrefix + " " + this.target?.LabelShortCap;

        public override void Notify_RelationAdded(Pawn otherPawn, PawnRelationDef relationDef)
        {
            if (otherPawn != this.target || relationDef != PawnRelationDefOf.Lover && relationDef != PawnRelationDefOf.Fiance && relationDef != PawnRelationDefOf.Spouse)
                return;
            this.pawn.health.RemoveHediff((Hediff)this);
        }
    }


    //Mod Settings
    /**
    public class SLP_MorePsycastSettings : ModSettings
    {

        //List of all psycasts added by this mod (prevents picking up all psycasts or all abilities with a loop)
        public bool ToggleSLP_PSY_ArcticPinhole;
        public bool ToggleSLP_PSY_ImmunityBoost;
        public bool ToggleSLP_PSY_HealingBoost;
        public bool ToggleSLP_PSY_RegenMajor;
        public bool ToggleSLP_PSY_FlashHeal;
        public bool ToggleSLP_PSY_RegenMinor;
        public bool ToggleSLP_PSY_Resurrect;
        public bool ToggleSLP_PSY_CleanSkip;
        public bool ToggleSLP_PSY_FertilitySkip;
        public bool ToggleSLP_PSY_Haemostasis;
        public bool ToggleSLP_PSY_Ignite;
        public bool ToggleSLP_PSY_Recondition;
        public bool ToggleSLP_PSY_Interdiction;
        public bool ToggleSLP_PSY_WordOfSleep;
        public bool ToggleSLP_PSY_WordOfVigor;
        public bool ToggleSLP_PSY_Skipscreen;
        public bool ToggleSLP_PSY_PotentialUnleashed;
        public bool ToggleSLP_PSY_Dash;
        public bool ToggleSLP_PSY_WordOfContempt;
        public bool ToggleSLP_PSY_Calm;
        public bool ToggleSLP_PSY_MassBeckon;
        public bool ToggleSLP_PSY_MassBurden;
        public bool ToggleSLP_PSY_StunPulse;
        public bool ToggleSLP_PSY_Flashstep;
        public bool ToggleSLP_PSY_Skipstep;
        public bool ToggleSLP_PSY_ComfortShield;
        public bool ToggleSLP_PSY_Engulf;
        public bool ToggleSLP_PSY_Dart;
        public bool ToggleSLP_PSY_EndoPinhole;
        public bool ToggleSLP_SupernovaPinhole;
        public bool ToggleSLP_PSY_WordOfFriendship;
        public bool ToggleSLP_PSY_Brickgate;
        public bool ToggleSLP_PSY_Chemskip;
        public bool ToggleSLP_PSY_StaticBurst;
        public bool ToggleSLP_PSY_Superskip;

        //Conditionally Added Psycasts
        public bool ToggleSLP_PSY_Revitalise;

        // The part that writes settings to file.
        public override void ExposeData()
        {
            Scribe_Values.Look(ref ToggleSLP_PSY_ArcticPinhole, "SLP_PSY_ArcticPinhole");
            Scribe_Values.Look(ref ToggleSLP_PSY_ImmunityBoost, "SLP_PSY_ImmunityBoost");
            Scribe_Values.Look(ref ToggleSLP_PSY_HealingBoost, "SLP_PSY_HealingBoost");
            Scribe_Values.Look(ref ToggleSLP_PSY_RegenMajor, "SLP_PSY_RegenMajor");
            Scribe_Values.Look(ref ToggleSLP_PSY_FlashHeal, "SLP_PSY_FlashHeal");
            Scribe_Values.Look(ref ToggleSLP_PSY_RegenMinor, "SLP_PSY_RegenMinor");

            Scribe_Values.Look(ref ToggleSLP_PSY_Resurrect, "SLP_PSY_Resurrect");
            Scribe_Values.Look(ref ToggleSLP_PSY_CleanSkip, "SLP_PSY_CleanSkip");
            Scribe_Values.Look(ref ToggleSLP_PSY_FertilitySkip, "SLP_PSY_FertilitySkip");
            Scribe_Values.Look(ref ToggleSLP_PSY_Haemostasis, "SLP_PSY_Haemostasis");
            Scribe_Values.Look(ref ToggleSLP_PSY_Ignite, "SLP_PSY_Ignite");
            Scribe_Values.Look(ref ToggleSLP_PSY_Recondition, "SLP_PSY_Recondition");

            Scribe_Values.Look(ref ToggleSLP_PSY_Interdiction, "SLP_PSY_Interdiction");
            Scribe_Values.Look(ref ToggleSLP_PSY_WordOfSleep, "SLP_PSY_WordOfSleep");
            Scribe_Values.Look(ref ToggleSLP_PSY_WordOfVigor, "SLP_PSY_WordOfVigor");
            Scribe_Values.Look(ref ToggleSLP_PSY_Skipscreen, "SLP_PSY_Skipscreen");
            Scribe_Values.Look(ref ToggleSLP_PSY_PotentialUnleashed, "SLP_PSY_PotentialUnleashed");
            Scribe_Values.Look(ref ToggleSLP_PSY_Dash, "SLP_PSY_Dash");

            Scribe_Values.Look(ref ToggleSLP_PSY_WordOfContempt, "SLP_PSY_WordOfContempt");
            Scribe_Values.Look(ref ToggleSLP_PSY_Calm, "SLP_PSY_Calm");
            Scribe_Values.Look(ref ToggleSLP_PSY_MassBeckon, "SLP_PSY_MassBeckon");
            Scribe_Values.Look(ref ToggleSLP_PSY_MassBurden, "SLP_PSY_MassBurden");
            Scribe_Values.Look(ref ToggleSLP_PSY_StunPulse, "SLP_PSY_StunPulse");
            Scribe_Values.Look(ref ToggleSLP_PSY_Flashstep, "SLP_PSY_Flashstep");

            Scribe_Values.Look(ref ToggleSLP_PSY_Skipstep, "SLP_PSY_Skipstep");
            Scribe_Values.Look(ref ToggleSLP_PSY_ComfortShield, "SLP_PSY_ComfortShield");
            Scribe_Values.Look(ref ToggleSLP_PSY_Engulf, "SLP_PSY_Engulf");
            Scribe_Values.Look(ref ToggleSLP_PSY_Dart, "SLP_PSY_Dart");
            Scribe_Values.Look(ref ToggleSLP_PSY_EndoPinhole, "SLP_PSY_EndoPinhole");
            Scribe_Values.Look(ref ToggleSLP_SupernovaPinhole, "SLP_SupernovaPinhole");

            Scribe_Values.Look(ref ToggleSLP_PSY_WordOfFriendship, "SLP_PSY_WordOfFriendship");
            Scribe_Values.Look(ref ToggleSLP_PSY_Brickgate, "SLP_PSY_Brickgate");
            Scribe_Values.Look(ref ToggleSLP_PSY_Chemskip, "SLP_PSY_Chemskip");
            Scribe_Values.Look(ref ToggleSLP_PSY_StaticBurst, "SLP_PSY_StaticBurst");
            Scribe_Values.Look(ref ToggleSLP_PSY_Superskip, "SLP_PSY_Superskip");
            Scribe_Values.Look(ref ToggleSLP_PSY_Revitalise, "SLP_PSY_Revitalise");
            base.ExposeData();
        }
    }

    public class SLPMorePsycast_Mod : Mod
    {
        //private Vector2 ScrollPosition = Vector2.zero;
        private static Vector2 scrollPosition = new Vector2(0f, 0f);
        private static float totalContentHeight = 1000f;
        private const float ScrollBarWidthMargin = 18f;

        // A reference to the settings.
        SLP_MorePsycastSettings settings;

        // A mandatory constructor which resolves the reference to our settings.
        public SLPMorePsycast_Mod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<SLP_MorePsycastSettings>();
        }

        // The GUI part to set settings.
        public override void DoSettingsWindowContents(Rect inRect)
        {
            //Big thank you to LWD Deep Storage, their mod settings code helped me add in the scroll bar
            Rect outerRect = inRect.ContractedBy(10f);
            Widgets.DrawHighlight(outerRect);
            bool scrollBarVisible = totalContentHeight > outerRect.height;
            var scrollViewTotal = new Rect(0f, 0f, outerRect.width - (scrollBarVisible ? ScrollBarWidthMargin : 0), totalContentHeight);
            Widgets.BeginScrollView(outerRect, ref scrollPosition, scrollViewTotal);
            Listing_Standard listingStandard = new Listing_Standard(GameFont.Tiny);
            listingStandard.Begin(new Rect(0f, 0f, scrollViewTotal.width, 9999f));

            listingStandard.Label("SLP_MustRestart".Translate());
            listingStandard.GapLine();
            listingStandard.Label("SLP_DefaultSettings".Translate());
            listingStandard.Label("SLP_EffectSettings".Translate());
            listingStandard.GapLine();
            listingStandard.CheckboxLabeled("SLP_PSY_ArcticPinholeExplain".Translate(), ref settings.ToggleSLP_PSY_ArcticPinhole, "SLP_PSY_ArcticPinholeTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_ImmunityBoostExplain".Translate(), ref settings.ToggleSLP_PSY_ImmunityBoost, "SLP_PSY_ImmunityBoostTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_HealingBoostExplain".Translate(), ref settings.ToggleSLP_PSY_HealingBoost, "SLP_PSY_HealingBoostTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_RegenMajorExplain".Translate(), ref settings.ToggleSLP_PSY_RegenMajor, "SLP_PSY_RegenMajorTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_FlashHealExplain".Translate(), ref settings.ToggleSLP_PSY_FlashHeal, "SLP_PSY_FlashHealTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_RegenMinorExplain".Translate(), ref settings.ToggleSLP_PSY_RegenMinor, "SLP_PSY_RegenMinorTooltip".Translate());

            listingStandard.CheckboxLabeled("SLP_PSY_ResurrectExplain".Translate(), ref settings.ToggleSLP_PSY_Resurrect, "SLP_PSY_ResurrectTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_CleanSkipExplain".Translate(), ref settings.ToggleSLP_PSY_CleanSkip, "SLP_PSY_CleanSkipTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_FertilitySkipExplain".Translate(), ref settings.ToggleSLP_PSY_FertilitySkip, "SLP_PSY_FertilitySkipTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_HaemostasisExplain".Translate(), ref settings.ToggleSLP_PSY_Haemostasis, "SLP_PSY_HaemostasisTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_IgniteExplain".Translate(), ref settings.ToggleSLP_PSY_Ignite, "SLP_PSY_IgniteTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_ReconditionExplain".Translate(), ref settings.ToggleSLP_PSY_Recondition, "SLP_PSY_ReconditionTooltip".Translate());

            listingStandard.CheckboxLabeled("SLP_PSY_InterdictionExplain".Translate(), ref settings.ToggleSLP_PSY_Interdiction, "SLP_PSY_InterdictionTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_WordOfSleepExplain".Translate(), ref settings.ToggleSLP_PSY_WordOfSleep, "SLP_PSY_WordOfSleepTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_WordOfVigorExplain".Translate(), ref settings.ToggleSLP_PSY_WordOfVigor, "SLP_PSY_WordOfVigorTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_SkipscreenExplain".Translate(), ref settings.ToggleSLP_PSY_Skipscreen, "SLP_PSY_SkipscreenTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_PotentialUnleashedExplain".Translate(), ref settings.ToggleSLP_PSY_PotentialUnleashed, "SLP_PSY_PotentialUnleashedTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_DashExplain".Translate(), ref settings.ToggleSLP_PSY_Dash, "SLP_PSY_DashTooltip".Translate());

            listingStandard.CheckboxLabeled("SLP_PSY_WordOfContemptExplain".Translate(), ref settings.ToggleSLP_PSY_WordOfContempt, "SLP_PSY_WordOfContemptTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_CalmExplain".Translate(), ref settings.ToggleSLP_PSY_Calm, "SLP_PSY_CalmTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_MassBeckonExplain".Translate(), ref settings.ToggleSLP_PSY_MassBeckon, "SLP_PSY_MassBeckonTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_MassBurdenExplain".Translate(), ref settings.ToggleSLP_PSY_MassBurden, "SLP_PSY_MassBurdenTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_StunPulseExplain".Translate(), ref settings.ToggleSLP_PSY_StunPulse, "SLP_PSY_StunPulseTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_FlashstepExplain".Translate(), ref settings.ToggleSLP_PSY_Flashstep, "SLP_PSY_FlashstepTooltip".Translate());

            listingStandard.CheckboxLabeled("SLP_PSY_SkipstepExplain".Translate(), ref settings.ToggleSLP_PSY_Skipstep, "SLP_PSY_SkipstepTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_ComfortShieldExplain".Translate(), ref settings.ToggleSLP_PSY_ComfortShield, "SLP_PSY_ComfortShieldTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_EngulfExplain".Translate(), ref settings.ToggleSLP_PSY_Engulf, "SLP_PSY_EngulfTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_DartExplain".Translate(), ref settings.ToggleSLP_PSY_Dart, "SLP_PSY_DartTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_EndoPinholeExplain".Translate(), ref settings.ToggleSLP_PSY_EndoPinhole, "SLP_PSY_EndoPinholeTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_SupernovaPinholeExplain".Translate(), ref settings.ToggleSLP_SupernovaPinhole, "SLP_SupernovaPinholeTooltip".Translate());

            listingStandard.CheckboxLabeled("SLP_PSY_WordOfFriendshipExplain".Translate(), ref settings.ToggleSLP_PSY_WordOfFriendship, "SLP_PSY_WordOfFriendshipTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_BrickgateExplain".Translate(), ref settings.ToggleSLP_PSY_Brickgate, "SLP_PSY_BrickgateTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_ChemskipExplain".Translate(), ref settings.ToggleSLP_PSY_Chemskip, "SLP_PSY_ChemskipTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_StaticBurstExplain".Translate(), ref settings.ToggleSLP_PSY_StaticBurst, "SLP_PSY_StaticBurstTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_SuperskipExplain".Translate(), ref settings.ToggleSLP_PSY_Superskip, "SLP_PSY_SuperskipTooltip".Translate());
            listingStandard.CheckboxLabeled("SLP_PSY_RevitaliseExplain".Translate(), ref settings.ToggleSLP_PSY_Revitalise, "SLP_PSY_RevitaliseTooltip".Translate());
            
            listingStandard.GapLine();
            totalContentHeight = listingStandard.CurHeight + 10f;
            listingStandard.End();
            Widgets.EndScrollView();
            base.DoSettingsWindowContents(inRect);
        }

        // Override SettingsCategory to show up in the list of settings.
        // Using .Translate() is optional, but does allow for localisation.
        public override string SettingsCategory()
        {
            return "SLP_MorePsycastSettings".Translate();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            //RemoveToggledPsycasts(); // This cant be here, it only seems to remove the toggled stuff if you check the mod settings before playing.
        }

        
        public void RemoveToggledPsycasts()
        {

            settings = GetSettings<SLP_MorePsycastSettings>();
            int numDisabled = 0;

            if (settings.ToggleSLP_PSY_ArcticPinhole == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_ArcticPinhole);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_ImmunityBoost == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_ImmunityBoost);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_HealingBoost == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_HealingBoost);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_RegenMajor == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_RegenMajor);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_FlashHeal == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_FlashHeal);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_RegenMinor == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_RegenMinor);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_Resurrect == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_Resurrect);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_CleanSkip == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_CleanSkip);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_FertilitySkip == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_FertilitySkip);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_Haemostasis == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_Haemostasis);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_Ignite == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_Ignite);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_Recondition == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_Recondition);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_Interdiction == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_Interdiction);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_WordOfSleep == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_WordOfSleep);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_WordOfVigor == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_WordOfVigor);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_Skipscreen == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_Skipscreen);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_PotentialUnleashed == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_PotentialUnleashed);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_Dash == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_Dash);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_WordOfContempt == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_WordOfContempt);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_Calm == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_Calm);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_MassBeckon == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_MassBeckon);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_MassBurden == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_MassBurden);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_StunPulse == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_StunPulse);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_Flashstep == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_Flashstep);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_Skipstep == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_Skipstep);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_ComfortShield == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_ComfortShield);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_Engulf == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_Engulf);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_Dart == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_Dart);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_EndoPinhole == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_EndoPinhole);
                numDisabled++;
            }
            if (settings.ToggleSLP_SupernovaPinhole == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_SupernovaPinhole);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_WordOfFriendship == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_WordOfFriendship);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_Brickgate == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_Brickgate);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_Chemskip == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_Chemskip);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_StaticBurst == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_StaticBurst);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_Superskip == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_Superskip);
                numDisabled++;
            }
            if (settings.ToggleSLP_PSY_Revitalise == true)
            {
                DefDatabase<AbilityDef>.AllDefsListForReading.Remove(SLP_PsycastDefs.SLP_PSY_Revitalise);
                numDisabled++;
            }
            Log.Message("Sleepy's More Psycasts - Disabled Psycasts: " + numDisabled);
            return;
        }
    **/
    

    //Utility Code
    public static class SLP_ResurrectionUtility
    {
        private static SimpleCurve DementiaChancePerRotDaysCurve = new SimpleCurve()
    {
      {
        new CurvePoint(0.1f, 0.02f),
        true
      },
      {
        new CurvePoint(5f, 0.8f),
        true
      }
    };
        private static SimpleCurve BlindnessChancePerRotDaysCurve = new SimpleCurve()
    {
      {
        new CurvePoint(0.1f, 0.02f),
        true
      },
      {
        new CurvePoint(5f, 0.8f),
        true
      }
    };

        public static bool TryResurrect(Pawn pawn, ResurrectionParams parms = null)
        {
            if (!pawn.Dead)
            {
                Log.Error("Tried to resurrect a pawn who is not dead: " + pawn.ToStringSafe<Pawn>());
                return false;
            }
            if (pawn.Discarded)
            {
                Log.Error("Tried to resurrect a discarded pawn: " + pawn.ToStringSafe<Pawn>());
                return false;
            }
            Corpse corpse = pawn.Corpse;
            bool flag1 = false;
            IntVec3 loc = IntVec3.Invalid;
            Map map = (Map)null;
            if (ModsConfig.AnomalyActive && corpse is UnnaturalCorpse)
            {
                Messages.Message((string)"MessageUnnaturalCorpseResurrect".Translate(corpse.InnerPawn.Named("PAWN")), (LookTargets)(Thing)corpse, MessageTypeDefOf.NeutralEvent);
                return false;
            }
            bool flag2 = Find.Selector.IsSelected((object)corpse);
            if (corpse != null)
            {
                flag1 = corpse.SpawnedOrAnyParentSpawned;
                loc = corpse.PositionHeld;
                map = corpse.MapHeld;
                corpse.InnerPawn = (Pawn)null;
                corpse.Destroy(DestroyMode.Vanish);
            }
            if (flag1 && pawn.IsWorldPawn())
                Find.WorldPawns.RemovePawn(pawn);
            pawn.ForceSetStateToUnspawned();
            PawnComponentsUtility.CreateInitialComponents(pawn);
            pawn.health.Notify_Resurrected(parms == null || parms.restoreMissingParts, parms != null ? parms.gettingScarsChance : 0.0f);
            if (pawn.Faction != null && pawn.Faction.IsPlayer)
            {
                pawn.workSettings?.EnableAndInitialize();
                Find.StoryWatcher.watcherPopAdaptation.Notify_PawnEvent(pawn, PopAdaptationEvent.GainedColonist);
            }
            if (pawn.RaceProps.IsMechanoid && MechRepairUtility.IsMissingWeapon(pawn))
                MechRepairUtility.GenerateWeapon(pawn);
            if (flag1 && (parms == null || !parms.dontSpawn))
            {
                GenSpawn.Spawn((Thing)pawn, loc, map);
                Lord lord;
                if ((lord = pawn.GetLord()) != null)
                    lord?.Notify_PawnUndowned(pawn);
                else if (pawn.Faction != null && pawn.Faction != Faction.OfPlayer && pawn.HostileTo(Faction.OfPlayer) && (parms == null || !parms.noLord))
                {
                    LordJob_AssaultColony jobAssaultColony = parms == null ? new LordJob_AssaultColony(pawn.Faction) : new LordJob_AssaultColony(pawn.Faction, parms.canKidnap, parms.canTimeoutOrFlee, parms.sappers, parms.useAvoidGridSmart, parms.canSteal, parms.breachers, parms.canPickUpOpportunisticWeapons);
                    LordMaker.MakeNewLord(pawn.Faction, (LordJob)jobAssaultColony, pawn.Map, Gen.YieldSingle<Pawn>(pawn));
                }
                if (pawn.apparel != null)
                {
                    List<Apparel> wornApparel = pawn.apparel.WornApparel;
                    for (int index = 0; index < wornApparel.Count; ++index)
                        wornApparel[index].Notify_PawnResurrected(pawn);
                }
            }
            if (parms != null && parms.removeDiedThoughts)
                PawnDiedOrDownedThoughtsUtility.RemoveDiedThoughts(pawn);
            pawn.royalty?.Notify_Resurrected();
            if (pawn.guest != null && pawn.guest.IsInteractionEnabled(PrisonerInteractionModeDefOf.Execution))
                pawn.guest.SetNoInteraction();
            if (flag2 && pawn != null)
                Find.Selector.Select((object)pawn, false, false);
            pawn.Drawer.renderer.SetAllGraphicsDirty();
            if (parms != null && parms.invisibleStun)
                pawn.stances.stunner.StunFor(5f.SecondsToTicks(), (Thing)pawn, false, false);
            return true;
        }

        public static bool TryResurrectWithSideEffects(Pawn pawn)
        {
            Corpse corpse = pawn.Corpse;
            float x1 = corpse == null ? 0.0f : corpse.GetComp<CompRottable>().RotProgress / 60000f;
            if (!SLP_ResurrectionUtility.TryResurrect(pawn))
                return false;
            BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
            Hediff hediff1 = HediffMaker.MakeHediff(SLP_HediffDefOf.SLP_ResurrectionSickness, pawn);
            Hediff hediffslp = HediffMaker.MakeHediff(SLP_HediffDefOf.SLP_PhoenixSyndrome, pawn);
            if (!pawn.health.WouldDieAfterAddingHediff(hediff1))
                pawn.health.AddHediff(hediff1);
            if (!pawn.health.WouldDieAfterAddingHediff(hediffslp))
                pawn.health.AddHediff(hediffslp);
            if (Rand.Chance(SLP_ResurrectionUtility.DementiaChancePerRotDaysCurve.Evaluate(x1)) && brain != null)
            {
                Hediff hediff2 = HediffMaker.MakeHediff(HediffDefOf.Dementia, pawn, brain);
                if (!pawn.health.WouldDieAfterAddingHediff(hediff2))
                    pawn.health.AddHediff(hediff2);
            }
            if (Rand.Chance(SLP_ResurrectionUtility.BlindnessChancePerRotDaysCurve.Evaluate(x1)))
            {
                foreach (BodyPartRecord bodyPartRecord in pawn.health.hediffSet.GetNotMissingParts().Where<BodyPartRecord>((Func<BodyPartRecord, bool>)(x => x.def == BodyPartDefOf.Eye)))
                {
                    if (!pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(bodyPartRecord))
                    {
                        Hediff hediff3 = HediffMaker.MakeHediff(HediffDefOf.Blindness, pawn, bodyPartRecord);
                        pawn.health.AddHediff(hediff3);
                    }
                }
            }
            if (pawn.Dead)
            {
                Log.Error("The pawn has died while being resurrected.");
                SLP_ResurrectionUtility.TryResurrect(pawn);
            }
            return true;
        }
    }

    public class SLP_Utilities
    {
        public static Hediff_Injury SLP_FindPermanentInjury(Pawn pawn, IEnumerable<BodyPartRecord> allowedBodyParts = null, params HediffDef[] exclude)
            {
                Hediff_Injury permanentInjury = (Hediff_Injury)null;
                List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                for (int index = 0; index < hediffs.Count; ++index)
                {
                    if (hediffs[index] is Hediff_Injury hd && hd.Visible && hd.IsPermanent() && hd.def.everCurableByItem && (allowedBodyParts == null || allowedBodyParts.Contains<BodyPartRecord>(hd.Part)) && (exclude == null || !((IEnumerable<HediffDef>)exclude).Contains<HediffDef>(hediffs[index].def)) && (permanentInjury == null || (double)hd.Severity > (double)permanentInjury.Severity))
                        permanentInjury = hd;
                }
                return permanentInjury;
            }

        public static IEnumerable<IntVec3> AffectedCells(LocalTargetInfo target, Map map, Ability parent)
        {
            if (!target.Cell.Filled(parent.pawn.Map))
            {
                foreach (IntVec3 intVec3 in GenRadial.RadialCellsAround(target.Cell, parent.def.EffectRadius, true))
                {
                    IntVec3 item = intVec3;
                    if (item.InBounds(map) && GenSight.LineOfSightToEdges(target.Cell, item, map, true))
                        yield return item;
                    item = new IntVec3();
                }
            }
        }

        public static void SpawnFleck(LocalTargetInfo target, FleckDef def, Map map)
        {
            if (target.HasThing)
                FleckMaker.AttachedOverlay(target.Thing, def, Vector3.zero);
            else
                FleckMaker.Static(target.Cell, map, def);
        }

        public static void SpawnEffecter(
          LocalTargetInfo target,
          EffecterDef def,
          Map map,
          int maintainForTicks,
          Ability parent)
        {
            Effecter eff = !target.HasThing ? def.Spawn(target.Cell, map) : def.Spawn(target.Thing, map);
            parent.AddEffecterToMaintain(eff, target.Cell, maintainForTicks);
        }
    }

    [DefOf]
    public class SLP_HediffDefOf
    {
        public static HediffDef SLP_Psycast_FlashHeal;
        public static HediffDef SLP_HealingPains;
        public static HediffDef SLP_PhoenixSyndrome;
        public static HediffDef SLP_ResurrectionSickness;
        public static HediffDef SLP_Overdriven;
        public static HediffDef SLP_PsychicContempt;
        public static HediffDef SLP_PsychicFriendship;

    }

    [DefOf]
    public static class SLP_SoundDefOf
    {
        public static SoundDef SLP_Psycast_Shunpo;
        public static SoundDef SLP_Psycast_Shunpo2;
        public static SoundDef SLP_Psycast_Sonido;
    }

    [DefOf]
    public static class SLP_ChunksDefOf
    {
        public static ThingDef ChunkSandstone;
        public static ThingDef BlocksSandstone;
        public static ThingDef ChunkGranite;
        public static ThingDef BlocksGranite;
        public static ThingDef ChunkLimestone;
        public static ThingDef BlocksLimestone;
        public static ThingDef ChunkSlate;
        public static ThingDef BlocksSlate;
        public static ThingDef ChunkMarble;
        public static ThingDef BlocksMarble;
    }

    [DefOf]
    public static class SLP_PsycastDefs
    {
        public static AbilityDef SLP_PSY_ArcticPinhole;
        public static AbilityDef SLP_PSY_ImmunityBoost;
        public static AbilityDef SLP_PSY_HealingBoost;
        public static AbilityDef SLP_PSY_RegenMajor;
        public static AbilityDef SLP_PSY_FlashHeal;
        public static AbilityDef SLP_PSY_RegenMinor;
        public static AbilityDef SLP_PSY_Resurrect;
        public static AbilityDef SLP_PSY_CleanSkip;
        public static AbilityDef SLP_PSY_FertilitySkip;
        public static AbilityDef SLP_PSY_Haemostasis;
        public static AbilityDef SLP_PSY_Ignite;
        public static AbilityDef SLP_PSY_Recondition;
        public static AbilityDef SLP_PSY_Interdiction;
        public static AbilityDef SLP_PSY_WordOfSleep;
        public static AbilityDef SLP_PSY_WordOfVigor;
        public static AbilityDef SLP_PSY_Skipscreen;
        public static AbilityDef SLP_PSY_PotentialUnleashed;
        public static AbilityDef SLP_PSY_Dash;
        public static AbilityDef SLP_PSY_WordOfContempt;
        public static AbilityDef SLP_PSY_Calm;
        public static AbilityDef SLP_PSY_MassBeckon;
        public static AbilityDef SLP_PSY_MassBurden;
        public static AbilityDef SLP_PSY_StunPulse;
        public static AbilityDef SLP_PSY_Flashstep;
        public static AbilityDef SLP_PSY_Skipstep;
        public static AbilityDef SLP_PSY_ComfortShield;
        public static AbilityDef SLP_PSY_Engulf;
        public static AbilityDef SLP_PSY_Dart;
        public static AbilityDef SLP_PSY_EndoPinhole;
        public static AbilityDef SLP_SupernovaPinhole;
        public static AbilityDef SLP_PSY_WordOfFriendship;
        public static AbilityDef SLP_PSY_Brickgate;
        public static AbilityDef SLP_PSY_Chemskip;
        public static AbilityDef SLP_PSY_StaticBurst;
        public static AbilityDef SLP_PSY_Superskip;

        //Conditionally Added Psycasts
        public static AbilityDef SLP_PSY_Revitalise;

    }

}
