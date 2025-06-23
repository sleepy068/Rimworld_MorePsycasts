using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;


namespace Sleepys_MorePsycastsVol3
{

    //Comp Properties
	public class CompProperties_SLPV2_AreaEffectLaunchProjectile : CompProperties_AbilityEffect
	{
		public ThingDef projectileDef;
		public float effectPreviewRadius = 1f;

		public CompProperties_SLPV2_AreaEffectLaunchProjectile() => this.compClass = typeof(CompAbilityEffect_SLPV2_AreaEffectLaunchProjectile);
	}

	public class CompProperties_SLPV2_AbilityGiveTwoHediff :
	   CompProperties_AbilityGiveHediff
	{
		public HediffDef hediffDef2;
	}

	//Comp Ability Effect
	public class CompAbilityEffect_SLPV2_AreaEffectLaunchProjectile : CompAbilityEffect
	{
		public CompProperties_SLPV2_AreaEffectLaunchProjectile Props => (CompProperties_SLPV2_AreaEffectLaunchProjectile)this.props;

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

    public class CompAbilityEffect_SLPV2_GiveTwoHediff : CompAbilityEffect_WithDuration
    {
        public new CompProperties_SLPV2_AbilityGiveTwoHediff Props => (CompProperties_SLPV2_AbilityGiveTwoHediff)this.props;

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

}
