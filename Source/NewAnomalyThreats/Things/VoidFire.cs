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

namespace NAT
{
	public class VoidFire : ThingWithComps, ISizeReporter
	{
		public float fireSize = 1f;

		public int fireTicksLeft = -1;

		public Thing instigator;

		public override int UpdateRateTicks => 30;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref fireTicksLeft, "fireTicksLeft", -1);
			Scribe_Values.Look(ref fireSize, "fireSize", 0f);
			Scribe_References.Look(ref instigator, "instigator");
		}

		public float CurrentSize()
		{
			return fireSize;
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			fireTicksLeft = 2500;
			base.SpawnSetup(map, respawningAfterLoad);
			RecalcPathsOnAndAroundMe(map);
		}

		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			Map map = base.Map;
			base.DeSpawn(mode);
			RecalcPathsOnAndAroundMe(map);
		}

		private void RecalcPathsOnAndAroundMe(Map map)
		{
			IntVec3[] adjacentCellsAndInside = GenAdj.AdjacentCellsAndInside;
			for (int i = 0; i < adjacentCellsAndInside.Length; i++)
			{
				IntVec3 c = base.Position + adjacentCellsAndInside[i];
				if (c.InBounds(map))
				{
					map.pathing.RecalculatePerceivedPathCostAt(c);
				}
			}
		}

		protected override void TickInterval(int delta)
		{
			fireTicksLeft -= delta;
			List<Thing> list = Position.GetThingList(Map);
			for (int i = list.Count - 1; i >= 0; i--)
			{
				DoFireDamage(list[i]);
			}
			if(fireTicksLeft < 0 || fireSize < 0.1f)
			{
				Destroy();
			}
		}

		private void DoFireDamage(Thing targ)
		{
			int num = GenMath.RoundRandom(Mathf.Clamp(0.0125f + 0.0036f * fireSize, 0.0125f, 0.05f) * 150f);
			if (num < 1)
			{
				num = 1;
			}
			if (targ is Pawn pawn)
			{
				BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(pawn, RulePackDefOf.DamageEvent_Fire);
				Find.BattleLog.Add(battleLogEntry_DamageTaken);
				DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, num, 0f, -1f, instigator ?? this);
				dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
				targ.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_DamageTaken);
				if (pawn.apparel != null && pawn.apparel.WornApparel.TryRandomElement(out var result))
				{
					result.TakeDamage(new DamageInfo(DamageDefOf.Burn, num, 0f, -1f, instigator ?? this));
				}
			}
			else
			{
				targ.TakeDamage(new DamageInfo(DamageDefOf.Burn, num, 0f, -1f, instigator ?? this));
			}
		}
	}
}
