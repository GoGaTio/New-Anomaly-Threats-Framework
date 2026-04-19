using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

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
			return $"EntityTracker_{loadID}";
		}
	}

	public class DataStorer : IExposable
	{
		public virtual void ExposeData()
		{
		}
	}

	public class GameComponent_NewAnomalyThreats : GameComponent
	{
		public List<EntityTracker> entityTrackers = new List<EntityTracker>();

		public List<DataStorer> dataStorage = new List<DataStorer>();

		public GameComponent_NewAnomalyThreats(Game game)
		{
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref entityTrackers, "entityTrackers", LookMode.Deep);
			Scribe_Collections.Look(ref dataStorage, "dataStorage", LookMode.Deep);
		}
	}
}
