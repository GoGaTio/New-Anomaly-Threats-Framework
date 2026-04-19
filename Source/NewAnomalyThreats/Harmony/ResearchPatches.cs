using DelaunatorSharp;
using Gilzoide.ManagedJobs;
using HarmonyLib;
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
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

namespace NAT
{
	public static class Patches_Research
    {
		public static IEnumerable<CodeInstruction> UniversalTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> list = new List<CodeInstruction>(instructions);
			for (int i = 0; i < list.Count - 1; i++)
			{
				if (list[i].opcode == OpCodes.Ldsfld && list[i].operand.ToString().Contains("ResearchTabDef Anomaly"))
				{
					list[i] = new CodeInstruction(OpCodes.Call, (object)AccessTools.Method(typeof(Patches_Research), "ResearchTabAnomalyOverride", (Type[])null, (Type[])null));
					/*if (list[i + 1].opcode == OpCodes.Bne_Un_S)
					{
						list[i + 1].opcode = OpCodes.Brfalse_S;
					}
					else if (list[i + 1].opcode == OpCodes.Bne_Un)
					{
						list[i + 1].opcode = OpCodes.Brfalse_S;
					}
					else if (list[i + 1].opcode == OpCodes.Beq_S)
					{
						list[i + 1].opcode = OpCodes.Brtrue_S;
					}*/
				}
			}
			return list.AsEnumerable();
		}

		public static ResearchTabDef ResearchTabAnomalyOverride()
		{
			MainTabWindow_Research tab = Find.MainTabsRoot?.OpenTab?.TabWindow as MainTabWindow_Research;
			if(tab == null)
			{
				return null;
			}
			ResearchTabDef def = tab.CurTab;
			if (def != ResearchTabDefOf.Anomaly && def.defName == "NewAnomalyThreats")
			{
				return def;
			}
			return ResearchTabDefOf.Anomaly;
		}
	}
}