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
	public class AltarEffect : IExposable
	{
		public int positivity = 0;

		public static string descPrefix = "";

		public virtual void AffectStage(HediffStage stage) { }

		public virtual void ExposeData()
		{
			Scribe_Values.Look(ref positivity, "positivity", 0);
		}

		public virtual bool TryMerge(AltarEffect other)
		{
			return false;
		}

		public virtual TaggedString Desc => String.Colorize(positivity > 0 ? ColoredText.FactionColor_Ally : (positivity < 0 ? ColoredText.FactionColor_Hostile : ColoredText.FactionColor_Neutral));

		protected virtual TaggedString String => "";

		protected enum MergeMode
		{
			Sum,
			Multiply,
			Min,
			Max
		}

		protected float? MergeNullables(float? first, float? second, MergeMode mode)
		{
			if (first == null)
			{
				return second;
			}
			if (second == null)
			{
				return first;
			}
			switch (mode)
			{
				case MergeMode.Sum:
					return first.Value + second.Value;
				case MergeMode.Multiply:
					return first.Value * second.Value;
				case MergeMode.Min:
					return Mathf.Min(first.Value, second.Value);
				case MergeMode.Max:
					return Mathf.Max(first.Value, second.Value);
			}
			return null;
		}

		public virtual void AddedOrMerged(HediffAltarTrade hediff) { }
		public virtual void Removed(HediffAltarTrade hediff) { }

		public virtual bool CanUse(Pawn pawn) { return true; }
	}

	public class PainEffect : AltarEffect
	{
		public PainEffect() { }

		public float? factor;

		public float? offset;

		protected override TaggedString String
		{
			get
			{
				string s = "";
				if (offset != null && !Mathf.Approximately(offset.Value, 0f))
				{
					if (!s.NullOrEmpty())
					{
						s += "\n";
					}
					s += descPrefix + "Pain".Translate() + ": " + offset.Value.ToStringByStyle(ToStringStyle.PercentOne, ToStringNumberSense.Offset);
				}
				if (factor != null && !Mathf.Approximately(factor.Value, 1f))
				{
					if (!s.NullOrEmpty())
					{
						s += "\n";
					}
					s += descPrefix + "Pain".Translate() + ": " + factor.Value.ToStringByStyle(ToStringStyle.PercentOne, ToStringNumberSense.Factor);
				}
				return s;
			}
		}

		public override void AffectStage(HediffStage stage)
		{
			if (factor != null)
			{
				stage.painFactor = factor.Value;
			}
			if (offset != null)
			{
				stage.painOffset = factor.Value;
			}
		}

		public override bool TryMerge(AltarEffect other)
		{
			if (other is PainEffect effect)
			{
				factor = MergeNullables(factor, effect.factor, MergeMode.Multiply);
				offset = MergeNullables(offset, effect.offset, MergeMode.Sum);
				return true;
			}
			return false;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref factor, "factor", null);
			Scribe_Values.Look(ref offset, "offset", null);
		}
	}

	public class StatEffect : AltarEffect
	{
		public StatEffect() { }

		public StatDef stat;

		public float? factor;

		public float? offset;

		protected override TaggedString String
		{
			get
			{
				string s = "";
				if (offset != null && !Mathf.Approximately(offset.Value, 0f))
				{
					if (!s.NullOrEmpty())
					{
						s += "\n";
					}
					s += descPrefix + stat.LabelCap + ": " + offset.Value.ToStringByStyle(stat.ToStringStyleUnfinalized, ToStringNumberSense.Offset);
				}
				if (factor != null && !Mathf.Approximately(factor.Value, 1f))
				{
					if (!s.NullOrEmpty())
					{
						s += "\n";
					}
					s += descPrefix + stat.LabelCap + ": " + factor.Value.ToStringByStyle(stat.ToStringStyleUnfinalized, ToStringNumberSense.Factor);
				}
				return s;
			}
		}

		public override void AffectStage(HediffStage stage)
		{
			if (offset != null)
			{
				StatModifier statOffset = new StatModifier();
				statOffset.stat = stat;
				statOffset.value = offset.Value;
				stage.statOffsets.Add(statOffset);
			}
			if (factor != null)
			{
				StatModifier statFactor = new StatModifier();
				statFactor.stat = stat;
				statFactor.value = factor.Value;
				stage.statFactors.Add(statFactor);
			}
		}

		public override bool TryMerge(AltarEffect other)
		{
			if (other is StatEffect effect && effect.stat == stat)
			{
				factor = MergeNullables(factor, effect.factor, MergeMode.Multiply);
				offset = MergeNullables(offset, effect.offset, MergeMode.Sum);
				return true;
			}
			return false;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look(ref stat, "stat");
			Scribe_Values.Look(ref factor, "factor", null);
			Scribe_Values.Look(ref offset, "offset", null);
		}
	}

	public class CapacityEffect : AltarEffect
	{
		public CapacityEffect() { }

		public PawnCapacityDef capacity;

		public float? offset;

		public float? setMax;

		public float? postFactor;

		protected override TaggedString String
		{
			get
			{
				string s = "";
				if (offset != null && !Mathf.Approximately(offset.Value, 0f))
				{
					if (!s.NullOrEmpty())
					{
						s += "\n";
					}
					s += descPrefix + capacity.LabelCap + ": " + offset.Value.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Offset);
				}
				if (postFactor != null && !Mathf.Approximately(postFactor.Value, 1f))
				{
					if (!s.NullOrEmpty())
					{
						s += "\n";
					}
					s += descPrefix + capacity.LabelCap + ": " + postFactor.Value.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Factor);
				}
				if (setMax != null)
				{
					if (!s.NullOrEmpty())
					{
						s += "\n";
					}
					s += descPrefix + capacity.LabelCap + ": " + setMax.Value.ToStringByStyle(ToStringStyle.PercentZero);
				}
				return s;
			}
		}

		public override void AffectStage(HediffStage stage)
		{
			PawnCapacityModifier modifier = new PawnCapacityModifier();
			modifier.capacity = capacity;
			if (offset != null)
			{
				modifier.offset = offset.Value;
			}
			if (postFactor != null)
			{
				modifier.postFactor = postFactor.Value;
			}
			if (setMax != null)
			{
				modifier.postFactor = setMax.Value;
			}
			stage.capMods.Add(modifier);
		}

		public override bool TryMerge(AltarEffect other)
		{
			if (other is CapacityEffect effect && effect.capacity == capacity)
			{
				postFactor = MergeNullables(postFactor, effect.postFactor, MergeMode.Multiply);
				offset = MergeNullables(offset, effect.offset, MergeMode.Sum);
				setMax = MergeNullables(setMax, effect.setMax, MergeMode.Min);
				return true;
			}
			return false;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look(ref capacity, "capacity");
			Scribe_Values.Look(ref offset, "offset", null);
			Scribe_Values.Look(ref setMax, "setMax", null);
			Scribe_Values.Look(ref postFactor, "postFactor", null);
		}
	}

	public class DamageEffect : AltarEffect
	{
		public DamageEffect() { }

		public DamageDef damage;

		public float factor;

		protected override TaggedString String
		{
			get
			{
				string s = "";
				if (!Mathf.Approximately(factor, 1f))
				{
					if (!s.NullOrEmpty())
					{
						s += "\n";
					}
					s += descPrefix + StatDefOf.IncomingDamageFactor.LabelCap + "(" + damage.LabelCap + "): " + factor.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Factor);
				}
				return s;
			}
		}

		public override void AffectStage(HediffStage stage)
		{
			stage.damageFactors.Add(new DamageFactor() { damageDef = damage, factor = factor });
		}

		public override bool TryMerge(AltarEffect other)
		{
			if (other is DamageEffect effect && effect.damage == damage)
			{
				factor = MergeNullables(factor, effect.factor, MergeMode.Multiply).Value;
				return true;
			}
			return false;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look(ref damage, "damage");
			Scribe_Values.Look(ref factor, "factor", 1f);
		}
	}

	public class AbilityEffect : AltarEffect
	{
		public AbilityEffect()
		{
			positivity = 2;
		}

		public AbilityDef ability;

		public bool useCharges = false;

		public override TaggedString Desc => descPrefix + ("GivesAbility".Translate().CapitalizeFirst() + ": " + ability.LabelCap).Colorize(ColoredText.TipSectionTitleColor);

		public override bool TryMerge(AltarEffect other)
		{
			return false;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref useCharges, "useCharges");
			Scribe_Defs.Look(ref ability, "ability");
		}

		public override void AddedOrMerged(HediffAltarTrade hediff)
		{
			Ability num = hediff.pawn.abilities.GetAbility(ability);
			if (num == null)
			{
				hediff.pawn.abilities.GainAbility(ability);
				return;
			}
			if (useCharges)
			{
				num.maxCharges += 1;
				num.RemainingCharges += 1;
			}
		}

		public override void Removed(HediffAltarTrade hediff)
		{
			Ability num = hediff.pawn.abilities.GetAbility(ability);
			if(num == null)
			{
				return;
			}
			if (!useCharges || num.maxCharges <= 1)
			{
				hediff.pawn.abilities.RemoveAbility(ability);
			}
			else
			{
				num.maxCharges -= 1;
				num.RemainingCharges -= 1;
			}
		}

		public override bool CanUse(Pawn pawn)
		{
			if(pawn.abilities == null)
			{
				return false;
			}
			return useCharges || pawn.abilities.GetAbility(ability) == null;
		}
	}
}
