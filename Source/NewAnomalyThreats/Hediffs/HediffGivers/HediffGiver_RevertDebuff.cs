using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace NAT
{
	public class HediffGiver_RevertDebuff : HediffGiver
	{
		public HediffDef debuffDef;

		public HediffDef buffDef;

		public override bool OnHediffAdded(Pawn pawn, Hediff hediff)
		{
			if (hediff.def == debuffDef)
			{
				pawn.health.GetOrAddHediff(buffDef).Severity += hediff.Severity;
				pawn.health.RemoveHediff(hediff);
			}
			return false;
		}

		public override IEnumerable<string> ConfigErrors()
		{
			if(buffDef == debuffDef)
			{
				yield return "NAT.HediffGiver_RevertDebuff has same buffDef and debuffDef, this would cause crash if debuff applied";
			}
		}
	}
}
