using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace NAT
{
	public static class NewAnomalyThreatsUtility
	{
		public static NewAnomalyThreatsSettings Settings
		{
			get
			{
				if (settings == null)
				{
					settings = LoadedModManager.GetMod<NewAnomalyThreatsMod>().GetSettings<NewAnomalyThreatsSettings>();
				}
				return settings;
			}
		}

		private static NewAnomalyThreatsSettings settings;

		public static void SetFactionNoError(this Pawn pawn, Faction faction)
		{
			PreventErrorFactionChange.prevent = true;
			pawn.SetFaction(faction);
			PreventErrorFactionChange.prevent = false;
		}

		public static void RemoveHediffs(this Pawn pawn, Predicate<Hediff> validator)
		{
			if(validator == null || pawn == null || pawn.health == null)
			{
				return;
			}
			foreach(Hediff hediff in pawn.health.hediffSet.hediffs.ToList())
			{
				if (validator(hediff))
				{
					pawn.health.RemoveHediff(hediff);
				}
			}
		}
	}
}
