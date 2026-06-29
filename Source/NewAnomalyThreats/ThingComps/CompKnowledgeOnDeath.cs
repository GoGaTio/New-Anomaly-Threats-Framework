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
	public class CompProperties_KnowledgeOnDeath : CompProperties
	{
		public float anomalyKnowledge;

		public KnowledgeCategoryDef knowledgeCategory;

		public float distanceToGetKnowledge = 1f;

		public SimpleCurve knowledgeFactorFromDistance = new SimpleCurve()
		{
			new CurvePoint(0.5f, 1f),
			new CurvePoint(1f, 0.5f)
		};

		public CompProperties_KnowledgeOnDeath()
		{
			compClass = typeof(CompKnowledgeOnDeath);
		}
	}
	public class CompKnowledgeOnDeath : ThingComp
	{
		public CompProperties_KnowledgeOnDeath Props => (CompProperties_KnowledgeOnDeath)props;

		public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
		{
			base.Notify_Killed(prevMap, dinfo);
			IntVec3 pos = parent.Position;
			if (!pos.IsValid)
			{
				return;
			}
			if (dinfo?.Instigator is Pawn pawn && pawn.Map != null && pawn.Position.DistanceTo(pos) < Props.distanceToGetKnowledge && PawnCanStudy(pawn))
			{
				Study(pawn, Props.anomalyKnowledge * Props.knowledgeFactorFromDistance.Evaluate(pawn.Position.DistanceTo(pos)));
			}
			if (prevMap != null)
			{
				foreach (Pawn item in prevMap.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
				{
					float distance = item.Position.DistanceTo(pos);
					if (distance < Props.distanceToGetKnowledge && PawnCanStudy(item) && GenSight.LineOfSightToThing(pos, item, prevMap))
					{
						Study(item, Props.anomalyKnowledge * Props.knowledgeFactorFromDistance.Evaluate(distance));
					}
				}
			}
		}

		private bool PawnCanStudy(Pawn pawn)
		{
			if (pawn.Downed || !pawn.Awake())
			{
				return false;
			}
			if (pawn.IsAnimal)
			{
				return false;
			}
			return !pawn.WorkTypeIsDisabled(WorkTypeDefOf.DarkStudy);
		}

		private void Study(Pawn pawn, float knowledgeAmount)
		{
			Find.StudyManager.Study(parent, pawn, knowledgeAmount);
			Find.StudyManager.StudyAnomaly(parent, pawn, knowledgeAmount, Props.knowledgeCategory);
			pawn?.skills.Learn(SkillDefOf.Intellectual, knowledgeAmount);
			if (parent.ParentHolder as Thing == null && parent.Position.InBounds(pawn.Map))
			{
				MoteMaker.ThrowText(parent.Position.ToVector3Shifted(), pawn.Map, $"{Props.knowledgeCategory.LabelCap} +{knowledgeAmount:0.00}", 3f);
			}
		}
	}
}