using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using static RimWorld.Reward_Pawn;

namespace NAT
{
	public abstract class AnomalyBoss : IExposable
	{
		public AnomalyBossDef def;

		public int lastCalledTick = 0;

		public int timesCalled = 0;

		public int ticksIncomingLeft = -10;

		public sbyte mapIndex = -1;

		public IntVec3 callCell;

		public Map Map
		{
			get
			{
				if (mapIndex >= 0)
				{
					return Find.Maps?[mapIndex];
				}
				return null;
			}
		}

		public virtual void Init()
		{

		}

		public virtual void Tick()
		{
			if (ticksIncomingLeft > 0)
			{
				ticksIncomingLeft--;
				if (ticksIncomingLeft <= 0 && TryArrive())
				{
					ticksIncomingLeft = -10;
					PostArrived();
				}
			}
		}

		public abstract string Confirmation(Map map);

		public virtual AcceptanceReport CanCall(IntVec3 cell, Map map)
		{
			return true;
		}

		public virtual void Call(Pawn pawn, IntVec3 cell, Map map)
		{
			ticksIncomingLeft = def.arrivalTimeHoursRange.RandomInRange;
			lastCalledTick = Find.TickManager.TicksGame;
			mapIndex = (sbyte)map.Index;
			timesCalled++;
			this.callCell = cell;
		}

		public abstract bool TryArrive();

		public virtual void PostArrived()
		{
			mapIndex = -1;
			callCell = IntVec3.Invalid;
		}

		public virtual void ExposeData()
		{
			Scribe_Values.Look(ref ticksIncomingLeft, "ticksIncomingLeft", defaultValue: -10);
			Scribe_Values.Look(ref lastCalledTick, "lastCalledTick", -999999);
			Scribe_Values.Look(ref timesCalled, "timesCalled", 0);
			Scribe_Values.Look(ref callCell, "callCell");
			Scribe_Values.Look<sbyte>(ref mapIndex, "mapIndex", -1);
		}
	}

	public abstract class AnomalyBoss_PawnGroup : AnomalyBoss
	{
		private AnomalyBossDef_PawnGroup Def => (AnomalyBossDef_PawnGroup)def;

		public List<PawnKindDef> escorts = null;

		public List<PawnKindDefCount> cachedEscorts = new List<PawnKindDefCount>();

		public float lastPoints = -1;

		public string BossWaveComposition(Map map)
		{
			string s = "NAT_BossEscorts".Translate(def.label, Def.escortsLabel ?? FactionDefOf.Entities.pawnsPlural) + ":"; //"Summoning {0} threat will summon following hostile {1}:"
			float points = StorytellerUtility.DefaultThreatPointsNow(map);
			List<PawnKindDefCount> list = GetEscorts(map, points);
			foreach (PawnKindDefCount item in list)
			{
				s += "\n" + "  - " + GenLabel.BestKindLabel(item.kindDef, Gender.None).CapitalizeFirst() + " x" + item.count;
			}
			return s;
		}

		public virtual float ProcessPoints(float points, Map map)
		{
			return Mathf.Clamp(points, Def.minPoints, Def.maxPoints);
		}

		public List<PawnKindDefCount> GetEscorts(Map map) => GetEscorts(map, StorytellerUtility.DefaultThreatPointsNow(map));

		public List<PawnKindDefCount> GetEscorts(Map map, float points)
		{
			float processedPoints = ProcessPoints(points, map);
			if (escorts.NullOrEmpty() || Mathf.Abs(processedPoints - lastPoints) > Mathf.Min(lastPoints, processedPoints) * 0.1f)
			{
				escorts?.Clear();
				escorts = new List<PawnKindDef>();
				escorts.AddRange(Def.GetBossGroup(timesCalled, processedPoints).Generate(processedPoints));
				lastPoints = processedPoints;
				ReCacheEscorts();
			}
			return cachedEscorts;
		}

		public virtual void ReCacheEscorts()
		{
			cachedEscorts = new List<PawnKindDefCount>();
			foreach (PawnKindDef def in escorts)
			{
				PawnKindDefCount item = cachedEscorts.FirstOrDefault(x => x.kindDef == def);
				if (item == null)
				{
					cachedEscorts.Add(new PawnKindDefCount() { count = 1, kindDef = def });
				}
				else
				{
					item.count++;
				}
			}
		}

		public virtual IEnumerable<Pawn> GeneratePawns()
		{
			PlanetTile tile = Map.Tile;
			foreach (PawnKindDef item in escorts)
			{
				yield return PawnGenerator.GeneratePawn(item, Faction.OfEntities, tile);
			}
		}

		public override AcceptanceReport CanCall(IntVec3 cell, Map map)
		{
			if (Def.arrivalMode == null || Def.arrivalMode.Worker.CanUseOnMap(map))
			{
				return base.CanCall(cell, map);
			}
			return false;
		}

		public override bool TryArrive()
		{
			if(Map == null)
			{
				return false;
			}
			List<Pawn> list = GeneratePawns().ToList();
			if (!TryArriveInt(list))
			{
				return false;
			}
			GenerateLord(list, Map);
			PrePostArrived(list);
			return true;
		}

		public virtual bool TryArriveInt(List<Pawn> list)
		{
			IncidentParms incidentParms = new IncidentParms();
			incidentParms.target = Map;
			incidentParms.spawnCenter = callCell;
			PawnsArrivalModeDef pawnsArrivalModeDef = Def.arrivalMode;
			if (!pawnsArrivalModeDef.Worker.CanUseOnMap(Map))
			{
				foreach (PawnsArrivalModeDef item in DefDatabase<PawnsArrivalModeDef>.AllDefsListForReading.InRandomOrder())
				{
					if (item.canBeBackup && item.Worker.CanUseOnMap(Map))
					{
						pawnsArrivalModeDef = item;
						break;
					}
				}
			}
			if (!pawnsArrivalModeDef.Worker.CanUseOnMap(Map))
			{
				return false;
			}
			if (!pawnsArrivalModeDef.Worker.TryResolveRaidSpawnCenter(incidentParms))
			{
				return false;
			}
			pawnsArrivalModeDef.Worker.Arrive(list, incidentParms);
			return true;
		}

		public virtual void GenerateLord(List<Pawn> list, Map map)
		{
			
		}

		public virtual void PrePostArrived(List<Pawn> list)
		{

		}

		public override void PostArrived()
		{
			escorts = null;
			lastPoints = -1;
			base.PostArrived();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref lastPoints, "lastPoints", -1);
			Scribe_Collections.Look(ref escorts, "escorts", LookMode.Def);
		}
	}

	public class AnomalyBoss_Pawn : AnomalyBoss_PawnGroup
	{
		private AnomalyBossDef_Pawn Def => (AnomalyBossDef_Pawn)def;

		public override string Confirmation(Map map)
		{
			return BossWaveComposition(map);
		}

		public override IEnumerable<Pawn> GeneratePawns()
		{
			yield return PawnGenerator.GeneratePawn(Def.bossKind, Faction.OfEntities, Map.Tile);
			foreach(Pawn pawn in base.GeneratePawns())
			{
				yield return pawn;
			}
		}

		public override void GenerateLord(List<Pawn> list, Map map)
		{
			LordMaker.MakeNewLord(Faction.OfEntities, new LordJob_BossgroupAssaultColony(Faction.OfEntities, list[0].PositionHeld, Gen.YieldSingle(list[0])), map, list);
		}
	}
}
