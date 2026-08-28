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
using static System.Net.Mime.MediaTypeNames;

namespace NAT
{
	public class CompProperties_Armor : CompProperties
	{
		public bool combatExtendedArmor = false;

		public FloatRange effectorOffsetRange = new FloatRange(-0.4f, 0.4f);

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
					yield return new StatDrawEntry(stat.category, stat.LabelCap, stat.Worker.ValueToString(num, true), stat.description, stat.displayPriorityInCategory);
				}
			}
		}
	}

	public class CompArmor : ThingComp
	{
		private CompProperties_Armor Props => (CompProperties_Armor)props;

		private int lastDamageCheckTick = -99999;

		public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
		{
			absorbed = false;
			if (dinfo.IgnoreInstantKillProtection || !parent.Spawned || dinfo.Def == null)
			{
				return;
			}
			Pawn pawn = parent as Pawn;
			bool spawnedOrAnyParentSpawned = parent.SpawnedOrAnyParentSpawned;
			if (spawnedOrAnyParentSpawned && pawn?.jobs != null)
			{
				Job job = pawn.CurJob;
				if (job != null && dinfo.Def.canInterruptJobs && !job.playerForced && Find.TickManager.TicksGame >= lastDamageCheckTick + 180)
				{
					Thing instigator = dinfo.Instigator;
					if (job.def.checkOverrideOnDamage == CheckJobOverrideOnDamageMode.Always || (job.def.checkOverrideOnDamage == CheckJobOverrideOnDamageMode.OnlyIfInstigatorNotJobTarget && !job.AnyTargetIs(instigator)))
					{
						lastDamageCheckTick = Find.TickManager.TicksGame;
						pawn.jobs?.CheckForJobOverride();
					}
				}
			}
			if (dinfo.Def.armorCategory != null)
			{
				StatDef armorRatingStat = dinfo.Def.armorCategory.armorRatingStat;
				float armorPenetration = dinfo.ArmorPenetrationInt;
				float armorRating = parent.GetStatValue(armorRatingStat);
				bool diminished = false;
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
						diminished = true;
					}
				}
				if (spawnedOrAnyParentSpawned)
				{
					if (absorbed || diminished)
					{
						EffecterDef effecterDef = (absorbed ? (dinfo.Def.canUseDeflectMetalEffect ? ((dinfo.Def != DamageDefOf.Bullet) ? EffecterDefOf.Deflect_Metal : EffecterDefOf.Deflect_Metal_Bullet) : ((dinfo.Def != DamageDefOf.Bullet) ? EffecterDefOf.Deflect_General : EffecterDefOf.Deflect_General_Bullet)) : EffecterDefOf.DamageDiminished_Metal);
						Effecter deflectionEffecter = effecterDef.Spawn();
						if (pawn != null && (pawn.health.deflectionEffecter == null || pawn.health.deflectionEffecter.def != effecterDef))
						{
							if (pawn.health.deflectionEffecter != null)
							{
								pawn.health.deflectionEffecter.Cleanup();
								pawn.health.deflectionEffecter = null;
							}
							pawn.health.deflectionEffecter = deflectionEffecter;
						}
						deflectionEffecter.offset = new Vector3(Props.effectorOffsetRange.RandomInRange, 0, Props.effectorOffsetRange.RandomInRange);
						TargetInfo targetInfo = new TargetInfo(parent.OccupiedRect().RandomCell, parent.MapHeld);
						Thing instigator = dinfo.Instigator;
						deflectionEffecter.Trigger(targetInfo, (instigator != null) ? ((TargetInfo)instigator) : targetInfo);
						if (absorbed)
						{
							pawn?.Drawer.Notify_DamageDeflected(dinfo);
							return;
						}
					}
					else if (pawn != null)
					{
						LifeStageUtility.PlayNearestLifestageSound(pawn, (LifeStageAge lifeStage) => lifeStage.soundWounded, null, null, 0.7f);
						pawn.Drawer.Notify_DamageApplied(dinfo);
						EffecterDef damageEffecter = pawn.RaceProps.FleshType.damageEffecter;
						if (damageEffecter != null)
						{
							if (pawn.health.woundedEffecter != null && pawn.health.woundedEffecter.def != damageEffecter)
							{
								pawn.health.woundedEffecter.Cleanup();
							}
							pawn.health.woundedEffecter = damageEffecter.Spawn();
							pawn.health.woundedEffecter.Trigger(pawn, dinfo.Instigator ?? pawn);
						}
						if (dinfo.Def.damageEffecter != null)
						{
							Effecter effecter = dinfo.Def.damageEffecter.Spawn();
							effecter.Trigger(pawn, pawn);
							effecter.Cleanup();
						}
					}
				}
				if (!absorbed && pawn != null)
				{
					float damage = dinfo.Amount * pawn.GetStatValue(StatDefOf.IncomingDamageFactor);
					dinfo.SetAmount(damage);
					pawn.records.AddTo(RecordDefOf.DamageTaken, damage);
					if (dinfo.Instigator is Pawn pawn2)
					{
						pawn2.records.AddTo(RecordDefOf.DamageDealt, damage);
					}
					pawn.mindState.Notify_DamageTaken(dinfo);
					pawn.GetLord()?.Notify_PawnDamaged(pawn, dinfo);
					if (dinfo.Def.makesBlood && Rand.Chance(0.5f))
					{
						pawn.health.DropBloodFilth();
					}
				}
			}
            
		}
	}
}