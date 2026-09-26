using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using static HarmonyLib.Code;

namespace NAT
{
	public class Building_HoraxTurret : Building_TurretGun
	{
		[Unsaved(false)]
		private Material cachedShadowMaterial;

		private Material ShadowMaterial
		{
			get
			{
				if (cachedShadowMaterial == null)
				{
					cachedShadowMaterial = MaterialPool.MatFrom("Things/Skyfaller/SkyfallerShadowDropPod", ShaderDatabase.Transparent);
				}
				return cachedShadowMaterial;
			}
		}

		private float extraSinParam;

		public override void PostPostMake()
		{
			base.PostPostMake();
			extraSinParam = Rand.ValueAsync(thingIDNumber) * 2 * Mathf.PI;
		}

		public override LocalTargetInfo TryFindNewTarget()
		{
			IAttackTargetSearcher attackTargetSearcher = TargSearcher();
			Faction faction = attackTargetSearcher.Thing.Faction;
			float range = AttackVerb.verbProps.range;
			TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
			if (!AttackVerb.ProjectileFliesOverhead())
			{
				targetScanFlags |= TargetScanFlags.NeedLOSToAll;
			}
			if (AttackVerb.IsIncendiary_Ranged())
			{
				targetScanFlags |= TargetScanFlags.NeedNonBurning;
			}
			return (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(attackTargetSearcher, targetScanFlags, IsValidTarget);
		}

		protected override void BeginBurst()
		{
			AttackVerb.TryStartCastOn(CurrentTarget, preventFriendlyFire: true);
			OnAttackedTarget(CurrentTarget);
		}

		private IAttackTargetSearcher TargSearcher()
		{
			if (mannableComp != null && mannableComp.MannedNow)
			{
				return mannableComp.ManningPawn;
			}
			return this;
		}

		private bool IsValidTarget(Thing t)
		{
			if (t is Pawn pawn)
			{
				if (base.Faction == Faction.OfPlayer && pawn.IsPrisoner)
				{
					return false;
				}
				if (AttackVerb.ProjectileFliesOverhead())
				{
					RoofDef roofDef = base.Map.roofGrid.RoofAt(t.Position);
					if (roofDef != null && roofDef.isThickRoof)
					{
						return false;
					}
				}
				if (mannableComp == null)
				{
					return !GenAI.MachinesLike(base.Faction, pawn);
				}
				if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer)
				{
					return false;
				}
			}
			return true;
		}

		public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
		{
			if(dinfo.Instigator != null && !dinfo.Instigator.HostileTo(this))
			{
				absorbed = true;
				return;
			}
			float amount = dinfo.Amount / dinfo.Def.buildingDamageFactor;
			if (this.def.passability == Traversability.Impassable)
			{
				amount *= dinfo.Def.buildingDamageFactorImpassable;
			}
			else
			{
				amount *= dinfo.Def.buildingDamageFactorPassable;
			}
			dinfo.SetAmount(amount);
			base.PreApplyDamage(ref dinfo, out absorbed);
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			if (base.Spawned)
			{
				Skyfaller.DrawDropSpotShadow(drawLoc, Rot4.North, ShadowMaterial, def.size.ToVector2(), 40);
				float num = 0.2f + 0.5f * (1f + Mathf.Sin((Mathf.PI * 2f * (float)GenTicks.TicksGame / 300f) + extraSinParam)) * 0.35f;
				drawLoc.z += num;
				Graphic.Draw(drawLoc, Rot4.North, this);
				SilhouetteUtility.DrawGraphicSilhouette(this, drawLoc);
				Comps_DrawAt(drawLoc, flip);
				Top.DrawTurret(drawLoc, Vector3.zero, 0f);
			}
			else
			{
				base.DrawAt(drawLoc, flip);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref extraSinParam, "extraSinParam");
		}

		public override void Notify_DebugSpawned()
		{
			base.Notify_DebugSpawned();
			SetFaction(Faction.OfEntities);
		}
	}
}
