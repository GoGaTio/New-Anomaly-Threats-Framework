using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.IO;
using RimWorld.Planet;
using RimWorld.QuestGen;
using RimWorld.SketchGen;
using RimWorld.Utility;
using LudeonTK;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;
using Verse.Noise;
using Verse.Profile;
using Verse.Sound;
using Verse.Steam;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NAT
{
	public class HediffAltarTrade : HediffWithComps
	{
		protected HediffStage curStage;

		public int timesUsed;

		public List<AltarEffect> altarEffects = new List<AltarEffect>();

		public override HediffStage CurStage
		{
			get
			{
				if (curStage == null)
				{
					curStage = new HediffStage();
					curStage.statOffsets = new List<StatModifier>();
					curStage.statFactors = new List<StatModifier>();
					curStage.capMods = new List<PawnCapacityModifier>();
					for (int i = 0; i < altarEffects.Count; i++)
					{
						altarEffects[i].AffectStage(curStage);
					}
				}
				return curStage;
			}
		}

		public override string LabelInBrackets => base.LabelInBrackets;

		public void AddEffect(AltarEffect effect)
		{
			pawn.health.Notify_HediffChanged(this);
			for (int i = 0; i < altarEffects.Count; i++)
			{
				if (altarEffects[i].TryMerge(effect))
				{
					effect.AddedOrMerged(this);
					return;
				}
			}
			altarEffects.Add(effect);
			effect.AddedOrMerged(this);
		}

		public void ReCache()
		{
			curStage = null;
		}

		public override void PreRemoved()
		{
			for (int i = 0; i < altarEffects.Count; i++)
			{
				altarEffects[i].Removed(this);
			}
			base.PreRemoved();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref timesUsed, "timesUsed");
			Scribe_Collections.Look(ref altarEffects, "altarEffects", LookMode.Deep);
		}
	}
}