using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Noise;

namespace NAT
{
	public class AnomalyBossDef : Def
	{
		public Type bossClass = typeof(AnomalyBoss);

		[MustTranslate]
		public string arrivedLetterLabel;

		[MustTranslate]
		public string arrivedLetterText;

		[MustTranslate]
		public string confirmationText;

		public int ticksCooldown = 180000;

		public IntRange arrivalTimeHoursRange = new IntRange(2, 10);
	}

	public class AnomalyBossDef_PawnGroup : AnomalyBossDef
	{
		public class BossGroup
		{
			public BossGroup() { }

			public string tag;

			public int minIndex = 0;

			public int maxIndex = int.MaxValue;

			public List<PawnGenOption> escorts = new List<PawnGenOption>();

			public List<PawnKindDefCount> fixedEscorts = new List<PawnKindDefCount>();

			public virtual bool CanUseNow(int index)
			{
				if (index < minIndex)
				{
					return false;
				}
				if (index > maxIndex)
				{
					return false;
				}
				return true;
			}

			public virtual IEnumerable<PawnKindDef> Generate(float points)
			{
				foreach (PawnKindDefCount item in fixedEscorts)
				{
					for (int i = 0; i < item.count; i++)
					{
						yield return item.kindDef;
					}
				}
				foreach (PawnKindDef item in NewAnomalyThreatsUtility.GeneratePawnsFromOptions(escorts, points))
				{
					yield return item;
				}
			}
		}

		public PawnsArrivalModeDef arrivalMode;

		[MustTranslate]
		public string escortsLabel = null;

		public List<BossGroup> groups = new List<BossGroup>();

		public float minPoints = 1000f;

		public float maxPoints = float.MaxValue;

		public virtual BossGroup GetBossGroup(int timesCalled, float points)
		{
			return groups.Where(x => x.CanUseNow(timesCalled)).RandomElement();
		}
	}

	public class AnomalyBossDef_Pawn : AnomalyBossDef_PawnGroup
	{
		public PawnKindDef bossKind;

		public override TaggedString LabelCap => bossKind.LabelCap;

		public override void ResolveReferences()
		{
			base.ResolveReferences();
			bossKind.race.GetCompProperties<CompProperties_BossStages>().def = this;
		}
	}
}
