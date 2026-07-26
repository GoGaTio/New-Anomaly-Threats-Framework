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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Xml.Xsl;
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
	public class CompProperties_AutoResearcher : CompProperties
	{
		public IntRange researchIntervalTicks;

		public bool anomalyResearch = true;

		public FloatRange progressAmount;

		public CompProperties_AutoResearcher()
		{
			compClass = typeof(CompAutoResearcher);
		}
	}

	public class CompAutoResearcher : ThingComp
	{
		public CompProperties_AutoResearcher Props => (CompProperties_AutoResearcher)props;

		[Unsaved(false)]
		private CompPowerTrader cachedPowerComp;

		public CompPowerTrader PowerTraderComp
		{
			get
			{
				if (cachedPowerComp == null)
				{
					cachedPowerComp = parent.TryGetComp<CompPowerTrader>();
				}
				return cachedPowerComp;
			}
		}

		public int ticksLeftTillResearch;

		public override void PostPostMake()
		{
			base.PostPostMake();
			ticksLeftTillResearch = Props.researchIntervalTicks.RandomInRange;
		}

		public override void CompTick()
		{
			base.CompTick();
			if (parent.Spawned)
			{
				ticksLeftTillResearch--;
				if(ticksLeftTillResearch < 0)
				{
					ticksLeftTillResearch = Props.researchIntervalTicks.RandomInRange;
					if (!PowerTraderComp.PowerOn)
					{
						return;
					}
					float progress = Props.progressAmount.RandomInRange;
					if (Props.anomalyResearch)
					{
						KnowledgeCategoryDef knowledgeCategory = KnowledgeCategoryDefOf.Advanced;
						if (Find.ResearchManager.GetProject(knowledgeCategory) == null && Find.ResearchManager.GetProject(knowledgeCategory.overflowCategory) == null)
						{
							return;
						}
						MoteMaker.ThrowText(parent.DrawPos, parent.Map, $"{knowledgeCategory.LabelCap} +{progress:0.00}", 3f);
						Find.ResearchManager.ApplyKnowledge(knowledgeCategory, progress);
					}
					else
					{
						ResearchProjectDef project = Find.ResearchManager.GetProject();
						if(project != null)
						{
							Find.ResearchManager.AddProgress(project, progress);
						}
					}
				}
			}
		}

		public override string CompInspectStringExtra()
		{
			if (PowerTraderComp.PowerOn)
			{
				KnowledgeCategoryDef knowledgeCategory = KnowledgeCategoryDefOf.Advanced;
				if (Find.ResearchManager.GetProject(knowledgeCategory) != null || Find.ResearchManager.GetProject(knowledgeCategory.overflowCategory) != null)
				{
					return ticksLeftTillResearch.ToStringTicksToPeriod();
				}
			}
			return base.CompInspectStringExtra();
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref ticksLeftTillResearch, "ticksLeftTillResearch");
		}
	}
}