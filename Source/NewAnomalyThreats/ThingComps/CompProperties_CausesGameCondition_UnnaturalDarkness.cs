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
	public class CompProperties_CausesGameCondition_UnnaturalDarkness : CompProperties_CausesGameCondition
	{
		public IntRange initialPhaseDurationRange = new IntRange(30000, 45000);

		public CompProperties_CausesGameCondition_UnnaturalDarkness()
		{
			worldRange = 0;
			compClass = typeof(CompCauseGameCondition_UnnaturalDarkness);
		}

		public override void ResolveReferences(ThingDef parentDef)
		{
			base.ResolveReferences(parentDef);
			if (conditionDef == null)
			{
				conditionDef = GameConditionDefOf.UnnaturalDarkness;
			}
		}
	}

	public class CompCauseGameCondition_UnnaturalDarkness : CompCauseGameCondition
	{
		public new CompProperties_CausesGameCondition_UnnaturalDarkness Props => (CompProperties_CausesGameCondition_UnnaturalDarkness)props;

		public int initialPhaseTicks;

		public override void PostPostMake()
		{
			base.PostPostMake();
			initialPhaseTicks = Props.initialPhaseDurationRange.RandomInRange;
		}

		public override void CompTick()
		{
			if(initialPhaseTicks > 0)
			{
				initialPhaseTicks--;
				if (initialPhaseTicks <= 0)
				{
					ReSetupAllConditions();
				}
			}
			base.CompTick();
			foreach (GameCondition condition in CausedConditions)
			{
				if (condition.TicksLeft + 1 < condition.TransitionTicks)
				{
					condition.Permanent = false;
					foreach (Map map in condition.AffectedMaps)
					{
						map.gameConditionManager.SetTargetBrightness(1f);
					}
					condition.TicksLeft = 0;
				}
			}
		}

		public override void CompTickInterval(int delta)
		{
			base.CompTickInterval(delta);
			if (initialPhaseTicks <= 0 && parent.IsHashIntervalTick(60, delta))
			{
				foreach (GameCondition condition in CausedConditions)
				{
					if (condition.TicksLeft + 1 >= condition.TransitionTicks)
					{
						foreach (Map map in condition.AffectedMaps)
						{
							map.gameConditionManager.SetTargetBrightness(0f);
						}
					}
				}
			}
		}

		public override void PostDestroy(DestroyMode mode, Map previousMap)
		{
			foreach (GameCondition condition in CausedConditions)
			{
				condition.Permanent = false;
				foreach (Map map in condition.AffectedMaps)
				{
					map.gameConditionManager.SetTargetBrightness(1f);
				}
				condition.TicksLeft = 0;
			}
			base.PostDestroy(mode, previousMap);
		}

		protected override void SetupCondition(GameCondition condition, Map map)
		{
			base.SetupCondition(condition, map);
			if (initialPhaseTicks <= 0)
			{
				map.gameConditionManager.SetTargetBrightness(0f);
			}
		}

		public override string CompInspectStringExtra()
		{
			return parent.Map.skyManager.CurSky.glow.ToStringPercent();
		}
	}
}