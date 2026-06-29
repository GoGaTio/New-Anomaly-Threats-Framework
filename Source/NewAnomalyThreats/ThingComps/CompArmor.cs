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
	public class CompProperties_Armor : CompProperties
	{
		public bool combatExtendedArmor = false;

		public FloatRange effectorOffsetRange = new FloatRange(-0.45f, 0.45f);

		public CompProperties_Armor()
		{
			compClass = typeof(CompArmor);
		}

		public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
		{
			foreach (StatDrawEntry item in base.SpecialDisplayStats(req))
			{
				yield return item;
			}
			bool flag = req.Thing != null;
			if (!flag && req.BuildableDef == null)
			{
				yield break;
			}
			List<StatDef> stats = new List<StatDef>();
			foreach (StatDef stat in DefDatabase<DamageArmorCategoryDef>.AllDefsListForReading.Select((x) => x.armorRatingStat))
			{
				if (stats.Contains(stat))
				{
					continue;
				}
				stats.Add(stat);
				float num = flag ? req.Thing.GetStatValue(stat) : req.BuildableDef.GetStatValueAbstract(stat);
				if (num > 0)
				{
					yield return new StatDrawEntry(stat.category, stat.LabelCap, num.ToStringByStyle(stat.toStringStyle), stat.description, stat.displayPriorityInCategory);
				}
			}
		}
	}

	public class CompArmor : ThingComp
	{
		public CompProperties_Armor Props => (CompProperties_Armor)props;

		public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
		{
			absorbed = false;
			if (dinfo.IgnoreInstantKillProtection || !parent.Spawned || dinfo.Def == null)
			{
				return;
			}
			if (dinfo.Def.armorCategory == null)
			{
				return;
			}
			StatDef armorRatingStat = dinfo.Def.armorCategory.armorRatingStat;
			float armorPenetration = dinfo.ArmorPenetrationInt;
			float armorRating = parent.GetStatValue(armorRatingStat);
            if (Props.combatExtendedArmor)
            {
				if (armorPenetration < armorRating)
				{
					absorbed = true;
				}
			}
            else
            {
				float num = Mathf.Max(armorRating - armorPenetration, 0f);
				float value = Rand.Value;
				float num2 = num * 0.5f;
				float num3 = num;
				if (value < num2)
				{
					absorbed = true;
				}
				else if (value < num3)
				{
					dinfo.SetAmount(GenMath.RoundRandom(dinfo.Amount / 2f));
				}
			}
            if (absorbed)
            {
				EffecterDef effecterDef = (dinfo.Def == DamageDefOf.Bullet) ? EffecterDefOf.Deflect_Metal_Bullet : EffecterDefOf.Deflect_Metal;
				effecterDef.Spawn(parent.OccupiedRect().RandomCell, parent.Map, new Vector3(Props.effectorOffsetRange.RandomInRange, 0, Props.effectorOffsetRange.RandomInRange));
			}
		}
	}
}