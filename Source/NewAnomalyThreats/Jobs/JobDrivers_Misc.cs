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
using static HarmonyLib.Code;

namespace NAT
{
	public class JobDriver_MoveEntity : JobDriver
	{
		protected MovableEntity Entity => job.GetTarget(TargetIndex.A).Thing as MovableEntity;

		protected IntVec3 TargetCell => job.GetTarget(TargetIndex.B).Cell;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if(pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed))
			{
				return pawn.Reserve(job.GetTarget(TargetIndex.B), job, 1, -1, null, errorOnFailed);
			}
			return false;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			Toil toil = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell);
			toil.initAction = (Action)Delegate.Combine(toil.initAction, (Action)delegate
			{
				Entity.StartMoving(toil.actor);
			});
			toil.tickAction = (Action)Delegate.Combine(toil.tickAction, (Action)delegate
			{
				if(Entity.nextCell != toil.actor.pather.nextCell)
				{
					Entity.Position = toil.actor.Position;
					Entity.nextCell = toil.actor.pather.nextCell;
				}
				Entity.movePercent = toil.actor.pather.MovePercentage;
				if(Entity.nextCell == TargetCell && Entity.movePercent >= 0.5f)
				{
					toil.actor.pather.StopDead();
					ReadyForNextToil();
				}
			});
			toil.AddFinishAction(delegate
			{
				if(Entity.movePercent >= 0.5f)
				{
					Entity.Position = Entity.nextCell;
				}
				Entity.StopMoving();
			});
			yield return toil;
		}
	}

	public class JobDriver_Seal : JobDriver
	{
		protected UndergroundEntrance Entrance => job.GetTarget(TargetIndex.A).Thing as UndergroundEntrance;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, Entrance.def.hasInteractionCell ? PathEndMode.InteractionCell : PathEndMode.Touch);
			yield return Toils_General.WaitWith(TargetIndex.A, Entrance.Comp.Props.sealTicks, useProgressBar: true).WithEffect(() => Entrance.Comp.Props.sealEffect, TargetIndex.A);
			Toil toil = ToilMaker.MakeToil("MakeNewToils");
			toil.initAction = delegate
			{
				Entrance.Seal();
			};
			yield return toil;
		}

		public override string GetReport()
		{

			if (string.IsNullOrEmpty(Entrance.Comp.Props.sealJobReportOverride))
			{
				return base.GetReport();
			}
			return Entrance.Comp.Props.sealJobReportOverride.Formatted(Entrance.LabelShort);
		}
	}

	public class JobDriver_InteractWithVault : JobDriver
	{
		protected Building_VaultDoor Vault => job.GetTarget(TargetIndex.A).Thing as Building_VaultDoor;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
		}

		private bool locked;

		public override void Notify_Starting()
		{
			base.Notify_Starting();
			locked = Vault.Locked;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			AddEndCondition(delegate
			{
				return Vault.Locked == locked ? JobCondition.Ongoing : JobCondition.Succeeded;
			});
			yield return Toils_Goto.GotoThing(TargetIndex.A, Vault.def.hasInteractionCell ? PathEndMode.InteractionCell : PathEndMode.Touch);
			Toil toil = ToilMaker.MakeToil("MakeNewToils");
			toil.handlingFacing = true;
			toil.WithProgressBar(TargetIndex.A, () => Vault.Locked ? Vault.UnlockPct : (1f - Vault.UnlockPct), interpolateBetweenActorAndTarget: false, -0.5f, alwaysShow: true);
			toil.tickAction = (Action)Delegate.Combine(toil.tickAction, (Action)delegate
			{
				pawn.rotationTracker.FaceTarget(Vault);
				Vault.TickInteracting();
			});
			toil.defaultCompleteMode = ToilCompleteMode.Never;
			yield return toil;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref locked, "locked");
		}
	}

	public class JobDriver_BringAdditionalOfferings : JobDriver
	{
		private Thing Item => job.GetTarget(TargetIndex.A).Thing;

		private IntVec3 Place => (IntVec3)job.GetTarget(TargetIndex.B).Cell;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Item, job, 1, job.count, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
			yield return Toils_Haul.StartCarryThing(TargetIndex.A);
			yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.Touch);
			Toil doWork = ToilMaker.MakeToil("MakeNewToils");
			doWork.initAction = delegate
			{
				doWork.actor.carryTracker.TryDropCarriedThing(Place, ThingPlaceMode.Near, out var thing);
				thing.SetForbidden(true);
			};
			yield return doWork;
		}
	}
}