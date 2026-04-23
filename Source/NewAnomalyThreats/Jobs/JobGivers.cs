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
using static System.Net.WebRequestMethods;

namespace NAT
{
	public class JobGiver_AIJumpMovement : ThinkNode_JobGiver
	{
		private AbilityDef ability;

		private static List<Thing> tmpHostileSpots = new List<Thing>();

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn.Drafted)
			{
				return null;
			}
			Ability ability = pawn.abilities?.GetAbility(this.ability);
			if (ability == null || !ability.CanCast)
			{
				return null;
			}
			IntVec3 relocatePosition;
			if (TryFindAttackPosition(pawn, out relocatePosition, ability.verb.EffectiveRange))
			{
				return ability.GetJob(relocatePosition, relocatePosition);
			}
			if (TryFindEscapePosition(pawn, out relocatePosition, ability.verb.EffectiveRange))
			{
				return ability.GetJob(relocatePosition, relocatePosition);
			}
			return null;
		}

		private bool TryFindAttackPosition(Pawn pawn, out IntVec3 escapePosition, float maxDistance)
		{
			if (true)
			{
				escapePosition = IntVec3.Invalid;
				return false;
			}
			tmpHostileSpots.Clear();
			tmpHostileSpots.AddRange(from a in pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn)
									 where !a.ThreatDisabled(pawn)
									 select a.Thing);
			Ability jump = pawn.abilities?.GetAbility(ability);
			escapePosition = CellFinderLoose.GetFallbackDest(pawn, tmpHostileSpots, maxDistance, 5f, 5f, 20, (IntVec3 c) => jump.verb.ValidateTarget(c, showMessages: false));
			tmpHostileSpots.Clear();
			return escapePosition.IsValid;
		}

		private bool TryFindEscapePosition(Pawn pawn, out IntVec3 escapePosition, float maxDistance)
		{
			if(pawn.mindState.meleeThreat == null)
			{
				escapePosition = IntVec3.Invalid;
				return false;
			}
			tmpHostileSpots.Clear();
			tmpHostileSpots.AddRange(from a in pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn)
									 where !a.ThreatDisabled(pawn)
									 select a.Thing);
			Ability jump = pawn.abilities?.GetAbility(ability);
			escapePosition = CellFinderLoose.GetFallbackDest(pawn, tmpHostileSpots, maxDistance, 5f, 5f, 20, (IntVec3 c) => jump.verb.ValidateTarget(c, showMessages: false));
			tmpHostileSpots.Clear();
			return escapePosition.IsValid;
		}

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_AIJumpMovement obj = (JobGiver_AIJumpMovement)base.DeepCopy(resolve);
			obj.ability = ability;
			return obj;
		}
	}

	public class JobGiver_AISapperSmart : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			IntVec3 intVec = pawn.mindState.duty.focus.Cell;
			if (intVec.IsValid && (float)intVec.DistanceToSquared(pawn.Position) < 100f && intVec.GetRoom(pawn.Map) == pawn.GetRoom() && intVec.WithinRegions(pawn.Position, pawn.Map, 9, TraverseMode.NoPassClosedDoors))
			{
				pawn.GetLord().Notify_ReachedDutyLocation(pawn);
				return null;
			}
			if (!intVec.IsValid)
			{
				if (!(from x in pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn)
					  where !x.ThreatDisabled(pawn) && x.Thing.Faction == Faction.OfPlayer && pawn.Position.DistanceToSquared(x.Thing.Position) <= 2500 && pawn.CanReach(x.Thing, PathEndMode.OnCell, Danger.Deadly, canBashDoors: false, canBashFences: false, TraverseMode.PassDoors)
					  select x).TryRandomElement(out var result))
				{
					return null;
				}
				intVec = result.Thing.Position;
			}
			if (!pawn.CanReach(intVec, PathEndMode.OnCell, Danger.Deadly, canBashDoors: false, canBashFences: false, TraverseMode.PassDoors))
			{
				return null;
			}
			using (PawnPath path = pawn.Map.pathFinder.FindPathNow(pawn.Position, intVec, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassDoors)))
			{
				IntVec3 cellBefore;
				Thing thing = path.FirstBlockingBuilding(out cellBefore, pawn);
				if (thing != null)
				{
					Job job = DigUtility.PassBlockerJob(pawn, thing, cellBefore, false, false);
					if (job != null)
					{
						return job;
					}
				}
			}
			return JobMaker.MakeJob(JobDefOf.Goto, intVec, 500, checkOverrideOnExpiry: true);
		}
	}

	public class JobGiver_ExtinguishSelfImmediately : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			Fire fire = (Fire)pawn.GetAttachment(ThingDefOf.Fire);
			if (fire != null)
			{
				return JobMaker.MakeJob(JobDefOf.ExtinguishSelf, fire);
			}
			return null;
		}
	}

	public class JobGiver_BringAdditionalOfferings : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			Lord lord;
			if ((lord = pawn.GetLord()) == null)
			{
				return null;
			}
			if (!(lord.CurLordToil is LordToil_PsychicRitual lordToil_PsychicRitual))
			{
				
				return null;
			}
			PsychicRitualDef_AdditionalOfferings ritualDef;
			if ((ritualDef = lordToil_PsychicRitual.RitualData.psychicRitual.def as PsychicRitualDef_AdditionalOfferings) == null)
			{
				
				return null;
			}
			PsychicRitual psychicRitual = lordToil_PsychicRitual.RitualData.psychicRitual;
			PsychicRitualToil_BringAdditionalOfferings toil = lordToil_PsychicRitual.RitualData.CurPsychicRitualToil as PsychicRitualToil_BringAdditionalOfferings;
			if (toil == null || toil.brought)
			{
				Log.Message(lordToil_PsychicRitual.RitualData.CurPsychicRitualToil);
				return null;
			}
			if (ritualDef.additionalOfferings.NullOrEmpty())
			{
				toil.brought = true;
				return null;
			}
			List<Thing> thingList = ritualDef.Offerings(psychicRitual.assignments.Target.CenterCell, psychicRitual.Map);
			string s = "List:";
			foreach(Thing x in thingList)
			{
				s += "\n" + x.LabelCap;
			}
			if (ritualDef.EnoughOfferings(thingList))
			{
				toil.brought = true;
				return null;
			}
			foreach (Pawn p in psychicRitual.assignments.AllAssignedPawns)
			{
				Thing t = p.carryTracker?.CarriedThing;
				if (t != null)
				{
					thingList.Add(t);
				}
				if(p.CurJob?.targetA.Thing != null)
				{
					thingList.Add(p.CurJob.targetA.Thing);
				}
			}
			if (ritualDef.EnoughOfferings(thingList))
			{
				return null;
			}
			List<ThingDefCountClass> list = ritualDef.OfferingsLeft(thingList);
			ThingDefCountClass num = null;
			LocalTargetInfo value = (LocalTargetInfo)psychicRitual.assignments.Target;
			Thing thing = GenClosest.ClosestThingReachable(pawn.PositionHeld, pawn.MapHeld, ThingRequest.ForGroup(ThingRequestGroup.HaulableAlways), PathEndMode.Touch, TraverseParms.For(pawn), 9999f, delegate (Thing thing2)
			{
				if (thingList.Contains(thing2))
				{
					return false;
				}
				num = list.FirstOrDefault((x) => x.thingDef == thing2.def);
				if (num == null)
				{
					return false;
				}
				if (thing2.IsForbidden(pawn))
				{
					return false;
				}
				return pawn.CanReserve(thing2, 10, Mathf.Min(num.count, thing2.stackCount)) ? true : false;
			});
			if (thing == null)
			{
				TaggedString reason = "PsychicRitualToil_GatherOfferings_OfferingUnavailable".Translate(pawn.Named("PAWN"), num.thingDef.LabelCap + " x" + num.count);
				psychicRitual.CancelPsychicRitual(reason);
				return null;
			}
			Job job = JobMaker.MakeJob(NATDefOf.NAT_BringAdditionalOfferings, thing, value.HasThing ? value.Thing.PositionHeld : value.Cell);
			job.count = thing.stackCount;
			if (num != null)
			{
				job.count = Mathf.Min(job.count, num.count);
			}
			return job;
		}
	}
}