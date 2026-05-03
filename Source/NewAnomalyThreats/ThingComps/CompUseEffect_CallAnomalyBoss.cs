using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace NAT
{
	public class CompProperties_CallAnomalyBoss : CompProperties_UseEffect
	{
		public AnomalyBossDef bossDef;

		public EffecterDef effecterDef;

		public EffecterDef prepareEffecterDef;

		[NoTranslate]
		public string spawnLetterTextKey;

		[NoTranslate]
		public string spawnLetterLabelKey;

		[NoTranslate]
		public string unlockedLetterTextKey;

		[NoTranslate]
		public string unlockedLetterLabelKey;

		public int delayTicks = -1;

		public CompProperties_CallAnomalyBoss()
		{
			compClass = typeof(CompUseEffect_CallAnomalyBoss);
		}

		public override void Notify_PostUnlockedByResearch(ThingDef parent)
		{
			if (Find.TickManager.TicksGame > 0 && !unlockedLetterLabelKey.NullOrEmpty() && !unlockedLetterTextKey.NullOrEmpty())
			{
				SendBossgroupDetailsLetter(unlockedLetterLabelKey, unlockedLetterTextKey, parent);
			}
		}

		public void SendBossgroupDetailsLetter(string labelKey, string textKey, ThingDef parent)
		{
			List<ThingDef> list = new List<ThingDef> { parent };
			//list.AddRange(bossgroupDef.boss.kindDef.race.killedLeavingsPlayerHostile.Select((ThingDefCountClass t) => t.thingDef));
			//Find.LetterStack.ReceiveLetter(FormatLetterLabel(labelKey), FormatLetterText(textKey, parent), LetterDefOf.NeutralEvent, null, null, null, list);
		}

		public string FormatLetterText(string text, ThingDef parent)
		{
			return text.Translate(NamedArgumentUtility.Named(parent, "PARENT"), NamedArgumentUtility.Named(bossDef.bossKind, "LEADER"));
		}
	}
	public class CompUseEffect_CallAnomalyBoss : CompUseEffect
	{
		private Effecter prepareEffecter;

		public CompProperties_CallAnomalyBoss Props => (CompProperties_CallAnomalyBoss)props;

		public bool ShouldSendSpawnLetter
		{
			get
			{
				if (Props.spawnLetterLabelKey.NullOrEmpty() || Props.spawnLetterTextKey.NullOrEmpty())
				{
					return false;
				}
				if (!MechanitorUtility.AnyMechanitorInPlayerFaction())
				{
					return false;
				}
				if (Find.BossgroupManager.lastBossgroupCalled > 0)
				{
					return false;
				}
				return true;
			}
		}

		public override void DoEffect(Pawn usedBy)
		{
			base.DoEffect(usedBy);
			if (Props.effecterDef != null)
			{
				Effecter obj = new Effecter(Props.effecterDef);
				obj.Trigger(new TargetInfo(parent.Position, parent.Map), TargetInfo.Invalid);
				obj.Cleanup();
			}
			prepareEffecter?.Cleanup();
			prepareEffecter = null;
			CallBoss();
		}

		private void CallBoss()
		{
			NewAnomalyThreatsUtility.Comp.bossManager.CallBoss(Props.bossDef, parent.MapHeld);
		}

		public override TaggedString ConfirmMessage(Pawn p)
		{
			return NewAnomalyThreatsUtility.Comp.bossManager.BossWaveComposition(Props.bossDef, p.Map);
		}

		public override void PrepareTick()
		{
			if (Props.prepareEffecterDef != null && prepareEffecter == null)
			{
				prepareEffecter = Props.prepareEffecterDef.Spawn(parent.Position, parent.MapHeld);
			}
			prepareEffecter?.EffectTick(parent, TargetInfo.Invalid);
		}

		public override AcceptanceReport CanBeUsedBy(Pawn p)
		{
			return NewAnomalyThreatsUtility.Comp.bossManager.CanCallBoss(Props.bossDef, parent.Map);
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			if (!respawningAfterLoad && ShouldSendSpawnLetter)
			{
				Props.SendBossgroupDetailsLetter(Props.spawnLetterLabelKey, Props.spawnLetterTextKey, parent.def);
			}
		}
	}
}
