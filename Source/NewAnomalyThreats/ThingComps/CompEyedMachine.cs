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
using static RimWorld.PsychicRitualRoleDef;
using static System.Collections.Specialized.BitVector32;

namespace NAT
{
	public class CompProperties_EyedMachine : CompProperties
	{
		public float maxOffset;

		public ThingDef lineMote;

		public ThingDef aimMote;

		public Vector3 eyeOffset;

		public int warmUpTicks = 600;

		public SimpleCurve cooldownTicksCurve;

		public int initialCooldownTicks;

		public float explosionRadius = 2.9f;

		public SimpleCurve targetingSpeedCurve  = new SimpleCurve
		{
			new CurvePoint(10f, 0.05f),
			new CurvePoint(40f, 0.03f),
			new CurvePoint(90f, 0.01f)
		};

		public CompProperties_EyedMachine()
		{
			compClass = typeof(CompEyedMachine);
		}
	}
	public class CompEyedMachine : ThingComp
	{
		public CompProperties_EyedMachine Props => props as CompProperties_EyedMachine;

		public Building_HoraxMachine Machine => parent as Building_HoraxMachine;

		private Graphic EyeGraphic
		{
			get
			{
				if (cachedEyeGraphic == null)
				{
					cachedEyeGraphic = GraphicDatabase.Get<Graphic_Single>(parent.def.graphicData.texPath + "_Eye", ShaderDatabase.Cutout, parent.def.graphicData.drawSize, Color.white);
				}
				return cachedEyeGraphic;
			}
		}

		[Unsaved(false)]
		private Graphic cachedEyeGraphic;

		public float ChargePercent => (currentTarget.IsValid && eyeWarmUpTicks > 0) ? Mathf.Lerp(1f, 0f, (float)eyeWarmUpTicks / Props.warmUpTicks) : 0f;

		public LocalTargetInfo currentTarget = LocalTargetInfo.Invalid;

		public Vector3 targetingPos;

		public IntVec3 TargetingCell => targetingPos.ToIntVec3();

		private Vector3 eyeOffset;

		private bool drawEyeOnTarget;

		private Mote mote;

		public int eyeCooldownTicks;

		public int eyeWarmUpTicks;

		public bool TargetValid
		{
			get
			{
				if (!currentTarget.IsValid)
				{
					return false;
				}
				if (currentTarget.Thing.Destroyed)
				{
					currentTarget = LocalTargetInfo.Invalid;
					return false;
				}
				if(currentTarget.Thing is Pawn p && p.DeadOrDowned)
				{
					currentTarget = LocalTargetInfo.Invalid;
					return false;
				}
				return true;
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref eyeCooldownTicks, "eyeCooldownTicks");
			Scribe_Values.Look(ref eyeWarmUpTicks, "eyeWarmUpTicks");
			Scribe_Values.Look(ref targetingPos, "targetingPos");
			Scribe_TargetInfo.Look(ref currentTarget, "currentTarget");
		}

		public override void PostPostMake()
		{
			base.PostPostMake();
			eyeCooldownTicks = Props.initialCooldownTicks;
		}

		public override void PostDrawExtraSelectionOverlays()
		{
			base.PostDrawExtraSelectionOverlays();
			if (!DebugSettings.godMode)
			{
				return;
			}
			if (currentTarget.IsValid)
			{
				GenDraw.DrawLineBetween(parent.DrawPos + eyeOffset, currentTarget.CenterVector3, SimpleColor.Red);
			}
			GenDraw.DrawLineBetween(parent.DrawPos + eyeOffset, targetingPos, SimpleColor.Green);
		}

		public override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			if (parent.Spawned)
			{
				if (drawEyeOnTarget)
				{
					Vector3 vec = (targetingPos - drawLoc).Yto0().normalized * Props.maxOffset;
					drawLoc += vec;
				}
				drawLoc.y += Altitudes.AltInc * 2f;
				eyeOffset = drawLoc - parent.DrawPos + Props.eyeOffset.RotatedBy(Machine.extraRotation);
				EyeGraphic.Draw(drawLoc, Rot4.North, parent, Machine.extraRotation);
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			targetingPos = parent.DrawPos;
		}

