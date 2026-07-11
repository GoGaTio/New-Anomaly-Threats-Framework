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
using static RimWorld.ColonistBar;

namespace NAT
{
	[StaticConstructorOnStartup]
	public class MovableEntity : ThingWithComps, IAttackTarget, IAlwaysTargetable
	{
		private static readonly CachedTexture MoveIcon = new CachedTexture("UI/Commands/TransferEntity");

		private static readonly Texture2D CancelTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

		Thing IAttackTarget.Thing => this;

		public virtual float TargetPriorityFactor => 0f;

		public virtual LocalTargetInfo TargetCurrentlyAimingAt => LocalTargetInfo.Invalid;

		public virtual bool ThreatDisabled(IAttackTargetSearcher disabledFor)
		{
			return true;
		}

		public virtual float JitterMax => 0.35f;

		public virtual float JitterOffset => 0.17f;

		public virtual float JitterDrop => 0.015f;

		public virtual bool CanBeMoved => true;

		public bool currentlyMoving = false;

		public IntVec3 nextCell = IntVec3.Invalid;

		public float movePercent = 0;

		public Pawn currentCarrier;

		private Vector3 damageOffset = new Vector3(0f, 0f, 0f);

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref nextCell, "nextCell");
			Scribe_Values.Look(ref damageOffset, "damageOffset");
			Scribe_Values.Look(ref movePercent, "movePercent");
			Scribe_Values.Look(ref currentlyMoving, "currentlyMoving");
			Scribe_References.Look(ref currentCarrier, "currentCarrier");
		}

		public override Vector3 DrawPos => base.DrawPos + DrawOffset + damageOffset;

		public virtual Vector3 DrawOffset
		{
			get
			{
				if (!currentlyMoving)
				{
					return Vector3.zero;
				}
				Vector3 direction = (nextCell.ToVector3Shifted() - Position.ToVector3Shifted());
				return (direction * movePercent) + (direction.normalized * 0.5f);
			}
		}

		public virtual void StartMoving(Pawn pawn)
		{
			currentlyMoving = true;
			movePercent = 0;
			currentCarrier = pawn;
			nextCell = Position;
		}

		public virtual void StopMoving()
		{
			nextCell = IntVec3.Invalid;
			currentlyMoving = false;
			movePercent = 0;
			currentCarrier = null;
		}

		public override void Notify_DebugSpawned()
		{
			SetFaction(Faction.OfEntities);
			base.Notify_DebugSpawned();
		}

		public void AddOffsetFromDamage(float dist, float dir)
		{
			damageOffset += Quaternion.AngleAxis(dir, Vector3.up) * Vector3.forward * dist;
			if (damageOffset.sqrMagnitude > JitterMax * JitterMax)
			{
				damageOffset *= JitterMax / damageOffset.magnitude;
			}
		}

		public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
		{
			base.PreApplyDamage(ref dinfo, out absorbed);
			if (dinfo.Def.hasForcefulImpact)
			{
				AddOffsetFromDamage(currentlyMoving ? 0.5f * JitterOffset : JitterOffset, dinfo.Angle);
			}
		}

		public override void DrawExtraSelectionOverlays()
		{
			base.DrawExtraSelectionOverlays();
			if(currentCarrier != null)
			{
				if (currentlyMoving)
				{
					currentCarrier.pather.PatherDraw();
				}
				else
				{
					Job job = null;
					if (currentCarrier.CurJob?.targetA == this)
					{
						job = currentCarrier.CurJob;
					}
					else
					{
						job = currentCarrier.jobs.jobQueue?.FirstOrDefault((x) => x.job.targetA == this)?.job;
					}
					if (job != null)
					{
						GenDraw.DrawLineBetween(DrawPos, job.targetB.CenterVector3);
					}
				}
			}
		}

		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
		{
			foreach (FloatMenuOption o in base.GetFloatMenuOptions(selPawn))
			{
				yield return o;
			}
			if (currentlyMoving || !CanBeMoved)
			{
				yield break;
			}
			if (ValidateCarrier(selPawn))
			{
				yield return new FloatMenuOption("NAT_MoveEntity".Translate(this), delegate
				{
					TargetRepositionCell(selPawn);
				});
			}
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach(Gizmo g in base.GetGizmos())
			{
				yield return g;
			}
			if (!CanBeMoved)
			{
				yield break;
			}
			if (currentCarrier == null)
			{
				yield return new Command_Action
				{
					defaultLabel = "NAT_MoveEntity".Translate(this) + "...",
					defaultDesc = "NAT_MoveEntityDesc".Translate(this).Resolve(),
					icon = MoveIcon.Texture,
					action = delegate
					{
						TargetNewCarrier();
					},
					activateSound = SoundDefOf.Click
				};
			}
			else
			{
				yield return new Command_Action
				{
					defaultLabel = "NAT_CancelMoveEntity".Translate(),
					defaultDesc = "NAT_CancelMoveEntityDesc".Translate().Resolve(),
					icon = CancelTex,
					action = delegate
					{
						if (currentCarrier.CurJob?.targetA == this)
						{
							currentCarrier.jobs.EndCurrentJob(JobCondition.Incompletable);
						}
						StopMoving();
					}
				};
			}
		}

		protected override void TickInterval(int delta)
		{
			base.TickInterval(delta);
			float num = (float)delta * JitterDrop;
			if (damageOffset.sqrMagnitude < num * num)
			{
				damageOffset = new Vector3(0f, 0f, 0f);
			}
			else
			{
				damageOffset -= damageOffset.normalized * num;
			}
		}

		public virtual bool ValidateCarrier(Pawn carrier)
		{
			if (carrier.DeadOrDowned)
			{
				return false;
			}
			if(carrier.RaceProps.intelligence < Intelligence.ToolUser)
			{
				return false;
			}
			if(carrier.Faction != Faction.OfPlayer)
			{
				return false;
			}
			if (carrier.InMentalState)
			{
				return false;
			}
			if (carrier.RaceProps.IsMechanoid && !carrier.IsColonyMechPlayerControlled)
			{
				return false;
			}
			return carrier.CanReserveAndReach(this, PathEndMode.Touch, Danger.Deadly);
		}

		public virtual bool ValidateRepositon(IntVec3 cell, Pawn carrier)
		{
			if (!cell.Standable(carrier.Map))
			{
				return false;
			}
			return carrier.CanReserveAndReach(cell, PathEndMode.Touch, Danger.Deadly);
		}

		public virtual void OnUpdateAction(LocalTargetInfo target)
		{

		}

		public void TargetNewCarrier()
		{
			Find.Targeter.BeginTargeting(TargetingParameters.ForPawns(), delegate (LocalTargetInfo t)
			{
				if (t.Thing is Pawn pawn)
				{
					TargetRepositionCell(pawn);
				}
			}, delegate (LocalTargetInfo t)
			{
				if (ValidateTarget(t))
				{
					GenDraw.DrawTargetHighlight(t);
				}
			}, ValidateTarget, null, null, BaseContent.ClearTex, playSoundOnAction: true, delegate (LocalTargetInfo t)
			{
				Widgets.MouseAttachedLabel("NAT_ChooseWhoShouldMove".Translate(this));
			});
			bool ValidateTarget(LocalTargetInfo t)
			{
				if(t.Thing is Pawn pawn && ValidateCarrier(pawn))
				{
					return true;
				}
				return false;
			}
		}

		public void TargetRepositionCell(Pawn carrier)
		{
			Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), delegate (LocalTargetInfo t)
			{
				if(currentCarrier != null)
				{
					if (currentCarrier.CurJob?.targetA == this)
					{
						currentCarrier.jobs.EndCurrentJob(JobCondition.Incompletable);
					}
					StopMoving();
				}
				currentCarrier = carrier;
				Job job = JobMaker.MakeJob(NATDefOf.NAT_MoveEntity, this, t.Cell);
				job.count = 1;
				carrier.jobs.TryTakeOrderedJob(job, JobTag.Misc);
			}, delegate (LocalTargetInfo t)
			{
				if (ValidateTarget(t))
				{
					GenDraw.DrawTargetHighlight(t);
				}
			}, ValidateTarget, null, null, BaseContent.ClearTex, playSoundOnAction: true, delegate (LocalTargetInfo t)
			{
				Widgets.MouseAttachedLabel("NAT_ChooseWhereToMove".Translate(this));
			}, OnUpdateAction);
			bool ValidateTarget(LocalTargetInfo t)
			{
				return ValidateRepositon(t.Cell, carrier);
			}
		}
	}
}