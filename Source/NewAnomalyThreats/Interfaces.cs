using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using static Verse.PawnCapacityUtility;

namespace NAT
{
	public interface ICapacityAffect
	{
		float AffectCapacity(float level, HediffSet diffSet, PawnCapacityDef capacity, ref List<CapacityImpactor> impactors);
	}

	public interface Interfaces 
	{
		bool NeedDelivery { get; }

		ThingCount FindItem(Pawn pawn);
	}

	public interface IAlwaysTargetable
	{
	}

	public interface IAnomalyEvent
	{
		Def Def { get; }

		float CommonalityFactor { get; set; }

		bool AdjustPoints { get; }

		float PointsFactor { get; set; }
	}
}
