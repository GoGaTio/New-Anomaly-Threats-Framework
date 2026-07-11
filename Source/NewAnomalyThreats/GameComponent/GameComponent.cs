using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;

namespace NAT
{
	public class GameComponent_NewAnomalyThreats : GameComponent
	{
		public List<EntityTracker> entityTrackers = new List<EntityTracker>();

		public List<DataStorer> dataStorage = new List<DataStorer>();

		private Dictionary<RepeatedResearchProjectDef, int> repeatedResearch = new Dictionary<RepeatedResearchProjectDef, int>();

		public AnomalyBossManager bossManager = new AnomalyBossManager();

		public int nextId;

		private int tickInterval;

		public GameComponent_NewAnomalyThreats(Game game)
		{
			NewAnomalyThreatsUtility.gameComp = this;
		}

		public int GetResearchStep(RepeatedResearchProjectDef def)
		{
			if (repeatedResearch.TryGetValue(def, out int value))
			{
				return value;
			}
			return 0;
		}

		public void AdvanceResearch(RepeatedResearchProjectDef def, Pawn researcher)
		{
			if (repeatedResearch.TryGetValue(def, out int value))
			{
				repeatedResearch[def] = value + 1;
			}
			else
			{
				repeatedResearch.Add(def, 1);
			}
			def.Researched(researcher, repeatedResearch[def]);
		}

		public void AddEntityTracker(EntityTracker entityTracker)
		{
			entityTracker.loadID = nextId;
			entityTracker.parent = this;
			nextId++;
			entityTrackers.Add(entityTracker);
		}

		public T TryGetData<T>() where T : DataStorer
		{
			for (int i = 0; i < dataStorage.Count; i++)
			{
				if (dataStorage[i] is T result)
				{
					return result;
				}
			}
			return null;
		}

		public T TryGetOrAddData<T>() where T : DataStorer
		{
			for (int i = 0; i < dataStorage.Count; i++)
			{
				if (dataStorage[i] is T result)
				{
					return result;
				}
			}
			DataStorer newData = Activator.CreateInstance(typeof(T)) as DataStorer;
			dataStorage.Add(newData);
			return newData as T;
		}

		public override void GameComponentTick()
		{
			for (int num = entityTrackers.Count - 1; num >= 0; num--)
			{
				entityTrackers[num].Tick();
			}
			tickInterval--;
			if (tickInterval < 0)
			{
				tickInterval = 2500;
				for (int num = entityTrackers.Count - 1; num >= 0; num--)
				{
					entityTrackers[num].TickRare();
				}
				bossManager.Tick();
			}
			/*foreach(EntityTracker item1 in entityTrackers.ToList())
			{
				item1.Tick();
			}
			tickInterval--;
			if (tickInterval < 0)
			{
				tickInterval = 2500;
				foreach (EntityTracker item2 in entityTrackers.ToList())
				{
					item2.TickRare();
				}
				bossManager.Tick();
			}*/
		}

		public override void ExposeData()
		{
			base.ExposeData();
			if(Scribe.mode == LoadSaveMode.Saving)
			{
				foreach (EntityTracker item in entityTrackers.ToList())
				{
					if (item.ShouldRemove)
					{
						entityTrackers.Remove(item);
					}
				}
			}
			Scribe_Deep.Look(ref bossManager, "bossManager");
			Scribe_Collections.Look(ref repeatedResearch, "repeatedResearch", LookMode.Def, LookMode.Value);
			Scribe_Collections.Look(ref entityTrackers, "entityTrackers", LookMode.Deep);
			Scribe_Collections.Look(ref dataStorage, "dataStorage", LookMode.Deep);
			Scribe_Values.Look(ref nextId, "nextId");
			Scribe_Values.Look(ref tickInterval, "tickInterval");
			foreach (EntityTracker item in entityTrackers)
			{
				item.parent = this;
			}
		}

		public void PostGameLoaded()
		{
			NewAnomalyThreatsUtility.gameComp = this;
			bossManager.InitBosses();
			;
			foreach (RepeatedResearchProjectDef item in DefDatabase<RepeatedResearchProjectDef>.AllDefsListForReading)
			{
				if (repeatedResearch.TryGetValue(item, out int count))
				{
					item.EnsureCostIsCorrect(count);
				}
				else
				{
					item.EnsureCostIsCorrect(0);
				}
			}
		}

		public override void LoadedGame()
		{
			base.LoadedGame();
			PostGameLoaded();
		}

		public override void StartedNewGame()
		{
			base.StartedNewGame();
			PostGameLoaded();
		}
	}
}
