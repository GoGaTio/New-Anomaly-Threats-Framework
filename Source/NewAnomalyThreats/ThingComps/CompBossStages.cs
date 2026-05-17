using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace NAT
{
	public class CompProperties_BossStages : CompProperties
	{
		public class BossStage
		{
			public BossStage() { }

			public List<StatModifier> statOffsets = new List<StatModifier>();

			public List<StatModifier> statFactors = new List<StatModifier>();

			public List<AbilityDef> abilities = new List<AbilityDef>();
		}

		public List<BossStage> stages = new List<BossStage>();

		public List<ThingDefCountRangeClass> finalLeavings = new List<ThingDefCountRangeClass>();

		[MustTranslate]
		public string finishedLetterLabel;

		[MustTranslate]
		public string finishedLetterText;

		public CompProperties_BossStages()
		{
			compClass = typeof(CompBossStages);
		}
	}

	public class CompBossStages : ThingComp, IRoofCollapseAlert
	{
		public CompProperties_BossStages Props => (CompProperties_BossStages)props;

		public Pawn Boss => parent as Pawn;

		public bool CanAdvanceStage => currentBossStage + 1 < Props.stages.Count;

		public int currentBossStage;

		public Lord preDeathLord;

		public IntVec3 preDeathPos;

		public Map preDeathMap;

		public CompProperties_BossStages.BossStage CurrentBossStage => Props.stages[currentBossStage];

		public override void PostPostMake()
		{
			base.PostPostMake();
			NewAnomalyThreatsUtility.Comp.AddEntityTracker(new AnomalyBossTracker() { boss = Boss });
		}

		public RoofCollapseResponse Notify_OnBeforeRoofCollapse()
		{
			return RoofCollapseResponse.RemoveThing;
		}

		public bool TryGoNextStage()
		{
			if(currentBossStage + 1 < Props.stages.Count)
			{
				currentBossStage++;
				if (!CurrentBossStage.abilities.NullOrEmpty())
				{
					if(Boss.abilities == null)
					{
						Boss.abilities = new Pawn_AbilityTracker(Boss);
					}
				}
				return true;
			}
			return false;
		}

		public override float GetStatFactor(StatDef stat)
		{
			return CurrentBossStage.statFactors.GetStatFactorFromList(stat);
		}

		public override float GetStatOffset(StatDef stat)
		{
			return CurrentBossStage.statOffsets.GetStatOffsetFromList(stat);
		}

		public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
		{
			preDeathLord = Boss.GetLord();
			preDeathPos = parent.PositionHeld;
			preDeathMap = prevMap;
			base.Notify_Killed(prevMap, dinfo);
			if (!CanAdvanceStage)
			{
				List<Thing>	list = new List<Thing>();
				for (int i = 0; i < Props.finalLeavings.Count; i++)
				{
					ThingDefCountRangeClass item = Props.finalLeavings[i];
					Thing thing = ThingMaker.MakeThing(item.thingDef);
					thing.stackCount = item.countRange.RandomInRange;
					if(GenPlace.TryPlaceThing(thing, preDeathPos, preDeathMap, ThingPlaceMode.Near))
					{
						list.Add(thing);
					}
				}
				Find.LetterStack.ReceiveLetter(Props.finishedLetterLabel, Props.finishedLetterText, LetterDefOf.PositiveEvent, list);
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref currentBossStage, "currentBossStage", 0);
		}
	}
}
