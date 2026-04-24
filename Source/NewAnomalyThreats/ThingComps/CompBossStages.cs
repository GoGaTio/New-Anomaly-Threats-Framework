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

		public CompProperties_BossStages.BossStage CurrentBossStage => Props.stages[currentBossStage];

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
			return CurrentBossStage.statFactors.GetStatOffsetFromList(stat); ;
		}

		public override float GetStatOffset(StatDef stat)
		{
			return CurrentBossStage.statOffsets.GetStatOffsetFromList(stat);
		}

		public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
		{
			base.Notify_Killed(prevMap, dinfo);
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref currentBossStage, "currentBossStage", 0);
		}
	}
}
