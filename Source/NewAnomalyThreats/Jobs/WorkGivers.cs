using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using static UnityEngine.GraphicsBuffer;

namespace NAT
{
	public class WorkGiver_Delivery : WorkGiver_Scanner
	{
		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			foreach (Building item in pawn.Map.listerBuildings.allBuildingsColonist)
			{
				if (item is Interfaces b && b.NeedDelivery)
				{
					yield return item;
				}
			}
			//return Building_WantsDelivery.buildings.Where((x) => x.Map == pawn.Map);
		}

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (t is Interfaces b)
			{
				/*if (!b.NeedDelivery)
				{
					return false;
				}*/
				if (!pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger(), 1, -1, null, forced))
				{
					return false;
				}
				if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
				{
					return false;
				}
				if (t.IsBurning())
				{
					return false;
				}
				if (b.FindItem(pawn).Thing == null)
				{
					return false;
				}
				return true;
			}
			return false;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Interfaces b))
			{
				return null;
			}
			if (!pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger(), 1, -1, null, forced))
			{
				return null;
			}
			ThingCount thing = b.FindItem(pawn);
			if(thing.Thing == null)
			{
				return null;
			}
			Job job = JobMaker.MakeJob(NATDefOf.NAT_Delivery, t, thing.Thing);
			job.count = thing.Count;
			return job;
		}
	}
}
