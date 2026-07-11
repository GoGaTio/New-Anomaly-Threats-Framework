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
using static System.Net.Mime.MediaTypeNames;

namespace NAT
{
	public class CompProperties_CallAnomalyBoss : CompProperties_Interactable
	{
		public AnomalyBossDef bossDef;

		public EffecterDef effecterDef;

		public EffecterDef destroyEffecterDef;

		public GraphicData floatingGraphic;

		public int destroyDelayTicks = -1;

		public CompProperties_CallAnomalyBoss()
		{
			compClass = typeof(CompCallAnomalyBoss);
			activateTexPath = "UI/Commands/NAT_CallAnomalyBoss";
		}
	}
	public class CompCallAnomalyBoss : CompInteractable
	{
		public new CompProperties_CallAnomalyBoss Props => (CompProperties_CallAnomalyBoss)props;

		public int ticksTillDestroy = -1;

		public override void OrderForceTarget(LocalTargetInfo target)
		{
			OrderActivation(target.Pawn);
		}

		protected override void OnInteracted(Pawn caster)
		{
			if(Props.effecterDef != null)
			{
				Vector3 offset = Vector3.zero;
				float num = 0.5f * (1f + Mathf.Sin(Mathf.PI * 2f * (float)GenTicks.TicksGame / 300f)) * 0.35f;
				offset.z += num;
				Props.effecterDef.Spawn(parent, parent.Map, offset);
			}
			ticksTillDestroy = Props.destroyDelayTicks;
			NewAnomalyThreatsUtility.Comp.bossManager.GetBoss(Props.bossDef).Call(parent.PositionHeld, parent.MapHeld);
		}

		public override string CompInspectStringExtra()
		{
			return null;
		}

		public override void CompTick()
		{
			base.CompTick();
			if(ticksTillDestroy > 0)
			{
				ticksTillDestroy--;
				if(ticksTillDestroy <= 0)
				{
					Props.destroyEffecterDef?.Spawn(parent.Position, parent.Map);
					parent.Kill();
				}
			}
		}

		public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
		{
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
			else
			{
				AcceptanceReport report = NewAnomalyThreatsUtility.Comp.bossManager.GetBoss(Props.bossDef).CanCall(parent.PositionHeld, parent.MapHeld);
				if (!report.Accepted)
				{
					floatMenuOption.Disabled = true;
					floatMenuOption.Label = floatMenuOption.Label + " (" + report.Reason + ")";
				}
			}
			yield return floatMenuOption;
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			AcceptanceReport report = NewAnomalyThreatsUtility.Comp.bossManager.GetBoss(Props.bossDef).CanCall(parent.PositionHeld, parent.MapHeld);
			foreach (Gizmo item in base.CompGetGizmosExtra())
			{
				if (!report.Accepted)
				{
					item.Disable(report.Reason);
				}
				yield return item;
			}
		}

		public override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			if (Props.floatingGraphic != null && ticksTillDestroy < 0)
			{
				float num = 0.5f * (1f + Mathf.Sin(Mathf.PI * 2f * (float)GenTicks.TicksGame / 300f)) * 0.35f;
				drawLoc.z += num;
				drawLoc += Altitudes.AltIncVect;
				Props.floatingGraphic.Graphic.Draw(drawLoc, parent.Rotation, parent);
			}
		}

		private void OrderActivation(Pawn pawn)
		{
			Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(NewAnomalyThreatsUtility.Comp.bossManager.GetBoss(Props.bossDef).Confirmation(pawn.MapHeld), delegate
			{
				Job job = JobMaker.MakeJob(JobDefOf.InteractThing, parent);
				job.count = 1;
				job.playerForced = true;
				pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
			}));
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			if (string.IsNullOrEmpty(ExposeKey))
			{
				Scribe_Values.Look(ref ticksTillDestroy, "ticksTillDestroy", -1);
			}
			else
			{
				Scribe_Values.Look(ref ticksTillDestroy, ExposeKey + "_ticksTillDestroy", -1);
			}
		}
	}
}
