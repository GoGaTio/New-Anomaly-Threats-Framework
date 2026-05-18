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
	public class HediffCompProperties_GiveAbilityCharges : HediffCompProperties
	{
		public AbilityDef abilityDef;

		public List<AbilityDef> abilityDefs;

		public HediffCompProperties_GiveAbilityCharges()
		{
			compClass = typeof(HediffComp_GiveAbilityCharges);
		}
	}
	public class HediffComp_GiveAbilityCharges : HediffComp
	{
		private HediffCompProperties_GiveAbilityCharges Props => (HediffCompProperties_GiveAbilityCharges)props;

		public override void CompPostPostAdd(DamageInfo? dinfo)
		{
			if (parent.pawn.abilities == null)
			{
				parent.pawn.abilities = new Pawn_AbilityTracker(parent.pawn);
			}
			if (Props.abilityDef != null)
			{
				AddOrIncreaseAbility(Props.abilityDef);
			}
			if (!Props.abilityDefs.NullOrEmpty())
			{
				for (int i = 0; i < Props.abilityDefs.Count; i++)
				{
					AddOrIncreaseAbility(Props.abilityDefs[i]);
				}
			}
		}

		public void AddOrIncreaseAbility(AbilityDef def)
		{
			Ability ability = parent.pawn.abilities.GetAbility(def);
			if(ability == null)
			{
				parent.pawn.abilities.GainAbility(def);
			}
			else
			{
				ability.maxCharges++;
				ability.RemainingCharges++;
			}
		}

		public void RemoveOrDecreaseAbility(AbilityDef def)
		{
			Ability ability = parent.pawn.abilities.GetAbility(def);
			if (ability == null)
			{
				return;
			}
			if(ability.maxCharges > 1)
			{
				ability.maxCharges--;
				ability.RemainingCharges = Mathf.Max(0, ability.RemainingCharges - 1);
			}
			else
			{
				parent.pawn.abilities.RemoveAbility(def);
			}
		}

		public override void CompPostPostRemoved()
		{
			if (Props.abilityDef != null)
			{
				RemoveOrDecreaseAbility(Props.abilityDef);
			}
			if (!Props.abilityDefs.NullOrEmpty())
			{
				for (int i = 0; i < Props.abilityDefs.Count; i++)
				{
					RemoveOrDecreaseAbility(Props.abilityDefs[i]);
				}
			}
		}
	}
}