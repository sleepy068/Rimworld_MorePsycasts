using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;


namespace Sleepys_MorePsycastsVol2
{

    //Comp Properties
    public class CompProperties_SLP_AbilityGaspop : CompProperties_AbilityEffect
    {
        public float smokeRadius;

        public CompProperties_SLP_AbilityGaspop() => this.compClass = typeof(CompAbilityEffect_SLP_Gaspop);
    }

    public class CompProperties_SLP_AbilityFoamskip : CompProperties_AbilityEffect
    {
        public CompProperties_SLP_AbilityFoamskip()
        {
            this.compClass = typeof(CompAbilityEffect_SLP_Foamskip);
        }
    }

	public class CompProperties_SLP_AbilityGiveAnotherHediff : CompProperties_AbilityEffectWithDuration
	{
		public HediffDef hediffDef;
		public bool onlyBrain;
		public bool applyToSelf;
		public bool onlyApplyToSelf;
		public bool applyToTarget = true;
		public bool replaceExisting;
		public float severity = -1f;
		public bool ignoreSelf;
	}

	//Comp Ability Effect
	public class CompAbilityEffect_SLP_Gaspop : CompAbilityEffect
    {
        public CompProperties_SLP_AbilityGaspop Props => (CompProperties_SLP_AbilityGaspop)this.props;

		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			base.Apply(target, dest);
			GenExplosion.DoExplosion(target.Cell, parent.pawn.MapHeld, Props.smokeRadius, DamageDefOf.ToxGas, null, -1, -1f, null, null, null, null, null, 0f, 1, GasType.ToxGas);
		}

		public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(target.Cell, this.Props.smokeRadius);
        }

    }

	public class CompAbilityEffect_SLP_Foamskip : CompAbilityEffect
	{
		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			base.Apply(target, dest);
			Map map = parent.pawn.Map;
			foreach (IntVec3 item in AffectedCells(target, map))
			{
				if (!item.InBounds(map))
				{
					continue;
				}
				List<Thing> thingList = item.GetThingList(map);
				for (int num = thingList.Count - 1; num >= 0; num--)
				{
					if (thingList[num] is Fire)
					{
						thingList[num].Destroy();
					}
					else if (thingList[num] is Pawn pawn)
					{
						pawn.GetInvisibilityComp()?.DisruptInvisibility();
					}
				}
				if (!item.Filled(map))
				{
					FilthMaker.TryMakeFilth(item, map, ThingDefOf.Filth_FireFoam);
				}
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(item.ToVector3Shifted(), map, FleckDefOf.Smoke);
				dataStatic.rotationRate = Rand.Range(-30, 30);
				dataStatic.rotation = 90 * Rand.RangeInclusive(0, 3);
				map.flecks.CreateFleck(dataStatic);
				CompAbilityEffect_Teleport.SendSkipUsedSignal(item, parent.pawn);
			}
		}

		private IEnumerable<IntVec3> AffectedCells(LocalTargetInfo target, Map map)
		{
			if (!target.Cell.InBounds(map) || target.Cell.Filled(parent.pawn.Map))
			{
				yield break;
			}
			foreach (IntVec3 item in GenRadial.RadialCellsAround(target.Cell, parent.def.EffectRadius, useCenter: true))
			{
				if (item.InBounds(map) && GenSight.LineOfSightToEdges(target.Cell, item, map, skipFirstCell: true))
				{
					yield return item;
				}
			}
		}

		public override void DrawEffectPreview(LocalTargetInfo target)
		{
			GenDraw.DrawFieldEdges(this.AffectedCells(target, this.parent.pawn.Map).ToList<IntVec3>(), this.Valid(target, false) ? Color.white : Color.red);
		}

		public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
		{
			if (target.Cell.Filled(parent.pawn.Map))
			{
				if (throwMessages)
				{
					Messages.Message("AbilityOccupiedCells".Translate(parent.def.LabelCap), target.ToTargetInfo(parent.pawn.Map), MessageTypeDefOf.RejectInput, historical: false);
				}
				return false;
			}
			return true;
		}
	}

	public class CompAbilityEffect_SLP_GiveAnotherHediff : CompAbilityEffect_WithDuration
	{
		public CompProperties_SLP_AbilityGiveAnotherHediff Props => (CompProperties_SLP_AbilityGiveAnotherHediff)this.props;

		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			base.Apply(target, dest);
			if (this.Props.ignoreSelf && target.Pawn == this.parent.pawn)
				return;
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
			if (this.TryResist(target))
			{
				MoteMaker.ThrowText(target.DrawPos, target.Map, (string)"Resisted".Translate());
			}
			else
			{
				if (this.Props.replaceExisting)
				{
					Hediff firstHediffOfDef = target.health.hediffSet.GetFirstHediffOfDef(this.Props.hediffDef);
					if (firstHediffOfDef != null)
						target.health.RemoveHediff(firstHediffOfDef);
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
			}
		}

		protected virtual bool TryResist(Pawn pawn) => false;

		public override bool AICanTargetNow(LocalTargetInfo target)
		{
			return this.parent.pawn.Faction != Faction.OfPlayer && target.Pawn != null;
		}
	}

}
