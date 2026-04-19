using LudeonTK;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.IO;
using RimWorld.Planet;
using RimWorld.QuestGen;
using RimWorld.SketchGen;
using RimWorld.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngineInternal;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;
using Verse.Noise;
using Verse.Profile;
using Verse.Sound;
using Verse.Steam;
using static RimWorld.FleshTypeDef;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;
using static UnityEngine.GraphicsBuffer;

namespace NAT
{
	public class PsychicRitualDef_AdditionalOfferings : PsychicRitualDef_InvocationCircle
	{
		public List<ThingDefCountClass> additionalOfferings = new List<ThingDefCountClass>();

		public List<ThingDef> unremovableThings = new List<ThingDef>();

		public bool allowMutants = false;

		public override PsychicRitualCandidatePool FindCandidatePool()
		{
			PsychicRitualCandidatePool pool = base.FindCandidatePool();
			if (!allowMutants)
			{
				return pool;
			}
			List<Pawn> list = pool.AllCandidatePawns;
			if (Faction.OfPlayerSilentFail != null)
			{
				list.AddRange(Find.CurrentMap.mapPawns.FreeColonistsAndPrisonersSpawned.Where((Pawn p) => p.IsSubhuman && !p.IsShambler));
			}
			return new PsychicRitualCandidatePool(list, pool.NonAssignablePawns);
		}