		public override void CompTick()
		{
			if (!parent.Spawned)
			{
				return;
			}
			if (eyeWarmUpTicks > 0)
			{
				eyeWarmUpTicks--;
				if (!TargetValid)
				{
					currentTarget = GetTarget();
				}
				if (currentTarget.IsValid)
				{
					float dist = currentTarget.Cell.DistanceTo(parent.Position);
					Vector3 step = currentTarget.CenterVector3 - targetingPos;
					if (step.magnitude > 0.1f)
					{
						targetingPos += step.normalized * Props.targetingSpeedCurve.Evaluate(dist);
					}
					if (mote == null || mote.Destroyed)
					{
						mote = MoteMaker.MakeStaticMote(targetingPos, parent.Map, Props.aimMote, 1f, makeOffscreen: true);
						mote.animationPaused = true;
					}
					mote.exactPosition = targetingPos;
					mote.Maintain();
					if (eyeWarmUpTicks <= 0)
					{
						eyeCooldownTicks = Mathf.RoundToInt(Props.cooldownTicksCurve.Evaluate(dist));
						mote?.Destroy();
						Cast();
						if (currentTarget.Thing != null && currentTarget.Thing.Map != parent.Map)
						{
							currentTarget = LocalTargetInfo.Invalid;
						}
					}
					return;
				}
				eyeWarmUpTicks = 0;
				drawEyeOnTarget = false;
			}
			if (eyeCooldownTicks > 0)
			{
				eyeCooldownTicks--;
			}
			if (eyeCooldownTicks <= 0 && parent.IsHashIntervalTick(60))
			{
				if(Machine.lastStabilizatorDamagedTick != null)
				{
					Machine.lastStabilizatorDamagedTick = null;
					currentTarget = LocalTargetInfo.Invalid;
				}
				currentTarget = GetTarget();
				targetingPos = currentTarget.CenterVector3;
				if (currentTarget.IsValid)
				{
					eyeWarmUpTicks = Props.warmUpTicks;
					drawEyeOnTarget = true;
				}
				else
				{
					drawEyeOnTarget = false;
				}
			}
		}

		protected virtual void Cast()
		{
			MoteMaker.MakeInteractionOverlay(Props.lineMote, new TargetInfo(parent.Position, parent.Map), new TargetInfo(TargetingCell, parent.Map), eyeOffset, Vector3.zero).Maintain();
			GenExplosion.DoExplosion(TargetingCell, parent.Map, Props.explosionRadius, NATDefOf.NociosphereVaporize, parent, 200, 9f, ignoredThings: new List<Thing>() { parent });
		}

		protected virtual LocalTargetInfo GetTarget()
		{
			if (TargetValid)
			{
				return currentTarget;
			}
			Faction faction = parent.Faction;
			List<IAttackTarget> targets = new List<IAttackTarget>();
			List<Pawn> pawnTargets = new List<Pawn>();
			foreach (IAttackTarget item in parent.Map.attackTargetsCache.TargetsHostileToFaction(faction))
			{
				if (item.Thing == null)
				{
					continue;
				}
				if (GenHostility.IsActiveThreatTo(item, faction))
				{
					if(item.Thing is Pawn p && !p.Downed)
					{
						pawnTargets.Add(p);
					}
					else targets.Add(item);
				}
			}
			if (!pawnTargets.NullOrEmpty() && pawnTargets.TryRandomElementByWeight(x => GetTargetScore(x), out Pawn result1))
			{
				return result1;
			}
			if (!targets.NullOrEmpty() && targets.TryRandomElementByWeight(x => GetTargetScore(x), out var result2))
			{
				return result2.Thing;
			}
			return LocalTargetInfo.Invalid;
		}

		protected virtual float GetTargetScore(IAttackTarget target)
		{
			float num = 60f;
			float dist = target.Thing.Position.DistanceTo(parent.Position);
			num /= Mathf.Clamp(Props.cooldownTicksCurve.Evaluate(dist) / Props.cooldownTicksCurve.MaxY, 0.2f, 1f);
			num += FriendlyFireBlastRadiusTargetScoreOffset(target);
			return num * target.TargetPriorityFactor;
		}

		private float FriendlyFireBlastRadiusTargetScoreOffset(IAttackTarget target)
		{
			Map map = target.Thing.Map;
			IntVec3 position = target.Thing.Position;
			int num = GenRadial.NumCellsInRadius(Props.explosionRadius);
			float num2 = 0f;
			for (int i = 0; i < num; i++)
			{
				IntVec3 intVec = position + GenRadial.RadialPattern[i];
				if (!intVec.InBounds(map))
				{
					continue;
				}
				bool flag = true;
				List<Thing> thingList = intVec.GetThingList(map);
				for (int j = 0; j < thingList.Count; j++)
				{
					if (!(thingList[j] is IAttackTarget) || thingList[j] == target)
					{
						continue;
					}
					if (flag)
					{
						if (!GenSight.LineOfSight(position, intVec, map, skipFirstCell: true))
						{
							break;
						}
						flag = false;
					}
					float num3 = !(thingList[j] is Pawn) ? 10f : (thingList[j].def.race.Animal ? 7f : 18f);
					num2 = ((!parent.HostileTo(thingList[j])) ? (num2 - num3) : (num2 + num3 * 0.6f));
				}
			}
			return num2;
		}

		public override string CompInspectStringExtra()
		{
			string s = "";
			if(eyeCooldownTicks > 0)
			{
				s = "CooldownTime".Translate() + " " + eyeCooldownTicks.ToStringTicksToPeriod();
			}
			if (DebugSettings.godMode)
			{
				if (!s.NullOrEmpty())
				{
					s += "\n";
				}
				s += "[DEV] Cooldown: " + eyeCooldownTicks + "\n[DEV] Warmup: " + eyeWarmUpTicks;
			}
			return s;
		}
	}
}