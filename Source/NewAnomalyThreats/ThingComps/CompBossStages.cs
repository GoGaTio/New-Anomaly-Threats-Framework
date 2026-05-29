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
using Verse.Noise;
using Verse.Sound;
using static HarmonyLib.Code;

namespace NAT
{
	public class BossStageAction
	{
		public BossStageAction() { }

		public bool endAction = true;

		public bool startAction = true;

		public virtual void Start(CompBossStages comp)
		{
			if (startAction)
			{
				DoAction(comp);
			}
		}

		public virtual void End(CompBossStages comp)
		{
			if (endAction)
			{
				DoAction(comp);
			}
		}
		public virtual void DoAction(CompBossStages comp)
		{

		}
	}

	public class BossStageAction_Explosion : BossStageAction
	{
		public float explosiveRadius = 1.9f;

		public DamageDef explosiveDamageType;

		public int damageAmountBase = -1;

		public float armorPenetrationBase = -1f;

		public ThingDef postExplosionSpawnThingDef;

		public float postExplosionSpawnChance;

		public int postExplosionSpawnThingCount = 1;

		public bool applyDamageToExplosionCellsNeighbors;

		public ThingDef preExplosionSpawnThingDef;

		public float preExplosionSpawnChance;

		public int preExplosionSpawnThingCount = 1;

		public float chanceToStartFire;

		public bool damageFalloff;

		public bool explodeOnKilled;

		public bool explodeOnDestroyed;

		public GasType? postExplosionGasType;

		public float? postExplosionGasRadiusOverride;

		public int postExplosionGasAmount = 255;

		public bool doVisualEffects = true;

		public bool doSoundEffects = true;

		public float propagationSpeed = 1f;

		public ThingDef postExplosionSpawnSingleThingDef;

		public ThingDef preExplosionSpawnSingleThingDef;

		public float explosiveExpandPerStackcount;

		public float explosiveExpandPerFuel;

		public EffecterDef explosionEffect;

		public SoundDef explosionSound;

		public override void DoAction(CompBossStages comp)
		{
			if (explosionEffect != null)
			{
				Effecter effecter = explosionEffect.Spawn();
				effecter.Trigger(new TargetInfo(comp.preDeathPos, comp.preDeathMap), new TargetInfo(comp.preDeathPos, comp.preDeathMap));
				effecter.Cleanup();
			}
			GenExplosion.DoExplosion(comp.preDeathPos, comp.preDeathMap, explosiveRadius, explosiveDamageType, comp.parent, damageAmountBase, armorPenetrationBase, explosionSound, null, null, null, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, postExplosionGasType, postExplosionGasRadiusOverride, postExplosionGasAmount, applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef, preExplosionSpawnChance, preExplosionSpawnThingCount, chanceToStartFire, damageFalloff, null, null, null, doVisualEffects, propagationSpeed, 0f, doSoundEffects, null, 1f, null, null, postExplosionSpawnSingleThingDef, preExplosionSpawnSingleThingDef);
		}
	}
	public class CompProperties_BossStages : CompProperties
	{
		public class BossStage
		{
			public BossStage() { }

			public List<StatModifier> statOffsets = new List<StatModifier>();

			public List<StatModifier> statFactors = new List<StatModifier>();

			public List<AbilityDef> abilities = new List<AbilityDef>();

			public List<BossStageAction> actions = new List<BossStageAction>();
		}

		public List<BossStage> stages = new List<BossStage>();

		public List<ThingDefCountRangeClass> finalLeavings = new List<ThingDefCountRangeClass>();

		[MustTranslate]
		public string finishedLetterLabel;

		[MustTranslate]
		public string finishedLetterText;

		public AnomalyBossDef def;

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
					foreach(AbilityDef def in CurrentBossStage.abilities)
					{
						Boss.abilities.GainAbility(def);
					}
				}
				return true;
			}
			return false;
		}

		public override void CompTick()
		{
			base.CompTick();
			if (parent.Spawned)
			{
				preDeathLord = Boss.GetLord();
				preDeathPos = parent.Position;
				preDeathMap = parent.Map;
			}
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
			if(preDeathMap == null)
			{
				preDeathPos = parent.PositionHeld;
				preDeathMap = prevMap;
			}
			foreach(BossStageAction action in CurrentBossStage.actions)
			{
				action.End(this);
			}
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
