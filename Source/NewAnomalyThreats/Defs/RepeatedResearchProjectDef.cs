using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace NAT
{
	public class RepeatedResearchProjectDef : ResearchProjectDef
	{
		public float baseCostSaved;

		public float? baseCostOffset;

		public float? baseCostFactor;

		public float knowledgeCostSaved;

		public float? knowledgeOffset;

		public float? knowledgeFactor;

		public override TaggedString LabelCap
		{
			get
			{
				int value = NewAnomalyThreatsUtility.Comp?.GetResearchStep(this) ?? 0;
				if(value > 0)
				{
					return base.LabelCap + " (x" + value + ")";
				}
				return base.LabelCap;
			}
		}

		public virtual void Researched(Pawn researcher, int count)
		{
			EnsureCostIsCorrect(count);
		}

		public virtual void EnsureCostIsCorrect(int count)
		{
			baseCost = baseCostSaved;
			knowledgeCost = knowledgeCostSaved;
			for(int i = 0; i < count; i++)
			{
				if(baseCostOffset != null)
				{
					baseCost += baseCostOffset.Value;
				}
				if (knowledgeFactor != null)
				{
					baseCost *= knowledgeFactor.Value;
				}
				if (knowledgeOffset != null)
				{
					knowledgeCost += knowledgeOffset.Value;
				}
				if (knowledgeFactor != null)
				{
					knowledgeCost *= knowledgeFactor.Value;
				}
			}
		}

		public override void ResolveReferences()
		{
			base.ResolveReferences();
			baseCostSaved = baseCost;
			knowledgeCostSaved = knowledgeCost;
		}
	}
}
