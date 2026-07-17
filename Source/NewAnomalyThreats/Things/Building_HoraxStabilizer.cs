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
	public class Building_HoraxStabilizer : Building, IAttackTarget
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

		public Building_HoraxMachine parent;

		private float extraSinParam;

		public float TargetPriorityFactor => 0.6f;

		public LocalTargetInfo TargetCurrentlyAimingAt => LocalTargetInfo.Invalid;

		Thing IAttackTarget.Thing => this;

		public override void PostPostMake()
		{
			base.PostPostMake();
			extraSinParam = Rand.ValueAsync(thingIDNumber) * 2 * Mathf.PI;
		}

		public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
		{
			if(dinfo.Instigator != null && !dinfo.Instigator.HostileTo(this))
			{
				absorbed = true;
				return;
			}
			base.PreApplyDamage(ref dinfo, out absorbed);
		}

		public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
		{
			base.PostApplyDamage(dinfo, totalDamageDealt);
			if(parent != null)
			{
				parent.lastStabilizatorDamagedTick = Find.TickManager.TicksGame;
			}
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
			}
			else
			{
				base.DrawAt(drawLoc, flip);
			}
		}

		public bool ThreatDisabled(IAttackTargetSearcher disabledFor)
		{
			if (!base.Spawned)
			{
				return true;
			}
			return false;
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			base.Destroy(mode);
			if(parent != null)
			{
				parent.stabilizers.Remove(this);
				parent.Notify_StabilizerRemoved(this);
			}
		}

		public override void DrawExtraSelectionOverlays()
		{
			base.DrawExtraSelectionOverlays();
			if(parent != null)
			{
				GenDraw.DrawLineBetween(parent.DrawPos, DrawPos, AltitudeLayer.MoteLow.AltitudeFor());
			}
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if(parent == null || (parent.MapHeld != null && map != parent.MapHeld))
			{
				Kill();
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref parent, "parent");
			Scribe_Values.Look(ref extraSinParam, "extraSinParam");
			if (Scribe.mode == LoadSaveMode.PostLoadInit && parent != null)
			{
				parent.stabilizers.Add(this);
			}
		}

		public override void Notify_DebugSpawned()
		{
			base.Notify_DebugSpawned();
			SetFaction(Faction.OfEntities);
		}
	}
}
