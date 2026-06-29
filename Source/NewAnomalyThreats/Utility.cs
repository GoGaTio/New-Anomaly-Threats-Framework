using LudeonTK;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace NAT
{
	public static class NewAnomalyThreatsUtility
	{
		public static GameComponent_NewAnomalyThreats Comp => gameComp ?? Current.Game.GetComponent<GameComponent_NewAnomalyThreats>();

		public static GameComponent_NewAnomalyThreats gameComp;

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

		/*public static void Log(string message)
		{
			if (Prefs.DevMode)
			{
				Verse.Log.Message(message);
				
			}
		}*/

		public static bool AnyAdjacentCellsWalkable(this IntVec3 cell, Map map)
		{
			
			if (new IntVec3(Mathf.Max(cell.x - 1, 0), 0, cell.z).Walkable(map))
			{
				return true;
			}
			if (new IntVec3(cell.x, 0, Mathf.Max(cell.z - 1, 0)).Walkable(map))
			{
				return true;
			}
			if (new IntVec3(Mathf.Min(cell.x + 1, map.Size.x), 0, cell.z).Walkable(map))
			{
				return true;
			}
			if (new IntVec3(cell.x, 0, Mathf.Min(cell.z + 1, map.Size.z)).Walkable(map))
			{
				return true;
			}
			return false;
		}

		public static IEnumerable<PawnKindDef> GeneratePawnsFromOptions(List<PawnGenOption> options, float points, bool breakOnlyOnZero = true)
		{
			float num = points;
			while (num > 0)
			{
				if(options.TryRandomElementByWeight(WeightGetter, out var result))
				{
					num -= result.kind.combatPower;
					yield return result.kind;
				}
				else
				{
					break;
				}
			}
			float WeightGetter(PawnGenOption option)
			{
				if(!breakOnlyOnZero && option.kind.combatPower > num)
				{
					return 0f;
				}
				return option.selectionWeight;
			}
		}

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

		[DebugOutput("NAT", true)]
		public static void NewAnomalyThreatsComponent()
		{
			Log.Message(Scribe.saver.DebugOutputFor(Current.Game.GetComponent<GameComponent_NewAnomalyThreats>()));
		}
	}
}
