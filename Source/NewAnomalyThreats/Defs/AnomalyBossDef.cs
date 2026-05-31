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
using Verse.Noise;

namespace NAT
{
	public class AnomalyBossDef : Def
	{
		public class BossGroup
		{
			public BossGroup() { }

			public string tag;

			public int minIndex = 0;

			public int maxIndex = int.MaxValue;

			public List<PawnGenOption> escorts = new List<PawnGenOption>();

			public List<PawnKindDefCount> fixedEscorts = new List<PawnKindDefCount>();

			public bool CanUseNow(int index)
			{
				if(index < minIndex)
				{
					return false;
				}
				if(index > maxIndex)
				{
					return false;
				}
				return true;
			}
		}

		[MustTranslate]
		public string arrivedLetterLabel;

		[MustTranslate]
		public string arrivedLetterText;

		public List<BossGroup> groups = new List<BossGroup>();

		public PawnKindDef bossKind;

		public float minPoints = 1000f;

		public float maxPoints = float.MaxValue;

		public int ticksCooldown = 180000;

		public IntRange arrivalTimeHoursRange = new IntRange(2, 10);

		public virtual float ProcessPoints(float points, Map map)
		{
			return Mathf.Clamp(points, minPoints, maxPoints);
		}

		public virtual BossGroup GetBossGroup(int timesCalled, float points)
		{
			return groups.Where(x => x.CanUseNow(timesCalled)).RandomElement();
		}

		public virtual List<PawnKindDef> GetBosses(int timesCalled, float points)
		{
			List<PawnKindDef> list = new List<PawnKindDef>() { bossKind };
			return list;
		}

		public void ArriveOnMap(AnomalyBossManager.AnomalyBoss boss)
		{
			List<Pawn> list = GeneratePawns(boss).ToList();
			Map map = boss.Map;
			ArriveInt(list, map);
			GenerateLord(list, map);
			Find.LetterStack.ReceiveLetter(arrivedLetterLabel.Formatted(bossKind.LabelCap), arrivedLetterText, LetterDefOf.ThreatBig, list);
			boss.escorts = null;
		}

		public virtual void ArriveInt(List<Pawn> list, Map map)
		{
			
		}

		public virtual void GenerateLord(List<Pawn> list, Map map)
		{

		}

		public virtual IEnumerable<Pawn> GeneratePawns(AnomalyBossManager.AnomalyBoss boss)
		{
			if (boss.escorts.NullOrEmpty())
			{
				boss.GetEscorts(boss.Map);
			}
			foreach(PawnKindDef item in boss.escorts)
			{
				yield return PawnGenerator.GeneratePawn(item, Faction.OfEntities);
			}
		}

		public override void ResolveReferences()
		{
			base.ResolveReferences();
			bossKind.race.GetCompProperties<CompProperties_BossStages>().def = this;
		}
	}
}
