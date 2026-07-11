using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using static System.Net.Mime.MediaTypeNames;

namespace NAT
{
	public class CompProperties_TradeAltar : CompProperties_Interactable
	{
		[MustTranslate]
		public string altarConfirmationText;

		public EffecterDef effecterDef;

		public HediffDef effectHediff;

		public AltarEffectGenerator generator;

		public CompProperties_TradeAltar()
		{
			compClass = typeof(CompTradeAltar);
		}
	}

	public class CompTradeAltar : CompInteractable
	{
		public new CompProperties_TradeAltar Props => (CompProperties_TradeAltar)props;

		protected override string ActivateOptionLabel => "Activate".Translate();

		public List<AltarEffect> effects = new List<AltarEffect>();

		private bool activated;

		public float Alpha => activated ? 0f : (0.2f + (Mathf.Sin((float)GenTicks.TicksGame / 300f) * 0.15f));

		public override void PostPostMake()
		{
			base.PostPostMake();
			effects = new List<AltarEffect>(Props.generator.Generate());
			effects.SortBy(x => -x.positivity);
		}

		protected override void OnInteracted(Pawn caster)
		{
			Props.effecterDef?.Spawn(parent, parent.Map);
			HediffAltarTrade hediff = caster.health.GetOrAddHediff(Props.effectHediff) as HediffAltarTrade;
			if(hediff == null)
			{
				return;
			}
			for (int i = 0; i < effects.Count; i++)
			{
				hediff.AddEffect(effects[i]);
			}
			hediff.ReCache();
			effects.Clear();
			activated = true;
		}

		public override AcceptanceReport CanInteract(Pawn activateBy = null, bool checkOptionalItems = true)
		{
			if (activated)
			{
				return false;
			}
			if(activateBy != null)
			{
				for (int i = 0;i < effects.Count; i++)
				{
					if (!effects[i].CanUse(activateBy))
					{
						return false;
					}
				}
			}
			return base.CanInteract(activateBy, checkOptionalItems);
		}

		public override string CompInspectStringExtra()
		{
			TaggedString s = "";
			for (int i = 0; i < effects.Count; i++)
			{
				if (!s.NullOrEmpty())
				{
					s += "\n";
				}
				s += effects[i].Desc;
			}
			return s.Resolve();
		}

		public override void OrderForceTarget(LocalTargetInfo target)
		{
			OrderActivation(target.Pawn);
		}

		private void OrderActivation(Pawn pawn)
		{
			TaggedString s = Props.altarConfirmationText.Formatted(pawn.Named("PAWN"), Props.effectHediff.label.Named("HEDIFF"));
			s += "\n\n" + "Effects".Translate().CapitalizeFirst() + ":";
			AltarEffect.descPrefix = "  ";
			try
			{
				for (int i = 0; i < effects.Count; i++)
				{
					s += "\n" + effects[i].Desc;
				}
			}
			finally
			{
				AltarEffect.descPrefix = "";
			}
			Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(s, delegate
			{
				Job job = JobMaker.MakeJob(JobDefOf.InteractThing, parent);
				job.count = 1;
				job.playerForced = true;
				pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
			}));
		}

		public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
		{
			if (activated)
			{
				yield break;
			}
			AcceptanceReport acceptanceReport = CanInteract(selPawn);
			FloatMenuOption floatMenuOption = new FloatMenuOption(ActivateOptionLabel, delegate
			{
				OrderActivation(selPawn);
			});
			if (!acceptanceReport.Accepted)
			{
				floatMenuOption.Disabled = true;
				floatMenuOption.Label = floatMenuOption.Label + " (" + acceptanceReport.Reason + ")";
			}
			yield return floatMenuOption;
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (activated)
			{
				return Enumerable.Empty<Gizmo>();
			}
			return base.CompGetGizmosExtra();
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Collections.Look(ref effects, "effects", LookMode.Deep);
		}
	}
}
