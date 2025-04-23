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

}
