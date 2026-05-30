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
using System.Reflection.Emit;
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
	/*public class CompProperties_MovableEntity : CompProperties
	{
		public ThingSetMakerDef contents;

		public float chance = 1f;

		public CompProperties_MovableEntity()
		{
			compClass = typeof(CompMovableEntity);
		}
	}
	[StaticConstructorOnStartup]
	public class CompMovableEntity : ThingComp
	{
		public CompProperties_MovableEntity Props => (CompProperties_MovableEntity)props;

		private static readonly CachedTexture CaptureIcon = new CachedTexture("UI/Commands/CaptureEntity");

		private static readonly CachedTexture TransferIcon = new CachedTexture("UI/Commands/TransferEntity");

		private static readonly Texture2D CancelTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

		public IntVec3 targetCell = IntVec3.Invalid;

		public Pawn currentCarrier;

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (targetCell == IntVec3.Invalid)
			{
				yield return new Command_Action
				{
					defaultLabel = "CaptureEntity".Translate() + "...",
					defaultDesc = "CaptureEntityDesc".Translate(parent).Resolve(),
					icon = CaptureIcon.Texture,
					action = delegate
					{
						StudyUtility.TargetHoldingPlatformForEntity(null, parent);
					},
					activateSound = SoundDefOf.Click,
					Disabled = !StudyUtility.HoldingPlatformAvailableOnCurrentMap(),
					disabledReason = "NoHoldingPlatformsAvailable".Translate()
				};
			}
			else
			{
				yield return new Command_Action
				{
					defaultLabel = "CancelCapture".Translate(),
					defaultDesc = "CancelCaptureDesc".Translate(parent).Resolve(),
					icon = CancelTex,
					action = delegate
					{
						targetCell = IntVec3.Invalid;
						if (currentCarrier.CurJob?.targetA == parent)
						{
							currentCarrier.jobs.EndCurrentJob(JobCondition.Incompletable);
						}
						currentCarrier = null;
					}
				};
			}
		}

		public static void TargetNewCarrier(Thing entity)
		{
			Find.Targeter.BeginTargeting(TargetingParameters.ForPawns(), delegate (LocalTargetInfo t)
			{
				if(t.Thing is Pawn pawn)
				{
					TargetRepositionCell(pawn, entity);
				}
			}, delegate (LocalTargetInfo t)
			{
				if (ValidateTarget(t))
				{
					GenDraw.DrawTargetHighlight(t);
				}
			}, ValidateTarget, null, null, BaseContent.ClearTex, playSoundOnAction: true, delegate (LocalTargetInfo t)
			{
				Widgets.MouseAttachedLabel(label);
			}, delegate
			{
				foreach (Building item2 in entity.MapHeld.listerBuildings.AllBuildingsColonistOfGroup(ThingRequestGroup.EntityHolder))
				{
					if (ValidateTarget(item2))
					{
						GenDraw.DrawArrowPointingAt(item2.DrawPos);
					}
				}
			});
			bool CanReserveForTransfer(LocalTargetInfo t)
			{
				return true;
			}
			bool ValidateTarget(LocalTargetInfo t)
			{
				if (t.HasThing && t.Thing.TryGetComp(out CompEntityHolder comp) && comp.HeldPawn == null)
				{
					if (carrier != null)
					{
						return carrier.CanReserveAndReach(t.Thing, PathEndMode.Touch, Danger.Some);
					}
					return true;
				}
				return false;
			}
		}

		public static void TargetRepositionCell(Pawn carrier, Thing entity)
		{
			Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), delegate (LocalTargetInfo t)
			{
				if (carrier != null && !CanReserveForTransfer(t))
				{
					Messages.Message("MessageHolderReserved".Translate(t.Thing.Label), MessageTypeDefOf.RejectInput);
				}
				else
				{
					foreach (Thing item in Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.EntityHolder))
					{
						if (item is Building_HoldingPlatform building_HoldingPlatform && entity != building_HoldingPlatform.HeldPawn)
						{
							CompHoldingPlatformTarget compHoldingPlatformTarget = building_HoldingPlatform.HeldPawn?.TryGetComp<CompHoldingPlatformTarget>();
							if (compHoldingPlatformTarget != null && compHoldingPlatformTarget.targetHolder == t.Thing)
							{
								Messages.Message("MessageHolderReserved".Translate(t.Thing.Label), MessageTypeDefOf.RejectInput);
								return;
							}
						}
					}
					CompHoldingPlatformTarget compHoldingPlatformTarget2 = entity.TryGetComp<CompHoldingPlatformTarget>();
					if (compHoldingPlatformTarget2 != null)
					{
						compHoldingPlatformTarget2.targetHolder = t.Thing;
					}
					if (carrier != null)
					{
						Job job = (transferBetweenPlatforms ? JobMaker.MakeJob(JobDefOf.TransferBetweenEntityHolders, sourcePlatform, t, entity) : JobMaker.MakeJob(JobDefOf.CarryToEntityHolder, t, entity));
						job.count = 1;
						carrier.jobs.TryTakeOrderedJob(job, JobTag.Misc);
					}
					if (t.Thing != null && !t.Thing.SafelyContains(entity))
					{
						Messages.Message("MessageTargetBelowMinimumContainmentStrength".Translate(t.Thing.Label, entity.Label), MessageTypeDefOf.ThreatSmall);
					}
				}
			}, delegate (LocalTargetInfo t)
			{
				if (ValidateTarget(t))
				{
					GenDraw.DrawTargetHighlight(t);
				}
			}, ValidateTarget, null, null, BaseContent.ClearTex, playSoundOnAction: true, delegate (LocalTargetInfo t)
			{
				CompEntityHolder compEntityHolder = t.Thing?.TryGetComp<CompEntityHolder>();
				if (compEntityHolder == null)
				{
					TaggedString label = "ChooseEntityHolder".Translate().CapitalizeFirst() + "...";
					Widgets.MouseAttachedLabel(label);
				}
				else
				{
					Pawn pawn = null;
					Pawn reserver;
					if (carrier != null)
					{
						pawn = t.Thing.Map.reservationManager.FirstRespectedReserver(t.Thing, carrier);
					}
					TaggedString label;
					if (pawn != null)
					{
						label = string.Format("{0}: {1}", "EntityHolderReservedBy".Translate(), pawn.LabelShortCap);
					}
					else
					{
						label = "FloatMenuContainmentStrength".Translate() + ": " + StatDefOf.ContainmentStrength.Worker.ValueToString(compEntityHolder.ContainmentStrength, finalized: false);
						label += "\n" + ("FloatMenuContainmentRequires".Translate(entity).CapitalizeFirst() + ": " + StatDefOf.MinimumContainmentStrength.Worker.ValueToString(entity.GetStatValue(StatDefOf.MinimumContainmentStrength), finalized: false)).Colorize(t.Thing.SafelyContains(entity) ? Color.white : Color.red);
					}
					Widgets.MouseAttachedLabel(label);
				}
			}, delegate
			{
				foreach (Building item2 in entity.MapHeld.listerBuildings.AllBuildingsColonistOfGroup(ThingRequestGroup.EntityHolder))
				{
					if (ValidateTarget(item2) && (carrier == null || CanReserveForTransfer(item2)))
					{
						GenDraw.DrawArrowPointingAt(item2.DrawPos);
					}
				}
			});
			bool CanReserveForTransfer(LocalTargetInfo t)
			{
				if (transferBetweenPlatforms)
				{
					if (t.HasThing)
					{
						return carrier.CanReserve(t.Thing);
					}
					return false;
				}
				return true;
			}
			bool ValidateTarget(LocalTargetInfo t)
			{
				if (t.HasThing && t.Thing.TryGetComp(out CompEntityHolder comp) && comp.HeldPawn == null)
				{
					if (carrier != null)
					{
						return carrier.CanReserveAndReach(t.Thing, PathEndMode.Touch, Danger.Some);
					}
					return true;
				}
				return false;
			}
		}
	}*/
}