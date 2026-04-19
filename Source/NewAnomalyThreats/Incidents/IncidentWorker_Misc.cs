using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.IO;
using RimWorld.Planet;
using RimWorld.QuestGen;
using RimWorld.SketchGen;
using RimWorld.Utility;
using LudeonTK;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;
using Verse.Noise;
using Verse.Profile;
using Verse.Sound;
using Verse.Steam;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NAT
{
	public class IncidentWorker_ObeliskFromExtension : IncidentWorker_Obelisk
	{
		public override ThingDef ObeliskDef => def.GetModExtension<IncidentExtension>().thingDef;
	}

	public class IncidentWorker_ThingArrive : IncidentWorker
	{
		public virtual ThingDef SkyfallerDef => def.GetModExtension<IncidentExtension>().skyfallerDef;

		public virtual ThingDef ThingDef => def.GetModExtension<IncidentExtension>()?.thingDef;

		public virtual PawnKindDef KindDef => def.GetModExtension<IncidentExtension>()?.pawnKindDef;

		public virtual FactionDef FactionDef => def.GetModExtension<IncidentExtension>()?.factionDef;

		public override float ChanceFactorNow(IIncidentTarget target)
		{
			if (!(target is Map map))
			{
				return base.ChanceFactorNow(target);
			}
			int num = map.listerBuildings.allBuildingsNonColonist.Count((Building b) => b.def.GetCompProperties<CompProperties_Obelisk>() != null);
			return ((num > 0) ? ((float)num * 0.7f) : 1f) * base.ChanceFactorNow(target);
		}

		protected override bool CanFireNowSub(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			IntVec3 cell;
			return TryFindCell(out cell, map, ThingDef ?? KindDef.race);
		}

		protected override bool TryExecuteWorker(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			Skyfaller skyfaller = SpawnIncoming(map);
			if (skyfaller == null)
			{
				return false;
			}
			skyfaller.impactLetter = LetterMaker.MakeLetter(def.letterLabel, def.letterText, def.letterDef ?? LetterDefOf.NeutralEvent, new TargetInfo(skyfaller.Position, map));
			return true;
		}

		private Skyfaller SpawnIncoming(Map map)
		{
			if (!TryFindCell(out var cell, map, ThingDef ?? KindDef.race))
			{
				return null;
			}
			Thing thing;
			Faction faction = FactionDef == null ? null : Find.FactionManager.FirstFactionOfDef(FactionDef);
			if (KindDef != null)
			{
				PreventErrorFactionChange.prevent = true;
				Pawn pawn = PawnGenerator.GeneratePawn(KindDef, faction, map.Tile);
				thing = pawn;
				PreventErrorFactionChange.prevent = false;
			}
			else
			{
				thing = ThingMaker.MakeThing(ThingDef);
				thing.SetFaction(faction);
			}
			return SkyfallerMaker.SpawnSkyfaller(SkyfallerDef, thing, cell, map);
		}

		private bool TryFindCell(out IntVec3 cell, Map map, ThingDef thingDef)
		{
			return CellFinderLoose.TryFindSkyfallerCell(SkyfallerDef, map, thingDef.terrainAffordanceNeeded ?? TerrainAffordanceDefOf.Walkable, out cell, 10, default(IntVec3), -1, allowRoofedCells: true, allowCellsWithItems: false, allowCellsWithBuildings: false, colonyReachable: false, avoidColonistsIfExplosive: true, alwaysAvoidColonists: true, delegate (IntVec3 x)
			{
				if ((float)x.DistanceToEdge(map) < 20f + (float)map.Size.x * 0.1f)
				{
					return false;
				}
				foreach (IntVec3 item in CellRect.CenteredOn(x, thingDef.Size.x, thingDef.Size.z))
				{
					if (!item.InBounds(map) || !item.Standable(map) || !item.GetAffordances(map).Contains(thingDef.terrainAffordanceNeeded ?? TerrainAffordanceDefOf.Walkable))
					{
						return false;
					}
				}
				return true;
			});
		}
	}
}