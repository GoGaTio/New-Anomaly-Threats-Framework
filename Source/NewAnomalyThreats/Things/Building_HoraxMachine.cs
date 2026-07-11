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

		private int destabilizationTick = -1;

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			if (base.Spawned)
			{
				Skyfaller.DrawDropSpotShadow(drawLoc, Rot4.North, ShadowMaterial, def.size.ToVector2(), 65);
				float a = 0.55f + 0.5f * (1f + Mathf.Sin(Mathf.PI * 2f * (float)GenTicks.TicksGame / 300f)) * 0.35f;
				float t = ((destabilizationTick == -1) ? 0f : Mathf.Pow(1f - Mathf.Clamp01((float)(destabilizationTick - Find.TickManager.TicksGame) / 120f), 1.75f));
				float num = Mathf.Lerp(a, 0f, t);
				float extraRotation = Mathf.Lerp(0f, 45f, t);
				if(destabilizationTick > 0)
				{
					drawLoc.x += Mathf.Sin(destabilizationTick - Find.TickManager.TicksGame) * 0.05f;
					drawLoc.z += Mathf.Sin(destabilizationTick - Find.TickManager.TicksGame * 1.1f) * 0.05f;
				}
				drawLoc.z += num;
				Graphic.Draw(drawLoc, flip ? base.Rotation.Opposite : base.Rotation, this, extraRotation);
				SilhouetteUtility.DrawGraphicSilhouette(this, drawLoc);
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
						Destabilize(300);
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
	}
}
