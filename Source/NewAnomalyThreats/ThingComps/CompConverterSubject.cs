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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
using static NAT.CompProperties_ConverterSubject;

namespace NAT
{
	public class CompProperties_ConverterSubject : CompProperties
	{
		public class ConverterSubjectCategory
		{
			public ConverterSubjectCategory() { }

			public string key;

			public bool allowByDefault = false;

			public bool fullView = false;

			public List<ThingDef> subjectDefs = new List<ThingDef>();
		}

		public class ConverterSubjectProduct
		{
			public ConverterSubjectProduct() { }

			public ThingDef def;

			public float chance = 1f;

			public IntRange countRange = IntRange.One;

			public StatDrawEntry Entry
			{
				get
				{
					int num1 = countRange.min;
					int num2 = countRange.max;
					float num3 = this.chance;
					if (num1 <= 0)
					{
						int fullCount = num2 + 1 + (-num1);
						chance *= ((float)num2 / (float)fullCount);
						num1 = 1;
					}
					string value = num1 == num2 ? num1.ToString() : (num1.ToString() + "~" + num2.ToString());
					if (num3 < 1)
					{
						value += " (" + "NAT_ConvertionChance".Translate(num3.ToStringPercentEmptyZero()) + ")";
					}
					return new StatDrawEntry(NATDefOf.NAT_ConvertionProducts, def.LabelCap, value, def.DescriptionDetailed, 100, hyperlinks: Gen.YieldSingle(new Dialog_InfoCard.Hyperlink(def)));
				}
			}
		}

		public static void RegenerateSubjects()
		{
			ThingDef def = null;
			try
			{
				subjectDefs = new List<ThingDef>();
				subjectDefCategories = new List<ConverterSubjectCategory>();
				foreach (ThingDef t in DefDatabase<ThingDef>.AllDefs)
				{
					CompProperties_ConverterSubject props = t.GetCompProperties<CompProperties_ConverterSubject>();
					if (props != null)
					{
						props.AddSubject(t);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("Exception regenerating subjects on " + (def == null ? "null ThingDef" : (def.defName.NullOrEmpty() ? "ThingDef with NullOrEmpty defName" : def.defName)) + ": " + ex);
			}
		}

		public static List<ThingDef> subjectDefs = new List<ThingDef>();

		public static List<ConverterSubjectCategory> subjectDefCategories = new List<ConverterSubjectCategory>();

		public bool useButchery = false;

		public string categoryKey;

		public bool allowByDefault = false;

		public int ticksToConvert = 1250;

		public float butcheryRandomOffsetPct = 0;

		public List<ConverterSubjectProduct> products = new List<ConverterSubjectProduct>();

		public CompProperties_ConverterSubject()
		{
			compClass = typeof(CompConverterSubject);
		}

		public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
		{
			foreach (StatDrawEntry item in base.SpecialDisplayStats(req))
			{
				yield return item;
			}
			if (Find.HiddenItemsManager.Hidden(NATDefOf.NAT_Converter))
			{
				yield break;
			}
			yield return new StatDrawEntry(StatCategoryDefOf.BasicsNonPawn, "NAT_ConvertionTime".Translate(), ticksToConvert.ToStringTicksToPeriod(), "NAT_ConvertionTime".Translate(), 0);
			if (useButchery)
			{
				ThingDef thingDef = req.Thing?.def ?? (req.Def as ThingDef);
				if(thingDef != null)
				{
					foreach (ThingDefCountClass item in thingDef.butcherProducts)
					{
						string s = "";
						if (butcheryRandomOffsetPct > 0)
						{
							int num = Mathf.RoundToInt(item.count * butcheryRandomOffsetPct);
							s = (item.count - num) + "~" + (item.count + num);
						}
						else
						{
							s = item.count.ToString();
						}
						yield return new StatDrawEntry(NATDefOf.NAT_ConvertionProducts, item.thingDef.LabelCap, s, item.thingDef.DescriptionDetailed, 110, hyperlinks: Gen.YieldSingle(new Dialog_InfoCard.Hyperlink(item.thingDef)));
					}
				}
			}
			foreach (ConverterSubjectProduct product in products)
			{
				yield return product.Entry;
			}
		}

		public override void ResolveReferences(ThingDef parentDef)
		{
			base.ResolveReferences(parentDef);
			AddSubject(parentDef);
		}

		public void AddSubject(ThingDef parentDef)
		{
			if (categoryKey.NullOrEmpty())
			{
				if (!subjectDefs.Contains(parentDef))
				{
					subjectDefs.Add(parentDef);
				}
			}
			else
			{
				ConverterSubjectCategory category = subjectDefCategories.FirstOrDefault(x => x.key == categoryKey);
				if (category == null)
				{
					category = new ConverterSubjectCategory();
					category.subjectDefs.Add(parentDef);
					category.key = categoryKey;
					category.allowByDefault = allowByDefault;
					subjectDefCategories.Add(category);
				}
				else
				{
					category.subjectDefs.Add(parentDef);
				}
			}
		}
	}

	public class CompConverterSubject : ThingComp
	{
		public static HashSet<Thing> subjects = new HashSet<Thing>();

		public CompProperties_ConverterSubject Props => (CompProperties_ConverterSubject)props;

		public IEnumerable<Thing> GetProducts()
		{
			foreach (ConverterSubjectProduct product in Props.products)
			{
				if (Rand.Chance(product.chance))
				{
					int count = product.countRange.RandomInRange;
					if(count <= 0)
					{
						continue;
					}
					Thing t = ThingMaker.MakeThing(product.def);
					t.stackCount = count;
					yield return t;
				}
			}
			if (Props.useButchery)
			{
				for (int i = 0; i < parent.def.butcherProducts.Count; i++)
				{
					ThingDefCountClass thingDefCountClass = parent.def.butcherProducts[i];
					Thing thing = ThingMaker.MakeThing(thingDefCountClass.thingDef);
					thing.stackCount = thingDefCountClass.count;
					if (Props.butcheryRandomOffsetPct > 0)
					{
						int num = Mathf.RoundToInt(thingDefCountClass.count * Props.butcheryRandomOffsetPct);
						thing.stackCount += new IntRange(-num, num).RandomInRange;
					}
					yield return thing;
				}
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			subjects.Add(parent);
		}

		public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
		{
			base.PostDeSpawn(map, mode);
			subjects.Remove(parent);
		}
	}
}