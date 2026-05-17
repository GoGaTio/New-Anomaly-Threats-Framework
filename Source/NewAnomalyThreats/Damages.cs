using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace NAT
{
	public class DamageWorker_Nail : DamageWorker_Stab
	{
		public override DamageResult Apply(DamageInfo dinfo, Thing thing)
		{
			DamageResult damageResult = base.Apply(dinfo, thing);
			if(!damageResult.deflected && !damageResult.diminished && damageResult.wounded && thing is Pawn pawn && pawn.RaceProps.IsFlesh && !pawn.health.hediffSet.PartIsMissing(damageResult.LastHitPart) && Rand.Chance(0.1f))
			{
				pawn.health.AddHediff(NATDefOf.NAT_Nail, damageResult.LastHitPart, dinfo);
			}
			return damageResult;
		}
	}
}
