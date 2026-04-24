using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;

namespace NAT
{
	public class EntityTracker : IExposable, ILoadReferenceable
	{
		public int loadID;

		public virtual void ExposeData()
		{
			Scribe_Values.Look(ref loadID, "loadID", 0);
		}

		public virtual string GetUniqueLoadID()
		{
			return $"NAT_EntityTracker_{loadID}";
		}

		public virtual void Tick()
		{

		}

		public virtual void TickRare()
		{

		}
	}

	public class DataStorer : IExposable
	{
		public virtual void ExposeData()
		{
		}
	}

	public class BossTracker : IExposable, ILoadReferenceable
	{
		public int loadID;

		public Pawn boss;

		public AnomalyBossDef bossKind;

		public CompBossStages CompBoss => boss.GetComp<CompBossStages>();

		public virtual void ExposeData()
		{
			Scribe_Values.Look(ref loadID, "loadID", 0);
			Scribe_References.Look(ref boss, "boss", saveDestroyedThings: true);
			Scribe_Defs.Look(ref bossKind, "bossKind");
		}

		public virtual string GetUniqueLoadID()
		{
			return $"NAT_AnomalyBossTracker_{loadID}";
		}

		public virtual void Tick()
		{
			if (boss != null && boss.Dead)
			{
				CompBossStages comp = CompBoss;
				if (comp != null && comp.CanAdvanceStage)
				{

				}
				else
				{

				}
			}
		}

		public void Resurrect(Pawn pawn)
		{
			if (pawn.Discarded)
			{
				Log.Warning("New Anomaly Threats - " + pawn.LabelCap + " was discarded during resurrection, fixing");
				pawn.ForceSetStateToUnspawned();
				pawn.DecrementMapIndex();
			}
			ResurrectionParams parms = new ResurrectionParams();
			parms.restoreMissingParts = true;
			parms.dontSpawn = true;
			ResurrectionUtility.TryResurrect(pawn, parms);
			pawn.RemoveHediffs((x) => x is Hediff_Injury || x.Part == null || !x.Part.def.tags.Any((y) => y == BodyPartTagDefOf.ConsciousnessSource));
			/*GenSpawn.Spawn(pawn, PositionHeld, MapHeld);
			try
			{
				if (rust.Faction != null && !rust.Faction.IsPlayer)
				{
					Lord lord = rust.GetLord();
					if (lord != null && lord.LordJob is LordJob_RustedArmy)
					{
						lord?.Notify_PawnUndowned(rust);
					}
					else if (rust.Faction == Faction.OfEntities && ((lord = MapHeld.lordManager.lords.FirstOrDefault((Lord x) => x.LordJob is LordJob_RustedArmy)) != null))
					{
						lord.AddPawn(rust);
					}
					else
					{
						LordJob lordJob = null;
						if (rust.Faction == Faction.OfEntities)
						{
							lordJob = new LordJob_RustedArmy(IntVec3.Invalid, -1);
						}
						else
						{
							lordJob = new LordJob_AssaultColony(rust.Faction, false, false, false, false, false, false, false);
						}
						LordMaker.MakeNewLord(lordJob: lordJob, faction: rust.Faction, map: MapHeld, startingPawns: Gen.YieldSingle(rust));
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("New Anomaly Threats - Error in RustedCore.Resurrect(Lord maker part): " + ex);
			}*/
		}
	}

	public class GameComponent_NewAnomalyThreats : GameComponent
	{
		public List<EntityTracker> entityTrackers = new List<EntityTracker>();

		public List<DataStorer> dataStorage = new List<DataStorer>();

		public List<BossTracker> bossTrackers = new List<BossTracker>();

		public int nextId;

		public GameComponent_NewAnomalyThreats(Game game)
		{
		}

		public void AddEntityTracker(EntityTracker entityTracker)
		{
			entityTracker.loadID = nextId;
			nextId++;
			entityTrackers.Add(entityTracker);
		}

		public void AddBossTracker(BossTracker bossTracker)
		{
			bossTracker.loadID = nextId;
			nextId++;
			bossTrackers.Add(bossTracker);
		}

		public override void GameComponentTick()
		{
			foreach(EntityTracker item1 in entityTrackers.ToList())
			{
				item1.Tick();
			}
			if(Find.TickManager.TicksGame % 2500 == 0)
			{
				foreach (EntityTracker item2 in entityTrackers.ToList())
				{
					item2.TickRare();
				}
				foreach (BossTracker item3 in bossTrackers.ToList())
				{
					item3.Tick();
				}
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref entityTrackers, "entityTrackers", LookMode.Deep);
			Scribe_Collections.Look(ref dataStorage, "dataStorage", LookMode.Deep);
			Scribe_Collections.Look(ref bossTrackers, "bossTrackers", LookMode.Deep);
			Scribe_Values.Look(ref nextId, "nextId");
		}
	}
}
