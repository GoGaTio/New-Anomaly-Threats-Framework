using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DelaunatorSharp;
using Gilzoide.ManagedJobs;
using Ionic.Crc;
using Ionic.Zlib;
using JetBrains.Annotations;
using KTrie;
using LudeonTK;
using NVorbis.NAudioSupport;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.IO;
using RimWorld.Planet;
using RimWorld.QuestGen;
using RimWorld.SketchGen;
using RimWorld.Utility;
using RuntimeAudioClipLoader;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;
using Verse.Noise;
using Verse.Profile;
using Verse.Sound;
using Verse.Steam;
using HarmonyLib;

namespace NAT
{
	/*[HarmonyPatch(typeof(PsychicRitualToil_GatherForInvocation), "InvokerGatherPhaseToils")]
	public static class Patch_InvokerGatherPhaseToils
	{
		[HarmonyPostfix]
		public static IEnumerable<PsychicRitualToil> Postfix(IEnumerable<PsychicRitualToil> __result, PsychicRitualDef_InvocationCircle def)
		{
			if (def is PsychicRitualDef_AdditionalOfferings ritual)
			{
				yield return new PsychicRitualToil_BringAdditionalOfferings(ritual);
			}
			foreach (PsychicRitualToil toil in __result)
			{
				yield return toil;
			}
		}
	}*/

	[HarmonyPatch(typeof(CompDisruptorFlare), nameof(CompDisruptorFlare.PostSpawnSetup))]
	public class Patch_CompDisruptorFlare
	{
		[HarmonyPostfix]
		public static void Postfix(bool respawningAfterLoad, CompDisruptorFlare __instance)
		{
			if (respawningAfterLoad)
			{
				return;
			}
			IntVec3 pos = __instance.parent.Position;
			float num = __instance.Props.radius;
			Map map = __instance.parent.Map;
			List<Thing> list = new List<Thing>();
			foreach (IntVec3 cell in CellRect.FromCell(pos).ExpandedBy(Mathf.CeilToInt(num)).ClipInsideMap(map))
			{
				foreach(Thing t in cell.GetThingList(map).ToList())
				{
					if (t is ThingWithComps twc && !list.Contains(t) && cell.DistanceTo(pos) <= num)
					{
						twc.BroadcastCompSignal("AffectedByFlare");
						list.Add(t);
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(TaleRecorder))]
	[HarmonyPatch(nameof(TaleRecorder.RecordTale))]
	public class Patch_Tales
	{
		[HarmonyPrefix]
		public static bool Prefix(TaleDef def, params object[] args)
		{
			if (!typeof(Tale_SinglePawn).IsAssignableFrom(def.taleClass))
			{
				return true;
			}
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i] is Pawn pawn)
				{
					return true;
				}
			}
			return false;
		}
	}

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.IsDuplicate), MethodType.Getter)]
	public class PreventErrorFactionChange
	{
		public static bool prevent = false;
		[HarmonyPostfix]
		public static void Postfix(ref bool __result, Pawn __instance)
		{
			if (__result) return;
            if (prevent)
            {
				__result = true;
			}
		}
	}

	[HarmonyPatch(typeof(Verb), nameof(Verb.ApparelPreventsShooting))]
	public class Patch_ApparelPreventsShooting
	{
		[HarmonyPostfix]
		public static void Postfix(ref bool __result, Verb __instance)
		{
			if (__result) return;
			if (__instance.CasterPawn?.health?.hediffSet.HasHediff<Hediff_Subdued>() == true)
			{
				__result = true;
			}
		}
	}

	[HarmonyPatch(typeof(QuestPart_EntityArrival), "Notify_QuestSignalReceived")]
	public class Patch_EntityArrivalOverride
	{
		private static readonly SimpleCurve ChanceOfOverrideByDefsCount = new SimpleCurve
		{
			new CurvePoint(0f, 0f),
			new CurvePoint(1f, 0.2f),
			new CurvePoint(5f, 0.55f),
			new CurvePoint(10f, 0.75f)
		};

		[HarmonyPrefix]
		public static bool Prefix(Map ___map, Signal signal, string ___inSignal)
		{
			if (!signal.tag.StartsWith(___inSignal) || !NewAnomalyThreatsUtility.Settings.allowEndGameRaid)
			{
				return true;
			}
			VoidAwakeningUtility.DecodeWaveType(signal.tag, out var waveType, out var pointsFactor);
			if (waveType != VoidAwakeningUtility.WaveType.Twisted)
			{
				return true;
			}
			List<EndGameRaidDef> defs = DefDatabase<EndGameRaidDef>.AllDefsListForReading;
			if (defs.NullOrEmpty())
			{
				return true;
			}
			defs.RemoveWhere((x) => !x.isWave);
			if (!Rand.Chance(NewAnomalyThreatsUtility.Settings.endGameRaidChanceFactor * ChanceOfOverrideByDefsCount.Evaluate(defs.Sum((x)=>x.commonality))))
			{
				return true;
			}
			bool flag = false;
			try
			{
				EndGameRaidDef def = defs.RandomElementByWeight((x) => x.commonality);
				flag = def.TryFireWave(___map, StorytellerUtility.DefaultThreatPointsNow(___map));
			}
			catch (Exception ex)
			{
				Log.Error("New Anomaly Threats - Could not create entity wave:" + ex);
				return true;
			}
			return !flag;
		}
	}

	[HarmonyPatch(typeof(BackCompatibility), nameof(BackCompatibility.BackCompatibleDefName))]
	public class Patch_BackCompatibility
	{
		[HarmonyPrefix]
		public static bool Prefix(Type defType, string defName, ref string __result)
		{
			string newDefName = BackCompatibleDefName(defType, defName);
			if (newDefName != null)
			{
				__result = newDefName;
				return false;
			}
			return true;
		}

		public static string BackCompatibleDefName(Type defType, string defName)
		{
			if (defType == typeof(ThingDef))
			{
				switch (defName)
				{
					case "NAT_EliteRustedSoldier":
						return "NAT_RustedSoldier";
					case "NAT_RustedFieldMarshal":
						return "NAT_RustedOfficer";
					case "NAT_RustedSphere":
						return "NAT_RustedMass";
					case "NAT_Collector_Reworked":
						return "NAT_Collector";
					case "NAT_RustedSculpture_Rifleman":
					case "NAT_RustedSculpture_Gunner":
					case "NAT_RustedSculpture_Grenadier":
					case "NAT_RustedSculpture_Hussar":
						return "NAT_RustedSculpture_Soldier";
					case "NAT_Gun_RustedHeatspiker":
						return "NAT_Gun_HeatspikeRifle";
					case "NAT_Gun_RustedFleshmelter":
						return "NAT_Gun_Fleshmelter";
				}
			}
			if (defType == typeof(PawnKindDef))
			{
				switch (defName)
				{
					case "NAT_EliteRustedSoldier":
						return "NAT_RustedSoldier";
					case "NAT_RustedFieldMarshal":
						return "NAT_RustedOfficer";
					case "NAT_RustedSphere":
						return "NAT_RustedMass";
					case "NAT_Collector_Reworked":
						return "NAT_Collector";
				}
			}
			if (defType == typeof(JobDef))
			{
				switch (defName)
				{
					case "NAT_CollectorWait_Reworked":
						return "NAT_CollectorWait";
					case "NAT_CollectorSteal_Reworked":
						return "NAT_CollectorSteal";
					case "NAT_RustedSphere":
						return "NAT_RustedMass";
					case "NAT_Collector_Reworked":
						return "NAT_Collector";
				}
			}
			return null;
		}
	}
}