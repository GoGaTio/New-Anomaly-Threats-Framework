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
	public class Building_HoraxMachine : Building
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

		public float extraRotation;

		private int destabilizationTick = -1;

		private float extraSinParam;

		public bool needStabilizers = false;

		public int? lastStabilizatorDamagedTick = null;

		public List<Building_HoraxStabilizer> stabilizers = new List<Building_HoraxStabilizer>();

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref destabilizationTick, "destabilizationTick");
			Scribe_Values.Look(ref lastStabilizatorDamagedTick, "lastStabilizatorDamagedTick");
			Scribe_Values.Look(ref needStabilizers, "needStabilizers");
			Scribe_Values.Look(ref extraSinParam, "extraSinParam");
		}

		public override void PostPostMake()
		{
			base.PostPostMake();
			extraSinParam = Rand.ValueAsync(thingIDNumber) * 2 * Mathf.PI;
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			if (base.Spawned)
			{
				Skyfaller.DrawDropSpotShadow(drawLoc, Rot4.North, ShadowMaterial, def.size.ToVector2(), 65);
				float a = 0.55f + 0.5f * (1f + Mathf.Sin((Mathf.PI * 2f * (float)GenTicks.TicksGame / 300f) + extraSinParam)) * 0.35f;
				float t = ((destabilizationTick == -1) ? 0f : Mathf.Pow(1f - Mathf.Clamp01((float)(destabilizationTick - Find.TickManager.TicksGame) / 120f), 2f));
				float num = Mathf.Lerp(a, 0f, t);
				extraRotation = Mathf.Lerp(0f, 45f, t);
				if (destabilizationTick > 0)
				{
					drawLoc.x += Mathf.Sin(destabilizationTick - Find.TickManager.TicksGame) * 0.05f;
					drawLoc.z += Mathf.Sin(destabilizationTick - Find.TickManager.TicksGame * 1.1f) * 0.05f;
				}
				drawLoc.z += num;
				Graphic.Draw(drawLoc, Rot4.North, this, extraRotation);
				SilhouetteUtility.DrawGraphicSilhouette(this, drawLoc);
				Comps_DrawAt(drawLoc, flip);
			}
			else
			{
				base.DrawAt(drawLoc, flip);
			}
		}

		protected override void Tick()
		{
			base.Tick();
			if (destabilizationTick <= 0 || Find.TickManager.TicksGame < destabilizationTick)
			{
				return;
			}
			destabilizationTick = -1;
			Map map = Map;
			IntVec3 position = Position;
			CellRect cellRect = this.OccupiedRect();
			Faction faction = Faction;
			Destroy();
			Thing thing = ThingMaker.MakeThing(NATDefOf.NAT_HoraxMachine_Crashed);
			if (thing.Faction != faction)
			{
				thing.SetFaction(faction);
			}
			GenPlace.TryPlaceThing(thing, position, map, ThingPlaceMode.Direct);
			GenExplosion.DoExplosion(position, map, 2.9f, NATDefOf.NociosphereVaporize, thing, 1200, 9f, ignoredThings: new List<Thing>() { thing });
			for (int i = 0; i < cellRect.Area; i++)
			{
				FleckMaker.ThrowDustPuff(cellRect.RandomVector3.WithY(AltitudeLayer.MoteLow.AltitudeFor()), map, 2f);
			}
		}

		public void Destabilize(int ticks = 0)
		{
			if (destabilizationTick < 0)
			{
				destabilizationTick = Find.TickManager.TicksGame + ticks + 120;
			}
		}

		public void Notify_StabilizerRemoved(Building_HoraxStabilizer stabilizer)
		{
			if (needStabilizers && stabilizers.NullOrEmpty())
			{
				Destabilize(600);
			}
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo item in base.GetGizmos())
			{
				yield return item;
			}
			if (!DebugSettings.ShowDevGizmos)
			{
				yield break;
			}
			if (destabilizationTick < 0)
			{
				yield return new Command_Action
				{
					defaultLabel = "DEV: Destabilize",
					action = delegate
					{
						Destabilize(600);
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "DEV: Destabilize now",
					action = delegate
					{
						Destabilize(0);
					}
				};
			}
		}

		public override string GetInspectString()
		{
			if (DebugSettings.godMode && destabilizationTick > 0)
			{
				string s1 = "[DEV] Destabilization ticks left: " + (destabilizationTick - Find.TickManager.TicksGame);
				string s2 = base.GetInspectString(); 
				if (!s2.NullOrEmpty())
				{
					return s1 + "\n" + s2;
				}
				return s1;
			}
			return base.GetInspectString();
		}

		public override void Notify_DebugSpawned()
		{
			base.Notify_DebugSpawned();
			SetFaction(Faction.OfEntities);
			for (int i = 0; i < 3; i++)
			{
				Building_HoraxStabilizer thing = (Building_HoraxStabilizer)ThingMaker.MakeThing(NATDefOf.NAT_HoraxStabilizer);
				thing.parent = this;
				thing.SetFaction(Faction.OfEntities);
				stabilizers.Add(thing);
				if(RCellFinder.TryFindRandomCellNearWith(Position, c => GenSpawn.CanSpawnAt(NATDefOf.NAT_HoraxStabilizer, c, Map), Map, out var result, 10))
				{
					GenPlace.TryPlaceThing(thing, result, Map, ThingPlaceMode.Near);
				}
			}
			needStabilizers = true;
		}

		public override void DrawExtraSelectionOverlays()
		{
			base.DrawExtraSelectionOverlays();
			if (stabilizers.NullOrEmpty())
			{
				return;
			}
			foreach(var stabilizer in stabilizers)
			{
				GenDraw.DrawLineBetween(stabilizer.DrawPos, DrawPos, AltitudeLayer.MoteLow.AltitudeFor());
			}
		}
	}
}
