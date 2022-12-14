using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Sleepys_MorePsycasts
{
    public class CompProperties_SLP_AbilityGiveTwoHediff :
       CompProperties_AbilityGiveHediff
    {
        public HediffDef hediffDef2;
    }

    public class HediffCompProperties_SLP_NeedOffset : HediffCompProperties
    {
        public NeedDef need;
        public float offset;

        public HediffCompProperties_SLP_NeedOffset() => this.compClass = typeof(SLP_HediffComp_AdjustNeed);
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
                    if (terrain.IsWater)
                    {
                        TerrainDef named = DefDatabase<TerrainDef>.GetNamed("Mud");
                        if (named != null)
                            map.terrainGrid.SetTerrain(affectedCell, named);
                        else
                            map.terrainGrid.SetTerrain(affectedCell, TerrainDefOf.Sand);
                    }
                    else
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
            float healrate = 0.1f;
            base.CompPostTick(ref severityAdjustment);
            Hediff_Injury hediff_Injury = SLP_Utilities.FindPermanentInjury(base.Pawn);
            if (hediff_Injury == null)
            {
                severityAdjustment = -1;
            }

            if (base.Pawn.IsHashIntervalTick(600))
                hediff_Injury.Heal(healrate);
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
            int num = ((thing.MaxHitPoints - thing.HitPoints) / 2);
            thing.HitPoints += num;
            SoundDefOf.Psycast_Skip_Exit.PlayOneShot((SoundInfo)new TargetInfo(target.Cell, map));
            SLP_Utilities.SpawnFleck(target, FleckDefOf.PsycastSkipInnerExit, map);
            SLP_Utilities.SpawnFleck(target, FleckDefOf.PsycastSkipOuterRingExit, map);
            SLP_Utilities.SpawnEffecter(target, EffecterDefOf.Skip_Exit, map, 60, this.parent);
        }
    }

    public class SLP_CompAbilityEffect_Ignite : CompAbilityEffect
    {
        public new CompProperties_AbilityEffect Props => (CompProperties_AbilityEffect)this.props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (target == (LocalTargetInfo)(Thing)null)
                Log.Message("Tried to apply ignite to nothing.");
            else
                FireUtility.TryStartFireIn(target.Cell, this.parent.pawn.Map, 0.1f);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest) => (double)FireUtility.ChanceToStartFireIn(target.Cell, this.parent.pawn.Map) > 0.0;
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
            SLP_ResurrectionUtility.ResurrectWithSideEffects(innerPawn);
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

        public static void Resurrect(Pawn pawn)
        {
            if (!pawn.Dead)
                Log.Error("Tried to resurrect a pawn who is not dead: " + pawn.ToStringSafe<Pawn>());
            else if (pawn.Discarded)
            {
                Log.Error("Tried to resurrect a discarded pawn: " + pawn.ToStringSafe<Pawn>());
            }
            else
            {
                Corpse corpse = pawn.Corpse;
                bool flag1 = false;
                IntVec3 loc = IntVec3.Invalid;
                Map map = (Map)null;
                bool flag2 = Find.Selector.IsSelected((object)corpse);
                if (corpse != null)
                {
                    flag1 = corpse.Spawned;
                    loc = corpse.Position;
                    map = corpse.Map;
                    corpse.InnerPawn = (Pawn)null;
                    corpse.Destroy(DestroyMode.Vanish);
                }
                if (flag1 && pawn.IsWorldPawn())
                    Find.WorldPawns.RemovePawn(pawn);
                pawn.ForceSetStateToUnspawned();
                PawnComponentsUtility.CreateInitialComponents(pawn);
                pawn.health.Notify_Resurrected();
                if (pawn.Faction != null && pawn.Faction.IsPlayer)
                {
                    if (pawn.workSettings != null)
                        pawn.workSettings.EnableAndInitialize();
                    Find.StoryWatcher.watcherPopAdaptation.Notify_PawnEvent(pawn, PopAdaptationEvent.GainedColonist);
                }
                if (pawn.RaceProps.IsMechanoid && MechRepairUtility.IsMissingWeapon(pawn))
                    MechRepairUtility.GenerateWeapon(pawn);
                if (flag1)
                {
                    GenSpawn.Spawn((Thing)pawn, loc, map);
                    if (pawn.Faction != null && pawn.Faction != Faction.OfPlayer && pawn.HostileTo(Faction.OfPlayer))
                        LordMaker.MakeNewLord(pawn.Faction, (LordJob)new LordJob_AssaultColony(pawn.Faction), pawn.Map, Gen.YieldSingle<Pawn>(pawn));
                    if (pawn.apparel != null)
                    {
                        List<Apparel> wornApparel = pawn.apparel.WornApparel;
                        for (int index = 0; index < wornApparel.Count; ++index)
                            wornApparel[index].Notify_PawnResurrected();
                    }
                }
                PawnDiedOrDownedThoughtsUtility.RemoveDiedThoughts(pawn);
                if (pawn.royalty != null)
                    pawn.royalty.Notify_Resurrected();
                if (pawn.guest != null && pawn.guest.interactionMode == PrisonerInteractionModeDefOf.Execution)
                    pawn.guest.interactionMode = PrisonerInteractionModeDefOf.NoInteraction;
                if (!flag2 || pawn == null)
                    return;
                Find.Selector.Select((object)pawn, false, false);
            }
        }

        public static void ResurrectWithSideEffects(Pawn pawn)
        {
            Corpse corpse = pawn.Corpse;
            float x1 = corpse == null ? 0.0f : corpse.GetComp<CompRottable>().RotProgress / 60000f;
            ResurrectionUtility.Resurrect(pawn);
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
            if (!pawn.Dead)
                return;
            Log.Error("The pawn has died while being resurrected.");
            ResurrectionUtility.Resurrect(pawn);
        }
    }

    public class SLP_Utilities
    {
        public static Hediff_Injury FindPermanentInjury(Pawn pawn, IEnumerable<BodyPartRecord> allowedBodyParts = null)
        {
            Hediff_Injury permanentInjury = (Hediff_Injury)null;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int index = 0; index < hediffs.Count; ++index)
            {
                if (hediffs[index] is Hediff_Injury hd && hd.Visible && hd.IsPermanent() && hd.def.everCurableByItem && (allowedBodyParts == null || allowedBodyParts.Contains<BodyPartRecord>(hd.Part)) && (permanentInjury == null || (double)hd.Severity > (double)permanentInjury.Severity))
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
    }

}
