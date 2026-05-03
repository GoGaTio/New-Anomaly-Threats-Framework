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

		public GameComponent_NewAnomalyThreats parent;

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

	public class AnomalyBossTracker : EntityTracker
	{
		public Pawn boss;

		public CompBossStages CompBoss => boss.GetComp<CompBossStages>();

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref boss, "boss", saveDestroyedThings: true);
		}

		public override void Tick()
		{
			base.Tick();
			if(boss != null && boss.Dead)
			{
				TryResurrect(boss);
			}
		}

		public void TryResurrect(Pawn pawn)
		{
			if (pawn.Discarded)
			{
				Log.Warning("New Anomaly Threats - " + pawn.LabelCap + " was discarded during resurrection, fixing");
				pawn.ForceSetStateToUnspawned();
				pawn.DecrementMapIndex();
			}
			CompBossStages comp = CompBoss;
			if(comp == null || !comp.CanAdvanceStage)
			{
				parent.entityTrackers.Remove(this);
				return;
			}
			ResurrectionParams parms = new ResurrectionParams();
			parms.restoreMissingParts = true;
			parms.dontSpawn = true;
			ResurrectionUtility.TryResurrect(pawn, parms);
			pawn.RemoveHediffs((x) => x is Hediff_Injury || x.Part == null || !x.Part.def.tags.Any((y) => y == BodyPartTagDefOf.ConsciousnessSource));
			GenSpawn.Spawn(pawn, comp.preDeathPos, comp.preDeathMap);
			if(comp.preDeathLord != null)
			{
				comp.preDeathLord.AddPawn(pawn);
			}
			comp.TryGoNextStage();
			/*try
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
}
