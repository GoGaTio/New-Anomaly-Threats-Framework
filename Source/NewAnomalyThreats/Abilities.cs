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

	public class CompProperties_AbilityBile : CompProperties_AbilityEffect
	{
		public CompProperties_AbilityBile()
		{
			compClass = typeof(CompAbilityEffect_Bile);
		}
	}

	public class CompAbilityEffect_Bile : CompAbilityEffect
	{
		public new CompProperties_AbilityBile Props => (CompProperties_AbilityBile)props;

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return target.Pawn != null && target.Pawn.RaceProps.IsFlesh && !target.Pawn.IsEntity;
        }
    }

	public class CompProperties_AbilityTeleportCaster : CompProperties_AbilityEffect
	{
		public IntRange stunTicks;

		public ClamorDef destClamorType;

		public float destClamorRadius;

		public CompProperties_AbilityTeleportCaster()
		{
			compClass = typeof(CompAbilityEffect_TeleportCaster);
		}
	}
	public class CompAbilityEffect_TeleportCaster : CompAbilityEffect
	{
		public new CompProperties_AbilityTeleportCaster Props => (CompProperties_AbilityTeleportCaster)props;

		public override IEnumerable<PreCastAction> GetPreCastActions()
		{
			yield return new PreCastAction
			{
				action = delegate (LocalTargetInfo t, LocalTargetInfo d)
				{
					if (!parent.def.HasAreaOfEffect)
					{
						Pawn pawn = t.Pawn;
						if (pawn != null)
						{
							FleckCreationData dataAttachedOverlay = FleckMaker.GetDataAttachedOverlay(pawn, FleckDefOf.PsycastSkipFlashEntry, new Vector3(-0.5f, 0f, -0.5f));
							dataAttachedOverlay.link.detachAfterTicks = 5;
							pawn.Map.flecks.CreateFleck(dataAttachedOverlay);
						}
						else
						{
							FleckMaker.Static(t.CenterVector3, parent.pawn.Map, FleckDefOf.PsycastSkipFlashEntry);
						}
						FleckMaker.Static(d.Cell, parent.pawn.Map, FleckDefOf.PsycastSkipInnerExit);
					}
					FleckMaker.Static(d.Cell, parent.pawn.Map, FleckDefOf.PsycastSkipOuterRingExit);
					if (!parent.def.HasAreaOfEffect)
					{
						SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(t.Cell, parent.pawn.Map));
						SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(d.Cell, parent.pawn.Map));
					}
				},
				ticksAwayFromCast = 5
			};
		}

		public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
		{
			if(!target.Cell.WalkableBy(parent.pawn.Map, parent.pawn))
			{
				return false;
			}
			return base.Valid(target, throwMessages);
		}

		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			base.Apply(target, dest);
			Pawn pawn = parent.pawn;
			if (!parent.def.HasAreaOfEffect)
			{
				parent.AddEffecterToMaintain(EffecterDefOf.Skip_Entry.Spawn(pawn, pawn.Map), pawn.Position, 60);
			}
			else
			{
				parent.AddEffecterToMaintain(EffecterDefOf.Skip_EntryNoDelay.Spawn(pawn, pawn.Map), pawn.Position, 60);
			}
			parent.AddEffecterToMaintain(EffecterDefOf.Skip_Exit.Spawn(target.Cell, pawn.Map), target.Cell, 60);
			SkipUtility.SkipTo(pawn, target.Cell, pawn.Map);
			if ((pawn.Faction == Faction.OfPlayer || pawn.IsPlayerControlled) && pawn.Position.Fogged(pawn.Map))
			{
				FloodFillerFog.FloodUnfog(pawn.Position, pawn.Map);
			}
			pawn.stances.stunner.StunFor(Props.stunTicks.RandomInRange, parent.pawn, addBattleLog: false, showMote: false);
			pawn.Notify_Teleported();
			SendSkipUsedSignal(pawn.Position, pawn);
			if (Props.destClamorType != null)
			{
				GenClamor.DoClamor(pawn, target.Cell, Props.destClamorRadius, Props.destClamorType);
			}
		}

		public static void SendSkipUsedSignal(LocalTargetInfo target, Thing initiator)
		{
			Find.SignalManager.SendSignal(new Signal(CompAbilityEffect_Teleport.SkipUsedSignalTag, target.Named("POSITION"), initiator.Named("SUBJECT")));
		}
	}
}