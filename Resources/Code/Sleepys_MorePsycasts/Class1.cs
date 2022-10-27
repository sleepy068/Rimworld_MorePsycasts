using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace Sleepys_MorePsycasts
{
    public class CompProperties_SLP_AbilityGiveTwoHediff :
       CompProperties_AbilityGiveHediff
    {
        public HediffDef hediffDef2;
    }

    public class CompAbilityEffect_GiveTwoHediff : CompAbilityEffect_WithDuration
    {
        public CompProperties_SLP_AbilityGiveTwoHediff Props => (CompProperties_SLP_AbilityGiveTwoHediff)this.props;

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

    public class CompAbilityEffect_FertilitySkip : CompAbilityEffect
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

    [DefOf]
    public class HediffDefOf
    {
        public static HediffDef SLP_Psycast_FlashHeal;
        public static HediffDef SLP_HealingPains;

    }

}
