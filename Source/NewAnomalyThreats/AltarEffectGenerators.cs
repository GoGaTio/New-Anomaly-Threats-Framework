using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace NAT
{
	public class AltarEffectGenerator
	{
		public int? positivity;

		public virtual IEnumerable<AltarEffect> Generate()
		{
			return Enumerable.Empty<AltarEffect>();
		}
	}

	public class AltarEffectGenerator_List : AltarEffectGenerator
	{
		public List<AltarEffectGenerator> subGenerators = new List<AltarEffectGenerator>();

		public float? fractionToSelect = null;

		public IntRange? countToSelect = null;
		public override IEnumerable<AltarEffect> Generate()
		{
			bool flag = countToSelect == null && fractionToSelect == null;
			List<AltarEffect> list = new List<AltarEffect>();
			for (int i = 0; i < subGenerators.Count; i++)
			{
				foreach (AltarEffect effect in subGenerators[i].Generate())
				{
					effect.positivity = positivity ?? effect.positivity;
					if (flag)
					{
						yield return effect;
						continue;
					}
					else list.Add(effect);
				}
			}
			if (flag)
			{
				yield break;
			}
			list.Shuffle();
			int count = Mathf.Max(countToSelect?.RandomInRange ?? Mathf.RoundToInt((fractionToSelect ?? 1f) * list.Count), 1);
			for (int i = 0; i < count; i++)
			{
				yield return list[i];
			}
		}
	}

	public class AltarEffectGenerator_Capacity : AltarEffectGenerator
	{
		public List<PawnCapacityDef> capacities = new List<PawnCapacityDef>();

		public FloatRange? offsetRange;

		public FloatRange? factorRange;

		public FloatRange? maxValueRange;

		public override IEnumerable<AltarEffect> Generate()
		{
			CapacityEffect effect = new CapacityEffect();
			effect.capacity = capacities.RandomElement();
			if (offsetRange != null)
			{
				effect.offset = offsetRange.Value.RandomInRange;
			}
			if (factorRange != null)
			{
				effect.postFactor = factorRange.Value.RandomInRange;
			}
			if (maxValueRange != null)
			{
				effect.setMax = maxValueRange.Value.RandomInRange;
			}
			yield return effect;
		}
	}

	public class AltarEffectGenerator_Stat : AltarEffectGenerator
	{
		public List<StatDef> stats = new List<StatDef>();

		public FloatRange? offsetRange;

		public FloatRange? factorRange;

		public override IEnumerable<AltarEffect> Generate()
		{
			StatEffect effect = new StatEffect();
			effect.stat = stats.RandomElement();
			if (offsetRange != null)
			{
				effect.offset = offsetRange.Value.RandomInRange;
			}
			if (factorRange != null)
			{
				effect.factor = factorRange.Value.RandomInRange;
			}
			effect.positivity = positivity ?? effect.positivity;
			yield return effect;
		}
	}

	public class AltarEffectGenerator_Damage : AltarEffectGenerator
	{
		public List<DamageDef> damages = new List<DamageDef>();

		public FloatRange factorRange;

		public override IEnumerable<AltarEffect> Generate()
		{
			DamageEffect effect = new DamageEffect();
			effect.damage = damages.RandomElement();
			effect.factor = factorRange.RandomInRange;
			effect.positivity = positivity ?? effect.positivity;
			yield return effect;
		}
	}

	public class AltarEffectGenerator_CreepjoinerAbility : AltarEffectGenerator
	{
		public override IEnumerable<AltarEffect> Generate()
		{
			AbilityEffect effect = new AbilityEffect();
			List<AbilityDef> abilities = new List<AbilityDef>();
			foreach(var def in DefDatabase<CreepJoinerBenefitDef>.AllDefsListForReading)
			{
				if(def.abilities.NullOrEmpty()) continue;
				abilities.AddRange(def.abilities);
			}
			effect.ability = abilities.RandomElement();
			effect.positivity = positivity ?? effect.positivity;
			yield return effect;
		}
	}
}
