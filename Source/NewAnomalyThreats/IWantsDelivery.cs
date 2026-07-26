using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NAT
{
	public interface IWantsDelivery 
	{
		bool NeedDelivery { get; }

		ThingCount FindItem(Pawn pawn);
	}
}