		public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph parent)
		{
			var list = base.CreateToils(psychicRitual, parent);
			list.Insert(0, new PsychicRitualToil_BringAdditionalOfferings(this));
			return list;
		}
        public override IEnumerable<string> BlockingIssues(PsychicRitualRoleAssignments assignments, Map map)
        {
			foreach (string item in base.BlockingIssues(assignments, map))
			{
				yield return item;
			}
			List<Pawn> tmpGatheringPawns = new List<Pawn>();
			foreach (var (psychicRitualRoleDef, collection) in assignments.RoleAssignments)
			{
				if (psychicRitualRoleDef.CanHandleOfferings)
				{
					tmpGatheringPawns.AddRange(collection);
				}
			}
			tmpGatheringPawns.RemoveAll(map, (Map _map, Pawn _pawn) => _pawn.MapHeld != _map);
			foreach(ThingDefCountClass item in additionalOfferings)
			{
				IngredientCount ingredients = new IngredientCount();
				ingredients.SetBaseCount(item.count);
				ingredients.filter.SetDisallowAll();
				ingredients.filter.SetAllow(item.thingDef, true);
				if (!OfferingReachable(map, tmpGatheringPawns, ingredients, out var reachableCount))
				{
					yield return "PsychicRitualOfferingsInsufficient".Translate(ingredients.SummaryFilterFirst, reachableCount);
				}
			}
		}

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
			foreach(StatDrawEntry item in base.SpecialDisplayStats(req))
            {
				if(item.LabelCap == "StatsReport_Offering".Translate().CapitalizeFirst())
                {
					string s = item.ValueString;
					foreach (ThingDefCountClass offering in additionalOfferings)
					{
						s += "\n" + offering.thingDef.LabelCap + " x" + offering.count;
					}
					yield return new StatDrawEntry(item.category, "StatsReport_Offering".Translate(), s, "StatsReport_Offering_Desc".Translate(), item.DisplayPriorityWithinCategory);
				}
                else
                {
					yield return item;
				}
            }
        }

		public override TaggedString TimeAndOfferingLabel()
		{
			string s = base.TimeAndOfferingLabel();
			foreach (ThingDefCountClass offering in additionalOfferings)
			{
				s += ", " + offering.thingDef.LabelCap + " x" + offering.count;
			}
			return s;
		}

		public static void RemoveItem(List<Thing> items, int count)
		{
			int num = 0;
			while (num < count)
			{
				Thing t = items.RandomElement();
				if (count - num >= t.stackCount)
				{
					num += t.stackCount;
					t.Destroy();
					items.Remove(t);
				}
				else
				{
					t.SplitOff(count - num).Destroy();
					num += count - num;
				}
			}
		}

		public List<ThingDefCountClass> OfferingsReadonly
		{
			get
			{
				List<ThingDefCountClass> list = new List<ThingDefCountClass>();
				foreach (ThingDefCountClass item in additionalOfferings)
				{
					list.Add(new ThingDefCountClass(item.thingDef, item.count));
				}
				return list;
			}
		}

		public List<Thing> Offerings(IntVec3 cell, Map map)
		{
			List<Thing> list2 = new List<Thing>();
			foreach (IntVec3 c in CellRect.FromCell(cell).ExpandedBy(2).Cells)
			{
				foreach (Thing t in c.GetThingList(map))
				{
					if (additionalOfferings.Any((x) => x.thingDef == t.def))
					{
						list2.Add(t);
					}
				}
			}
			return list2;
		}

		public bool EnoughOfferings(IntVec3 cell, Map map)
		{
			return EnoughOfferings(Offerings(cell, map));
		}

		public bool EnoughOfferings(List<Thing> things)
		{
			List<ThingDefCountClass> list = OfferingsReadonly;
			if (list.NullOrEmpty())
			{
				return true;
			}
			if (things.NullOrEmpty())
			{
				return false;
			}
			foreach(Thing t in things)
			{
				if (list.NullOrEmpty())
				{
					return true;
				}
				ThingDefCountClass num = list.FirstOrDefault((x)=>x.thingDef == t.def);
				if(num != null)
				{
					num.count -= t.stackCount;
					if(num.count <= 0)
					{
						list.Remove(num);
					}
				}
			}
			return list.NullOrEmpty();
		}

		public List<ThingDefCountClass> OfferingsLeft(List<Thing> things)
		{
			List<ThingDefCountClass> list = OfferingsReadonly;
			if (list.NullOrEmpty())
			{
				return list;
			}
			foreach (Thing t in things)
			{
				ThingDefCountClass num = list.FirstOrDefault((x) => x.thingDef == t.def);
				if (num != null)
				{
					num.count -= t.stackCount;
					if (num.count <= 0)
					{
						list.Remove(num);
					}
				}
			}
			return list;
		}

		public void RemoveOfferings(List<Thing> items, float fraction = 1f, bool removeUnremovable = false)
		{
			List<ThingDefCountClass> list = OfferingsReadonly;
			if (list.NullOrEmpty())
			{
				return;
			}
			foreach (var item in list.ToList())
			{
				if (!removeUnremovable && unremovableThings.Contains(item.thingDef))
				{
					list.Remove(item);
				}
				else item.count = GenMath.RoundRandom(fraction * item.count);
			}
			int flag = 0;
			while(flag < 100)
			{
				flag++;
				Thing t = items.RandomElement();
				ThingDefCountClass num = list.FirstOrDefault((x) => x.thingDef == t.def);
				if (num != null)
				{
					int count = num.count;
					if(count <= 0)
					{
						list.Remove(num);
					}
					else
					{
						if (count < t.stackCount)
						{
							t.SplitOff(count).Destroy();
							list.Remove(num);
						}
						else
						{
							num.count -= t.stackCount;
							t.Destroy();
							items.Remove(t);
						}
					}
				}
				if (items.NullOrEmpty())
				{
					break;
				}
			}
		}
	}

	public class PsychicRitualToil_BringAdditionalOfferings : PsychicRitualToil
	{
		public bool brought;

		private PsychicRitualDef_AdditionalOfferings def;

		protected PsychicRitualToil_BringAdditionalOfferings()
		{
		}

		public PsychicRitualToil_BringAdditionalOfferings(PsychicRitualDef_AdditionalOfferings def)
		{
			this.def = def;
			if (def.additionalOfferings.NullOrEmpty())
			{
				this.brought = true;
			}
		}

		public override void UpdateAllDuties(PsychicRitual psychicRitual, PsychicRitualGraph parent)
		{
			foreach (Pawn item in psychicRitual.assignments.AllAssignedPawns)
			{
				SetPawnDuty(item, psychicRitual, parent, brought ? DutyDefOf.Idle : NATDefOf.NAT_BringAdditionalOfferingsForPsychicRitual);
			}
		}

		public override bool Tick(PsychicRitual psychicRitual, PsychicRitualGraph parent)
		{
			return brought;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref brought, "brought", defaultValue: false);
			Scribe_Defs.Look(ref def, "def");
		}

		public override void Notify_PawnJobDone(PsychicRitual psychicRitual, PsychicRitualGraph parent, Pawn pawn, Job job, JobCondition condition)
		{
			base.Notify_PawnJobDone(psychicRitual, parent, pawn, job, condition);
			if (brought)
			{
				SetPawnDuty(pawn, psychicRitual, parent, DutyDefOf.Idle);
			}
		}

		public override string GetJobReport(PsychicRitual psychicRitual, PsychicRitualGraph parent, Pawn pawn)
		{
			if (pawn.CurJobDef == NATDefOf.NAT_BringAdditionalOfferings)
			{
				return "PsychicRitualToil_GatherOfferings_JobReport".Translate();
			}
			return base.GetJobReport(psychicRitual, parent, pawn);
		}
	}

	public class PsychicRitualToil_AdditionalOfferingsOutcome : PsychicRitualToil
	{

		public PsychicRitualRoleDef invokerRole;

		public PsychicRitualToil_AdditionalOfferingsOutcome()
		{
		}

		public PsychicRitualToil_AdditionalOfferingsOutcome(PsychicRitualRoleDef invokerRole)
		{
			this.invokerRole = invokerRole;
		}

		public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
		{
			Pawn pawn = psychicRitual.assignments.FirstAssignedPawn(invokerRole);
			if (pawn != null)
			{
				PsychicRitualDef_AdditionalOfferings def = psychicRitual.def as PsychicRitualDef_AdditionalOfferings;
				if (def != null)
				{
					List<Thing> list = def.Offerings(psychicRitual.assignments.Target.Cell, psychicRitual.assignments.Target.Map);
					if (def.EnoughOfferings(list))
					{
						ApplyOutcome(list, psychicRitual, pawn);
					}
					else
					{
						psychicRitual.CancelPsychicRitual("NAT_PsychicRitual_NotEnoughOfferings".Translate());
					}
				}
			}
		}

		protected virtual void ApplyOutcome(List<Thing> offerings, PsychicRitual psychicRitual, Pawn invoker)
		{
			((PsychicRitualDef_AdditionalOfferings)psychicRitual.def).RemoveOfferings(offerings, 1, true);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look(ref invokerRole, "invokerRole");
		}
	}

	public class PsychicRitualDef_CreatePawn : PsychicRitualDef_AdditionalOfferings
	{
		public SimpleCurve successChanceFromQualityCurve;

		public PawnKindDef pawnKind;

		public FactionDef factionOverride;

		public bool clearEquipment = false;

		public FloatRange? ageBiologicalYears = null;

		public FloatRange? ageChronologicalYears = null;

		[MustTranslate]
		public string successLetterText;

		[MustTranslate]
		public string failLetterText;

		public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph parent)
		{
			List<PsychicRitualToil> list = base.CreateToils(psychicRitual, parent);
			list.Add(new PsychicRitualToil_CreatePawn(InvokerRole));
			return list;
		}

		public override TaggedString OutcomeDescription(FloatRange qualityRange, string qualityNumber, PsychicRitualRoleAssignments assignments)
		{
			string text = successChanceFromQualityCurve.Evaluate(qualityRange.min).ToStringPercent();
			return outcomeDescription.Formatted(text);
		}
	}

	public class PsychicRitualToil_CreatePawn : PsychicRitualToil_AdditionalOfferingsOutcome
	{
		public PsychicRitualToil_CreatePawn() : base() { }

		public PsychicRitualToil_CreatePawn(PsychicRitualRoleDef invokerRole) : base(invokerRole)
		{
		}

		protected override void ApplyOutcome(List<Thing> offerings, PsychicRitual psychicRitual, Pawn invoker)
		{
			PsychicRitualDef_CreatePawn def = psychicRitual.def as PsychicRitualDef_CreatePawn;
			float power = psychicRitual.PowerPercent;
			float chance = def.successChanceFromQualityCurve.Evaluate(power);
			if (Rand.Chance(chance))
			{
				Pawn pawn = PawnGenerator.GeneratePawn(def.pawnKind, def.factionOverride == null ? invoker.Faction : Find.FactionManager.FirstFactionOfDef(def.factionOverride));
				if (def.clearEquipment)
				{
					pawn.equipment.DestroyAllEquipment();
					pawn.inventory.DestroyAll();
					pawn.apparel.DestroyAll();
				}
				if(def.ageBiologicalYears != null)
				{
					pawn.ageTracker.AgeBiologicalTicks = Mathf.RoundToInt(def.ageBiologicalYears.Value.RandomInRange * 60000);
				}
				if (def.ageChronologicalYears != null)
				{
					pawn.ageTracker.AgeChronologicalTicks = Mathf.RoundToInt(def.ageChronologicalYears.Value.RandomInRange * 60000);
				}
				pawn.Notify_SignalReceived(new Signal("NAT_CreatedByPsychicRitual", power.Named("QUALITY")));
				base.ApplyOutcome(offerings, psychicRitual, invoker);
				GenSpawn.Spawn(pawn, psychicRitual.assignments.Target.Thing.Position, psychicRitual.Map);
				TaggedString text1 = def.successLetterText.Formatted(invoker.Named("INVOKER"), def.Named("RITUAL"), pawn.Named("PAWN"));
				Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(def.label), text1, LetterDefOf.PositiveEvent, new LookTargets(pawn));
			}
			else
			{
				def.RemoveOfferings(offerings, 0.5f);
				TaggedString text2 = def.failLetterText.Formatted(invoker.Named("INVOKER"), def.Named("RITUAL"));
				Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(def.label), text2, LetterDefOf.ThreatSmall);
				Find.PsychicRitualManager.ClearCooldown(def);
			}
		}
	}

	public class PsychicRitualDef_CreateThing : PsychicRitualDef_AdditionalOfferings
	{
		public List<ThingDef> thingDefs = new List<ThingDef>();

		public FactionDef factionOverride;

		[MustTranslate]
		public string successLetterText;

		[MustTranslate]
		public string failLetterText;

		public bool useChanceToSpawn = false;

		public SimpleCurve successChanceFromQualityCurve;

		public ThingDef Thing => thingDefs.RandomElement();

		public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph parent)
		{
			List<PsychicRitualToil> list = base.CreateToils(psychicRitual, parent);
			list.Add(new PsychicRitualToil_CreateThing(InvokerRole));
			return list;
		}

		public override TaggedString OutcomeDescription(FloatRange qualityRange, string qualityNumber, PsychicRitualRoleAssignments assignments)
		{
			string text = successChanceFromQualityCurve.Evaluate(qualityRange.min).ToStringPercent();
			return outcomeDescription.Formatted(text);
		}
	}

	public class PsychicRitualToil_CreateThing : PsychicRitualToil_AdditionalOfferingsOutcome
	{
		public PsychicRitualToil_CreateThing() : base() { }

		public PsychicRitualToil_CreateThing(PsychicRitualRoleDef invokerRole) : base(invokerRole)
		{
		}

		protected override void ApplyOutcome(List<Thing> offerings, PsychicRitual psychicRitual, Pawn invoker)
		{
			PsychicRitualDef_CreateThing def = psychicRitual.def as PsychicRitualDef_CreateThing;
			float power = psychicRitual.PowerPercent;
			if (def.useChanceToSpawn)
			{
				float chance = def.successChanceFromQualityCurve.Evaluate(power);
				if (!Rand.Chance(chance))
				{
					def.RemoveOfferings(offerings, 0.5f);
					TaggedString text2 = def.failLetterText.Formatted(invoker.Named("INVOKER"), def.Named("RITUAL"));
					Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(def.label), text2, LetterDefOf.NegativeEvent);
					Find.PsychicRitualManager.ClearCooldown(def);
				}
			}
			Thing thing = ThingMaker.MakeThing(def.Thing, null);
			thing.Notify_SignalReceived(new Signal("NAT_CreatedByPsychicRitual", power.Named("QUALITY")));
			base.ApplyOutcome(offerings, psychicRitual, invoker);
			GenSpawn.Spawn(thing, psychicRitual.assignments.Target.Thing.Position, psychicRitual.Map);
			TaggedString text1 = def.successLetterText.Formatted(invoker.Named("INVOKER"), def.Named("RITUAL"), thing.Named("THING"));
			Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(def.label), text1, LetterDefOf.PositiveEvent, new LookTargets(thing));
		}
	}

	public class PsychicRitualDef_GiveHediff : PsychicRitualDef_AdditionalOfferings
	{
		public HediffDef hediffDef;

		public HediffDef sideEffectDef;

		public FactionDef factionOverride;
		        
		[MustTranslate]
		public string successLetterText;

		[MustTranslate]
		public string failLetterText;

		[MustTranslate]
		public string sideEffectText;

		public SimpleCurve severityFromQualityCurve;

		public SimpleCurve successChanceFromQualityCurve;

		public SimpleCurve sideEffectChanceFromQualityCurve;

		public bool sideEffectOnlyIfFail = true;

		public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph parent)
		{
			List<PsychicRitualToil> list = base.CreateToils(psychicRitual, parent);
			list.Add(new PsychicRitualToil_GiveHediff(InvokerRole));
			return list;
		}

		public override TaggedString OutcomeDescription(FloatRange qualityRange, string qualityNumber, PsychicRitualRoleAssignments assignments)
		{
			string text1 = severityFromQualityCurve?.Evaluate(qualityRange.min).ToStringPercent() ?? "";
			string text2 = successChanceFromQualityCurve?.Evaluate(qualityRange.min).ToStringPercent() ?? "";
			string text3 = sideEffectChanceFromQualityCurve?.Evaluate(qualityRange.min).ToStringPercent() ?? "";
			return outcomeDescription.Formatted(text1, text2, text3);
		}

		public override IEnumerable<string> BlockingIssues(PsychicRitualRoleAssignments assignments, Map map)
		{
			foreach (string item in base.BlockingIssues(assignments, map))
			{
				yield return item;
			}
			Pawn target = TargetRole == null ? assignments.FirstAssignedPawn(InvokerRole) : assignments.FirstAssignedPawn(TargetRole);
			if(target == null)
			{
				yield break;
			}
			if(!TryGetPartForHediff(target, out var _))
			{
				yield return "NAT_PsychicRitualAlreadyHasHediff".Translate(hediffDef.label).CapitalizeFirst();
			}
		}

		public bool TryGetPartForHediff(Pawn pawn, out BodyPartRecord part)
		{
			BodyPartDef partDef = hediffDef.defaultInstallPart;
			if(partDef == null)
			{
				part = null;
				return pawn.health.hediffSet.HasHediff(hediffDef);
			}
			List<BodyPartRecord> list = new List<BodyPartRecord>();
			foreach (BodyPartRecord notMissingPart in pawn.health.hediffSet.GetNotMissingParts())
			{
				if (notMissingPart.def == partDef)
				{
					list.Add(notMissingPart);
				}
			}
			foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
			{
				if (hediff.def == hediffDef && hediff.Part != null)
				{
					list.Remove(hediff.Part);
					if (list.NullOrEmpty())
					{
						part = null;
						return false;
					}
				}
			}
			part = list.RandomElement();
			return true;
		}
	}
	
	public class PsychicRitualToil_GiveHediff : PsychicRitualToil_AdditionalOfferingsOutcome
	{
		public PsychicRitualToil_GiveHediff() : base() { }

		public PsychicRitualToil_GiveHediff(PsychicRitualRoleDef invokerRole) : base(invokerRole)
		{
		}

		protected override void ApplyOutcome(List<Thing> offerings, PsychicRitual psychicRitual, Pawn invoker)
		{
			PsychicRitualDef_GiveHediff def = psychicRitual.def as PsychicRitualDef_GiveHediff;
			float power = psychicRitual.PowerPercent;
			bool success = false;
			TaggedString text = "";
			Pawn target = invoker;
			if(def.TargetRole != null)
			{
				target = psychicRitual.assignments.FirstAssignedPawn(def.TargetRole) ?? invoker;
			}
			if (def.successChanceFromQualityCurve != null)
			{
				float chance = def.successChanceFromQualityCurve.Evaluate(power);
				if (!Rand.Chance(chance))
				{
					success = false;
					text = def.failLetterText.Formatted(invoker.Named("INVOKER"), def.Named("RITUAL"));
				}
				else
				{
					success = true;
				}
			}
			else
			{
				success = true;
			}
			if (success && def.TryGetPartForHediff(target, out var part))
			{

				Hediff hediff = target.health.AddHediff(def.hediffDef, part);
				if(def.severityFromQualityCurve != null)
				{
					float severity = def.severityFromQualityCurve.Evaluate(power);
					if(severity <= 0f)
					{
						text = def.failLetterText.Formatted(invoker.Named("INVOKER"), def.Named("RITUAL"));
						success = false;
					}
				}
				if (success)
				{
					text = def.successLetterText.Formatted(invoker.Named("INVOKER"), def.Named("RITUAL"));
				}
			}
			if (def.sideEffectChanceFromQualityCurve != null && (!def.sideEffectOnlyIfFail || !success))
			{
				float chance = def.sideEffectChanceFromQualityCurve.Evaluate(power);
				if (Rand.Chance(chance))
				{
					target.health.AddHediff(def.sideEffectDef);
					text += "\n\n" + def.sideEffectText.Formatted(invoker.Named("INVOKER"), def.Named("RITUAL"));
				}
			}
			base.ApplyOutcome(offerings, psychicRitual, invoker);
			Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(def.label), text, success ? LetterDefOf.PositiveEvent : LetterDefOf.NegativeEvent, new LookTargets(target));
		}
	}
}